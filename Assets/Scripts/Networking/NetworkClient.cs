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
        private string _receivedDataBuffer = "";

        // Simple encryption state from original Flash/Kotlin code
        private int _lastKey = 1;

        public event Action<Command> OnCommandReceived;

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
            try
            {
                Debug.Log($"[Network] Connecting to {_host}:{_port}...");
                _client = new TcpClient();
                _client.BeginConnect(_host, _port, OnConnectCallback, null);
            }
            catch (Exception e)
            {
                Debug.LogError($"[Network] Connection failed: {e.Message}");
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
                
                UnityMainThreadDispatcher.Instance.Enqueue(() => {
                    Debug.Log("[Network] Connected to server. Sending get_aes_data...");
                    Send("system", "get_aes_data", "RU"); 
                    _onConnected?.Raise();
                });
            }
            catch (Exception e)
            {
                Debug.LogError($"[Network] Error on connect: {e.Message}");
            }
        }

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

                // Use UTF-8 as it is the standard for Flash writeUTFBytes/readUTFBytes
                // and correctly handles multi-byte characters created during shifting.
                string rawData = Encoding.UTF8.GetString(_buffer, 0, bytesRead);
                _receivedDataBuffer += rawData;

                ProcessRawData();

                if (_stream != null)
                {
                    _stream.BeginRead(_buffer, 0, _buffer.Length, OnReadCallback, null);
                }
            }
            catch (ObjectDisposedException)
            {
                // Expected when disconnecting
            }
            catch (Exception e)
            {
                Debug.LogError($"[Network] Read error: {e.Message}");
                Disconnect();
            }
        }

        private void ProcessRawData()
        {
            int delimiterIndex;
            while ((delimiterIndex = _receivedDataBuffer.IndexOf(ProtocolConstants.CommandDelimiter)) != -1)
            {
                string packet = _receivedDataBuffer.Substring(0, delimiterIndex);
                _receivedDataBuffer = _receivedDataBuffer.Substring(delimiterIndex + ProtocolConstants.CommandDelimiter.Length);

                if (!string.IsNullOrEmpty(packet))
                {
                    string decrypted = Decrypt(packet);
                    if (decrypted != null)
                    {
                        var commands = Command.Parse(decrypted);
                        foreach (var cmd in commands)
                        {
                            UnityMainThreadDispatcher.Instance.Enqueue(() => OnCommandReceived?.Invoke(cmd));
                        }
                    }
                }
            }
        }

        public void Send(string commandType, params string[] args)
        {
            string payload = commandType;
            if (args != null && args.Length > 0)
            {
                payload += ProtocolConstants.ArgumentDelimiter + string.Join(ProtocolConstants.ArgumentDelimiter, args);
            }

            // 1. Generate shifted string (mirroring Flash String.fromCharCode logic)
            int key = (_lastKey + 1) % 9;
            if (key <= 0) key = 1;
            _lastKey = key;

            string shiftedPayload = "";
            for (int i = 0; i < payload.Length; i++)
            {
                shiftedPayload += (char)((payload[i] + (key + 1)) & 0xFFFF);
            }

            // 2. Convert to UTF-8 bytes (mirroring Flash writeUTFBytes)
            byte[] payloadBytes = Encoding.UTF8.GetBytes(shiftedPayload);
            byte[] encryptedBytes = new byte[payloadBytes.Length + 1];
            encryptedBytes[0] = (byte)((key + '0') & 0xFF);
            Buffer.BlockCopy(payloadBytes, 0, encryptedBytes, 1, payloadBytes.Length);

            // 3. Add delimiter and send
            byte[] delimiter = Encoding.ASCII.GetBytes(ProtocolConstants.CommandDelimiter);
            byte[] finalPacket = new byte[encryptedBytes.Length + delimiter.Length];
            Buffer.BlockCopy(encryptedBytes, 0, finalPacket, 0, encryptedBytes.Length);
            Buffer.BlockCopy(delimiter, 0, finalPacket, encryptedBytes.Length, delimiter.Length);

            try
            {
                if (_stream != null && _stream.CanWrite)
                {
                    _stream.BeginWrite(finalPacket, 0, finalPacket.Length, null, null);
                    Debug.Log($"[Network] → {payload} (key: {key})");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[Network] Send error: {e.Message}");
            }
        }

        private string Decrypt(string encrypted)
        {
            if (string.IsNullOrEmpty(encrypted)) return null;

            char firstChar = encrypted[0];
            if (firstChar < '1' || firstChar > '8')
            {
                return encrypted;
            }

            try
            {
                int key = firstChar - '0';
                string result = "";

                for (int i = 1; i < encrypted.Length; i++)
                {
                    result += (char)((encrypted[i] - (key + 1)) & 0xFFFF);
                }

                return result;
            }
            catch (Exception e)
            {
                Debug.LogError($"[Network] Decrypt error on packet starting with '{firstChar}': {e.Message}");
                return null;
            }
        }

        public void Disconnect()
        {
            _stream?.Close();
            _client?.Close();
            _client = null;
            
            if (UnityMainThreadDispatcher.Instance != null)
            {
                UnityMainThreadDispatcher.Instance.Enqueue(() => {
                    Debug.Log("[Network] Disconnected.");
                    _onDisconnected?.Raise();
                });
            }
            else
            {
                Debug.Log("[Network] Disconnected (Main thread dispatcher already destroyed).");
            }
        }

        private void OnDestroy() => Disconnect();
    }
}
