using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AssetManager.Models;

namespace AssetManager.Services
{
    /// <summary>
    /// High-performance asset scanner with parallel processing
    /// Implements Quick Win #1: Parallel instance scanning
    /// </summary>
    public class AssetScanner
    {
        private readonly CacheManager _cache;
        private readonly string _tradingRoot;
        private readonly List<TradingInstance> _instances;
        
        public AssetScanner(string tradingRoot, CacheManager cache)
        {
            _tradingRoot = tradingRoot;
            _cache = cache;
            _instances = new List<TradingInstance>();
        }
        
        /// <summary>
        /// Loads trading instances from instances-config.json
        /// </summary>
        public async Task<bool> LoadInstancesAsync()
        {
            try
            {
                var configPath = Path.Combine(_tradingRoot, "instances-config.json");
                if (!File.Exists(configPath))
                {
                    Console.WriteLine($"❌ Config file not found: {configPath}");
                    return false;
                }
                
                var json = await File.ReadAllTextAsync(configPath);
                var config = JsonSerializer.Deserialize<Configuration>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                
                if (config?.Instances != null)
                {
                    _instances.Clear();
                    
                    // Set trading root on each instance and add to list
                    foreach (var instance in config.Instances)
                    {
                        instance.TradingRoot = _tradingRoot;
                        _instances.Add(instance);
                    }
                    
                    Console.WriteLine($"✅ Loaded {_instances.Count} trading instances");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error loading instances: {ex.Message}");
            }
            
            return false;
        }
        
        /// <summary>
        /// Quick Win #1: Parallel scanning of all enabled instances
        /// </summary>
        public async Task<Dictionary<string, List<AssetInfo>>> ScanAllInstancesAsync()
        {
            var enabledInstances = _instances.Where(i => i.IsValidForScanning()).ToList();
            
            if (!enabledInstances.Any())
            {
                Console.WriteLine("❌ No valid instances found for scanning");
                return new Dictionary<string, List<AssetInfo>>();
            }
            
            Console.WriteLine($"🔍 Starting parallel scan of {enabledInstances.Count} instances...");
            
            // Quick Win #1: Scan all instances in parallel
            var scanTasks = enabledInstances.Select(async instance =>
            {
                var assets = await Task.Run(() => ScanSingleInstanceAsync(instance));
                return new KeyValuePair<string, List<AssetInfo>>(instance.Name, assets);
            });
            
            var results = await Task.WhenAll(scanTasks);
            var resultDictionary = results.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            
            var totalAssets = resultDictionary.Values.SelectMany(v => v).Count();
            Console.WriteLine($"✅ Parallel scan completed! Found {totalAssets} assets across {enabledInstances.Count} instances");
            
            return resultDictionary;
        }
        
        /// <summary>
        /// Scans a single trading instance for assets
        /// </summary>
        private async Task<List<AssetInfo>> ScanSingleInstanceAsync(TradingInstance instance)
        {
            var assets = new List<AssetInfo>();
            var instancePath = instance.InstancePath;
            
            try
            {
                Console.WriteLine($"  📂 Scanning {instance.Name} ({instance.Platform})...");
                
                var assetFolders = instance.GetAssetFolders(instancePath);
                var extensions = instance.GetAssetExtensions();
                
                foreach (var folder in assetFolders)
                {
                    if (!Directory.Exists(folder)) continue;
                    
                    var folderType = GetFolderType(folder);
                    var foundAssets = await ScanFolderAsync(folder, extensions, instance, folderType);
                    assets.AddRange(foundAssets);
                }
                
                Console.WriteLine($"  ✅ {instance.Name}: Found {assets.Count} assets");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ❌ Error scanning {instance.Name}: {ex.Message}");
            }
            
            return assets;
        }
        
        /// <summary>
        /// Scans a specific folder for asset files
        /// </summary>
        private async Task<List<AssetInfo>> ScanFolderAsync(string folderPath, string[] extensions, 
            TradingInstance instance, string assetType)
        {
            var assets = new List<AssetInfo>();
            
            try
            {
                foreach (var extension in extensions)
                {
                    var files = Directory.GetFiles(folderPath, extension, SearchOption.AllDirectories);
                    
                    foreach (var filePath in files)
                    {
                        var asset = await ScanAssetFileAsync(filePath, instance, assetType);
                        if (asset != null)
                        {
                            assets.Add(asset);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"    ⚠️  Error scanning folder {folderPath}: {ex.Message}");
            }
            
            return assets;
        }
        
        /// <summary>
        /// Scans a single asset file, using cache for performance
        /// </summary>
        private async Task<AssetInfo?> ScanAssetFileAsync(string filePath, TradingInstance instance, string assetType)
        {
            try
            {
                var fileName = Path.GetFileNameWithoutExtension(filePath);
                var cacheKey = $"{instance.Platform}:{instance.Name}:{fileName}";
                
                // Try cache first
                var cachedAsset = _cache.GetCachedAsset(cacheKey);
                if (cachedAsset != null)
                {
                    return cachedAsset; // Cache hit!
                }
                
                // Cache miss - scan the file
                var fileInfo = new FileInfo(filePath);
                
                var asset = new AssetInfo
                {
                    Name = fileName,
                    Platform = instance.Platform,
                    Type = assetType,
                    FilePath = filePath,
                    InstanceName = instance.Name,
                    LastModified = fileInfo.LastWriteTime,
                    FileSize = fileInfo.Length,
                    QuickHash = CacheManager.ComputeQuickHash(filePath),
                    Version = await ExtractVersionAsync(filePath),
                    ConversionComplexity = AnalyzeConversionComplexity(filePath, instance.Platform)
                };
                
                // Cache for next time
                _cache.CacheAsset(cacheKey, asset);
                
                return asset;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"    ⚠️  Error scanning file {filePath}: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Extracts version information from asset file
        /// </summary>
        private async Task<string> ExtractVersionAsync(string filePath)
        {
            try
            {
                // For compiled files (.ex4, .ex5, .dll), version extraction is limited
                // For source files (.mq4, .mq5, .cs), we could parse the source
                
                var extension = Path.GetExtension(filePath).ToLower();
                
                if (extension == ".mq4" || extension == ".mq5" || extension == ".cs")
                {
                    // Try to extract version from source code
                    var lines = await File.ReadAllLinesAsync(filePath);
                    foreach (var line in lines.Take(50)) // Check first 50 lines
                    {
                        if (line.Contains("version", StringComparison.OrdinalIgnoreCase) ||
                            line.Contains("ver ", StringComparison.OrdinalIgnoreCase))
                        {
                            // Simple pattern matching for version
                            var versionMatch = System.Text.RegularExpressions.Regex.Match(line, @"(\d+\.\d+(?:\.\d+)?)");
                            if (versionMatch.Success)
                            {
                                return versionMatch.Groups[1].Value;
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
        private string AnalyzeConversionComplexity(string filePath, string platform)
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
        /// Determines asset type based on folder path
        /// </summary>
        private string GetFolderType(string folderPath)
        {
            var folderName = Path.GetFileName(folderPath).ToLower();
            
            return folderName switch
            {
                "experts" => "EA",
                "indicators" => "Indicator", 
                "scripts" => "Script",
                "libraries" => "Library",
                "robots" => "Robot",
                "plugins" => "Plugin",
                "templates" => "Template",
                "presets" => "Preset",
                "workspaces" => "Workspace",
                _ => "Unknown"
            };
        }
        
        /// <summary>
        /// Gets list of loaded instances
        /// </summary>
        public List<TradingInstance> GetInstances()
        {
            return _instances.ToList();
        }
        
        /// <summary>
        /// Gets instances filtered by platform
        /// </summary>
        public List<TradingInstance> GetInstancesByPlatform(string platform)
        {
            return _instances.Where(i => i.Platform.Equals(platform, StringComparison.OrdinalIgnoreCase)).ToList();
        }
    }
    
    /// <summary>
    /// Configuration structure to match instances-config.json
    /// </summary>
    public class Configuration
    {
        public string TradingRoot { get; set; } = "";
        public string DefaultDataRoot { get; set; } = "";
        public List<TradingInstance> Instances { get; set; } = new();
    }
}