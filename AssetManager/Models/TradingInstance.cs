using System;
using System.Collections.Generic;

namespace AssetManager.Models
{
    /// <summary>
    /// Represents a trading platform instance (MT4, MT5, or TraderEvolution)
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
        
        // Computed properties for asset scanning (set by scanner)
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
            
            return System.IO.Path.Combine(TradingRoot, "PlatformInstances", Destination);
        }
        
        /// <summary>
        /// Gets the data path for this instance
        /// </summary>
        private string GetDataPath()
        {
            if (string.IsNullOrEmpty(TradingRoot) || string.IsNullOrEmpty(Destination))
                return "";
            
            return System.IO.Path.Combine(TradingRoot, "InstanceData", Destination);
        }
        
        /// <summary>
        /// Gets the asset folder paths for this platform
        /// </summary>
        public string[] GetAssetFolders(string basePath)
        {
            return Platform.ToUpper() switch
            {
                "MT4" => new[]
                {
                    System.IO.Path.Combine(basePath, "MQL4", "Indicators"),
                    System.IO.Path.Combine(basePath, "MQL4", "Experts"),
                    System.IO.Path.Combine(basePath, "MQL4", "Scripts"),
                    System.IO.Path.Combine(basePath, "MQL4", "Libraries"),
                    System.IO.Path.Combine(basePath, "templates"),
                    System.IO.Path.Combine(basePath, "MQL4", "Presets")
                },
                "MT5" => new[]
                {
                    System.IO.Path.Combine(basePath, "MQL5", "Indicators"),
                    System.IO.Path.Combine(basePath, "MQL5", "Experts"),
                    System.IO.Path.Combine(basePath, "MQL5", "Scripts"),
                    System.IO.Path.Combine(basePath, "MQL5", "Libraries"),
                    System.IO.Path.Combine(basePath, "templates"),
                    System.IO.Path.Combine(basePath, "MQL5", "Presets")
                },
                "TRADEREVOLUTION" => new[]
                {
                    System.IO.Path.Combine(basePath, "Indicators"),
                    System.IO.Path.Combine(basePath, "Robots"),
                    System.IO.Path.Combine(basePath, "Scripts"),
                    System.IO.Path.Combine(basePath, "Plugins"),
                    System.IO.Path.Combine(basePath, "Workspaces")
                },
                _ => Array.Empty<string>()
            };
        }
        
        /// <summary>
        /// Gets the file extensions to scan for this platform
        /// </summary>
        public string[] GetAssetExtensions()
        {
            return Platform.ToUpper() switch
            {
                "MT4" => new[] { "*.ex4", "*.mq4", "*.tpl", "*.set" },
                "MT5" => new[] { "*.ex5", "*.mq5", "*.tpl", "*.set" },
                "TRADEREVOLUTION" => new[] { "*.dll", "*.cs", "*.xml" },
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
        /// </summary>
        public bool IsValidForScanning()
        {
            if (!Enabled) return false;
            if (string.IsNullOrEmpty(Platform)) return false;
            if (string.IsNullOrEmpty(TradingRoot) || string.IsNullOrEmpty(Destination)) return false;
            
            // Check if instance path exists
            return System.IO.Directory.Exists(InstancePath);
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
    }
}