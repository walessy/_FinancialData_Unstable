using System;
using System.IO;
using System.Text.RegularExpressions;

namespace AssetManager.Models
{
    /// <summary>
    /// Represents a trading asset (EA, Indicator, Script, etc.) with platform awareness
    /// </summary>
    public class AssetInfo
    {
        // Basic asset information
        public string Name { get; set; } = "";
        public string Platform { get; set; } = ""; // MT4, MT5, TraderEvolution
        public string Type { get; set; } = "";     // EA, Indicator, Script, Robot, Plugin
        public string FilePath { get; set; } = "";
        public string FullPath { get; set; } = ""; // NEW: Full file path for matrix manager
        public string InstanceName { get; set; } = "";
        
        // Version and metadata
        public string Version { get; set; } = "Unknown";
        public DateTime LastModified { get; set; }
        public long FileSize { get; set; }
        
        // Caching optimization fields
        public long QuickHash { get; set; }        // Hash of first 1KB for fast change detection
        public long FullHash { get; set; }         // Full file hash for integrity
        public DateTime CacheTime { get; set; }    // When this was cached
        
        // Deployment status
        public bool IsDeployed { get; set; }
        public string DeploymentStatus { get; set; } = "Unknown"; // Missing, Match, Outdated, Error
        
        // Asset analysis (for conversion planning)
        public string ConversionComplexity { get; set; } = "Unknown"; // Simple, Medium, Complex, Manual
        public string[] Dependencies { get; set; } = Array.Empty<string>();
        public string Notes { get; set; } = "";
        
        /// <summary>
        /// CRITICAL: Creates AssetInfo from file path with platform detection
        /// Required by the new AssetMatrixManager
        /// </summary>
        public static AssetInfo? FromFile(string filePath, string platform)
        {
            try
            {
                if (!File.Exists(filePath))
                    return null;
                
                var fileInfo = new FileInfo(filePath);
                var fileName = Path.GetFileNameWithoutExtension(filePath);
                var directory = Path.GetDirectoryName(filePath) ?? "";
                
                return new AssetInfo
                {
                    Name = fileName,
                    Platform = platform,
                    Type = GetAssetTypeFromPath(filePath, platform),
                    FilePath = filePath,
                    FullPath = filePath, // NEW: Required by matrix manager
                    Version = ExtractVersionFromFile(filePath),
                    LastModified = fileInfo.LastWriteTime,
                    FileSize = fileInfo.Length,
                    ConversionComplexity = AnalyzeConversionComplexity(filePath, platform),
                    CacheTime = DateTime.Now
                };
            }
            catch (Exception)
            {
                return null; // Return null for invalid files
            }
        }
        
        /// <summary>
        /// Creates a cache key for this asset
        /// </summary>
        public string GetCacheKey()
        {
            return $"{Platform}:{InstanceName}:{Name}";
        }
        
        /// <summary>
        /// Gets the asset type based on file path and platform
        /// </summary>
        public static string GetAssetTypeFromPath(string filePath, string platform)
        {
            var fileName = Path.GetFileName(filePath).ToLower();
            var directory = Path.GetDirectoryName(filePath)?.ToLower() ?? "";
            
            return platform switch
            {
                "MT4" or "MT5" => GetMetaTraderAssetType(directory, fileName),
                "TraderEvolution" => GetTraderEvolutionAssetType(directory, fileName),
                _ => "Unknown"
            };
        }
        
        private static string GetMetaTraderAssetType(string directory, string fileName)
        {
            if (directory.Contains("experts")) return "EA";
            if (directory.Contains("indicators")) return "Indicator";
            if (directory.Contains("scripts")) return "Script";
            if (directory.Contains("libraries")) return "Library";
            if (fileName.EndsWith(".tpl")) return "Template";
            if (fileName.EndsWith(".set")) return "Preset";
            return "Unknown";
        }
        
        private static string GetTraderEvolutionAssetType(string directory, string fileName)
        {
            if (directory.Contains("robots")) return "Robot";
            if (directory.Contains("indicators")) return "Indicator";
            if (directory.Contains("scripts")) return "Script";
            if (directory.Contains("plugins")) return "Plugin";
            if (fileName.EndsWith(".xml")) return "Workspace";
            return "Unknown";
        }
        
        /// <summary>
        /// Attempts to extract version information from the file
        /// </summary>
        private static string ExtractVersionFromFile(string filePath)
        {
            try
            {
                var extension = Path.GetExtension(filePath).ToLower();
                
                // For source files, try to read version from comments
                if (extension == ".mq4" || extension == ".mq5" || extension == ".cs")
                {
                    var lines = File.ReadLines(filePath);
                    foreach (var line in lines)
                    {
                        // Look for version patterns in comments
                        var versionPatterns = new[]
                        {
                            @"version\s*[:=]\s*([0-9]+\.[0-9]+(?:\.[0-9]+)?)",
                            @"v\.?\s*([0-9]+\.[0-9]+(?:\.[0-9]+)?)",
                            @"Ver\s*\.?\s*([0-9]+\.[0-9]+(?:\.[0-9]+)?)"
                        };
                        
                        foreach (var pattern in versionPatterns)
                        {
                            var match = Regex.Match(line, pattern, RegexOptions.IgnoreCase);
                            if (match.Success)
                            {
                                return match.Groups[1].Value;
                            }
                        }
                    }
                }
                
                // Fallback: use file modification date as version indicator
                var fileInfo = new FileInfo(filePath);
                return fileInfo.LastWriteTime.ToString("yyyy.MM.dd");
            }
            catch
            {
                return "Unknown";
            }
        }
        
        /// <summary>
        /// Analyzes how complex it would be to convert this asset to other platforms
        /// </summary>
        private static string AnalyzeConversionComplexity(string filePath, string platform)
        {
            try
            {
                var extension = Path.GetExtension(filePath).ToLower();
                
                // Only analyze source files
                if (extension != ".mq4" && extension != ".mq5" && extension != ".cs")
                {
                    return "Unknown"; // Can't analyze compiled files
                }
                
                // Simple analysis based on file size and common patterns
                var fileInfo = new FileInfo(filePath);
                
                if (fileInfo.Length < 5000) return "Simple";      // Small files usually simple
                if (fileInfo.Length < 50000) return "Medium";     // Medium files need review
                return "Complex";                                  // Large files likely complex
            }
            catch
            {
                return "Unknown";
            }
        }
        
        /// <summary>
        /// Creates a display-friendly string for this asset
        /// </summary>
        public override string ToString()
        {
            return $"{Platform} {Type}: {Name} v{Version}";
        }
    }
}