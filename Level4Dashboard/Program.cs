using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Level4Dashboard
{
    public partial class MainForm : Form
    {
        private FlowLayoutPanel mainPanel;
        private MenuStrip menuStrip;
        private StatusStrip statusStrip;
        private ToolStripStatusLabel statusLabel;
        private ToolStripStatusLabel processCountLabel;
        private ToolStripStatusLabel performanceLabel;
        private System.Windows.Forms.Timer refreshTimer;
        private System.Windows.Forms.Timer performanceTimer;
        private string configPath;
        private TradingConfiguration config;
        private Dictionary<string, GroupBox> groups;
        private Dictionary<string, Process[]> runningProcesses;
        private ContextMenuStrip instanceContextMenu;
        private ToolStripMenuItem darkModeMenuItem;

        public MainForm()
        {
            InitializeComponent();
            InitializeLayout();
            LoadConfiguration();
            StartRefreshTimer();
            StartPerformanceMonitoring();
            runningProcesses = new Dictionary<string, Process[]>();
        }

        private void SelectNewConfigFile()
        {
            using var dialog = new OpenFileDialog
            {
                Title = "Select Trading Configuration File",
                Filter = "JSON Config Files (*.json)|*.json|All Files (*.*)|*.*",
                FileName = "instances-config.json",
                InitialDirectory = Path.GetDirectoryName(configPath) ?? @"C:\Projects\FinancialData"
            };
            
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                configPath = dialog.FileName;
                
                // Save this path for future use
                var settingsPath = Path.Combine(Application.StartupPath, "dashboard-settings.json");
                var settings = new { LastConfigPath = configPath };
                File.WriteAllText(settingsPath, JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }));
                
                LoadConfiguration();
            }
        }

        private void InitializeComponent()
        {
            this.Text = "Level 5: Trading Platform Control Dashboard";
            this.Size = new Size(1400, 900);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Icon = SystemIcons.Application;
            
            // Set minimum size
            this.MinimumSize = new Size(1000, 700);
            
            // Enable dark mode support
            this.BackColor = Color.FromArgb(45, 45, 48);
            this.ForeColor = Color.White;
        }

        private void InitializeLayout()
        {
            // Menu Strip
            menuStrip = new MenuStrip();
            var fileMenu = new ToolStripMenuItem("File");
            fileMenu.DropDownItems.Add("Refresh Configuration", null, (s, e) => LoadConfiguration());
            fileMenu.DropDownItems.Add("Change Config File", null, (s, e) => SelectNewConfigFile());
            fileMenu.DropDownItems.Add("Run Icon Generator", null, (s, e) => RunIconGenerator());
            fileMenu.DropDownItems.Add("-");
            fileMenu.DropDownItems.Add("Exit", null, (s, e) => Close());
            
            var actionsMenu = new ToolStripMenuItem("Level 3/4 Actions");
            actionsMenu.DropDownItems.Add("Start All Enabled", null, (s, e) => RunTradingManagerAction("Start"));
            actionsMenu.DropDownItems.Add("Stop All", null, (s, e) => RunTradingManagerAction("Stop"));
            actionsMenu.DropDownItems.Add("Check Status", null, (s, e) => RunTradingManagerAction("Status"));
            actionsMenu.DropDownItems.Add("-");
            actionsMenu.DropDownItems.Add("Install Automation", null, (s, e) => RunTradingManagerAction("Install"));
            actionsMenu.DropDownItems.Add("Remove Automation", null, (s, e) => RunTradingManagerAction("Remove"));
            
            var level5Menu = new ToolStripMenuItem("Level 5 Features");
            level5Menu.DropDownItems.Add("Performance Monitor", null, (s, e) => ShowPerformanceMonitor());
            level5Menu.DropDownItems.Add("Batch Operations", null, (s, e) => ShowBatchOperations());
            level5Menu.DropDownItems.Add("Configuration Editor", null, (s, e) => ShowConfigurationEditor());
            level5Menu.DropDownItems.Add("Process Explorer", null, (s, e) => ShowProcessExplorer());
            level5Menu.DropDownItems.Add("-");
            darkModeMenuItem = new ToolStripMenuItem("Dark Mode", null, ToggleDarkMode);
            darkModeMenuItem.Checked = true;
            level5Menu.DropDownItems.Add(darkModeMenuItem);
            
            menuStrip.Items.Add(fileMenu);
            menuStrip.Items.Add(actionsMenu);
            menuStrip.Items.Add(level5Menu);
            
            // Main Panel with scroll
            mainPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                Padding = new Padding(10),
                BackColor = Color.FromArgb(45, 45, 48)
            };
            
            // Status Strip
            statusStrip = new StatusStrip();
            statusLabel = new ToolStripStatusLabel("Ready") { Spring = true, TextAlign = ContentAlignment.MiddleLeft };
            processCountLabel = new ToolStripStatusLabel("Processes: 0") { AutoSize = true };
            performanceLabel = new ToolStripStatusLabel("CPU: 0% | RAM: 0MB") { AutoSize = true };
            statusStrip.Items.AddRange(new ToolStripItem[] { statusLabel, processCountLabel, performanceLabel });
            
            // Context Menu for instances
            CreateInstanceContextMenu();
            
            this.Controls.Add(mainPanel);
            this.Controls.Add(menuStrip);
            this.Controls.Add(statusStrip);
            
            groups = new Dictionary<string, GroupBox>();
        }

        private void CreateInstanceContextMenu()
        {
            instanceContextMenu = new ContextMenuStrip();
            instanceContextMenu.Items.Add("Start Instance", null, StartInstance_Click);
            instanceContextMenu.Items.Add("Stop Instance", null, StopInstance_Click);
            instanceContextMenu.Items.Add("-");
            instanceContextMenu.Items.Add("Enable Auto-Start", null, EnableAutoStart_Click);
            instanceContextMenu.Items.Add("Disable Auto-Start", null, DisableAutoStart_Click);
            instanceContextMenu.Items.Add("-");
            instanceContextMenu.Items.Add("Enable Instance", null, EnableInstance_Click);
            instanceContextMenu.Items.Add("Disable Instance", null, DisableInstance_Click);
            instanceContextMenu.Items.Add("-");
            instanceContextMenu.Items.Add("Open Instance Folder", null, OpenInstanceFolder_Click);
            instanceContextMenu.Items.Add("View Configuration", null, ViewConfiguration_Click);
        }

        private void LoadConfiguration()
        {
            try
            {
                // Multiple search paths for configuration file
                var searchPaths = new[]
                {
                    // 1. Current directory (if running from trading environment)
                    Path.Combine(Application.StartupPath, "instances-config.json"),
                    
                    // 2. Parent directory (if running from subfolder)
                    Path.Combine(Directory.GetParent(Application.StartupPath)?.FullName ?? "", "instances-config.json"),
                    
                    // 3. Standard trading environment locations
                    @"C:\Projects\FinancialData\instances-config.json",
                    @"C:\TradingRoot\instances-config.json",
                    
                    // 4. Look for it in common development locations
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Projects", "FinancialData", "instances-config.json"),
                    
                    // 5. Check if there's an environment variable set
                    Environment.GetEnvironmentVariable("TRADING_ROOT") != null ? 
                        Path.Combine(Environment.GetEnvironmentVariable("TRADING_ROOT")!, "instances-config.json") : "",
                    
                    // 6. Search in typical project structures
                    Path.Combine(Directory.GetCurrentDirectory(), "..", "instances-config.json"),
                    Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "instances-config.json"),
                    Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "instances-config.json")
                };

                // Find the first existing configuration file
                configPath = searchPaths.FirstOrDefault(path => !string.IsNullOrEmpty(path) && File.Exists(path));
                
                if (string.IsNullOrEmpty(configPath))
                {
                    // If no config found, show file dialog to let user select it
                    using var dialog = new OpenFileDialog
                    {
                        Title = "Select Trading Configuration File",
                        Filter = "JSON Config Files (*.json)|*.json|All Files (*.*)|*.*",
                        FileName = "instances-config.json",
                        InitialDirectory = @"C:\Projects\FinancialData"
                    };
                    
                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        configPath = dialog.FileName;
                        
                        // Save this path for future use
                        var settingsPath = Path.Combine(Application.StartupPath, "dashboard-settings.json");
                        var settings = new { LastConfigPath = configPath };
                        File.WriteAllText(settingsPath, JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }));
                    }
                    else
                    {
                        MessageBox.Show("No configuration file selected. Please ensure instances-config.json exists in your trading environment.", 
                                      "Configuration Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }
                
                // Try to load saved path from previous session
                if (string.IsNullOrEmpty(configPath))
                {
                    var settingsPath = Path.Combine(Application.StartupPath, "dashboard-settings.json");
                    if (File.Exists(settingsPath))
                    {
                        try
                        {
                            var settingsJson = File.ReadAllText(settingsPath);
                            var settings = JsonSerializer.Deserialize<Dictionary<string, object>>(settingsJson);
                            if (settings?.ContainsKey("LastConfigPath") == true)
                            {
                                var lastPath = settings["LastConfigPath"].ToString();
                                if (!string.IsNullOrEmpty(lastPath) && File.Exists(lastPath))
                                {
                                    configPath = lastPath;
                                }
                            }
                        }
                        catch { /* Ignore settings load errors */ }
                    }
                }

                if (string.IsNullOrEmpty(configPath))
                {
                    MessageBox.Show("Unable to locate instances-config.json. Please ensure your trading environment is set up correctly.", 
                                  "Configuration Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string json = File.ReadAllText(configPath);
                config = JsonSerializer.Deserialize<TradingConfiguration>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (config == null)
                {
                    MessageBox.Show("Failed to parse configuration file. The file may be corrupted or invalid.", 
                                  "Configuration Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    statusLabel.Text = "Configuration parse failed";
                    return;
                }

                if (config.Instances == null)
                {
                    config.Instances = new List<TradingInstance>();
                }

                statusLabel.Text = $"Loaded {config.Instances.Count} instances from: {Path.GetFileName(configPath)}";
                this.Text = $"Level 5: Trading Dashboard - {Path.GetDirectoryName(configPath)}";
                
                // Add debug info to see what we loaded
                if (config.Instances.Count == 0)
                {
                    statusLabel.Text = "Configuration loaded but no instances found";
                }
                
                RefreshDisplay();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading configuration: {ex.Message}", "Configuration Error", 
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
                statusLabel.Text = "Configuration load failed";
            }
        }

        private void RefreshDisplay()
        {
            try
            {
                mainPanel.Controls.Clear();
                groups.Clear();

                if (config?.Instances == null || !config.Instances.Any())
                {
                    // Show a message when no instances are found
                    var noInstancesLabel = new Label
                    {
                        Text = "No trading instances found in configuration.\n\nCheck your instances-config.json file.",
                        Dock = DockStyle.Fill,
                        TextAlign = ContentAlignment.MiddleCenter,
                        Font = new Font("Segoe UI", 12f),
                        ForeColor = Color.White,
                        BackColor = Color.FromArgb(45, 45, 48)
                    };
                    mainPanel.Controls.Add(noInstancesLabel);
                    return;
                }

                // DEBUG: Add a test label first to see if ANY controls work
                var debugLabel = new Label
                {
                    Text = $"DEBUG: About to process {config.Instances.Count} instances...",
                    Height = 30,
                    Width = mainPanel.Width - 20,
                    ForeColor = Color.Yellow,
                    BackColor = Color.FromArgb(60, 60, 63),
                    TextAlign = ContentAlignment.MiddleLeft
                };
                mainPanel.Controls.Add(debugLabel);

                // Group instances by broker/platform or custom grouping
                var groupedInstances = config.Instances
                    .GroupBy(i => GetGroupName(i))
                    .OrderBy(g => g.Key);

                // Debug: Show what groups we found
                var groupCount = groupedInstances.Count();
                statusLabel.Text = $"Loaded {config.Instances.Count} instances, {groupCount} groups";

                // DEBUG: Add another test label showing groups
                var groupDebugLabel = new Label
                {
                    Text = $"DEBUG: Found {groupCount} groups: {string.Join(", ", groupedInstances.Select(g => g.Key))}",
                    Height = 30,
                    Width = mainPanel.Width - 20,
                    ForeColor = Color.Cyan,
                    BackColor = Color.FromArgb(60, 60, 63),
                    TextAlign = ContentAlignment.MiddleLeft
                };
                mainPanel.Controls.Add(groupDebugLabel);

                if (!groupedInstances.Any())
                {
                    var noGroupsLabel = new Label
                    {
                        Text = "No valid instance groups found.",
                        Dock = DockStyle.Fill,
                        TextAlign = ContentAlignment.MiddleCenter,
                        Font = new Font("Segoe UI", 12f),
                        ForeColor = Color.White
                    };
                    mainPanel.Controls.Add(noGroupsLabel);
                    return;
                }

                foreach (var group in groupedInstances)
                {
                    try
                    {
                        // DEBUG: Add a label before each group creation
                        var preGroupLabel = new Label
                        {
                            Text = $"DEBUG: Creating group '{group.Key}' with {group.Count()} instances...",
                            Height = 25,
                            Width = mainPanel.Width - 20,
                            ForeColor = Color.LightGreen,
                            BackColor = Color.FromArgb(60, 60, 63),
                            TextAlign = ContentAlignment.MiddleLeft
                        };
                        mainPanel.Controls.Add(preGroupLabel);

                        CreateGroup(group.Key, group.ToList());

                        // DEBUG: Add a label after successful group creation
                        var postGroupLabel = new Label
                        {
                            Text = $"DEBUG: Successfully created group '{group.Key}'",
                            Height = 25,
                            Width = mainPanel.Width - 20,
                            ForeColor = Color.LightBlue,
                            BackColor = Color.FromArgb(60, 60, 63),
                            TextAlign = ContentAlignment.MiddleLeft
                        };
                        mainPanel.Controls.Add(postGroupLabel);
                    }
                    catch (Exception ex)
                    {
                        // Add error display for each failed group
                        var errorLabel = new Label
                        {
                            Text = $"ERROR creating group '{group.Key}': {ex.Message}",
                            Height = 50,
                            Width = mainPanel.Width - 20,
                            ForeColor = Color.Red,
                            BackColor = Color.FromArgb(60, 60, 63),
                            TextAlign = ContentAlignment.MiddleLeft
                        };
                        mainPanel.Controls.Add(errorLabel);
                    }
                }

                // Add summary panel
                CreateSummaryPanel();
                
                // Force refresh
                mainPanel.Invalidate();
                this.Refresh();
            }
            catch (Exception ex)
            {
                var errorLabel = new Label
                {
                    Text = $"MAIN ERROR displaying instances: {ex.Message}\n\nStack: {ex.StackTrace}",
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Font = new Font("Consolas", 9f),
                    ForeColor = Color.Red,
                    BackColor = Color.FromArgb(45, 45, 48)
                };
                mainPanel.Controls.Add(errorLabel);
            }
        }

        private string GetGroupName(TradingInstance instance)
        {
            // Debug: Show what we're getting
            var groupName = "Unknown";
            
            // Try to extract broker name or use source directory
            if (!string.IsNullOrEmpty(instance.Source))
            {
                // Extract broker from source (e.g., "ICMarkets_MT4" -> "ICMarkets")
                var parts = instance.Source.Split('_');
                if (parts.Length > 1)
                {
                    groupName = parts[0]; // Broker name
                }
                else
                {
                    groupName = instance.Source;
                }
            }
            else if (!string.IsNullOrEmpty(instance.Platform))
            {
                groupName = instance.Platform;
            }
            else if (!string.IsNullOrEmpty(instance.Name))
            {
                // Use name as fallback
                var parts = instance.Name.Split('_', ' ');
                groupName = parts[0];
            }
            
            // Ensure we always return something
            if (string.IsNullOrEmpty(groupName))
            {
                groupName = "Ungrouped";
            }
            
            return groupName;
        }

        private void CreateGroup(string groupName, List<TradingInstance> instances)
        {
            try
            {
                var groupBox = new GroupBox
                {
                    Text = $"{groupName} ({instances.Count} instances)",
                    Width = mainPanel.Width - 50,
                    Height = Math.Max(150, 120 + (instances.Count / 4) * 60), // Dynamic height with minimum
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                    Margin = new Padding(10),
                    ForeColor = Color.White,
                    BackColor = Color.FromArgb(60, 60, 63)
                };

                var instancePanel = new FlowLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    FlowDirection = FlowDirection.LeftToRight,
                    WrapContents = true,
                    Padding = new Padding(5),
                    BackColor = Color.FromArgb(60, 60, 63)
                };

                // Group action buttons
                var groupActionPanel = new Panel
                {
                    Height = 35,
                    Dock = DockStyle.Top,
                    BackColor = Color.FromArgb(60, 60, 63)
                };

                var startGroupBtn = new Button
                {
                    Text = "Start Group",
                    Size = new Size(85, 25),
                    Location = new Point(5, 5),
                    BackColor = Color.FromArgb(0, 120, 212),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat
                };
                startGroupBtn.Click += (s, e) => StartGroup(instances);

                var stopGroupBtn = new Button
                {
                    Text = "Stop Group",
                    Size = new Size(85, 25),
                    Location = new Point(95, 5),
                    BackColor = Color.FromArgb(196, 43, 28),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat
                };
                stopGroupBtn.Click += (s, e) => StopGroup(instances);

                var enableGroupBtn = new Button
                {
                    Text = "Enable All",
                    Size = new Size(85, 25),
                    Location = new Point(185, 5),
                    BackColor = Color.FromArgb(16, 124, 16),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat
                };
                enableGroupBtn.Click += (s, e) => ToggleGroupEnabled(instances, true);

                var disableGroupBtn = new Button
                {
                    Text = "Disable All",
                    Size = new Size(85, 25),
                    Location = new Point(275, 5),
                    BackColor = Color.FromArgb(117, 117, 117),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat
                };
                disableGroupBtn.Click += (s, e) => ToggleGroupEnabled(instances, false);

                groupActionPanel.Controls.AddRange(new Control[] { startGroupBtn, stopGroupBtn, enableGroupBtn, disableGroupBtn });

                // Add instance controls
                foreach (var instance in instances)
                {
                    var instanceControl = CreateInstanceControl(instance);
                    instancePanel.Controls.Add(instanceControl);
                }

                groupBox.Controls.Add(instancePanel);
                groupBox.Controls.Add(groupActionPanel);
                groups[groupName] = groupBox;
                mainPanel.Controls.Add(groupBox);
            }
            catch (Exception ex)
            {
                var errorLabel = new Label
                {
                    Text = $"Error creating group {groupName}: {ex.Message}",
                    Size = new Size(400, 50),
                    ForeColor = Color.Red,
                    BackColor = Color.FromArgb(60, 60, 63)
                };
                mainPanel.Controls.Add(errorLabel);
            }
        }

        private Control CreateInstanceControl(TradingInstance instance)
        {
            var panel = new Panel
            {
                Size = new Size(120, 80),
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(2),
                Tag = instance
            };

            // Load icon if available
            var iconPath = GetInstanceIconPath(instance);
            PictureBox iconBox;
            
            if (File.Exists(iconPath))
            {
                iconBox = new PictureBox
                {
                    Size = new Size(32, 32),
                    Location = new Point(44, 5),
                    SizeMode = PictureBoxSizeMode.StretchImage,
                    Image = Image.FromFile(iconPath)
                };
            }
            else
            {
                iconBox = new PictureBox
                {
                    Size = new Size(32, 32),
                    Location = new Point(44, 5),
                    SizeMode = PictureBoxSizeMode.CenterImage,
                    Image = SystemIcons.Application.ToBitmap()
                };
            }

            var nameLabel = new Label
            {
                Text = instance.Name ?? instance.Destination,
                Size = new Size(116, 15),
                Location = new Point(2, 40),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 7f),
                ForeColor = Color.Black
            };

            var statusLabel = new Label
            {
                Size = new Size(116, 12),
                Location = new Point(2, 55),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 6.5f),
                ForeColor = GetStatusColor(instance)
            };

            UpdateInstanceStatus(statusLabel, instance);

            // Set background color based on account type
            panel.BackColor = GetInstanceBackgroundColor(instance);

            // Context menu
            panel.ContextMenuStrip = instanceContextMenu;
            iconBox.ContextMenuStrip = instanceContextMenu;
            nameLabel.ContextMenuStrip = instanceContextMenu;
            statusLabel.ContextMenuStrip = instanceContextMenu;

            // Double-click to start
            panel.DoubleClick += (s, e) => StartSingleInstance(instance);
            iconBox.DoubleClick += (s, e) => StartSingleInstance(instance);

            panel.Controls.AddRange(new Control[] { iconBox, nameLabel, statusLabel });
            return panel;
        }

        private string GetInstanceIconPath(TradingInstance instance)
        {
            var basePath = Path.GetDirectoryName(configPath) ?? "";
            
            // Try multiple icon locations
            var paths = new[]
            {
                Path.Combine(basePath, "PlatformInstallations", instance.Source, "ShortCutImage", "icon.ico"),
                Path.Combine(basePath, "PlatformInstallations", instance.Source, "ShortCutImage", $"{instance.Source}.ico"),
                Path.Combine(basePath, "Generated Icons", $"{instance.Destination}.ico"),
                Path.Combine(basePath, "InstanceData", instance.Destination, "icon.ico")
            };

            return paths.FirstOrDefault(File.Exists) ?? "";
        }

        private Color GetInstanceBackgroundColor(TradingInstance instance)
        {
            var accountType = instance.AccountType?.ToLower() ?? "";
            
            // Check for custom icon settings first
            if (instance.IconSettings?.BackgroundColor != null)
            {
                return ColorTranslator.FromHtml(instance.IconSettings.BackgroundColor);
            }

            // Default colors based on account type
            return accountType switch
            {
                var type when type.Contains("live") || type.Contains("real") || type.Contains("production") => Color.FromArgb(255, 240, 240),
                var type when type.Contains("demo") || type.Contains("practice") || type.Contains("training") => Color.FromArgb(240, 240, 255),
                var type when type.Contains("paper") || type.Contains("sim") || type.Contains("simulation") => Color.FromArgb(240, 255, 240),
                var type when type.Contains("premium") || type.Contains("pro") || type.Contains("vip") || type.Contains("gold") => Color.FromArgb(255, 255, 220),
                var type when type.Contains("test") || type.Contains("staging") || type.Contains("dev") => Color.FromArgb(245, 245, 245),
                _ => Color.FromArgb(250, 240, 255)
            };
        }

        private Color GetStatusColor(TradingInstance instance)
        {
            if (!instance.Enabled) return Color.Gray;
            if (instance.StartupSettings?.AutoStart == false) return Color.Orange;
            return Color.Green;
        }

        private void UpdateInstanceStatus(Label statusLabel, TradingInstance instance)
        {
            if (!instance.Enabled)
            {
                statusLabel.Text = "Disabled";
                statusLabel.ForeColor = Color.Gray;
            }
            else if (instance.StartupSettings?.AutoStart == false)
            {
                statusLabel.Text = "Manual Only";
                statusLabel.ForeColor = Color.Orange;
            }
            else
            {
                statusLabel.Text = "Auto-Start";
                statusLabel.ForeColor = Color.Green;
            }
        }

        private void CreateSummaryPanel()
        {
            var summaryBox = new GroupBox
            {
                Text = "Summary",
                Width = mainPanel.Width - 40,
                Height = 100,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Margin = new Padding(5)
            };

            var enabled = config.Instances.Count(i => i.Enabled);
            var autoStart = config.Instances.Count(i => i.Enabled && (i.StartupSettings?.AutoStart ?? true));
            var disabled = config.Instances.Count(i => !i.Enabled);

            var summaryLabel = new Label
            {
                Text = $"Total Instances: {config.Instances.Count}\n" +
                       $"Enabled: {enabled} | Auto-Start: {autoStart} | Disabled: {disabled}",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };

            summaryBox.Controls.Add(summaryLabel);
            mainPanel.Controls.Add(summaryBox);
        }

        private void StartRefreshTimer()
        {
            refreshTimer = new System.Windows.Forms.Timer
            {
                Interval = 30000 // Refresh every 30 seconds
            };
            refreshTimer.Tick += (s, e) => RefreshDisplay();
            refreshTimer.Start();
        }

        private void StartPerformanceMonitoring()
        {
            performanceTimer = new System.Windows.Forms.Timer
            {
                Interval = 5000 // Update every 5 seconds
            };
            performanceTimer.Tick += UpdatePerformanceMetrics;
            performanceTimer.Start();
        }

        private void UpdatePerformanceMetrics(object sender, EventArgs e)
        {
            try
            {
                // Count running trading processes
                var allProcesses = Process.GetProcesses();
                var tradingProcesses = allProcesses.Where(p => 
                    p.ProcessName.ToLower().Contains("terminal") || 
                    p.ProcessName.ToLower().Contains("tradeterm") ||
                    p.ProcessName.ToLower().Contains("metatrader")).ToArray();

                processCountLabel.Text = $"Trading Processes: {tradingProcesses.Length}";

                // Calculate total CPU and memory usage for trading processes
                long totalMemory = 0;
                double totalCpu = 0;

                foreach (var process in tradingProcesses)
                {
                    try
                    {
                        totalMemory += process.WorkingSet64;
                        // Note: Getting accurate CPU usage requires more complex implementation
                    }
                    catch { } // Ignore access denied
                }

                var memoryMB = totalMemory / (1024 * 1024);
                performanceLabel.Text = $"Trading RAM: {memoryMB}MB | Total Processes: {allProcesses.Length}";

                // Update process cache
                UpdateRunningProcessCache();
            }
            catch (Exception ex)
            {
                performanceLabel.Text = $"Monitor Error: {ex.Message}";
            }
        }

        private void UpdateRunningProcessCache()
        {
            runningProcesses.Clear();
            if (config?.Instances == null) return;

            foreach (var instance in config.Instances)
            {
                var executable = instance.StartupSettings?.Executable ?? GetDefaultExecutable(instance.Platform);
                var processName = Path.GetFileNameWithoutExtension(executable);
                var processes = Process.GetProcessesByName(processName);
                
                // Filter by instance directory if possible
                var instancePath = Path.Combine(Path.GetDirectoryName(configPath) ?? "", "PlatformInstances", instance.Destination);
                var filteredProcesses = processes.Where(p =>
                {
                    try
                    {
                        return p.MainModule?.FileName?.StartsWith(instancePath, StringComparison.OrdinalIgnoreCase) == true;
                    }
                    catch { return false; }
                }).ToArray();

                if (filteredProcesses.Any())
                {
                    runningProcesses[instance.Destination] = filteredProcesses;
                }
            }
        }

        // Level 5 Feature Methods
        private void ShowPerformanceMonitor()
        {
            var perfForm = new Form
            {
                Text = "Level 5: Performance Monitor",
                Size = new Size(800, 600),
                StartPosition = FormStartPosition.CenterParent,
                Owner = this
            };

            var perfText = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true,
                BackColor = Color.Black,
                ForeColor = Color.Lime,
                Font = new Font("Consolas", 9)
            };

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("=== LEVEL 5 PERFORMANCE MONITOR ===\n");

            foreach (var kvp in runningProcesses)
            {
                sb.AppendLine($"Instance: {kvp.Key}");
                foreach (var process in kvp.Value)
                {
                    try
                    {
                        sb.AppendLine($"  PID: {process.Id}");
                        sb.AppendLine($"  Memory: {process.WorkingSet64 / (1024 * 1024)} MB");
                        sb.AppendLine($"  Start Time: {process.StartTime}");
                        sb.AppendLine($"  CPU Time: {process.TotalProcessorTime}");
                        sb.AppendLine($"  Threads: {process.Threads.Count}");
                        sb.AppendLine($"  Handles: {process.HandleCount}");
                        sb.AppendLine();
                    }
                    catch (Exception ex)
                    {
                        sb.AppendLine($"  Error reading process info: {ex.Message}\n");
                    }
                }
            }

            perfText.Text = sb.ToString();
            perfForm.Controls.Add(perfText);
            perfForm.ShowDialog();
        }

        private void ShowBatchOperations()
        {
            var batchForm = new Form
            {
                Text = "Level 5: Batch Operations",
                Size = new Size(600, 400),
                StartPosition = FormStartPosition.CenterParent,
                Owner = this
            };

            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 6,
                Padding = new Padding(10)
            };

            // Batch operation buttons
            var operations = new (string, Action)[]
            {
                ("Start All Live Accounts", () => BatchOperation(i => i.AccountType?.ToLower().Contains("live") == true, "start")),
                ("Start All Demo Accounts", () => BatchOperation(i => i.AccountType?.ToLower().Contains("demo") == true, "start")),
                ("Stop All MT4 Platforms", () => BatchOperation(i => i.Platform?.ToUpper() == "MT4", "stop")),
                ("Stop All MT5 Platforms", () => BatchOperation(i => i.Platform?.ToUpper() == "MT5", "stop")),
                ("Enable All Auto-Start", () => BatchOperation(i => true, "enable_autostart")),
                ("Disable All Auto-Start", () => BatchOperation(i => true, "disable_autostart"))
            };

            for (int i = 0; i < operations.Length; i++)
            {
                var btn = new Button
                {
                    Text = operations[i].Item1,
                    Dock = DockStyle.Fill,
                    Margin = new Padding(5)
                };
                btn.Click += (s, e) => operations[i].Item2();
                panel.Controls.Add(btn, 0, i);

                var countLabel = new Label
                {
                    Text = GetBatchOperationCount(operations[i].Item1),
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Margin = new Padding(5)
                };
                panel.Controls.Add(countLabel, 1, i);
            }

            batchForm.Controls.Add(panel);
            batchForm.ShowDialog();
        }

        private string GetBatchOperationCount(string operation)
        {
            if (config?.Instances == null) return "0 instances";

            var count = operation switch
            {
                var op when op.Contains("Live") => config.Instances.Count(i => i.AccountType?.ToLower().Contains("live") == true),
                var op when op.Contains("Demo") => config.Instances.Count(i => i.AccountType?.ToLower().Contains("demo") == true),
                var op when op.Contains("MT4") => config.Instances.Count(i => i.Platform?.ToUpper() == "MT4"),
                var op when op.Contains("MT5") => config.Instances.Count(i => i.Platform?.ToUpper() == "MT5"),
                _ => config.Instances.Count
            };

            return $"{count} instances";
        }

        private void BatchOperation(Func<TradingInstance, bool> filter, string operation)
        {
            var instances = config?.Instances?.Where(filter).ToList() ?? new List<TradingInstance>();
            
            if (!instances.Any())
            {
                MessageBox.Show("No instances match the selected criteria.", "Batch Operation", 
                              MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var result = MessageBox.Show($"Apply {operation} to {instances.Count} instances?", 
                                       "Confirm Batch Operation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            
            if (result != DialogResult.Yes) return;

            foreach (var instance in instances)
            {
                switch (operation)
                {
                    case "start":
                        StartSingleInstance(instance);
                        break;
                    case "stop":
                        StopSingleInstance(instance);
                        break;
                    case "enable_autostart":
                        if (instance.StartupSettings == null) instance.StartupSettings = new StartupSettings();
                        instance.StartupSettings.AutoStart = true;
                        break;
                    case "disable_autostart":
                        if (instance.StartupSettings == null) instance.StartupSettings = new StartupSettings();
                        instance.StartupSettings.AutoStart = false;
                        break;
                }
            }

            if (operation.Contains("autostart"))
            {
                SaveConfiguration();
                RefreshDisplay();
            }

            statusLabel.Text = $"Batch operation '{operation}' applied to {instances.Count} instances";
        }

        private void ShowConfigurationEditor()
        {
            var configForm = new Form
            {
                Text = "Level 5: Configuration Editor",
                Size = new Size(900, 700),
                StartPosition = FormStartPosition.CenterParent,
                Owner = this
            };

            var textBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ScrollBars = ScrollBars.Both,
                Font = new Font("Consolas", 10),
                WordWrap = false
            };

            try
            {
                textBox.Text = File.ReadAllText(configPath);
            }
            catch (Exception ex)
            {
                textBox.Text = $"Error loading configuration: {ex.Message}";
            }

            var buttonPanel = new Panel { Height = 40, Dock = DockStyle.Bottom };
            var saveBtn = new Button { Text = "Save & Reload", Size = new Size(120, 30), Location = new Point(10, 5) };
            var cancelBtn = new Button { Text = "Cancel", Size = new Size(80, 30), Location = new Point(140, 5) };

            saveBtn.Click += (s, e) =>
            {
                try
                {
                    File.WriteAllText(configPath, textBox.Text);
                    LoadConfiguration();
                    configForm.Close();
                    MessageBox.Show("Configuration saved and reloaded!", "Success", 
                                  MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error saving configuration: {ex.Message}", "Error", 
                                  MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            cancelBtn.Click += (s, e) => configForm.Close();

            buttonPanel.Controls.AddRange(new Control[] { saveBtn, cancelBtn });
            configForm.Controls.AddRange(new Control[] { textBox, buttonPanel });
            configForm.ShowDialog();
        }

        private void ShowProcessExplorer()
        {
            var processForm = new Form
            {
                Text = "Level 5: Trading Process Explorer",
                Size = new Size(1000, 600),
                StartPosition = FormStartPosition.CenterParent,
                Owner = this
            };

            var listView = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true
            };

            listView.Columns.AddRange(new[]
            {
                new ColumnHeader { Text = "Instance", Width = 150 },
                new ColumnHeader { Text = "PID", Width = 80 },
                new ColumnHeader { Text = "Memory (MB)", Width = 100 },
                new ColumnHeader { Text = "CPU Time", Width = 120 },
                new ColumnHeader { Text = "Start Time", Width = 150 },
                new ColumnHeader { Text = "Status", Width = 100 },
                new ColumnHeader { Text = "Path", Width = 250 }
            });

            foreach (var kvp in runningProcesses)
            {
                foreach (var process in kvp.Value)
                {
                    try
                    {
                        var item = new ListViewItem(kvp.Key);
                        item.SubItems.AddRange(new[]
                        {
                            process.Id.ToString(),
                            (process.WorkingSet64 / (1024 * 1024)).ToString(),
                            process.TotalProcessorTime.ToString(@"hh\:mm\:ss"),
                            process.StartTime.ToString("yyyy-MM-dd HH:mm:ss"),
                            process.Responding ? "Responding" : "Not Responding",
                            process.MainModule?.FileName ?? "N/A"
                        });

                        if (!process.Responding)
                        {
                            item.BackColor = Color.LightPink;
                        }

                        listView.Items.Add(item);
                    }
                    catch { } // Skip processes we can't access
                }
            }

            processForm.Controls.Add(listView);
            processForm.ShowDialog();
        }

        private void ToggleDarkMode(object sender, EventArgs e)
        {
            var isDark = darkModeMenuItem.Checked;
            darkModeMenuItem.Checked = !isDark;

            if (!isDark)
            {
                // Switch to dark mode
                this.BackColor = Color.FromArgb(45, 45, 48);
                this.ForeColor = Color.White;
                mainPanel.BackColor = Color.FromArgb(45, 45, 48);
            }
            else
            {
                // Switch to light mode
                this.BackColor = SystemColors.Control;
                this.ForeColor = SystemColors.ControlText;
                mainPanel.BackColor = SystemColors.Control;
            }

            RefreshDisplay();
        }

        // Action methods
        private async void RunTradingManagerAction(string action)
        {
            try
            {
                statusLabel.Text = $"Running {action}...";
                var tradingRoot = Path.GetDirectoryName(configPath);
                var setupPath = Path.Combine(tradingRoot, "Setup");
                
                // Try multiple possible script locations
                var scriptPaths = new[]
                {
                    Path.Combine(setupPath, "3 SimpleTradingManager.ps1"),
                    Path.Combine(tradingRoot, "3 SimpleTradingManager.ps1"),
                    Path.Combine(tradingRoot, "Setup", "3 trading_manager.ps1"),
                    Path.Combine(tradingRoot, "3 trading_manager.ps1")
                };
                
                var scriptPath = scriptPaths.FirstOrDefault(File.Exists);
                
                if (string.IsNullOrEmpty(scriptPath))
                {
                    MessageBox.Show($"PowerShell script not found. Looked in:\n{string.Join("\n", scriptPaths)}", 
                                  "Script Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    statusLabel.Text = "Script not found";
                    return;
                }

                var startInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-ExecutionPolicy Bypass -File \"{scriptPath}\" -Action {action}",
                    WorkingDirectory = tradingRoot,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = Process.Start(startInfo);
                if (process != null)
                {
                    await process.WaitForExitAsync();
                    var output = await process.StandardOutput.ReadToEndAsync();
                    var error = await process.StandardError.ReadToEndAsync();
                    
                    if (process.ExitCode == 0)
                    {
                        statusLabel.Text = $"{action} completed successfully";
                        if (!string.IsNullOrEmpty(output) && action == "Status")
                        {
                            MessageBox.Show(output, "Platform Status", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                    else
                    {
                        statusLabel.Text = $"{action} failed";
                        MessageBox.Show($"Error: {error}", "Action Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                statusLabel.Text = "Action failed";
                MessageBox.Show($"Error running action: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void RunIconGenerator()
        {
            try
            {
                statusLabel.Text = "Generating icons...";
                var tradingRoot = Path.GetDirectoryName(configPath);
                var setupPath = Path.Combine(tradingRoot, "Setup");
                
                // Try multiple possible script locations
                var scriptPaths = new[]
                {
                    Path.Combine(setupPath, "4 Level4-IconGenerator.ps1"),
                    Path.Combine(tradingRoot, "4 Level4-IconGenerator.ps1"),
                    Path.Combine(setupPath, "Level4-IconGenerator.ps1"),
                    Path.Combine(tradingRoot, "Level4-IconGenerator.ps1")
                };
                
                var scriptPath = scriptPaths.FirstOrDefault(File.Exists);
                
                if (string.IsNullOrEmpty(scriptPath))
                {
                    MessageBox.Show($"Icon generator script not found. Looked in:\n{string.Join("\n", scriptPaths)}", 
                                  "Script Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    statusLabel.Text = "Icon generator not found";
                    return;
                }

                var startInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-ExecutionPolicy Bypass -File \"{scriptPath}\" -Action Generate -Verbose",
                    WorkingDirectory = tradingRoot,
                    UseShellExecute = false,
                    CreateNoWindow = false // Show window for icon generation
                };

                using var process = Process.Start(startInfo);
                if (process != null)
                {
                    await process.WaitForExitAsync();
                    statusLabel.Text = "Icon generation completed";
                    RefreshDisplay(); // Refresh to show new icons
                }
            }
            catch (Exception ex)
            {
                statusLabel.Text = "Icon generation failed";
                MessageBox.Show($"Error generating icons: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void StartGroup(List<TradingInstance> instances)
        {
            foreach (var instance in instances.Where(i => i.Enabled))
            {
                StartSingleInstance(instance);
            }
        }

        private void StopGroup(List<TradingInstance> instances)
        {
            foreach (var instance in instances)
            {
                StopSingleInstance(instance);
            }
        }

        private void ToggleGroupEnabled(List<TradingInstance> instances, bool enabled)
        {
            foreach (var instance in instances)
            {
                instance.Enabled = enabled;
            }
            SaveConfiguration();
            RefreshDisplay();
        }

        private void StartSingleInstance(TradingInstance instance)
        {
            try
            {
                var instancePath = Path.Combine(Path.GetDirectoryName(configPath) ?? "", "PlatformInstances", instance.Destination);
                var executable = instance.StartupSettings?.Executable ?? GetDefaultExecutable(instance.Platform);
                var exePath = Path.Combine(instancePath, executable);

                if (File.Exists(exePath))
                {
                    var args = instance.StartupSettings?.Arguments ?? "";
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = exePath,
                        Arguments = args,
                        WorkingDirectory = instancePath,
                        UseShellExecute = true
                    });
                    
                    statusLabel.Text = $"Started {instance.Name ?? instance.Destination}";
                }
                else
                {
                    MessageBox.Show($"Executable not found: {exePath}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error starting instance: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void StopSingleInstance(TradingInstance instance)
        {
            try
            {
                var executable = instance.StartupSettings?.Executable ?? GetDefaultExecutable(instance.Platform);
                var processName = Path.GetFileNameWithoutExtension(executable);
                
                var processes = Process.GetProcessesByName(processName);
                var instancePath = Path.Combine(Path.GetDirectoryName(configPath) ?? "", "PlatformInstances", instance.Destination);
                
                foreach (var process in processes)
                {
                    try
                    {
                        if (process.MainModule?.FileName?.StartsWith(instancePath, StringComparison.OrdinalIgnoreCase) == true)
                        {
                            process.CloseMainWindow();
                            if (!process.WaitForExit(5000))
                            {
                                process.Kill();
                            }
                        }
                    }
                    catch { } // Ignore access denied errors
                }
                
                statusLabel.Text = $"Stopped {instance.Name ?? instance.Destination}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error stopping instance: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string GetDefaultExecutable(string platform)
        {
            return platform?.ToUpper() switch
            {
                "MT4" => "terminal.exe",
                "MT5" => "terminal64.exe",
                "TRADEREVOLUTION" => "TradeTerminal.exe",
                _ => "terminal.exe"
            };
        }

        // Context menu event handlers
        private void StartInstance_Click(object sender, EventArgs e)
        {
            if (instanceContextMenu.SourceControl?.Tag is TradingInstance instance)
            {
                StartSingleInstance(instance);
            }
        }

        private void StopInstance_Click(object sender, EventArgs e)
        {
            if (instanceContextMenu.SourceControl?.Tag is TradingInstance instance)
            {
                StopSingleInstance(instance);
            }
        }

        private void EnableAutoStart_Click(object sender, EventArgs e)
        {
            if (instanceContextMenu.SourceControl?.Tag is TradingInstance instance)
            {
                if (instance.StartupSettings == null)
                    instance.StartupSettings = new StartupSettings();
                instance.StartupSettings.AutoStart = true;
                SaveConfiguration();
                RefreshDisplay();
            }
        }

        private void DisableAutoStart_Click(object sender, EventArgs e)
        {
            if (instanceContextMenu.SourceControl?.Tag is TradingInstance instance)
            {
                if (instance.StartupSettings == null)
                    instance.StartupSettings = new StartupSettings();
                instance.StartupSettings.AutoStart = false;
                SaveConfiguration();
                RefreshDisplay();
            }
        }

        private void EnableInstance_Click(object sender, EventArgs e)
        {
            if (instanceContextMenu.SourceControl?.Tag is TradingInstance instance)
            {
                instance.Enabled = true;
                SaveConfiguration();
                RefreshDisplay();
            }
        }

        private void DisableInstance_Click(object sender, EventArgs e)
        {
            if (instanceContextMenu.SourceControl?.Tag is TradingInstance instance)
            {
                instance.Enabled = false;
                SaveConfiguration();
                RefreshDisplay();
            }
        }

        private void OpenInstanceFolder_Click(object sender, EventArgs e)
        {
            if (instanceContextMenu.SourceControl?.Tag is TradingInstance instance)
            {
                var instancePath = Path.Combine(Path.GetDirectoryName(configPath) ?? "", "PlatformInstances", instance.Destination);
                if (Directory.Exists(instancePath))
                {
                    Process.Start("explorer.exe", instancePath);
                }
            }
        }

        private void ViewConfiguration_Click(object sender, EventArgs e)
        {
            if (instanceContextMenu.SourceControl?.Tag is TradingInstance instance)
            {
                var details = $"Instance: {instance.Name}\n" +
                             $"Platform: {instance.Platform}\n" +
                             $"Source: {instance.Source}\n" +
                             $"Destination: {instance.Destination}\n" +
                             $"Enabled: {instance.Enabled}\n" +
                             $"Account Type: {instance.AccountType}\n" +
                             $"Auto-Start: {instance.StartupSettings?.AutoStart ?? true}\n" +
                             $"Startup Delay: {instance.StartupSettings?.StartupDelay ?? 0}s\n" +
                             $"Executable: {instance.StartupSettings?.Executable ?? GetDefaultExecutable(instance.Platform)}";
                
                MessageBox.Show(details, "Instance Configuration", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void SaveConfiguration()
        {
            try
            {
                var json = JsonSerializer.Serialize(config, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                File.WriteAllText(configPath, json);
                statusLabel.Text = "Configuration saved";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving configuration: {ex.Message}", "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                refreshTimer?.Dispose();
                performanceTimer?.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    // Data classes for JSON configuration
    public class TradingConfiguration
    {
        public List<TradingInstance> Instances { get; set; } = new();
    }

    public class TradingInstance
    {
        public string Name { get; set; }
        public string Platform { get; set; }
        public string Source { get; set; }
        public string Destination { get; set; }
        public bool Enabled { get; set; }
        public string AccountType { get; set; }
        public StartupSettings StartupSettings { get; set; }
        public IconSettings IconSettings { get; set; }
    }

    public class StartupSettings
    {
        public bool AutoStart { get; set; } = true;
        public int StartupDelay { get; set; }
        public string Executable { get; set; }
        public string Arguments { get; set; }
    }

    public class IconSettings
    {
        public string BackgroundColor { get; set; }
        public string TextColor { get; set; }
        public string OverlayText { get; set; }
    }

    // Application entry point
    public static class Program
    {
        [STAThread]
        public static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}