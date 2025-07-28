using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using AssetManager.Models;

namespace AssetManager
{
    /// <summary>
    /// Asset Matrix Manager - Enterprise asset management with PLATFORM COMPATIBILITY enforcement
    /// Shows Assets (rows) vs Compatible Trading Instances (columns) with sync capabilities
    /// CRITICAL: Enforces MT4 ↔ MT4, MT5 ↔ MT5, TraderEvolution ↔ TraderEvolution compatibility only
    /// </summary>
    public partial class AssetMatrixManager : Form
    {
        private readonly List<TradingInstance> _instances;
        private readonly Dictionary<string, Dictionary<string, bool>> _assetMatrix;
        private readonly Dictionary<string, string> _assetPaths;
        private readonly Dictionary<string, AssetInfo> _assetDetails; // NEW: Platform-aware asset info
        
        // UI Components
        private TabControl _tabControl = null!;
        private StatusStrip _statusStrip = null!;
        private ToolStripStatusLabel _statusLabel = null!;
        private ToolStrip _toolStrip = null!;
        private ListBox _activityLog = null!;
        private Panel _summaryPanel = null!;
        private Label _summaryLabel = null!;
        
        // Asset type tabs - organized by platform compatibility
        private readonly string[] _assetTypes = { "Expert Advisors", "Indicators", "Scripts", "Templates" };

        public AssetMatrixManager(List<TradingInstance> instances)
        {
            _instances = instances ?? new List<TradingInstance>();
            _assetMatrix = new Dictionary<string, Dictionary<string, bool>>();
            _assetPaths = new Dictionary<string, string>();
            _assetDetails = new Dictionary<string, AssetInfo>(); // NEW
            
            InitializeComponent();
            InitializeLayout();
            ScanAllAssets();
            CreateMatrixView();
            
            LogActivity("🎯 Platform-Aware Asset Matrix Manager initialized");
            LogActivity($"📊 Found {_assetDetails.Count} assets across {_instances.Count} instances");
            LogActivity("⚠️ Platform compatibility enforced: MT4↔MT4, MT5↔MT5, TE↔TE only");
        }

        private void InitializeComponent()
        {
            this.Text = "Level 7: Asset Matrix Manager - Platform-Aware Enterprise Distribution";
            this.Size = new Size(1400, 900);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimumSize = new Size(1000, 600);
            
            // Dark theme to match main window
            this.BackColor = Color.FromArgb(30, 30, 30);
            this.ForeColor = Color.FromArgb(240, 240, 240);
        }

        private void InitializeLayout()
        {
            this.SuspendLayout();
            
            // Tool Strip with Platform Compatibility Info
            _toolStrip = new ToolStrip();
            _toolStrip.BackColor = Color.FromArgb(45, 45, 48);
            _toolStrip.ForeColor = Color.FromArgb(240, 240, 240);
            
            _toolStrip.Items.Add(new ToolStripButton("🔄 Refresh", null, (s, e) => RefreshAssets()) { DisplayStyle = ToolStripItemDisplayStyle.ImageAndText });
            _toolStrip.Items.Add(new ToolStripSeparator());
            _toolStrip.Items.Add(new ToolStripButton("📋 Select Compatible", null, (s, e) => SelectCompatibleAssets()) { DisplayStyle = ToolStripItemDisplayStyle.ImageAndText });
            _toolStrip.Items.Add(new ToolStripButton("❌ Clear All", null, (s, e) => ClearAllSelections()) { DisplayStyle = ToolStripItemDisplayStyle.ImageAndText });
            _toolStrip.Items.Add(new ToolStripSeparator());
            _toolStrip.Items.Add(new ToolStripButton("🚀 Sync Compatible", null, (s, e) => SyncCompatibleAssets()) { DisplayStyle = ToolStripItemDisplayStyle.ImageAndText });
            _toolStrip.Items.Add(new ToolStripSeparator());
            _toolStrip.Items.Add(new ToolStripButton("📊 Compatibility Report", null, (s, e) => GenerateCompatibilityReport()) { DisplayStyle = ToolStripItemDisplayStyle.ImageAndText });
            _toolStrip.Items.Add(new ToolStripSeparator());
            
            // NEW: Platform compatibility indicator
            var compatibilityLabel = new ToolStripLabel("⚠️ Platform Isolation: MT4↔MT4 | MT5↔MT5 | TE↔TE");
            compatibilityLabel.ForeColor = Color.Orange;
            _toolStrip.Items.Add(compatibilityLabel);
            
            // Tab Control for asset types with platform grouping
            _tabControl = new TabControl()
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(40, 40, 40),
                ForeColor = Color.FromArgb(240, 240, 240)
            };
            
