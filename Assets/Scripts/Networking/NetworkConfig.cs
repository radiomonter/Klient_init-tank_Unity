using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Tanki.Networking
{
    public static class NetworkConfig
    {
        public static string Host { get; private set; } = "127.0.0.1";
        public static int Port { get; private set; } = 12345;

        public static void Load()
        {
            string path = Path.Combine(Application.streamingAssetsPath, "config.txt");
            if (!File.Exists(path))
            {
                Debug.LogWarning($"[Config] Config file not found at {path}. Using defaults.");
                return;
            }

            try
            {
                string content = File.ReadAllText(path);
                
                // Parse host
                var hostMatch = Regex.Match(content, @"host\s*=\s*""([^""]+)""");
                if (hostMatch.Success)
                {
                    Host = hostMatch.Groups[1].Value;
                }

                // Parse port
                var portMatch = Regex.Match(content, @"port\s*=\s*(\d+)");
                if (portMatch.Success)
                {
                    Port = int.Parse(portMatch.Groups[1].Value);
                }

                Debug.Log($"[Config] Loaded server config: {Host}:{Port}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Config] Error loading config: {e.Message}");
            }
        }
    }
}
