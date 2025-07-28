using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Forms;
using AssetManager.Models;
using AssetManager.Services;

namespace AssetManager
{
    /// <summary>
    /// Level 8: Unified Trading Platform Manager
    /// Combines Level 6 (Instance Management) + Level 7 (Asset Management) + Asset Matrix Manager
    /// </summary>
    public partial class Level8UnifiedManager : Form
    {
        private readonly CacheManager _cacheManager;
        private Models.Configuration _config = new Models.Configuration();
        private readonly string _configPath;
        private List<AssetInfo> _assets = new List<AssetInfo>();
        
        // Timers
        private System.Windows.Forms.Timer _refreshTimer = null!;
        private System.Windows.Forms.Timer _cacheStatsTimer = null!;
        
        // Main UI Components
        private MenuStrip _menuStrip = null!;
        private ToolStrip _toolStrip = null!;
        private StatusStrip _statusStrip = null!;
        private ToolStripStatusLabel _statusLabel = null!;
        private ToolStripProgressBar _progressBar = null!;
        private TabControl _mainTabControl = null!;
        
        // Instance Management Tab (Level 6)
        private TabPage _instanceTab = null!;
        private ListView _instanceListView = null!;
        private PropertyGrid _instancePropertyGrid = null!;
        private Panel _instanceControlPanel = null!;
        private Button _addInstanceButton = null!;
        private Button _editInstanceButton = null!;
        private Button _deleteInstanceButton = null!;
        private Button _testInstanceButton = null!;
        
        // Asset Management Tab (Level 7)
        private TabPage _assetTab = null!;
        private ListView _assetListView = null!;
        private PropertyGrid _assetPropertyGrid = null!;
        private Panel _assetControlPanel = null!;
        private Button _scanAssetsButton = null!;
        private Button _refreshAssetsButton = null!;
        private Button _validatePathsButton = null!;
        
        // Asset Matrix Tab (Asset Matrix Manager)
        private TabPage _matrixTab = null!;
        private Button _openMatrixManagerButton = null!;
        private Panel _matrixPreviewPanel = null!;
        private Label _matrixSummaryLabel = null!;
        
        // Cache & Monitoring Tab
        private TabPage _monitoringTab = null!;
        private Panel _cacheStatsPanel = null!;
        private ListBox _activityLog = null!;
        private Label _memoryEntriesLabel = null!;
        private Label _diskEntriesLabel = null!;
        private Label _memoryUsageLabel = null!;
        private ProgressBar _memoryUsageProgress = null!;

        public Level8UnifiedManager()
        {
            _configPath = FindConfigPath();
            _cacheManager = new CacheManager();
            
            InitializeComponent();
            InitializeLayout();
            InitializeTimers();
            
            LoadConfiguration();
            LoadAssets();
            RefreshInstanceList();
            RefreshAssetList();
            RefreshCacheStats();
            
            LogActivity("🚀 Level 8: Unified Trading Platform Manager Initialized");
            LogActivity("📊 Combined Instance Management + Asset Management + Matrix Manager");
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
            this.Text = "Level 8: Unified Trading Platform Manager";
            this.Size = new Size(1600, 1000);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimumSize = new Size(1200, 800);
            this.Icon = SystemIcons.Application;
            
            // Dark enterprise theme
            this.BackColor = Color.FromArgb(30, 30, 30);
            this.ForeColor = Color.FromArgb(240, 240, 240);
        }