            foreach (var assetType in _assetTypes)
            {
                var tabPage = new TabPage(assetType)
                {
                    BackColor = Color.FromArgb(30, 30, 30),
                    ForeColor = Color.FromArgb(240, 240, 240)
                };
                _tabControl.TabPages.Add(tabPage);
            }
            
            // Activity Log Panel
            var splitter = new SplitContainer()
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 600,
                BackColor = Color.FromArgb(45, 45, 48)
            };
            
            splitter.Panel1.Controls.Add(_tabControl);
            
            // Bottom panel for activity log
            _activityLog = new ListBox()
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(20, 20, 20),
                ForeColor = Color.FromArgb(200, 200, 200),
                Font = new Font("Consolas", 8)
            };
            
            var logPanel = new Panel() { Dock = DockStyle.Fill };
            logPanel.Controls.Add(_activityLog);
            
            var logLabel = new Label()
            {
                Text = "📋 Activity Log & Platform Compatibility Status",
                Dock = DockStyle.Top,
                Height = 25,
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.FromArgb(240, 240, 240),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0)
            };
            logPanel.Controls.Add(logLabel);
            
            splitter.Panel2.Controls.Add(logPanel);
            
            // Status Strip
            _statusStrip = new StatusStrip();
            _statusStrip.BackColor = Color.FromArgb(45, 45, 48);
            _statusLabel = new ToolStripStatusLabel("Ready - Platform compatibility enforced");
            _statusLabel.ForeColor = Color.FromArgb(240, 240, 240);
            _statusStrip.Items.Add(_statusLabel);
            
            this.Controls.Add(splitter);
            this.Controls.Add(_toolStrip);
            this.Controls.Add(_statusStrip);
            
            this.ResumeLayout();
        }

        /// <summary>
        /// NEW: Platform compatibility check - CORE BUSINESS LOGIC
        /// </summary>
        private bool ArePlatformsCompatible(string assetPlatform, string instancePlatform)
        {
            // Normalize platform names
            assetPlatform = assetPlatform.ToUpper();
            instancePlatform = instancePlatform.ToUpper();
            
            // STRICT COMPATIBILITY RULES
            return assetPlatform switch
            {
                "MT4" => instancePlatform == "MT4",
                "MT5" => instancePlatform == "MT5", 
                "TRADEREVOLUTION" => instancePlatform == "TRADEREVOLUTION",
                _ => false // Unknown platforms are incompatible
            };
        }

        /// <summary>
        /// Enhanced asset scanning with platform detection
        /// </summary>
        private void ScanAllAssets()
        {
            _assetMatrix.Clear();
            _assetPaths.Clear();
            _assetDetails.Clear();
            
            LogActivity("🔍 Scanning assets with platform detection...");
            
            foreach (var instance in _instances.Where(i => i.Enabled))
            {
                var instancePath = Path.Combine("PlatformInstances", instance.Name);
                if (!Directory.Exists(instancePath)) continue;
                
                LogActivity($"📂 Scanning {instance.Platform} instance: {instance.Name}");
                
                var assets = ScanInstanceAssets(instancePath, instance.Platform);
                
                foreach (var asset in assets)
                {
                    var assetKey = $"{instance.Platform}/{asset.Name}";
                    
                    // Store enhanced asset info with platform
                    if (!_assetDetails.ContainsKey(assetKey))
                    {
                        _assetDetails[assetKey] = asset;
                        _assetPaths[assetKey] = asset.FullPath;
                        _assetMatrix[assetKey] = new Dictionary<string, bool>();
                    }
                    
                    // Initialize compatibility matrix for ALL instances
                    foreach (var targetInstance in _instances.Where(i => i.Enabled))
                    {
                        var isCompatible = ArePlatformsCompatible(asset.Platform, targetInstance.Platform);
                        _assetMatrix[assetKey][targetInstance.Name] = false; // Default unchecked, but compatibility determines if enabled
                    }
                }
            }
            
            LogActivity($"✅ Scan complete: {_assetDetails.Count} assets found");
            LogActivity($"🔗 Platform distribution: MT4: {_assetDetails.Values.Count(a => a.Platform == "MT4")}, " +
                       $"MT5: {_assetDetails.Values.Count(a => a.Platform == "MT5")}, " +
                       $"TE: {_assetDetails.Values.Count(a => a.Platform == "TraderEvolution")}");
        }

        private List<AssetInfo> ScanInstanceAssets(string instancePath, string platform)
        {
            var assets = new List<AssetInfo>();
            
            try
            {
                var instance = _instances.First(i => i.Name == Path.GetFileName(instancePath));
                var assetFolders = instance.GetAssetFolders(instancePath);
                var extensions = instance.GetAssetExtensions();
                
                foreach (var folder in assetFolders.Where(Directory.Exists))
                {
                    var files = Directory.GetFiles(folder, "*.*", SearchOption.AllDirectories)
                        .Where(f => extensions.Any(ext => f.EndsWith(ext, StringComparison.OrdinalIgnoreCase)));
                    
                    foreach (var file in files)
                    {
                        var asset = AssetInfo.FromFile(file, platform);
                        if (asset != null)
                        {
                            assets.Add(asset);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogActivity($"⚠️ Error scanning {instancePath}: {ex.Message}");
            }
            
            return assets;
        }

        /// <summary>
        /// Enhanced matrix view with platform compatibility enforcement
        /// </summary>
        private void CreateMatrixView()
        {
            foreach (TabPage tabPage in _tabControl.TabPages)
            {
                var assetType = tabPage.Text.Replace(" ", "");
                var grid = CreateCompatibilityAwareGrid(assetType);
                tabPage.Controls.Add(grid);
            }
        }

        private DataGridView CreateCompatibilityAwareGrid(string assetType)
        {
            var grid = new DataGridView()
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.FromArgb(40, 40, 40),
                GridColor = Color.FromArgb(60, 60, 60),
                DefaultCellStyle = new DataGridViewCellStyle()
                {
                    BackColor = Color.FromArgb(40, 40, 40),
                    ForeColor = Color.FromArgb(240, 240, 240),
                    SelectionBackColor = Color.FromArgb(0, 120, 215),
                    SelectionForeColor = Color.White
                },
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle()
                {
                    BackColor = Color.FromArgb(60, 60, 60),
                    ForeColor = Color.FromArgb(240, 240, 240),
                    Font = new Font("Segoe UI", 9, FontStyle.Bold)
                },
                EnableHeadersVisualStyles = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RowHeadersWidth = 300 // Wider for platform info
            };
            
            // Enhanced asset name column with platform indicator
            var assetColumn = new DataGridViewTextBoxColumn()
            {
                Name = "Asset",
                HeaderText = "Asset Name [Platform]",
                Width = 280,
                ReadOnly = true
            };
            grid.Columns.Add(assetColumn);
            
            // Add instance columns with platform compatibility indicators
            foreach (var instance in _instances.Where(i => i.Enabled))
            {
                var instanceColumn = new DataGridViewCheckBoxColumn()
                {
                    Name = instance.Name,
                    HeaderText = $"{instance.Name}\n[{instance.Platform}]",
                    Width = 120,
                    TrueValue = true,
                    FalseValue = false
                };
                grid.Columns.Add(instanceColumn);
            }
            
            // Filter assets by type and populate rows with compatibility logic
            var filteredAssets = _assetDetails.Where(kvp => 
                GetAssetTypeFromPath(kvp.Value.FullPath) == assetType).ToList();
            
            foreach (var assetEntry in filteredAssets)
            {
                var asset = assetEntry.Value;
                var assetKey = assetEntry.Key;
                
                var row = new DataGridViewRow();
                
                // Asset name with platform indicator
                row.Cells.Add(new DataGridViewTextBoxCell { Value = $"{asset.Name} [{asset.Platform}]" });
                
                // Instance compatibility cells
                foreach (var instance in _instances.Where(i => i.Enabled))
                {
                    var isCompatible = ArePlatformsCompatible(asset.Platform, instance.Platform);
                    var cell = new DataGridViewCheckBoxCell()
                    {
                        Value = _assetMatrix[assetKey][instance.Name]
                    };
                    
                    // CRITICAL: Disable incompatible combinations
                    if (!isCompatible)
                    {
                        cell.ReadOnly = true;
                        cell.Style.BackColor = Color.FromArgb(60, 30, 30); // Dark red for incompatible
                        cell.Style.ForeColor = Color.Gray;
                        cell.ToolTipText = $"❌ Incompatible: {asset.Platform} asset cannot run on {instance.Platform} platform";
                    }
                    else
                    {
                        cell.Style.BackColor = Color.FromArgb(30, 60, 30); // Dark green for compatible
                        cell.ToolTipText = $"✅ Compatible: {asset.Platform} asset can run on {instance.Platform} platform";
                    }
                    
                    row.Cells.Add(cell);
                }
                
                grid.Rows.Add(row);
            }
            
            // Handle cell value changes with compatibility validation
            grid.CellValueChanged += (sender, e) =>
            {
                if (e.RowIndex >= 0 && e.ColumnIndex > 0)
                {
                    var assetName = grid.Rows[e.RowIndex].Cells[0].Value?.ToString();
                    if (assetName != null)
                    {
                        // Extract asset key from display name
                        var assetKey = _assetDetails.FirstOrDefault(kvp => 
                            ($"{kvp.Value.Name} [{kvp.Value.Platform}]") == assetName).Key;
                        
                        if (!string.IsNullOrEmpty(assetKey))
                        {
                            var instanceName = grid.Columns[e.ColumnIndex].Name;
                            var newValue = (bool)grid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value;
                            
                            // Validate compatibility before allowing change
                            var asset = _assetDetails[assetKey];
                            var instance = _instances.First(i => i.Name == instanceName);
                            
                            if (!ArePlatformsCompatible(asset.Platform, instance.Platform))
                            {
                                LogActivity($"❌ Blocked incompatible assignment: {asset.Platform} asset to {instance.Platform} instance");
                                grid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value = false;
                                return;
                            }
                            
                            _assetMatrix[assetKey][instanceName] = newValue;
                            LogActivity($"✅ {(newValue ? "Selected" : "Deselected")} compatible: {assetName} → {instanceName}");
                        }
                    }
                }
            };
            
            return grid;
        }

        private string GetAssetTypeFromPath(string fullPath)
        {
            var directory = Path.GetDirectoryName(fullPath)?.ToLower() ?? "";
            
            if (directory.Contains("experts") || directory.Contains("robots")) return "ExpertAdvisors";
            if (directory.Contains("indicators")) return "Indicators";
            if (directory.Contains("scripts")) return "Scripts";
            if (directory.Contains("templates") || directory.Contains("workspaces")) return "Templates";
            
            return "Other";
        }

        /// <summary>
        /// NEW: Select only compatible assets for each instance
        /// </summary>
        private void SelectCompatibleAssets()
        {
            var selectedCount = 0;
            
            foreach (var assetEntry in _assetDetails)
            {
                var assetKey = assetEntry.Key;
                var asset = assetEntry.Value;
                
                foreach (var instance in _instances.Where(i => i.Enabled))
                {
                    if (ArePlatformsCompatible(asset.Platform, instance.Platform))
                    {
                        _assetMatrix[assetKey][instance.Name] = true;
                        selectedCount++;
                    }
                }
            }
            
            RefreshGrids();
            LogActivity($"📋 Selected {selectedCount} compatible asset-instance combinations");
        }

        /// <summary>
        /// Enhanced sync with strict compatibility validation
        /// </summary>
        private void SyncCompatibleAssets()
        {
            try
            {
                var compatibleChanges = new List<string>();
                var blockedChanges = new List<string>();
                var changesCount = 0;
                
                foreach (var assetEntry in _assetMatrix)
                {
                    var assetKey = assetEntry.Key;
                    var asset = _assetDetails[assetKey];
                    
                    foreach (var instanceEntry in assetEntry.Value)
                    {
                        var instanceName = instanceEntry.Key;
                        var shouldDeploy = instanceEntry.Value;
                        var instance = _instances.First(i => i.Name == instanceName);
                        
                        // STRICT COMPATIBILITY CHECK
                        if (!ArePlatformsCompatible(asset.Platform, instance.Platform))
                        {
                            if (shouldDeploy)
                            {
                                blockedChanges.Add($"❌ BLOCKED: {asset.Name} [{asset.Platform}] → {instanceName} [{instance.Platform}]");
                            }
                            continue; // Skip incompatible combinations entirely
                        }
                        
                        // Check if asset exists in target
                        var targetPath = GetTargetAssetPath(asset, instance);
                        var exists = File.Exists(targetPath);
                        
                        if (shouldDeploy && !exists)
                        {
                            changesCount++;
                            compatibleChanges.Add($"✅ DEPLOY: {asset.Name} [{asset.Platform}] → {instanceName}");
                        }
                        else if (!shouldDeploy && exists)
                        {
                            changesCount++;
                            compatibleChanges.Add($"🗑️ REMOVE: {asset.Name} from {instanceName}");
                        }
                    }
                }
                
                if (blockedChanges.Any())
                {
                    var blockedText = string.Join("\n", blockedChanges.Take(5));
                    if (blockedChanges.Count > 5) blockedText += $"\n... and {blockedChanges.Count - 5} more blocked";
                    
                    MessageBox.Show($"Platform compatibility violations detected:\n\n{blockedText}\n\nThese operations were blocked to prevent runtime errors.", 
                        "Compatibility Violations", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                
                if (changesCount == 0)
                {
                    MessageBox.Show("No compatible changes detected. Asset matrix matches current compatible selections.", 
                        "No Compatible Changes", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                
                var changeText = string.Join("\n", compatibleChanges.Take(10));
                if (compatibleChanges.Count > 10)
                    changeText += $"\n... and {compatibleChanges.Count - 10} more compatible changes";
                
                var result = MessageBox.Show($"Ready to sync {changesCount} COMPATIBLE asset changes:\n\n{changeText}\n\nProceed with synchronization?", 
                    "Confirm Compatible Asset Sync", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                
                if (result == DialogResult.Yes)
                {
                    // TODO: Implement actual file copy/delete operations with platform validation
                    LogActivity($"📊 Compatible sync preview: {changesCount} changes identified");
                    LogActivity($"🚫 Blocked {blockedChanges.Count} incompatible operations");
                    LogActivity("⚠️ Actual sync implementation coming soon!");
                    
                    MessageBox.Show($"Compatible sync preview completed!\n\n✅ {changesCount} compatible changes identified\n❌ {blockedChanges.Count} incompatible operations blocked\n\nActual file operations will be implemented in next phase.", 
                        "Compatible Sync Preview", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                LogActivity($"❌ Error during compatible sync: {ex.Message}");
            }
        }

        private string GetTargetAssetPath(AssetInfo asset, TradingInstance instance)
        {
            var instancePath = Path.Combine("PlatformInstances", instance.Name);
            var relativePath = asset.FullPath.Substring(asset.FullPath.IndexOf("PlatformInstances") + "PlatformInstances".Length + 1);
            var instanceStartIndex = relativePath.IndexOf(Path.DirectorySeparatorChar) + 1;
            var assetRelativePath = relativePath.Substring(instanceStartIndex);
            
            return Path.Combine(instancePath, assetRelativePath);
        }

        /// <summary>
        /// Generate comprehensive compatibility report
        /// </summary>
        private void GenerateCompatibilityReport()
        {
            try
            {
                var reportText = "=== PLATFORM COMPATIBILITY MATRIX REPORT ===\n\n";
                reportText += $"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n";
                reportText += $"Total Assets: {_assetDetails.Count}\n";
                reportText += $"Active Instances: {_instances.Count(i => i.Enabled)}\n\n";
                
                // Platform distribution
                reportText += "PLATFORM DISTRIBUTION:\n";
                reportText += $"MT4 Assets: {_assetDetails.Values.Count(a => a.Platform == "MT4")}\n";
                reportText += $"MT5 Assets: {_assetDetails.Values.Count(a => a.Platform == "MT5")}\n";
                reportText += $"TraderEvolution Assets: {_assetDetails.Values.Count(a => a.Platform == "TraderEvolution")}\n\n";
                
                // Instance distribution  
                reportText += "INSTANCE DISTRIBUTION:\n";
                reportText += $"MT4 Instances: {_instances.Count(i => i.Enabled && i.Platform == "MT4")}\n";
                reportText += $"MT5 Instances: {_instances.Count(i => i.Enabled && i.Platform == "MT5")}\n";
                reportText += $"TraderEvolution Instances: {_instances.Count(i => i.Enabled && i.Platform == "TraderEvolution")}\n\n";
                
                // Compatibility matrix summary
                reportText += "COMPATIBILITY MATRIX:\n";
                reportText += "Asset Platform".PadRight(20) + "Compatible Instances".PadRight(20) + "Total Combinations\n";
                reportText += new string('-', 60) + "\n";
                
                foreach (var platformGroup in _assetDetails.Values.GroupBy(a => a.Platform))
                {
                    var platform = platformGroup.Key;
                    var assetCount = platformGroup.Count();
                    var compatibleInstances = _instances.Count(i => i.Enabled && i.Platform == platform);
                    var totalCombinations = assetCount * compatibleInstances;
                    
                    reportText += $"{platform}".PadRight(20) + $"{compatibleInstances}".PadRight(20) + $"{totalCombinations}\n";
                }
                
                reportText += "\n=== INCOMPATIBLE COMBINATIONS (BLOCKED) ===\n";
                var incompatibleCount = 0;
                foreach (var asset in _assetDetails.Values)
                {
                    foreach (var instance in _instances.Where(i => i.Enabled))
                    {
                        if (!ArePlatformsCompatible(asset.Platform, instance.Platform))
                        {
                            incompatibleCount++;
                        }
                    }
                }
                reportText += $"Total Blocked Combinations: {incompatibleCount}\n";
                reportText += "These combinations are automatically disabled to prevent runtime errors.\n\n";
                
                // Show the report
                var reportForm = new Form()
                {
                    Text = "Platform Compatibility Report",
                    Size = new Size(800, 600),
                    StartPosition = FormStartPosition.CenterParent,
                    BackColor = Color.FromArgb(30, 30, 30),
                    ForeColor = Color.FromArgb(240, 240, 240)
                };
                
                var textBox = new TextBox()
                {
                    Text = reportText,
                    Dock = DockStyle.Fill,
                    Multiline = true,
                    ReadOnly = true,
                    ScrollBars = ScrollBars.Both,
                    BackColor = Color.FromArgb(20, 20, 20),
                    ForeColor = Color.FromArgb(200, 200, 200),
                    Font = new Font("Consolas", 9)
                };
                
                reportForm.Controls.Add(textBox);
                reportForm.ShowDialog(this);
                
                LogActivity("📊 Platform compatibility report generated");
            }
            catch (Exception ex)
            {
                LogActivity($"❌ Error generating compatibility report: {ex.Message}");
            }
        }

        private void RefreshAssets()
        {
            LogActivity("🔄 Refreshing assets with platform compatibility validation...");
            ScanAllAssets();
            CreateMatrixView();
            LogActivity("✅ Refresh complete - compatibility matrix updated");
        }

        private void RefreshGrids()
        {
            // Refresh all grids to show updated selections
            foreach (TabPage tab in _tabControl.TabPages)
            {
                if (tab.Controls.Count > 0 && tab.Controls[0] is DataGridView grid)
                {
                    var assetType = tab.Text.Replace(" ", "");
                    var newGrid = CreateCompatibilityAwareGrid(assetType);
                    tab.Controls.Clear();
                    tab.Controls.Add(newGrid);
                }
            }
        }

        private void ClearAllSelections()
        {
            foreach (var assetKey in _assetMatrix.Keys.ToList())
            {
                foreach (var instanceName in _assetMatrix[assetKey].Keys.ToList())
                {
                    _assetMatrix[assetKey][instanceName] = false;
                }
            }
            
            RefreshGrids();
            LogActivity("❌ All selections cleared");
        }

        private void LogActivity(string message)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            var logEntry = $"[{timestamp}] {message}";
            
            _activityLog?.Items.Add(logEntry);
            if (_activityLog?.Items.Count > 100)
            {
                _activityLog.Items.RemoveAt(0);
            }
            
            _activityLog?.TopIndex = Math.Max(0, _activityLog.Items.Count - _activityLog.ClientSize.Height / _activityLog.ItemHeight);
            _statusLabel.Text = message;
        }
    }
}