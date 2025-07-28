using System;
using System.Collections.Generic;
using System.IO;

namespace AssetManager.Models
{
    /// <summary>
    /// Represents a trading platform instance (MT4, MT5, or TraderEvolution)
    /// Enhanced with asset scanning capabilities for the Asset Matrix Manager
    /// </summary>
    public class TradingInstance
    {
        public string Name { get; set; } = "";
        public string Broker { get; set; } = "";
        public string Platform { get; set; } = ""; // MT4, MT5, TraderEvolution
        public string Source { get; set; } = "";
        public string Destination { get; set; } = "";
        public string AccountType { get; set; } = ""; // demo, live
        public bool Enabled { get; set; } = true;
        public bool AutoStart { get; set; } = false;
        
        // Computed properties for asset scanning (set by scanner or matrix manager)
        public string TradingRoot { get; set; } = "";
        public string InstancePath => GetInstancePath();
        public string DataPath => GetDataPath();
        
        /// <summary>
        /// Gets the full path to this instance's folder
        /// </summary>
        private string GetInstancePath()
        {
            if (string.IsNullOrEmpty(TradingRoot) || string.IsNullOrEmpty(Destination))
                return "";
            
            return Path.Combine(TradingRoot, "PlatformInstances", Destination);
        }
        
        /// <summary>
        /// Gets the data path for this instance
        /// </summary>
        private string GetDataPath()
        {
            if (string.IsNullOrEmpty(TradingRoot) || string.IsNullOrEmpty(Destination))
                return "";
            
            return Path.Combine(TradingRoot, "InstanceData", Destination);
        }
        
        /// <summary>
        /// CRITICAL: Gets the asset folder paths for this platform
        /// Required by AssetMatrixManager for asset scanning
        /// </summary>
        public string[] GetAssetFolders(string basePath)
        {
            return Platform.ToUpper() switch
            {
                "MT4" => new[]
                {
                    Path.Combine(basePath, "MQL4", "Indicators"),
                    Path.Combine(basePath, "MQL4", "Experts"),
                    Path.Combine(basePath, "MQL4", "Scripts"),
                    Path.Combine(basePath, "MQL4", "Libraries"),
                    Path.Combine(basePath, "templates"),
                    Path.Combine(basePath, "MQL4", "Presets")
                },
                "MT5" => new[]
                {
                    Path.Combine(basePath, "MQL5", "Indicators"),
                    Path.Combine(basePath, "MQL5", "Experts"),
                    Path.Combine(basePath, "MQL5", "Scripts"),
                    Path.Combine(basePath, "MQL5", "Libraries"),
                    Path.Combine(basePath, "templates"),
                    Path.Combine(basePath, "MQL5", "Presets")
                },
                "TRADEREVOLUTION" => new[]
                {
                    Path.Combine(basePath, "Indicators"),
                    Path.Combine(basePath, "Robots"),
                    Path.Combine(basePath, "Scripts"),
                    Path.Combine(basePath, "Plugins"),
                    Path.Combine(basePath, "Workspaces")
                },
                _ => Array.Empty<string>()
            };
        }
        
        /// <summary>
        /// CRITICAL: Gets the file extensions to scan for this platform
        /// Required by AssetMatrixManager for asset discovery
        /// </summary>
        public string[] GetAssetExtensions()
        {
            return Platform.ToUpper() switch
            {
                "MT4" => new[] { ".ex4", ".mq4", ".tpl", ".set" },
                "MT5" => new[] { ".ex5", ".mq5", ".tpl", ".set" },
                "TRADEREVOLUTION" => new[] { ".dll", ".cs", ".xml" },
                _ => Array.Empty<string>()
            };
        }
        
        /// <summary>
        /// Gets the main executable name for this platform
        /// </summary>
        public string GetExecutableName()
        {
            return Platform.ToUpper() switch
            {
                "MT4" => "terminal.exe",
                "MT5" => "terminal64.exe",
                "TRADEREVOLUTION" => "TradeTerminal.exe",
                _ => ""
            };
        }
        