        private void InitializeLayout()
        {
            this.SuspendLayout();
            
            // Menu Strip
            _menuStrip = new MenuStrip()
            {
                BackColor = Color.FromArgb(45, 45, 48),
                ForeColor = Color.FromArgb(240, 240, 240)
            };
            
            var fileMenu = new ToolStripMenuItem("File") { ForeColor = Color.FromArgb(240, 240, 240) };
            fileMenu.DropDownItems.Add("New Instance", null, (s, e) => AddNewInstance());
            fileMenu.DropDownItems.Add("Refresh All", null, (s, e) => RefreshAll());
            fileMenu.DropDownItems.Add(new ToolStripSeparator());
            fileMenu.DropDownItems.Add("Export Configuration", null, (s, e) => ExportConfiguration());
            fileMenu.DropDownItems.Add("Import Configuration", null, (s, e) => ImportConfiguration());
            fileMenu.DropDownItems.Add(new ToolStripSeparator());
            fileMenu.DropDownItems.Add("Exit", null, (s, e) => this.Close());
            
            var instanceMenu = new ToolStripMenuItem("Instances") { ForeColor = Color.FromArgb(240, 240, 240) };
            instanceMenu.DropDownItems.Add("Add Instance", null, (s, e) => AddNewInstance());
            instanceMenu.DropDownItems.Add("Test All Connections", null, (s, e) => TestAllInstances());
            instanceMenu.DropDownItems.Add("Validate Paths", null, (s, e) => ValidateAllPaths());
            
            var assetMenu = new ToolStripMenuItem("Assets") { ForeColor = Color.FromArgb(240, 240, 240) };
            assetMenu.DropDownItems.Add("Scan All Assets", null, (s, e) => ScanAllAssets());
            assetMenu.DropDownItems.Add("Open Asset Matrix", null, (s, e) => OpenAssetMatrixManager());
            assetMenu.DropDownItems.Add("Validate Asset Paths", null, (s, e) => ValidateAssetPaths());
            assetMenu.DropDownItems.Add("Clear Asset Cache", null, (s, e) => ClearAssetCache());
            
            var toolsMenu = new ToolStripMenuItem("Tools") { ForeColor = Color.FromArgb(240, 240, 240) };
            toolsMenu.DropDownItems.Add("Open Asset Matrix Manager", null, (s, e) => OpenAssetMatrixManager());
            toolsMenu.DropDownItems.Add("Cache Statistics", null, (s, e) => ShowCacheStats());
            toolsMenu.DropDownItems.Add("System Information", null, (s, e) => ShowSystemInfo());
            
            _menuStrip.Items.AddRange(new ToolStripItem[] { fileMenu, instanceMenu, assetMenu, toolsMenu });
            
            // Tool Strip
            _toolStrip = new ToolStrip()
            {
                BackColor = Color.FromArgb(45, 45, 48),
                ForeColor = Color.FromArgb(240, 240, 240)
            };
            
            _toolStrip.Items.Add(new ToolStripButton("New Instance", null, (s, e) => AddNewInstance()) { DisplayStyle = ToolStripItemDisplayStyle.ImageAndText });
            _toolStrip.Items.Add(new ToolStripSeparator());
            _toolStrip.Items.Add(new ToolStripButton("Scan Assets", null, (s, e) => ScanAllAssets()) { DisplayStyle = ToolStripItemDisplayStyle.ImageAndText });
            _toolStrip.Items.Add(new ToolStripButton("Asset Matrix", null, (s, e) => OpenAssetMatrixManager()) { DisplayStyle = ToolStripItemDisplayStyle.ImageAndText });
            _toolStrip.Items.Add(new ToolStripSeparator());
            _toolStrip.Items.Add(new ToolStripButton("Refresh All", null, (s, e) => RefreshAll()) { DisplayStyle = ToolStripItemDisplayStyle.ImageAndText });
            
            // Main Tab Control
            _mainTabControl = new TabControl()
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(45, 45, 48),
                ForeColor = Color.FromArgb(240, 240, 240)
            };
            
            // Create all tabs
            CreateInstanceManagementTab();
            CreateAssetManagementTab();
            CreateAssetMatrixTab();
            CreateMonitoringTab();
            
            // Status Strip
            _statusStrip = new StatusStrip()
            {
                BackColor = Color.FromArgb(45, 45, 48),
                ForeColor = Color.FromArgb(240, 240, 240)
            };
            
            _statusLabel = new ToolStripStatusLabel("Ready - Level 8 Unified Manager");
            _progressBar = new ToolStripProgressBar() { Visible = false };
            _statusStrip.Items.AddRange(new ToolStripItem[] { _statusLabel, _progressBar });
            
            // Layout assembly
            this.Controls.Add(_mainTabControl);
            this.Controls.Add(_toolStrip);
            this.Controls.Add(_statusStrip);
            this.Controls.Add(_menuStrip);
            this.MainMenuStrip = _menuStrip;
            
            this.ResumeLayout();
        }

        #region Tab Creation Methods

        private void CreateInstanceManagementTab()
        {
            _instanceTab = new TabPage("Instance Management")
            {
                BackColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.FromArgb(240, 240, 240)
            };
            
            var splitter = new SplitContainer()
            {
                Dock = DockStyle.Fill,
                SplitterDistance = 400,
                BackColor = Color.FromArgb(45, 45, 48)
            };
            
            // Left panel - Instance list
            _instanceListView = new ListView()
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                BackColor = Color.FromArgb(40, 40, 40),
                ForeColor = Color.FromArgb(240, 240, 240)
            };
            
