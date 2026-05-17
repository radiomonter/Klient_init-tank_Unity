using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using Tanki.Core.Events;
using Tanki.Core;

namespace Tanki.Networking
{
    public class NetworkClient : MonoBehaviour
    {
        [Header("Connection Settings")]
        [SerializeField] private string _host = "127.0.0.1";
        [SerializeField] private int _port = 12345;
        [SerializeField] private bool _autoConnect = true;

        [Header("Events")]
        [SerializeField] private GameEvent _onConnected;
        [SerializeField] private GameEvent _onDisconnected;

        private TcpClient _client;
        private NetworkStream _stream;
        private byte[] _buffer = new byte[16384]; // Увеличен для больших пакетов

        // Simple encryption state from original Flash/Kotlin code
        private int _lastKey = 0;

        public static NetworkClient Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public enum ConnectionState { Disconnected, Connecting, Connected, Error }
        public ConnectionState State { get; private set; } = ConnectionState.Disconnected;
        public string LastError { get; private set; } = "";

        public event Action<Command> OnCommandReceived;
        public event Action<string> OnConnectionError;
        public event Action OnConnectionSuccess;

        private void Start()
        {
            // Initialize dispatcher on main thread
            var dispatcher = UnityMainThreadDispatcher.Instance;

            NetworkConfig.Load();
            _host = NetworkConfig.Host;
            _port = NetworkConfig.Port;

            if (_autoConnect) Connect();
        }

        public void Connect()
        {
            if (State == ConnectionState.Connecting || State == ConnectionState.Connected) return;
            
            State = ConnectionState.Connecting;
            LastError = "";
            
            try
            {
                Debug.Log($"[Network] Connecting to {_host}:{_port}...");
                _client = new TcpClient();
                _client.BeginConnect(_host, _port, OnConnectCallback, null);
            }
            catch (Exception e)
            {
                State = ConnectionState.Error;
                LastError = e.Message;
                Debug.LogError($"[Network] Connection failed: {e.Message}");
                UnityMainThreadDispatcher.EnqueueAction(() => OnConnectionError?.Invoke(e.Message));
            }
        }

        private void OnConnectCallback(IAsyncResult ar)
        {
            try
            {
                _client.EndConnect(ar);
                _stream = _client.GetStream();
                _lastKey = 1; // Reset key on new connection
                
                _stream.BeginRead(_buffer, 0, _buffer.Length, OnReadCallback, null);
                
                UnityMainThreadDispatcher.EnqueueAction(() => {
                    State = ConnectionState.Connected;
                    LastError = "";
                    Debug.Log("[Network] Connected to server. Sending get_aes_data...");
                    Send("system", "get_aes_data", "RU"); 
                    _onConnected?.Raise();
                    OnConnectionSuccess?.Invoke();
                });
            }
            catch (Exception e)
            {
                State = ConnectionState.Error;
                LastError = e.Message;
                Debug.LogError($"[Network] Error on connect: {e.Message}");
                UnityMainThreadDispatcher.EnqueueAction(() => OnConnectionError?.Invoke(e.Message));
            }
        }

        private List<byte> _receivedDataBuffer = new List<byte>();

        private void OnReadCallback(IAsyncResult ar)
        {
            try
            {
                if (_stream == null) return;

                int bytesRead = _stream.EndRead(ar);
                if (bytesRead <= 0)
                {
                    Debug.LogWarning("[Network] Server closed connection.");
                    Disconnect();
                    return;
                }

                // Add raw bytes to buffer
                byte[] incomingBytes = new byte[bytesRead];
                Buffer.BlockCopy(_buffer, 0, incomingBytes, 0, bytesRead);
                _receivedDataBuffer.AddRange(incomingBytes);

                ProcessRawData();

                if (_stream != null)
                {
                    _stream.BeginRead(_buffer, 0, _buffer.Length, OnReadCallback, null);
                }
            }
            catch (ObjectDisposedException) { }
            catch (Exception e)
            {
                Debug.LogError($"[Network] Read error: {e.Message}");
                Disconnect();
            }
        }

