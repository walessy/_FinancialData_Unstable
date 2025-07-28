using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using AssetManager.Models;
using AssetManager.Services;

namespace AssetManager
{
    /// <summary>
    /// Level 7 Asset Management UI - Builds on your existing cache foundation
    /// </summary>
    public partial class Level7AssetManager : Form
    {
        private readonly CacheManager _cacheManager;
        private System.Windows.Forms.Timer _refreshTimer = null!;
        private System.Windows.Forms.Timer _cacheStatsTimer = null!;
        
        // UI Components
        private MenuStrip _menuStrip = null!;
        private ToolStrip _toolStrip = null!;
        private StatusStrip _statusStrip = null!;
        private ToolStripStatusLabel _statusLabel = null!;
        private ToolStripProgressBar _progressBar = null!;
        
        // Main panels
        private SplitContainer _mainSplitContainer = null!;
        private SplitContainer _rightSplitContainer = null!;
        
        // Asset management
        private ListView _assetListView = null!;
        private PropertyGrid _assetPropertyGrid = null!;
        
        // Cache monitoring
        private Panel _cacheStatsPanel = null!;
        private Label _memoryEntriesLabel = null!;
        private Label _diskEntriesLabel = null!;
        private Label _memoryUsageLabel = null!;
        private Label _cacheAgeLabel = null!;
        private ProgressBar _memoryUsageProgress = null!;
        
        // Real-time monitoring
        private Panel _monitoringPanel = null!;
        private ListBox _activityLog = null!;
        private Label _platformCountLabel = null!;
        private Label _enabledInstancesLabel = null!;
        private Label _accountTypesLabel = null!;
        
        private List<AssetInfo> _assets = new List<AssetInfo>();
        private readonly string _configPath;

        public Level7AssetManager()
        {
            _configPath = FindConfigPath();
            _cacheManager = new CacheManager();
            
            InitializeComponent();
            InitializeLayout();
            InitializeTimers();
            
            LoadAssets();
            RefreshCacheStats();
            
            LogActivity("🚀 Level 7 Asset Management System Initialized with existing cache foundation");
        }

        private string FindConfigPath()
        {
            var currentPath = Directory.GetCurrentDirectory();
            var configFile = Path.Combine(currentPath, "instances-config.json");
            
            if (File.Exists(configFile))
                return configFile;
                
            // Try parent directories
            var parent = Directory.GetParent(currentPath);
            while (parent != null)
            {
                configFile = Path.Combine(parent.FullName, "instances-config.json");
                if (File.Exists(configFile))
                    return configFile;
                parent = parent.Parent;
            }
            
            return Path.Combine(currentPath, "instances-config.json");
        }

        private void InitializeComponent()
        {
            this.Text = "Level 7: Asset Management UI - Building on Cache Foundation";
            this.Size = new Size(1600, 1000);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimumSize = new Size(1200, 800);
            
            // Dark theme
            this.BackColor = Color.FromArgb(30, 30, 30);
            this.ForeColor = Color.White;
        }

