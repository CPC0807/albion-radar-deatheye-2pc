using Newtonsoft.Json;
using System;
using System.IO;

namespace VRise.Radar.Sniffer
{
    /// <summary>
    /// Simple configuration for game port setting
    /// Allows easy configuration of Albion Online network port without recompiling
    /// </summary>
    public class NetworkConfig
    {
        /// <summary>
        /// Game server port to capture (usually 5056 or 5050)
        /// </summary>
        [JsonProperty("game_port")]
        public int GamePort { get; set; } = 5056;

        /// <summary>
        /// Load configuration from file, or create default if not exists
        /// </summary>
        public static NetworkConfig Load(string configPath = "network_config.json")
        {
            try
            {
                if (File.Exists(configPath))
                {
                    string json = File.ReadAllText(configPath);
                    var config = JsonConvert.DeserializeObject<NetworkConfig>(json);
                    Console.WriteLine($"[NetworkConfig] Loaded from {configPath}");
                    Console.WriteLine($"[NetworkConfig] Game Port: {config.GamePort}");
                    return config;
                }
                else
                {
                    // Create default config
                    var defaultConfig = new NetworkConfig();
                    defaultConfig.Save(configPath);
                    Console.WriteLine($"[NetworkConfig] Created default config at {configPath}");
                    Console.WriteLine($"[NetworkConfig] Game Port: {defaultConfig.GamePort}");
                    return defaultConfig;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"[NetworkConfig] Error loading config: {e.Message}, using defaults");
                return new NetworkConfig();
            }
        }

        /// <summary>
        /// Save configuration to file
        /// </summary>
        public void Save(string configPath = "network_config.json")
        {
            try
            {
                string json = JsonConvert.SerializeObject(this, Formatting.Indented);
                File.WriteAllText(configPath, json);
                Console.WriteLine($"[NetworkConfig] Saved to {configPath}");
            }
            catch (Exception e)
            {
                Console.WriteLine($"[NetworkConfig] Error saving config: {e.Message}");
            }
        }
    }
}