        private void ProcessRawData()
        {
            byte[] delimiter = Encoding.ASCII.GetBytes(ProtocolConstants.CommandDelimiter);
            int delimiterIndex;

            while ((delimiterIndex = IndexOfSequence(_receivedDataBuffer.ToArray(), delimiter)) != -1)
            {
                byte[] packetBytes = new byte[delimiterIndex];
                _receivedDataBuffer.CopyTo(0, packetBytes, 0, delimiterIndex);
                _receivedDataBuffer.RemoveRange(0, delimiterIndex + delimiter.Length);

                if (packetBytes.Length > 0)
                {
                    string decrypted = Decrypt(packetBytes);
                    if (!string.IsNullOrEmpty(decrypted))
                    {
                        var commands = Command.Parse(decrypted);
                        foreach (var cmd in commands)
                        {
                            UnityMainThreadDispatcher.EnqueueAction(() => OnCommandReceived?.Invoke(cmd));
                        }
                    }
                }
            }
        }

        private int IndexOfSequence(byte[] buffer, byte[] pattern)
        {
            for (int i = 0; i <= buffer.Length - pattern.Length; i++)
            {
                bool match = true;
                for (int j = 0; j < pattern.Length; j++)
                {
                    if (buffer[i + j] != pattern[j]) { match = false; break; }
                }
                if (match) return i;
            }
            return -1;
        }



        public void Send(string commandType, params string[] args)
        {
            string payload = commandType;
            if (args != null && args.Length > 0)
            {
                payload += ProtocolConstants.ArgumentDelimiter + string.Join(ProtocolConstants.ArgumentDelimiter, args);
            }

            int key = (_lastKey + 1) % 9;
            if (key <= 0) key = 1;
            _lastKey = key;

            // 1. Shift characters (Legacy Flash Style)
            string shiftedPayload = "";
            int shift = key + 1;
            for (int i = 0; i < payload.Length; i++)
            {
                shiftedPayload += (char)((payload[i] + shift) & 0xFFFF);
            }

            // 2. Add key prefix
            string finalString = key.ToString() + shiftedPayload;

            // 3. Convert to UTF-8 bytes (writeUTFBytes)
            byte[] payloadBytes = Encoding.UTF8.GetBytes(finalString);
            
            // 4. Add plain delimiter (~dne)
            byte[] delimiter = Encoding.ASCII.GetBytes(ProtocolConstants.CommandDelimiter);
            byte[] finalPacket = new byte[payloadBytes.Length + delimiter.Length];
            
            Buffer.BlockCopy(payloadBytes, 0, finalPacket, 0, payloadBytes.Length);
            Buffer.BlockCopy(delimiter, 0, finalPacket, payloadBytes.Length, delimiter.Length);

            try
            {
                if (_stream != null && _stream.CanWrite)
                {
                    _stream.Write(finalPacket, 0, finalPacket.Length);
                    Debug.Log($"[Network] → {payload} (key: {key})");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[Network] Send error: {e.Message}");
                Disconnect();
            }
        }

        private string Decrypt(byte[] encrypted)
        {
            if (encrypted == null || encrypted.Length == 0) return null;

            // 1. Decode UTF-8 (readUTFBytes)
            string data = Encoding.UTF8.GetString(encrypted);
            
            int key = data[0] - '0';
            if (key < 1 || key > 8) return data;

            // 2. Unshift characters
            char[] unshifted = new char[data.Length - 1];
            int shift = key + 1;
            for (int i = 1; i < data.Length; i++)
            {
                unshifted[i - 1] = (char)((data[i] - shift) & 0xFFFF);
            }

            return new string(unshifted);
        }

        public void Disconnect()
        {
            State = ConnectionState.Disconnected;
            _stream?.Close();
            _client?.Close();
            _client = null;
            
            UnityMainThreadDispatcher.EnqueueAction(() => {
                Debug.Log("[Network] Disconnected.");
                _onDisconnected?.Raise();
            });
        }

        private void OnDestroy() => Disconnect();
    }
}