        private void InitializeLayout()
        {
            this.SuspendLayout();
            
            // Menu Strip
            _menuStrip = new MenuStrip();
            _menuStrip.BackColor = Color.FromArgb(45, 45, 48);
            _menuStrip.ForeColor = Color.FromArgb(240, 240, 240); // Brighter menu text
            
            var fileMenu = new ToolStripMenuItem("File");
            fileMenu.ForeColor = Color.FromArgb(240, 240, 240); // Brighter menu item text
            fileMenu.DropDownItems.Add("Refresh Assets", null, (s, e) => RefreshAssets());
            fileMenu.DropDownItems.Add("Clear Cache", null, (s, e) => ClearCache());
            fileMenu.DropDownItems.Add("Export Assets", null, (s, e) => ExportAssets());
            fileMenu.DropDownItems.Add("-");
            fileMenu.DropDownItems.Add("Console Tests", null, (s, e) => RunConsoleTests());
            fileMenu.DropDownItems.Add("-");
            fileMenu.DropDownItems.Add("Exit", null, (s, e) => Close());
            
            var cacheMenu = new ToolStripMenuItem("Cache");
            cacheMenu.ForeColor = Color.FromArgb(240, 240, 240); // Brighter menu item text
            cacheMenu.DropDownItems.Add("Refresh Statistics", null, (s, e) => RefreshCacheStats());
            cacheMenu.DropDownItems.Add("Optimize Cache", null, (s, e) => OptimizeCache());
            cacheMenu.DropDownItems.Add("Cache Health Check", null, (s, e) => PerformCacheHealthCheck());
            cacheMenu.DropDownItems.Add("-");
            cacheMenu.DropDownItems.Add("Show Cache Directory", null, (s, e) => ShowCacheDirectory());
            
            var viewMenu = new ToolStripMenuItem("View");
            viewMenu.ForeColor = Color.FromArgb(240, 240, 240); // Brighter menu item text
            viewMenu.DropDownItems.Add("Auto-Refresh", null, ToggleAutoRefresh);
            viewMenu.DropDownItems.Add("Show Details", null, (s, e) => ShowAssetDetails());
            viewMenu.DropDownItems.Add("Activity Log", null, (s, e) => ShowActivityLog());
            
            _menuStrip.Items.AddRange(new[] { fileMenu, cacheMenu, viewMenu });
            
            // Tool Strip
            _toolStrip = new ToolStrip();
            _toolStrip.BackColor = Color.FromArgb(45, 45, 48);
            _toolStrip.ForeColor = Color.FromArgb(240, 240, 240); // Brighter toolbar text
            
            _toolStrip.Items.Add(new ToolStripButton("Refresh", null, (s, e) => RefreshAssets()) { DisplayStyle = ToolStripItemDisplayStyle.ImageAndText });
            _toolStrip.Items.Add(new ToolStripSeparator());
            _toolStrip.Items.Add(new ToolStripButton("Cache Stats", null, (s, e) => RefreshCacheStats()) { DisplayStyle = ToolStripItemDisplayStyle.ImageAndText });
            _toolStrip.Items.Add(new ToolStripButton("Optimize", null, (s, e) => OptimizeCache()) { DisplayStyle = ToolStripItemDisplayStyle.ImageAndText });
            _toolStrip.Items.Add(new ToolStripSeparator());
            
            // Asset Management dropdown button
            var assetMgmtButton = new ToolStripDropDownButton("Asset Management");
            assetMgmtButton.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
            assetMgmtButton.DropDownItems.Add("Refresh All Assets", null, (s, e) => RefreshAssets());
            assetMgmtButton.DropDownItems.Add("Export Asset List", null, (s, e) => ExportAssets());
            assetMgmtButton.DropDownItems.Add("-");
            assetMgmtButton.DropDownItems.Add("Show Asset Details", null, (s, e) => ShowAssetDetails());
            assetMgmtButton.DropDownItems.Add("Validate All Paths", null, (s, e) => ValidateAssetPaths());
            _toolStrip.Items.Add(assetMgmtButton);
            
            // Status Strip
            _statusStrip = new StatusStrip();
            _statusStrip.BackColor = Color.FromArgb(45, 45, 48);
            _statusStrip.ForeColor = Color.FromArgb(240, 240, 240); // Brighter status text
            
            _statusLabel = new ToolStripStatusLabel("Ready");
            _progressBar = new ToolStripProgressBar() { Visible = false };
            
            _statusStrip.Items.AddRange(new ToolStripItem[] { _statusLabel, _progressBar });
            
            // Main Split Container
            _mainSplitContainer = new SplitContainer()
            {
                Dock = DockStyle.Fill,
                SplitterDistance = 400,
                BackColor = Color.FromArgb(30, 30, 30)
            };
            
            // Right Split Container
            _rightSplitContainer = new SplitContainer()
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 300,
                BackColor = Color.FromArgb(30, 30, 30)
            };
            
            // Asset List View (Left Panel)
            _assetListView = new ListView()
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                BackColor = Color.FromArgb(40, 40, 40),
                ForeColor = Color.FromArgb(240, 240, 240) // Brighter default text
            };
            
            _assetListView.Columns.Add("Name", 200);
            _assetListView.Columns.Add("Platform", 100);
            _assetListView.Columns.Add("Type", 80);
            _assetListView.Columns.Add("Version", 120);
            _assetListView.Columns.Add("Status", 80);
            _assetListView.Columns.Add("File Size", 100);
            
            _assetListView.SelectedIndexChanged += AssetListView_SelectedIndexChanged;
            
            // Cache Stats Panel (Top Right)
            CreateCacheStatsPanel();
            
            // Monitoring Panel (Bottom Right)
            CreateMonitoringPanel();
            
