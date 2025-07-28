using System;
using System.Windows.Forms;
using AssetManager;

namespace AssetManager
{
    /// <summary>
    /// Level 8: Unified Trading Platform Manager - Main Entry Point
    /// Combines Level 6 (Instance Management) + Level 7 (Asset Management) + Asset Matrix Manager
    /// </summary>
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            // Check for command line arguments
            if (args.Length > 0 && args[0] == "--console")
            {
                RunConsoleMode();
            }
            else
            {
                RunGUI();
            }
        }
        
        /// <summary>
        /// Run the unified GUI application
        /// </summary>
        static void RunGUI()
        {
            try
            {
                // Enable Windows visual styles
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                
                // Set high DPI awareness for better display on modern monitors
                if (Environment.OSVersion.Version.Major >= 6)
                {
                    SetProcessDPIAware();
                }
                
                // Create and run the main Level 8 form
                using var mainForm = new Level8UnifiedManager();
                Application.Run(mainForm);
            }
            catch (Exception ex)
            {
                // Show fatal error dialog
                MessageBox.Show(
                    $"Fatal error in Level 8 Unified Manager:\n\n{ex.Message}\n\nStack trace:\n{ex.StackTrace}", 
                    "Level 8 Fatal Error", 
                    MessageBoxButtons.OK, 
                    MessageBoxIcon.Error);
                
                // Log to file if possible
                try
                {
                    var logPath = System.IO.Path.Combine(
                        System.IO.Directory.GetCurrentDirectory(), 
                        "level8-crash.log");
                    
                    var crashLog = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] FATAL ERROR in Level 8\n" +
                                  $"Exception: {ex.GetType().Name}\n" +
                                  $"Message: {ex.Message}\n" +
                                  $"Stack Trace:\n{ex.StackTrace}\n" +
                                  $"Inner Exception: {ex.InnerException?.Message ?? "None"}\n" +
                                  new string('=', 80) + "\n";
                    
                    System.IO.File.AppendAllText(logPath, crashLog);
                }
                catch
                {
                    // Silent fail on logging - don't show another error dialog
                }
            }
        }
        
        /// <summary>
        /// Run in console mode for testing and diagnostics
        /// </summary>
        static void RunConsoleMode()
        {
            Console.WriteLine("=== Level 8: Unified Trading Platform Manager - Console Mode ===\n");
            
            try
            {
                // Initialize cache manager for testing
                var cacheManager = new AssetManager.Services.CacheManager();
                
                Console.WriteLine("🚀 Level 8 Console Mode Initialized");
                Console.WriteLine("   Combined Instance Management + Asset Management + Matrix Manager");
                Console.WriteLine();
                
                // Run diagnostics
                RunSystemDiagnostics();
                TestCacheSystem(cacheManager);
                TestConfigurationSystem();
                
                Console.WriteLine("\n=== Level 8 Console Tests Complete ===");
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Console mode error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                Console.WriteLine("\nPress any key to exit...");
                Console.ReadKey();
            }
        }
        
        /// <summary>
        /// Run system diagnostics
        /// </summary>
        static void RunSystemDiagnostics()
        {
            Console.WriteLine("🔧 System Diagnostics:");
            Console.WriteLine($"   OS: {Environment.OSVersion}");
            Console.WriteLine($"   .NET: {Environment.Version}");
            Console.WriteLine($"   Machine: {Environment.MachineName}");
            Console.WriteLine($"   User: {Environment.UserName}");
            Console.WriteLine($"   Working Directory: {Environment.CurrentDirectory}");
            Console.WriteLine($"   Processor Count: {Environment.ProcessorCount}");
            Console.WriteLine($"   System Memory: {GC.GetTotalMemory(false) / (1024 * 1024)} MB allocated");
            Console.WriteLine();
            
            // Check for required files
            var configPath = System.IO.Path.Combine(Environment.CurrentDirectory, "instances-config.json");
            var configExists = System.IO.File.Exists(configPath);
            
            Console.WriteLine("📁 File System Check:");
            Console.WriteLine($"   Configuration file: {(configExists ? "✅ Found" : "❌ Missing")} - {configPath}");
            Console.WriteLine($"   Current directory access: {(System.IO.Directory.Exists(Environment.CurrentDirectory) ? "✅ OK" : "❌ Failed")}");
            Console.WriteLine();
        }
        
        /// <summary>
        /// Test the cache system
        /// </summary>
        static void TestCacheSystem(AssetManager.Services.CacheManager cacheManager)
        {
            Console.WriteLine("💾 Cache System Test:");
            
            try
            {
                // Test basic caching
                var testAsset = new AssetManager.Models.AssetInfo
                {
                    Name = "TestAsset",
                    Platform = "MT4",
                    Type = "Test",
                    InstanceName = "TestInstance",
                    FilePath = "test.ex4"
                };
                
                var cacheKey = testAsset.GetCacheKey();
                cacheManager.CacheAsset(cacheKey, testAsset);
                
                var retrieved = cacheManager.GetCachedAsset(cacheKey);
                var cacheWorking = retrieved != null && retrieved.Name == testAsset.Name;
                
                Console.WriteLine($"   Basic caching: {(cacheWorking ? "✅ Working" : "❌ Failed")}");
                
                // Get cache statistics
               var stats = cacheManager.GetCacheStats();
                Console.WriteLine($"   Memory entries: {stats.MemoryCacheEntries}");
                Console.WriteLine($"   Disk entries: {stats.DiskCacheEntries}");
                Console.WriteLine($"   Memory usage: {stats.GetFormattedMemoryUsage()}");
                Console.WriteLine($"   Oldest entry: {stats.OldestCacheEntry}");
                Console.WriteLine($"   Newest entry: {stats.NewestCacheEntry}");
                Console.WriteLine("   Cache system: ✅ Operational");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   Cache system: ❌ Error - {ex.Message}");
            }
            
            Console.WriteLine();
        }
        
        /// <summary>
        /// Test configuration system
        /// </summary>
        static void TestConfigurationSystem()
        {
            Console.WriteLine("⚙️ Configuration System Test:");
            
            try
            {
                var configPath = System.IO.Path.Combine(Environment.CurrentDirectory, "instances-config.json");
                
                if (System.IO.File.Exists(configPath))
                {
                    var json = System.IO.File.ReadAllText(configPath);
                    var options = new System.Text.Json.JsonSerializerOptions 
                    { 
                        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase 
                    };
                    var config = System.Text.Json.JsonSerializer.Deserialize<AssetManager.Models.Configuration>(json, options);
                    
                    if (config != null)
                    {
                        Console.WriteLine($"   Configuration loaded: ✅ Success");
                        Console.WriteLine($"   Trading root: {config.TradingRoot}");
                        Console.WriteLine($"   Instances defined: {config.Instances.Count}");
                        Console.WriteLine($"   Enabled instances: {config.Instances.Count(i => i.Enabled)}");
                        
                        // Test instance paths
                        var validPaths = 0;
                        foreach (var instance in config.Instances.Where(i => i.Enabled))
                        {
                            instance.TradingRoot = config.TradingRoot; // Set for path calculation
                            if (System.IO.Directory.Exists(instance.InstancePath))
                                validPaths++;
                        }
                        
                        Console.WriteLine($"   Valid instance paths: {validPaths}/{config.Instances.Count(i => i.Enabled)}");
                    }
                    else
                    {
                        Console.WriteLine("   Configuration: ❌ Failed to parse");
                    }
                }
                else
                {
                    Console.WriteLine("   Configuration: ❌ File not found");
                    Console.WriteLine("   Creating sample configuration...");
                    CreateSampleConfiguration(configPath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   Configuration: ❌ Error - {ex.Message}");
            }
            
            Console.WriteLine();
        }
        
        /// <summary>
        /// Create a sample configuration file
        /// </summary>
        static void CreateSampleConfiguration(string configPath)
        {
            try
            {
                var sampleConfig = new AssetManager.Models.Configuration
                {
                    TradingRoot = @"C:\Projects\FinancialData",
                    DefaultDataRoot = @"C:\Projects\FinancialData\TradingData",
                    Instances = new List<AssetManager.Models.TradingInstance>
                    {
                        new AssetManager.Models.TradingInstance
                        {
                            Name = "Sample_MT4_Demo",
                            Broker = "SampleBroker",
                            Platform = "MT4",
                            Source = "MT4_Template",
                            Destination = "Sample_MT4_Demo",
                            AccountType = "Demo",
                            Enabled = true,
                            AutoStart = false
                        },
                        new AssetManager.Models.TradingInstance
                        {
                            Name = "Sample_MT5_Live",
                            Broker = "SampleBroker",
                            Platform = "MT5",
                            Source = "MT5_Template",
                            Destination = "Sample_MT5_Live",
                            AccountType = "Live",
                            Enabled = true,
                            AutoStart = false
                        }
                    }
                };
                
                var options = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                };
                
                var json = System.Text.Json.JsonSerializer.Serialize(sampleConfig, options);
                System.IO.File.WriteAllText(configPath, json);
                
                Console.WriteLine($"   Sample configuration created: ✅ {configPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   Sample configuration creation: ❌ {ex.Message}");
            }
        }
        
        /// <summary>
        /// Import for high DPI awareness (Windows Vista+)
        /// </summary>
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();
    }
}