            _instanceListView.Columns.Add("Name", 200);
            _instanceListView.Columns.Add("Platform", 80);
            _instanceListView.Columns.Add("Broker", 120);
            _instanceListView.Columns.Add("Account Type", 100);
            _instanceListView.Columns.Add("Status", 80);
            _instanceListView.Columns.Add("Enabled", 60);
            
            _instanceListView.SelectedIndexChanged += (s, e) => UpdateInstanceProperties();
            
            // Control panel for instances
            _instanceControlPanel = new Panel()
            {
                Dock = DockStyle.Bottom,
                Height = 50,
                BackColor = Color.FromArgb(50, 50, 50)
            };
            
            _addInstanceButton = new Button()
            {
                Text = "Add Instance",
                Location = new Point(10, 10),
                Size = new Size(100, 30),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _addInstanceButton.Click += (s, e) => AddNewInstance();
            
            _editInstanceButton = new Button()
            {
                Text = "Edit",
                Location = new Point(120, 10),
                Size = new Size(80, 30),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _editInstanceButton.Click += (s, e) => EditSelectedInstance();
            
            _deleteInstanceButton = new Button()
            {
                Text = "Delete",
                Location = new Point(210, 10),
                Size = new Size(80, 30),
                BackColor = Color.FromArgb(196, 43, 28),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _deleteInstanceButton.Click += (s, e) => DeleteSelectedInstance();
            
            _testInstanceButton = new Button()
            {
                Text = "Test",
                Location = new Point(300, 10),
                Size = new Size(80, 30),
                BackColor = Color.FromArgb(16, 124, 16),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _testInstanceButton.Click += (s, e) => TestSelectedInstance();
            
            _instanceControlPanel.Controls.AddRange(new Control[] { _addInstanceButton, _editInstanceButton, _deleteInstanceButton, _testInstanceButton });
            
            var leftPanel = new Panel() { Dock = DockStyle.Fill };
            leftPanel.Controls.Add(_instanceListView);
            leftPanel.Controls.Add(_instanceControlPanel);
            
            // Right panel - Properties
            _instancePropertyGrid = new PropertyGrid()
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(40, 40, 40),
                CommandsBackColor = Color.FromArgb(50, 50, 50),
                HelpBackColor = Color.FromArgb(50, 50, 50),
                ViewBackColor = Color.FromArgb(40, 40, 40),
                ViewForeColor = Color.FromArgb(240, 240, 240)
            };
            
            splitter.Panel1.Controls.Add(leftPanel);
            splitter.Panel2.Controls.Add(_instancePropertyGrid);
            _instanceTab.Controls.Add(splitter);
            _mainTabControl.TabPages.Add(_instanceTab);
        }

        private void CreateAssetManagementTab()
        {
            _assetTab = new TabPage("Asset Management")
            {
                BackColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.FromArgb(240, 240, 240)
            };
            
            var splitter = new SplitContainer()
            {
                Dock = DockStyle.Fill,
                SplitterDistance = 400,
                BackColor = Color.FromArgb(45, 45, 48)
            };
            
            // Left panel - Asset list
            _assetListView = new ListView()
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                BackColor = Color.FromArgb(40, 40, 40),
                ForeColor = Color.FromArgb(240, 240, 240)
            };
            
            _assetListView.Columns.Add("Name", 200);
            _assetListView.Columns.Add("Platform", 80);
            _assetListView.Columns.Add("Type", 100);
            _assetListView.Columns.Add("Instance", 150);
            _assetListView.Columns.Add("Status", 80);
            _assetListView.Columns.Add("Size", 80);
            
            _assetListView.SelectedIndexChanged += (s, e) => UpdateAssetProperties();
            
            // Control panel for assets
            _assetControlPanel = new Panel()
            {
                Dock = DockStyle.Bottom,
                Height = 50,
                BackColor = Color.FromArgb(50, 50, 50)
            };
            
            _scanAssetsButton = new Button()
            {
                Text = "Scan Assets",
                Location = new Point(10, 10),
                Size = new Size(100, 30),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _scanAssetsButton.Click += (s, e) => ScanAllAssets();
            
            _refreshAssetsButton = new Button()
            {
                Text = "Refresh",
                Location = new Point(120, 10),
                Size = new Size(80, 30),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _refreshAssetsButton.Click += (s, e) => RefreshAssetList();
            
            _validatePathsButton = new Button()
            {
                Text = "Validate Paths",
                Location = new Point(210, 10),
                Size = new Size(100, 30),
                BackColor = Color.FromArgb(16, 124, 16),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _validatePathsButton.Click += (s, e) => ValidateAssetPaths();
            
            _assetControlPanel.Controls.AddRange(new Control[] { _scanAssetsButton, _refreshAssetsButton, _validatePathsButton });
            
            var leftPanel = new Panel() { Dock = DockStyle.Fill };
            leftPanel.Controls.Add(_assetListView);
            leftPanel.Controls.Add(_assetControlPanel);
            
            // Right panel - Properties
            _assetPropertyGrid = new PropertyGrid()
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(40, 40, 40),
                CommandsBackColor = Color.FromArgb(50, 50, 50),
                HelpBackColor = Color.FromArgb(50, 50, 50),
                ViewBackColor = Color.FromArgb(40, 40, 40),
                ViewForeColor = Color.FromArgb(240, 240, 240)
            };
            
            splitter.Panel1.Controls.Add(leftPanel);
            splitter.Panel2.Controls.Add(_assetPropertyGrid);
            _assetTab.Controls.Add(splitter);
            _mainTabControl.TabPages.Add(_assetTab);
        }

        private void CreateAssetMatrixTab()
        {
            _matrixTab = new TabPage("Asset Matrix")
            {
                BackColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.FromArgb(240, 240, 240)
            };
            
            // Main panel
            var mainPanel = new Panel()
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20)
            };
            
            // Title and description
            var titleLabel = new Label()
            {
                Text = "Asset Matrix Manager",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.FromArgb(240, 240, 240),
                AutoSize = true,
                Location = new Point(20, 20)
            };
            
            var descLabel = new Label()
            {
                Text = "Manage asset distribution across trading instances with platform compatibility enforcement.",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(200, 200, 200),
                AutoSize = true,
                Location = new Point(20, 55)
            };
            
            // Summary panel
            _matrixSummaryLabel = new Label()
            {
                Text = "Matrix summary will appear here...",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(240, 240, 240),
                AutoSize = true,
                Location = new Point(20, 90)
            };
            
            // Open Matrix Manager button
            _openMatrixManagerButton = new Button()
            {
                Text = "🚀 Open Asset Matrix Manager",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Size = new Size(300, 50),
                Location = new Point(20, 130),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _openMatrixManagerButton.Click += (s, e) => OpenAssetMatrixManager();
            
            // Preview panel
            _matrixPreviewPanel = new Panel()
            {
                Location = new Point(20, 200),
                Size = new Size(600, 300),
                BackColor = Color.FromArgb(40, 40, 40),
                BorderStyle = BorderStyle.FixedSingle
            };
            
            var previewLabel = new Label()
            {
                Text = "Matrix Preview (Last scan results will appear here)",
                Dock = DockStyle.Top,
                Height = 30,
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.FromArgb(240, 240, 240),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            _matrixPreviewPanel.Controls.Add(previewLabel);
            
            mainPanel.Controls.AddRange(new Control[] { titleLabel, descLabel, _matrixSummaryLabel, _openMatrixManagerButton, _matrixPreviewPanel });
            _matrixTab.Controls.Add(mainPanel);
            _mainTabControl.TabPages.Add(_matrixTab);
        }

        private void CreateMonitoringTab()
        {
            _monitoringTab = new TabPage("Cache & Monitoring")
            {
                BackColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.FromArgb(240, 240, 240)
            };
            
            var mainSplitter = new SplitContainer()
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 200,
                BackColor = Color.FromArgb(45, 45, 48)
            };
            
            // Top panel - Cache stats
            _cacheStatsPanel = new Panel()
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(40, 40, 40),
                Padding = new Padding(10)
            };
            
            var cacheTitle = new Label()
            {
                Text = "Cache Statistics",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.FromArgb(240, 240, 240),
                AutoSize = true,
                Location = new Point(10, 10)
            };
            
            _memoryEntriesLabel = new Label()
            {
                Text = "Memory Entries: 0",
                ForeColor = Color.FromArgb(240, 240, 240),
                AutoSize = true,
                Location = new Point(10, 40)
            };
            
            _diskEntriesLabel = new Label()
            {
                Text = "Disk Entries: 0",
                ForeColor = Color.FromArgb(240, 240, 240),
                AutoSize = true,
                Location = new Point(10, 60)
            };
            
            _memoryUsageLabel = new Label()
            {
                Text = "Memory Usage: 0 MB",
                ForeColor = Color.FromArgb(240, 240, 240),
                AutoSize = true,
                Location = new Point(10, 80)
            };
            
            _memoryUsageProgress = new ProgressBar()
            {
                Location = new Point(10, 100),
                Size = new Size(300, 20),
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.FromArgb(0, 120, 215)
            };
            
            _cacheStatsPanel.Controls.AddRange(new Control[] { cacheTitle, _memoryEntriesLabel, _diskEntriesLabel, _memoryUsageLabel, _memoryUsageProgress });
            
            // Bottom panel - Activity log
            _activityLog = new ListBox()
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(20, 20, 20),
                ForeColor = Color.FromArgb(240, 240, 240),
                Font = new Font("Consolas", 9)
            };
            
            mainSplitter.Panel1.Controls.Add(_cacheStatsPanel);
            mainSplitter.Panel2.Controls.Add(_activityLog);
            _monitoringTab.Controls.Add(mainSplitter);
            _mainTabControl.TabPages.Add(_monitoringTab);
        }