        /// <summary>
        /// Checks if this instance is valid for asset scanning
        /// Used by both AssetScanner and AssetMatrixManager
        /// </summary>
        public bool IsValidForScanning()
        {
            if (!Enabled) return false;
            if (string.IsNullOrEmpty(Platform)) return false;
            if (string.IsNullOrEmpty(Name) || string.IsNullOrEmpty(Destination)) return false;
            
            // Check if instance path exists (when TradingRoot is set)
            if (!string.IsNullOrEmpty(TradingRoot))
            {
                return Directory.Exists(InstancePath);
            }
            
            return true; // Valid for scanning if basic properties are set
        }
        
        /// <summary>
        /// Overload for backward compatibility with asset scanner
        /// </summary>
        public bool IsValidForScanning(string tradingRoot)
        {
            // Temporarily set TradingRoot for validation
            var originalTradingRoot = TradingRoot;
            TradingRoot = tradingRoot;
            
            var result = IsValidForScanning();
            
            // Restore original TradingRoot
            TradingRoot = originalTradingRoot;
            
            return result;
        }
        
        /// <summary>
        /// Gets the target asset path for a given asset type
        /// </summary>
        public string GetAssetTargetPath(string assetType, string assetName)
        {
            var subFolder = Platform.ToUpper() switch
            {
                "MT4" => assetType switch
                {
                    "EA" => "MQL4\\Experts",
                    "Indicator" => "MQL4\\Indicators", 
                    "Script" => "MQL4\\Scripts",
                    "Library" => "MQL4\\Libraries",
                    "Template" => "templates",
                    "Preset" => "MQL4\\Presets",
                    _ => ""
                },
                "MT5" => assetType switch
                {
                    "EA" => "MQL5\\Experts",
                    "Indicator" => "MQL5\\Indicators",
                    "Script" => "MQL5\\Scripts", 
                    "Library" => "MQL5\\Libraries",
                    "Template" => "templates",
                    "Preset" => "MQL5\\Presets",
                    _ => ""
                },
                "TRADEREVOLUTION" => assetType switch
                {
                    "Robot" => "Robots",
                    "Indicator" => "Indicators",
                    "Script" => "Scripts",
                    "Plugin" => "Plugins",
                    "Workspace" => "Workspaces",
                    _ => ""
                },
                _ => ""
            };
            
            if (string.IsNullOrEmpty(subFolder))
                return "";
                
            return Path.Combine(InstancePath, subFolder, assetName);
        }
        
        /// <summary>
        /// Checks if this instance is compatible with a given platform
        /// </summary>
        public bool IsCompatibleWith(string assetPlatform)
        {
            return Platform.Equals(assetPlatform, StringComparison.OrdinalIgnoreCase);
        }
        
        public override string ToString()
        {
            return $"{Platform} - {Name} ({AccountType})";
        }
    }

    /// <summary>
    /// Configuration container for trading environment
    /// </summary>
    public class Configuration
    {
        public string TradingRoot { get; set; } = "";
        public string DefaultDataRoot { get; set; } = "";
        public List<TradingInstance> Instances { get; set; } = new List<TradingInstance>();
        
        /// <summary>
        /// Loads configuration from instances-config.json
        /// </summary>
        public static Configuration LoadFromFile(string configPath)
        {
            try
            {
                if (!File.Exists(configPath))
                    return new Configuration();
                
                var json = File.ReadAllText(configPath);
                var config = System.Text.Json.JsonSerializer.Deserialize<Configuration>(json);
                
                // Set TradingRoot on each instance for asset scanning
                if (config != null)
                {
                    foreach (var instance in config.Instances)
                    {
                        instance.TradingRoot = config.TradingRoot;
                    }
                }
                
                return config ?? new Configuration();
            }
            catch
            {
                return new Configuration();
            }
        }
        
        /// <summary>
        /// Gets only enabled instances that are valid for asset operations
        /// </summary>
        public List<TradingInstance> GetValidInstances()
        {
            return Instances.Where(i => i.Enabled && i.IsValidForScanning()).ToList();
        }
    }
}