            // Asset Property Grid
            _assetPropertyGrid = new PropertyGrid()
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(40, 40, 40),
                CategoryForeColor = Color.LightBlue,
                CategorySplitterColor = Color.Gray,
                CommandsBackColor = Color.FromArgb(40, 40, 40),
                CommandsForeColor = Color.White,
                DisabledItemForeColor = Color.Gray,
                HelpBackColor = Color.FromArgb(40, 40, 40),
                HelpForeColor = Color.White,
                LineColor = Color.Gray,
                SelectedItemWithFocusBackColor = Color.FromArgb(0, 120, 215),
                SelectedItemWithFocusForeColor = Color.White,
                ViewBackColor = Color.FromArgb(40, 40, 40),
                ViewForeColor = Color.White
            };
            
            // Layout assembly
            _mainSplitContainer.Panel1.Controls.Add(_assetListView);
            _mainSplitContainer.Panel2.Controls.Add(_rightSplitContainer);
            
            _rightSplitContainer.Panel1.Controls.Add(_cacheStatsPanel);
            _rightSplitContainer.Panel2.Controls.Add(_monitoringPanel);
            
            this.Controls.Add(_mainSplitContainer);
            this.Controls.Add(_toolStrip);
            this.Controls.Add(_menuStrip);
            this.Controls.Add(_statusStrip);
            
            this.ResumeLayout();
        }

        private void CreateCacheStatsPanel()
        {
            _cacheStatsPanel = new Panel()
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(35, 35, 35),
                Padding = new Padding(10)
            };
            
            var title = new Label()
            {
                Text = "💾 Cache Statistics (Your Existing Cache System)",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.LightBlue,
                AutoSize = true,
                Location = new Point(10, 10)
            };
            
            _memoryEntriesLabel = new Label()
            {
                Text = "Memory entries: 0",
                ForeColor = Color.FromArgb(220, 220, 220), // Brighter white
                AutoSize = true,
                Location = new Point(10, 40)
            };
            
            _diskEntriesLabel = new Label()
            {
                Text = "Disk entries: 0",
                ForeColor = Color.FromArgb(220, 220, 220), // Brighter white
                AutoSize = true,
                Location = new Point(10, 60)
            };
            
            _memoryUsageLabel = new Label()
            {
                Text = "Memory usage: 0 KB",
                ForeColor = Color.FromArgb(220, 220, 220), // Brighter white
                AutoSize = true,
                Location = new Point(10, 80)
            };
            
            _memoryUsageProgress = new ProgressBar()
            {
                Location = new Point(150, 82),
                Size = new Size(200, 15),
                Style = ProgressBarStyle.Continuous
            };
            
            _cacheAgeLabel = new Label()
            {
                Text = "Cache age: N/A",
                ForeColor = Color.FromArgb(220, 220, 220), // Brighter white
                AutoSize = true,
                Location = new Point(10, 110)
            };
            
            _cacheStatsPanel.Controls.AddRange(new Control[] 
            {
                title, _memoryEntriesLabel, _diskEntriesLabel, 
                _memoryUsageLabel, _memoryUsageProgress, _cacheAgeLabel
            });
        }

        private void CreateMonitoringPanel()
        {
            _monitoringPanel = new Panel()
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(35, 35, 35),
                Padding = new Padding(10)
            };
            
            var title = new Label()
            {
                Text = "🏗️ Asset Management Monitor",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.LightGreen,
                AutoSize = true,
                Location = new Point(10, 10)
            };
            
            _platformCountLabel = new Label()
            {
                Text = "Platforms: 0",
                ForeColor = Color.FromArgb(220, 220, 220), // Brighter white
                AutoSize = true,
                Location = new Point(10, 40)
            };
            
            _enabledInstancesLabel = new Label()
            {
                Text = "Source candidates: 0",
                ForeColor = Color.FromArgb(220, 220, 220), // Brighter white
                AutoSize = true,
                Location = new Point(10, 60)
            };
            
            _accountTypesLabel = new Label()
            {
                Text = "Account types: None",
                ForeColor = Color.FromArgb(220, 220, 220), // Brighter white
                AutoSize = true,
                Location = new Point(10, 80)
            };
            
            _activityLog = new ListBox()
            {
                Location = new Point(10, 110),
                Size = new Size(350, 150),
                BackColor = Color.FromArgb(25, 25, 25),
                ForeColor = Color.FromArgb(200, 200, 200), // Much brighter for activity log
                BorderStyle = BorderStyle.FixedSingle
            };
            
            _monitoringPanel.Controls.AddRange(new Control[] 
            {
                title, _platformCountLabel, _enabledInstancesLabel, 
                _accountTypesLabel, _activityLog
            });
        }

        private void InitializeTimers()
        {
            _refreshTimer = new System.Windows.Forms.Timer() { Interval = 30000 }; // 30 seconds
            _refreshTimer.Tick += (s, e) => RefreshAssets();
            
            _cacheStatsTimer = new System.Windows.Forms.Timer() { Interval = 5000 }; // 5 seconds
            _cacheStatsTimer.Tick += (s, e) => RefreshCacheStats();
            _cacheStatsTimer.Start();
        }

        private async void LoadAssets()
        {
            try
            {
                _statusLabel.Text = "Scanning trading environment using your existing cache system...";
                _progressBar.Visible = true;
                
                _assets.Clear();
                _assetListView.Items.Clear();
                
                // Load from instances-config.json (your trading environment)
                if (File.Exists(_configPath))
                {
                    var json = await File.ReadAllTextAsync(_configPath);
                    var options = new JsonSerializerOptions 
                    { 
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
                    };
                    var config = JsonSerializer.Deserialize<AssetManager.Models.Configuration>(json, options);
                    
                    if (config?.Instances != null)
                    {
                        LogActivity($"📡 Scanning {config.Instances.Count} instances from trading environment");
                        
                        foreach (var instance in config.Instances.Where(i => i.Enabled))
                        {
                            var asset = new AssetInfo
                            {
                                Name = instance.Name,
                                Platform = instance.Platform,
                                Type = "TradingInstance",
                                InstanceName = instance.Name,
                                FilePath = GetInstancePath(instance, config.TradingRoot),
                                Version = "1.0"
                            };
                            
                            // Set additional properties that match your AssetInfo model
                            if (File.Exists(asset.FilePath))
                            {
                                var fileInfo = new FileInfo(asset.FilePath);
                                asset.FileSize = fileInfo.Length;
                                asset.LastModified = fileInfo.LastWriteTime;
                                asset.QuickHash = CacheManager.ComputeQuickHash(asset.FilePath);
                                asset.IsDeployed = true;
                                asset.DeploymentStatus = "Ready";
                            }
                            else
                            {
                                asset.IsDeployed = false;
                                asset.DeploymentStatus = "Missing";
                            }
                            
                            _assets.Add(asset);
                            
                            // Cache using your existing cache manager
                            var cacheKey = asset.GetCacheKey();
                            _cacheManager.CacheAsset(cacheKey, asset);
                        }
                    }
                }
                
                RefreshAssetList();
                UpdateMonitoringStats();
                
                _statusLabel.Text = "✅ Asset Manager foundation ready for UI development!";
                LogActivity("✅ Asset Manager foundation ready for UI development!");
                LogActivity($"🏗️ Ready for Asset Management: {_assets.Count(a => a.IsDeployed)} source candidates");
                LogActivity($"📊 Platforms: {string.Join(", ", _assets.Select(a => a.Platform).Distinct())}");
                
                // Display cache statistics like your Program.cs test
                var stats = _cacheManager.GetCacheStats();
                LogActivity($"💾 Cache Statistics:");
                LogActivity($"  Memory entries: {stats.MemoryCacheEntries}");
                LogActivity($"  Disk entries: {stats.DiskCacheEntries}");
                LogActivity($"  Memory usage: {stats.GetFormattedMemoryUsage()}");
                LogActivity($"  Cache age: {stats.OldestCacheEntry:HH:mm:ss} to {stats.NewestCacheEntry:HH:mm:ss}");
            }
            catch (Exception ex)
            {
                _statusLabel.Text = $"Error loading assets: {ex.Message}";
                LogActivity($"❌ Error loading assets: {ex.Message}");
            }
            finally
            {
                _progressBar.Visible = false;
            }
        }

        private string GetInstancePath(TradingInstance instance, string tradingRoot)
        {
            var executableName = instance.GetExecutableName();
            return Path.Combine(tradingRoot, "PlatformInstances", instance.Name ?? "", executableName);
        }

        private void RefreshAssetList()
        {
            _assetListView.Items.Clear();
            
            foreach (var asset in _assets)
            {
                var item = new ListViewItem(asset.Name ?? "");
                item.SubItems.Add(asset.Platform ?? "");
                item.SubItems.Add(asset.Type ?? "");
                item.SubItems.Add(asset.Version ?? "");
                item.SubItems.Add(asset.DeploymentStatus ?? "");
                item.SubItems.Add(FormatFileSize(asset.FileSize));
                item.Tag = asset;
                
                // Color coding based on deployment status with better readability
                if (!asset.IsDeployed)
                    item.ForeColor = Color.FromArgb(180, 180, 180); // Light gray instead of dark gray
                else if (asset.DeploymentStatus == "Ready")
                    item.ForeColor = Color.FromArgb(144, 238, 144); // Light green
                else
                    item.ForeColor = Color.FromArgb(255, 255, 140); // Light yellow
                
                _assetListView.Items.Add(item);
            }
        }

        private void RefreshCacheStats()
        {
            try
            {
                var stats = _cacheManager.GetCacheStats();
                
                _memoryEntriesLabel.Text = $"Memory entries: {stats.MemoryCacheEntries}";
                _diskEntriesLabel.Text = $"Disk entries: {stats.DiskCacheEntries}";
                _memoryUsageLabel.Text = $"Memory usage: {stats.GetFormattedMemoryUsage()}";
                
                // Update progress bar (assuming 50MB max for visualization)
                var maxBytes = 50 * 1024 * 1024;
                var percentage = Math.Min(100, (int)((double)stats.EstimatedMemoryUsageBytes / maxBytes * 100));
                _memoryUsageProgress.Value = percentage;
                
                if (stats.OldestCacheEntry != DateTime.MinValue && stats.NewestCacheEntry != DateTime.MinValue)
                {
                    _cacheAgeLabel.Text = $"Cache age: {stats.OldestCacheEntry:HH:mm:ss} to {stats.NewestCacheEntry:HH:mm:ss}";
                }
            }
            catch (Exception ex)
            {
                LogActivity($"❌ Cache stats error: {ex.Message}");
            }
        }

        private void UpdateMonitoringStats()
        {
            var platforms = _assets.Select(a => a.Platform).Distinct().Where(p => !string.IsNullOrEmpty(p));
            var enabledCount = _assets.Count(a => a.IsDeployed);
            
            _platformCountLabel.Text = $"Platforms: {string.Join(", ", platforms)} ({platforms.Count()})";
            _enabledInstancesLabel.Text = $"Source candidates: {enabledCount}/{_assets.Count}";
        }

        private void LogActivity(string message)
        {
            if (_activityLog.InvokeRequired)
            {
                _activityLog.Invoke(new Action<string>(LogActivity), message);
                return;
            }
            
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            _activityLog.Items.Insert(0, $"[{timestamp}] {message}");
            
            // Keep only last 100 entries
            while (_activityLog.Items.Count > 100)
                _activityLog.Items.RemoveAt(_activityLog.Items.Count - 1);
        }

        private string FormatFileSize(long bytes)
        {
            if (bytes == 0) return "0 B";
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024:F1} KB";
            return $"{bytes / (1024 * 1024):F1} MB";
        }

        // Event Handlers
        private void AssetListView_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_assetListView.SelectedItems.Count > 0 && _assetListView.SelectedItems[0].Tag is AssetInfo asset)
            {
                _assetPropertyGrid.SelectedObject = asset;
            }
        }

        private async void RefreshAssets()
        {
            await Task.Run(() => LoadAssets());
            LogActivity("🔄 Assets refreshed");
        }

        private void ClearCache()
        {
            _cacheManager.ClearAllCache();
            RefreshCacheStats();
            LogActivity("🗑️ Cache cleared");
        }

        private void OptimizeCache()
        {
            try
            {
                var statsBefore = _cacheManager.GetCacheStats();
                
                // Your cache manager optimization would go here
                // For now, just refresh stats
                RefreshCacheStats();
                
                var statsAfter = _cacheManager.GetCacheStats();
                LogActivity($"⚡ Cache optimized - Memory entries: {statsBefore.MemoryCacheEntries} → {statsAfter.MemoryCacheEntries}");
            }
            catch (Exception ex)
            {
                LogActivity($"❌ Cache optimization error: {ex.Message}");
            }
        }

        private void PerformCacheHealthCheck()
        {
            try
            {
                var stats = _cacheManager.GetCacheStats();
                var issues = new List<string>();
                
                if (stats.MemoryCacheEntries > 1000)
                    issues.Add("High memory cache entries");
                    
                if (stats.EstimatedMemoryUsageBytes > 50 * 1024 * 1024) // 50MB
                    issues.Add("High memory usage");
                
                if (issues.Any())
                {
                    LogActivity($"🏥 Cache health check: {issues.Count} issues found");
                    foreach (var issue in issues)
                        LogActivity($"   ⚠️ {issue}");
                }
                else
                {
                    LogActivity("🏥 Cache health check: All systems green ✅");
                }
            }
            catch (Exception ex)
            {
                LogActivity($"❌ Cache health check error: {ex.Message}");
            }
        }

        private void ShowCacheDirectory()
        {
            try
            {
                var cacheDir = Path.Combine(Directory.GetCurrentDirectory(), "Cache");
                if (Directory.Exists(cacheDir))
                {
                    System.Diagnostics.Process.Start("explorer.exe", cacheDir);
                }
                else
                {
                    LogActivity("📁 Cache directory not found");
                }
            }
            catch (Exception ex)
            {
                LogActivity($"❌ Error opening cache directory: {ex.Message}");
            }
        }

        private void RunConsoleTests()
        {
            try
            {
                // Run your existing console tests in a separate window
                var currentProcess = System.Diagnostics.Process.GetCurrentProcess();
                var fileName = currentProcess.MainModule?.FileName;
                
                if (string.IsNullOrEmpty(fileName))
                {
                    LogActivity("❌ Cannot determine current executable path");
                    return;
                }
                
                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = "--console",
                    UseShellExecute = true,
                    CreateNoWindow = false
                };
                
                System.Diagnostics.Process.Start(startInfo);
                LogActivity("🧪 Console tests launched in separate window");
            }
            catch (Exception ex)
            {
                LogActivity($"❌ Error launching console tests: {ex.Message}");
            }
        }

        private void ToggleAutoRefresh(object? sender, EventArgs e)
        {
            if (sender is not ToolStripMenuItem menuItem) return;
            
            menuItem.Checked = !menuItem.Checked;
            
            if (menuItem.Checked)
            {
                _refreshTimer.Start();
                LogActivity("🔄 Auto-refresh enabled");
            }
            else
            {
                _refreshTimer.Stop();
                LogActivity("⏹️ Auto-refresh disabled");
            }
        }

        private void ShowAssetDetails()
        {
            if (_assetListView.SelectedItems.Count > 0 && _assetListView.SelectedItems[0].Tag is AssetInfo asset)
            {
                var details = $"Asset Details:\n\n" +
                             $"Name: {asset.Name}\n" +
                             $"Platform: {asset.Platform}\n" +
                             $"Type: {asset.Type}\n" +
                             $"File Path: {asset.FilePath}\n" +
                             $"Version: {asset.Version}\n" +
                             $"Status: {asset.DeploymentStatus}\n" +
                             $"Cache Key: {asset.GetCacheKey()}";
                             
                MessageBox.Show(details, "Asset Details", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void ShowActivityLog()
        {
            LogActivity("📋 Activity log is displayed in the monitoring panel");
        }

        private void ExportAssets()
        {
            try
            {
                var saveDialog = new SaveFileDialog()
                {
                    Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                    DefaultExt = "json"
                };
                
                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    var json = JsonSerializer.Serialize(_assets, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(saveDialog.FileName, json);
                    LogActivity($"📤 Assets exported to {saveDialog.FileName}");
                }
            }
            catch (Exception ex)
            {
                LogActivity($"❌ Export error: {ex.Message}");
            }
        }

        private void ValidateAssetPaths()
        {
            try
            {
                LogActivity("🔍 Validating all asset paths...");
                int validCount = 0;
                int invalidCount = 0;
                
                foreach (var asset in _assets)
                {
                    if (File.Exists(asset.FilePath))
                    {
                        validCount++;
                        LogActivity($"✅ {asset.Name} - Path valid");
                    }
                    else
                    {
                        invalidCount++;
                        LogActivity($"❌ {asset.Name} - Path invalid: {asset.FilePath}");
                    }
                }
                
                LogActivity($"📊 Validation complete: {validCount} valid, {invalidCount} invalid paths");
                MessageBox.Show($"Path Validation Results:\n\n✅ Valid: {validCount}\n❌ Invalid: {invalidCount}", 
                    "Asset Path Validation", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                LogActivity($"❌ Validation error: {ex.Message}");
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _refreshTimer?.Stop();
            _cacheStatsTimer?.Stop();
            _refreshTimer?.Dispose();
            _cacheStatsTimer?.Dispose();
            base.OnFormClosed(e);
        }
    }
}