        #endregion

        #region Timer and Background Operations

        private void InitializeTimers()
        {
            _refreshTimer = new System.Windows.Forms.Timer()
            {
                Interval = 30000 // 30 seconds
            };
            _refreshTimer.Tick += (s, e) => RefreshCacheStats();
            _refreshTimer.Start();
            
            _cacheStatsTimer = new System.Windows.Forms.Timer()
            {
                Interval = 5000 // 5 seconds
            };
            _cacheStatsTimer.Tick += (s, e) => UpdateCacheDisplay();
            _cacheStatsTimer.Start();
        }

        #endregion

        #region Configuration Management

  private void LoadConfiguration()
{
    try
    {
        if (File.Exists(_configPath))
        {
            var json = File.ReadAllText(_configPath);
            
            var options = new JsonSerializerOptions 
            { 
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            };
            var config = JsonSerializer.Deserialize<Models.Configuration>(json, options) ?? new Models.Configuration();

            // CRITICAL: Set TradingRoot for all instances
            foreach (var instance in config.Instances)
            {
                instance.TradingRoot = config.TradingRoot;
            }
            
            // MISSING LINE: Assign the loaded config to _config
            _config = config;
            
            LogActivity($"✅ Configuration loaded: {_config.Instances.Count} instances from {_configPath}");
        }
        else
        {
            LogActivity($"⚠️ Configuration file not found: {_configPath}");
            _config = new Models.Configuration();
        }
    }
    catch (Exception ex)
    {
        LogActivity($"❌ Error loading configuration: {ex.Message}");
        _config = new Models.Configuration();
    }
}

