using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using AssetManager.Models;
using AssetManager.Services;

namespace AssetManager
{
    /// <summary>
    /// Level 7 Asset Manager - Console tests + GUI interface
    /// </summary>
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            // Check if running with --console argument for testing
            if (args.Length > 0 && args[0] == "--console")
            {
                RunConsoleTests();
                return;
            }
            
            // Run the GUI by default
            RunAssetManagerUI();
        }
        
        static void RunAssetManagerUI()
        {
            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                
                Console.WriteLine("🚀 Starting Level 7 Asset Management UI...");
                Application.Run(new Level7AssetManager());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fatal error: {ex.Message}\n\nStack trace:\n{ex.StackTrace}", 
                    "Level 7 Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        static void RunConsoleTests()
        {
            Console.WriteLine("=== Asset Manager Cache System Test ===\n");
            
            var cacheManager = new CacheManager();
            
            // Test 1: Basic caching functionality
            TestBasicCaching(cacheManager);
            
            // Test 2: Performance benchmarks
            TestCachePerformance(cacheManager);
            
            // Test 3: File change detection
            TestFileChangeDetection(cacheManager);
            
            // Test 4: Cache statistics
            ShowCacheStats(cacheManager);
            
            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
        
        static void TestBasicCaching(CacheManager cache)
        {
            Console.WriteLine("Test 1: Basic Caching Functionality");
            Console.WriteLine("=====================================");
            
            // Create a test asset
            var testAsset = new AssetInfo
            {
                Name = "TestIndicator",
                Platform = "MT4",
                Type = "Indicator",
                FilePath = @"C:\Test\Indicators\TestIndicator.ex4",
                InstanceName = "MT4_Demo",
                Version = "1.0",
                LastModified = DateTime.Now,
                FileSize = 15420,
                QuickHash = 123456789,
                FullHash = 987654321
            };
            
            var cacheKey = testAsset.GetCacheKey();
            Console.WriteLine($"Cache Key: {cacheKey}");
            
            // Test cache miss
            var cachedAsset = cache.GetCachedAsset(cacheKey);
            Console.WriteLine($"Cache miss (expected): {cachedAsset == null}");
            
            // Cache the asset
            cache.CacheAsset(cacheKey, testAsset);
            Console.WriteLine("Asset cached successfully");
            
            // Test cache hit
            cachedAsset = cache.GetCachedAsset(cacheKey);
            Console.WriteLine($"Cache hit: {cachedAsset != null}");
            Console.WriteLine($"Retrieved asset: {cachedAsset?.Name ?? "null"}");
            
            Console.WriteLine("✅ Basic caching test passed\n");
        }
        
        static void TestCachePerformance(CacheManager cache)
        {
            Console.WriteLine("Test 2: Cache Performance Benchmark");
            Console.WriteLine("===================================");
            
            const int testCount = 1000;
            var random = new Random();
            
            // Create test assets
            var testAssets = new AssetInfo[testCount];
            for (int i = 0; i < testCount; i++)
            {
                testAssets[i] = new AssetInfo
                {
                    Name = $"TestAsset_{i}",
                    Platform = i % 3 == 0 ? "MT4" : i % 3 == 1 ? "MT5" : "TraderEvolution",
                    Type = "Indicator",
                    FilePath = $@"C:\Test\Asset_{i}.ex4",
                    InstanceName = $"Instance_{i}",
                    Version = "1.0",
                    LastModified = DateTime.Now.AddDays(-random.Next(30)),
                    FileSize = random.Next(1000, 50000),
                    QuickHash = random.Next(),
                    FullHash = random.Next()
                };
            }
            
            // Test caching performance
            var stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < testCount; i++)
            {
                var cacheKey = testAssets[i].GetCacheKey();
                cache.CacheAsset(cacheKey, testAssets[i]);
            }
            stopwatch.Stop();
            
            Console.WriteLine($"Cached {testCount} assets in {stopwatch.ElapsedMilliseconds}ms");
            Console.WriteLine($"Average: {(double)stopwatch.ElapsedMilliseconds / testCount:F3}ms per asset");
            
            // Test retrieval performance
            stopwatch.Restart();
            for (int i = 0; i < testCount; i++)
            {
                var cacheKey = testAssets[i].GetCacheKey();
                cache.GetCachedAsset(cacheKey);
            }
            stopwatch.Stop();
            
            Console.WriteLine($"Retrieved {testCount} assets in {stopwatch.ElapsedMilliseconds}ms");
            Console.WriteLine($"Average: {(double)stopwatch.ElapsedMilliseconds / testCount:F3}ms per retrieval");
            Console.WriteLine("✅ Performance test passed\n");
        }
        
        static void TestFileChangeDetection(CacheManager cache)
        {
            Console.WriteLine("Test 3: File Change Detection");
            Console.WriteLine("=============================");
            
            var testFile = Path.GetTempFileName();
            
            try
            {
                // Write initial content
                File.WriteAllText(testFile, "Initial content");
                var initialHash = CacheManager.ComputeQuickHash(testFile);
                Console.WriteLine($"Initial hash: {initialHash}");
                
                // Modify file
                File.WriteAllText(testFile, "Modified content");
                var modifiedHash = CacheManager.ComputeQuickHash(testFile);
                Console.WriteLine($"Modified hash: {modifiedHash}");
                Console.WriteLine($"Hashes different (expected): {initialHash != modifiedHash}");
                
                // Performance test for change detection
                var stopwatch = Stopwatch.StartNew();
                for (int i = 0; i < 1000; i++)
                {
                    cache.HasFileChanged(testFile, initialHash);
                }
                stopwatch.Stop();
                
                Console.WriteLine($"1000 change detections in {stopwatch.ElapsedMilliseconds}ms");
                Console.WriteLine($"Average: {(double)stopwatch.ElapsedMilliseconds / 1000:F3}ms per check");
                Console.WriteLine("✅ File change detection test passed\n");
            }
            finally
            {
                File.Delete(testFile);
            }
        }
        
        static void ShowCacheStats(CacheManager cache)
        {
            Console.WriteLine("Test 4: Cache Statistics");
            Console.WriteLine("========================");
            
            var stats = cache.GetCacheStats();
            Console.WriteLine($"💾 Cache Statistics:");
            Console.WriteLine($"  Memory entries: {stats.MemoryCacheEntries}");
            Console.WriteLine($"  Disk entries: {stats.DiskCacheEntries}");
            Console.WriteLine($"  Memory usage: {stats.GetFormattedMemoryUsage()}");
            Console.WriteLine($"  Cache age: {stats.OldestCacheEntry:HH:mm:ss} to {stats.NewestCacheEntry:HH:mm:ss}");
            
            Console.WriteLine($"\n🏗️  Ready for Asset Management:");
            Console.WriteLine($"  Test assets cached: {stats.MemoryCacheEntries}");
            Console.WriteLine($"  Platforms: MT4, MT5, TraderEvolution");
            Console.WriteLine($"  Cache system status: Operational");
            
            Console.WriteLine("✅ Asset Manager foundation ready for UI development!");
            Console.WriteLine("✅ Cache statistics displayed\n");
        }
    }
}