        private void SaveConfiguration()
        {
            try
            {
                var options = new JsonSerializerOptions 
                { 
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                };
                var json = JsonSerializer.Serialize(_config, options);
                File.WriteAllText(_configPath, json);
                
                LogActivity($"✅ Configuration saved to {_configPath}");
            }
            catch (Exception ex)
            {
                LogActivity($"❌ Error saving configuration: {ex.Message}");
                MessageBox.Show($"Error saving configuration: {ex.Message}", "Save Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        #region Asset Management

        private void LoadAssets()
        {
            try
            {
                _assets.Clear();
                
                foreach (var instance in _config.Instances.Where(i => i.Enabled))
                {
                    LogActivity($"📂 Loading assets from {instance.Name}...");
                    
                    var asset = new AssetInfo
                    {
                        Name = instance.Name,
                        Platform = instance.Platform,
                        Type = "TradingInstance",
                        InstanceName = instance.Name,
                        FilePath = instance.InstancePath,
                        Version = "1.0",
                        IsDeployed = Directory.Exists(instance.InstancePath),
                        DeploymentStatus = Directory.Exists(instance.InstancePath) ? "Ready" : "Missing"
                    };
                    
                    if (File.Exists(asset.FilePath))
                    {
                        var fileInfo = new FileInfo(asset.FilePath);
                        asset.FileSize = fileInfo.Length;
                        asset.LastModified = fileInfo.LastWriteTime;
                    }
                    
                    _assets.Add(asset);
                    _cacheManager.CacheAsset(asset.GetCacheKey(), asset);
                }
                
                LogActivity($"✅ Loaded {_assets.Count} assets");
            }
            catch (Exception ex)
            {
                LogActivity($"❌ Error loading assets: {ex.Message}");
            }
        }

        private void ScanAllAssets()
        {
            LogActivity("🔍 Starting comprehensive asset scan...");
            _progressBar.Visible = true;
            _statusLabel.Text = "Scanning assets...";
            
            try
            {
                // Implement asset scanning logic here
                // This would scan all instance directories for actual trading assets
                
                LoadAssets();
                RefreshAssetList();
                UpdateMatrixSummary();
                
                LogActivity("✅ Asset scan completed");
                _statusLabel.Text = $"Ready - {_assets.Count} assets loaded";
            }
            catch (Exception ex)
            {
                LogActivity($"❌ Asset scan error: {ex.Message}");
                _statusLabel.Text = "Asset scan failed";
            }
            finally
            {
                _progressBar.Visible = false;
            }
        }

        #endregion

        #region UI Update Methods

        private void RefreshInstanceList()
        {
            _instanceListView.Items.Clear();
            
            foreach (var instance in _config.Instances)
            {
                var item = new ListViewItem(instance.Name)
                {
                    Tag = instance
                };
                
                item.SubItems.Add(instance.Platform);
                item.SubItems.Add(instance.Broker);
                item.SubItems.Add(instance.AccountType);
                item.SubItems.Add(instance.Enabled ? "Ready" : "Disabled");
                item.SubItems.Add(instance.Enabled ? "✓" : "✗");
                
                // Color coding
                if (!instance.Enabled)
                    item.ForeColor = Color.Gray;
                else if (!Directory.Exists(instance.InstancePath))
                    item.ForeColor = Color.Orange;
                else
                    item.ForeColor = Color.LightGreen;
                
                _instanceListView.Items.Add(item);
            }
            
            LogActivity($"📊 Instance list refreshed: {_config.Instances.Count} instances");
        }

        private void RefreshAssetList()
        {
            _assetListView.Items.Clear();
            
            foreach (var asset in _assets)
            {
                var item = new ListViewItem(asset.Name)
                {
                    Tag = asset
                };
                
                item.SubItems.Add(asset.Platform);
                item.SubItems.Add(asset.Type);
                item.SubItems.Add(asset.InstanceName);
                item.SubItems.Add(asset.DeploymentStatus);
                item.SubItems.Add(FormatFileSize(asset.FileSize));
                
                // Color coding based on status
                item.ForeColor = asset.DeploymentStatus switch
                {
                    "Ready" => Color.LightGreen,
                    "Missing" => Color.Orange,
                    "Error" => Color.Red,
                    _ => Color.FromArgb(240, 240, 240)
                };
                
                _assetListView.Items.Add(item);
            }
            
            LogActivity($"📊 Asset list refreshed: {_assets.Count} assets");
        }

        private void RefreshCacheStats()
        {
            var stats = _cacheManager.GetCacheStats();
            
            _memoryEntriesLabel.Text = $"Memory Entries: {stats.MemoryCacheEntries}";
            _diskEntriesLabel.Text = $"Disk Entries: {stats.DiskCacheEntries}";
            _memoryUsageLabel.Text = $"Memory Usage: {stats.GetFormattedMemoryUsage()}";
            
            // Update progress bar (assuming max 100MB)
            var maxMemoryBytes = 100 * 1024 * 1024; // 100MB in bytes
            var usagePercent = Math.Min((stats.EstimatedMemoryUsageBytes / (double)maxMemoryBytes) * 100, 100);
            _memoryUsageProgress.Value = (int)usagePercent;
            
            LogActivity($"📊 Cache: {stats.MemoryCacheEntries} memory, {stats.DiskCacheEntries} disk, {stats.GetFormattedMemoryUsage()}");
        }

        private void UpdateCacheDisplay()
        {
            // Real-time cache monitoring updates
            RefreshCacheStats();
        }

        private void UpdateInstanceProperties()
        {
            if (_instanceListView.SelectedItems.Count > 0)
            {
                var selectedInstance = _instanceListView.SelectedItems[0].Tag as TradingInstance;
                _instancePropertyGrid.SelectedObject = selectedInstance;
            }
            else
            {
                _instancePropertyGrid.SelectedObject = null;
            }
        }

        private void UpdateAssetProperties()
        {
            if (_assetListView.SelectedItems.Count > 0)
            {
                var selectedAsset = _assetListView.SelectedItems[0].Tag as AssetInfo;
                _assetPropertyGrid.SelectedObject = selectedAsset;
            }
            else
            {
                _assetPropertyGrid.SelectedObject = null;
            }
        }

        private void UpdateMatrixSummary()
        {
            var totalAssets = _assets.Count;
            var totalInstances = _config.Instances.Count(i => i.Enabled);
            var platforms = _config.Instances.Where(i => i.Enabled).Select(i => i.Platform).Distinct().Count();
            
            _matrixSummaryLabel.Text = $"📊 Assets: {totalAssets} | Instances: {totalInstances} | Platforms: {platforms}";
        }

        #endregion

        #region Event Handlers

        private void RefreshAll()
        {
            LogActivity("🔄 Refreshing all data...");
            LoadConfiguration();
            LoadAssets();
            RefreshInstanceList();
            RefreshAssetList();
            RefreshCacheStats();
            UpdateMatrixSummary();
            LogActivity("✅ Refresh completed");
        }

        private void OpenAssetMatrixManager()
        {
            try
            {
                LogActivity("🚀 Opening Asset Matrix Manager...");
                
                using var matrixManager = new AssetMatrixManager(_config.Instances);
                matrixManager.ShowDialog(this);
                
                LogActivity("📊 Asset Matrix Manager closed");
                UpdateMatrixSummary();
            }
            catch (Exception ex)
            {
                LogActivity($"❌ Error launching Asset Matrix Manager: {ex.Message}");
                MessageBox.Show($"Error launching Asset Matrix Manager: {ex.Message}", 
                    "Launch Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void AddNewInstance()
        {
            // Implement instance creation dialog
            LogActivity("➕ Add new instance requested");
            MessageBox.Show("Instance creation dialog would open here.", "Add Instance", 
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void EditSelectedInstance()
        {
            if (_instanceListView.SelectedItems.Count > 0)
            {
                var instance = _instanceListView.SelectedItems[0].Tag as TradingInstance;
                LogActivity($"✏️ Edit instance: {instance?.Name}");
                // Implement edit dialog
            }
        }

        private void DeleteSelectedInstance()
        {
            if (_instanceListView.SelectedItems.Count > 0)
            {
                var instance = _instanceListView.SelectedItems[0].Tag as TradingInstance;
                var result = MessageBox.Show($"Delete instance '{instance?.Name}'?", "Confirm Delete", 
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                
                if (result == DialogResult.Yes)
                {
                    _config.Instances.Remove(instance!);
                    SaveConfiguration();
                    RefreshInstanceList();
                    LogActivity($"🗑️ Deleted instance: {instance?.Name}");
                }
            }
        }

        private void TestSelectedInstance()
        {
            if (_instanceListView.SelectedItems.Count > 0)
            {
                var instance = _instanceListView.SelectedItems[0].Tag as TradingInstance;
                LogActivity($"🧪 Testing instance: {instance?.Name}");
                
                var exists = Directory.Exists(instance?.InstancePath);
                var message = exists ? "Instance path exists and is accessible." : "Instance path not found or inaccessible.";
                var icon = exists ? MessageBoxIcon.Information : MessageBoxIcon.Warning;
                
                MessageBox.Show(message, "Instance Test", MessageBoxButtons.OK, icon);
            }
        }

        private void TestAllInstances()
        {
            LogActivity("🧪 Testing all instances...");
            
            var validCount = 0;
            var invalidCount = 0;
            
            foreach (var instance in _config.Instances.Where(i => i.Enabled))
            {
                if (Directory.Exists(instance.InstancePath))
                    validCount++;
                else
                    invalidCount++;
            }
            
            MessageBox.Show($"Instance Test Results:\n\n✅ Valid: {validCount}\n❌ Invalid: {invalidCount}", 
                "All Instances Test", MessageBoxButtons.OK, MessageBoxIcon.Information);
            
            LogActivity($"🧪 Instance test complete: {validCount} valid, {invalidCount} invalid");
        }

        private void ValidateAllPaths()
        {
            LogActivity("🔍 Validating all instance paths...");
            TestAllInstances();
        }

        private void ValidateAssetPaths()
        {
            LogActivity("🔍 Validating asset paths...");
            
            var validCount = 0;
            var invalidCount = 0;
            
            foreach (var asset in _assets)
            {
                if (File.Exists(asset.FilePath) || Directory.Exists(asset.FilePath))
                    validCount++;
                else
                    invalidCount++;
            }
            
            MessageBox.Show($"Asset Path Validation:\n\n✅ Valid: {validCount}\n❌ Invalid: {invalidCount}", 
                "Asset Path Validation", MessageBoxButtons.OK, MessageBoxIcon.Information);
            
            LogActivity($"🔍 Asset validation complete: {validCount} valid, {invalidCount} invalid");
        }

        private void ClearAssetCache()
        {
            var result = MessageBox.Show("Clear all cached asset data?", "Clear Cache", 
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            
            if (result == DialogResult.Yes)
            {
                _cacheManager.ClearAllCache();
                LogActivity("🧹 Asset cache cleared");
                RefreshCacheStats();
            }
        }

        private void ShowCacheStats()
        {
            var stats = _cacheManager.GetCacheStats();
            var message = $"Cache Statistics:\n\n" +
                         $"Memory Entries: {stats.MemoryCacheEntries}\n" +
                         $"Disk Entries: {stats.DiskCacheEntries}\n" +
                         $"Memory Usage: {stats.GetFormattedMemoryUsage()}\n" +
                         $"Oldest Entry: {stats.OldestCacheEntry:yyyy-MM-dd HH:mm}\n" +
                         $"Newest Entry: {stats.NewestCacheEntry:yyyy-MM-dd HH:mm}";
            
            MessageBox.Show(message, "Cache Statistics", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ShowSystemInfo()
        {
            var stats = _cacheManager.GetCacheStats();
            var message = $"Level 8 Unified Trading Platform Manager\n\n" +
                         $"Configuration: {_configPath}\n" +
                         $"Instances: {_config.Instances.Count}\n" +
                         $"Assets: {_assets.Count}\n" +
                         $"Cache Entries: {stats.MemoryCacheEntries}\n" +
                         $"Version: 8.0";
            
            MessageBox.Show(message, "System Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ExportConfiguration()
        {
            using var saveDialog = new SaveFileDialog()
            {
                Filter = "JSON files (*.json)|*.json",
                DefaultExt = "json",
                FileName = "instances-config-export.json"
            };
            
            if (saveDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    var options = new JsonSerializerOptions 
                    { 
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        WriteIndented = true
                    };
                    var json = JsonSerializer.Serialize(_config, options);
                    File.WriteAllText(saveDialog.FileName, json);
                    
                    LogActivity($"📤 Configuration exported to {saveDialog.FileName}");
                    MessageBox.Show("Configuration exported successfully!", "Export Complete", 
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    LogActivity($"❌ Export error: {ex.Message}");
                    MessageBox.Show($"Export failed: {ex.Message}", "Export Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void ImportConfiguration()
        {
            using var openDialog = new OpenFileDialog()
            {
                Filter = "JSON files (*.json)|*.json",
                DefaultExt = "json"
            };
            
            if (openDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    var json = File.ReadAllText(openDialog.FileName);
                    var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                    var importedConfig = JsonSerializer.Deserialize<Models.Configuration>(json, options);
                    
                    if (importedConfig != null)
                    {
                        _config = importedConfig;
                        SaveConfiguration();
                        RefreshAll();
                        
                        LogActivity($"📥 Configuration imported from {openDialog.FileName}");
                        MessageBox.Show("Configuration imported successfully!", "Import Complete", 
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                catch (Exception ex)
                {
                    LogActivity($"❌ Import error: {ex.Message}");
                    MessageBox.Show($"Import failed: {ex.Message}", "Import Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        #endregion

        #region Utility Methods

        private void LogActivity(string message)
        {
            try
            {
                var timestamp = DateTime.Now.ToString("HH:mm:ss");
                var logEntry = $"[{timestamp}] {message}";
                
                _activityLog.Items.Add(logEntry);
                _activityLog.TopIndex = _activityLog.Items.Count - 1;
                
                // Keep log manageable
                if (_activityLog.Items.Count > 1000)
                {
                    _activityLog.Items.RemoveAt(0);
                }
                
                Application.DoEvents();
            }
            catch
            {
                // Silent fail for logging
            }
        }

        private string FormatFileSize(long bytes)
        {
            if (bytes == 0) return "0 B";
            
            string[] sizes = { "B", "KB", "MB", "GB" };
            var order = 0;
            double size = bytes;
            
            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size /= 1024;
            }
            
            return $"{size:0.##} {sizes[order]}";
        }

        #endregion

        #region Cleanup

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _refreshTimer?.Stop();
                _refreshTimer?.Dispose();
                _cacheStatsTimer?.Stop();
                _cacheStatsTimer?.Dispose();
                
                _menuStrip?.Dispose();
                _toolStrip?.Dispose();
                _statusStrip?.Dispose();
                _mainTabControl?.Dispose();
                _instanceListView?.Dispose();
                _assetListView?.Dispose();
                _instancePropertyGrid?.Dispose();
                _assetPropertyGrid?.Dispose();
                _activityLog?.Dispose();
            }
            base.Dispose(disposing);
        }

        #endregion
    }
}