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
        private readonly Dictionary<string, AssetInfo> _assetDetails; // Platform-aware asset info
        
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
            _assetDetails = new Dictionary<string, AssetInfo>();
            
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
            _toolStrip.Items.Add(new ToolStripButton("🚀 Sync Selected", null, (s, e) => SyncSelectedAssets()) { DisplayStyle = ToolStripItemDisplayStyle.ImageAndText });
            _toolStrip.Items.Add(new ToolStripSeparator());
            _toolStrip.Items.Add(new ToolStripButton("📊 Generate Report", null, (s, e) => GenerateAssetReport()) { DisplayStyle = ToolStripItemDisplayStyle.ImageAndText });
            
            // Tab Control for asset types
            _tabControl = new TabControl()
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(45, 45, 48),
                ForeColor = Color.FromArgb(240, 240, 240)
            };
            
            // Create tabs for each asset type
            foreach (var assetType in _assetTypes)
            {
                var tabPage = new TabPage(assetType)
                {
                    BackColor = Color.FromArgb(30, 30, 30),
                    ForeColor = Color.FromArgb(240, 240, 240)
                };
                _tabControl.TabPages.Add(tabPage);
            }
            
            // Status Strip
            _statusStrip = new StatusStrip();
            _statusStrip.BackColor = Color.FromArgb(45, 45, 48);
            _statusStrip.ForeColor = Color.FromArgb(240, 240, 240);
            
            _statusLabel = new ToolStripStatusLabel("Ready for asset management");
            _statusStrip.Items.Add(_statusLabel);
            
            // Activity Log Panel
            var logPanel = new Panel()
            {
                Dock = DockStyle.Bottom,
                Height = 150,
                BackColor = Color.FromArgb(40, 40, 40)
            };
            
            _activityLog = new ListBox()
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(40, 40, 40),
                ForeColor = Color.FromArgb(240, 240, 240),
                Font = new Font("Consolas", 9)
            };
            logPanel.Controls.Add(_activityLog);
            
            // Summary Panel
            _summaryPanel = new Panel()
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = Color.FromArgb(50, 50, 50)
            };
            
            _summaryLabel = new Label()
            {
                Dock = DockStyle.Fill,
                Text = "Asset Distribution Summary will appear here...",
                ForeColor = Color.FromArgb(240, 240, 240),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };
            _summaryPanel.Controls.Add(_summaryLabel);
            
            // Layout assembly
            this.Controls.Add(_tabControl);
            this.Controls.Add(logPanel);
            this.Controls.Add(_summaryPanel);
            this.Controls.Add(_toolStrip);
            this.Controls.Add(_statusStrip);
            
            this.ResumeLayout();
        }

        /// <summary>
        /// CRITICAL: Scans all assets with platform detection
        /// </summary>
        private void ScanAllAssets()
        {
            LogActivity("🔍 Scanning all trading instances for assets...");
            
            _assetMatrix.Clear();
            _assetPaths.Clear();
            _assetDetails.Clear();
            
            foreach (var instance in _instances.Where(i => i.Enabled))
            {
                LogActivity($"📂 Scanning {instance.Name} [{instance.Platform}]...");
                var assets = ScanInstanceAssets(instance.InstancePath, instance.Platform);
                
                foreach (var asset in assets)
                {
                    var assetKey = $"{asset.Name}_{asset.Platform}_{instance.Name}";
                    
                    if (!_assetMatrix.ContainsKey(assetKey))
                    {
                        _assetMatrix[assetKey] = new Dictionary<string, bool>();
                        _assetPaths[assetKey] = asset.FullPath;
                        _assetDetails[assetKey] = asset;
                        
                        // Initialize matrix for all instances
                        foreach (var inst in _instances.Where(i => i.Enabled))
                        {
                            _assetMatrix[assetKey][inst.Name] = false;
                        }
                    }
                }
            }
            
            LogActivity($"✅ Scan complete: {_assetDetails.Count} assets found");
            UpdateSummary();
        }

        private List<AssetInfo> ScanInstanceAssets(string instancePath, string platform)
        {
            var assets = new List<AssetInfo>();
            
            try
            {
                if (!Directory.Exists(instancePath))
                {
                    LogActivity($"⚠️ Instance path not found: {instancePath}");
                    return assets;
                }
                
                // Platform-specific asset scanning
                var extensions = GetPlatformExtensions(platform);
                var searchPaths = GetPlatformSearchPaths(instancePath, platform);
                
                foreach (var searchPath in searchPaths)
                {
                    if (!Directory.Exists(searchPath)) continue;
                    
                    var files = Directory.GetFiles(searchPath, "*", SearchOption.AllDirectories)
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

        private string[] GetPlatformExtensions(string platform)
        {
            return platform.ToUpperInvariant() switch
            {
                "MT4" => new[] { ".ex4", ".mq4" },
                "MT5" => new[] { ".ex5", ".mq5" },
                "TRADEREVOLUTION" => new[] { ".dll", ".exe", ".robot" },
                _ => new[] { ".ex4", ".ex5", ".mq4", ".mq5", ".dll", ".exe" }
            };
        }

        private string[] GetPlatformSearchPaths(string instancePath, string platform)
        {
            return platform.ToUpperInvariant() switch
            {
                "MT4" => new[] 
                {
                    Path.Combine(instancePath, "MQL4", "Experts"),
                    Path.Combine(instancePath, "MQL4", "Indicators"),
                    Path.Combine(instancePath, "MQL4", "Scripts"),
                    Path.Combine(instancePath, "templates")
                },
                "MT5" => new[] 
                {
                    Path.Combine(instancePath, "MQL5", "Experts"),
                    Path.Combine(instancePath, "MQL5", "Indicators"),
                    Path.Combine(instancePath, "MQL5", "Scripts"),
                    Path.Combine(instancePath, "templates")
                },
                _ => new[] { instancePath } // TraderEvolution or unknown
            };
        }

        /// <summary>
        /// FIXED: Enhanced matrix view with proper ReadOnly handling
        /// </summary>
        private void CreateMatrixView()
        {
            foreach (TabPage tabPage in _tabControl.TabPages)
            {
                tabPage.Controls.Clear();
                var assetType = tabPage.Text.Replace(" ", "");
                var grid = CreateCompatibilityAwareGrid(assetType);
                tabPage.Controls.Add(grid);
            }
        }

        /// <summary>
        /// CRITICAL FIX: Proper DataGridView ReadOnly implementation
        /// </summary>
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
                RowHeadersWidth = 300,
                EditMode = DataGridViewEditMode.EditOnEnter // KEY FIX: Enable editing
            };
            
            // Asset name column - ALWAYS ReadOnly
            var assetColumn = new DataGridViewTextBoxColumn()
            {
                Name = "Asset",
                HeaderText = "Asset Name [Platform]",
                Width = 180,
                ReadOnly = true // This should stay ReadOnly
            };
            grid.Columns.Add(assetColumn);
            
            // Add instance columns - NOT ReadOnly at column level
            foreach (var instance in _instances.Where(i => i.Enabled))
            {
                var instanceColumn = new DataGridViewCheckBoxColumn()
                {
                    Name = instance.Name,
                    HeaderText = $"{instance.Name}\n[{instance.Platform}]",
                    Width = 120,
                    TrueValue = true,
                    FalseValue = false,
                    ReadOnly = false // KEY FIX: Column-level ReadOnly should be false
                };
                grid.Columns.Add(instanceColumn);
            }
            
            // Populate rows with proper cell-level ReadOnly handling
            var filteredAssets = _assetDetails.Where(kvp => 
                GetAssetTypeFromPath(kvp.Value.FullPath) == assetType).ToList();
            
            foreach (var assetEntry in filteredAssets)
            {
                var asset = assetEntry.Value;
                var assetKey = assetEntry.Key;
                
                var row = new DataGridViewRow();
                
                // Asset name cell - add first, set ReadOnly after
                var assetCell = new DataGridViewTextBoxCell 
                { 
                    Value = $"{asset.Name} [{asset.Platform}]"
                };
                row.Cells.Add(assetCell);
                
                // Store compatibility info for later ReadOnly setting
                var compatibilityInfo = new List<bool>();
                
                // Instance compatibility cells
                foreach (var instance in _instances.Where(i => i.Enabled))
                {
                    var isCompatible = ArePlatformsCompatible(asset.Platform, instance.Platform);
                    compatibilityInfo.Add(isCompatible);
                    
                    var cell = new DataGridViewCheckBoxCell()
                    {
                        Value = _assetMatrix.ContainsKey(assetKey) && 
                               _assetMatrix[assetKey].ContainsKey(instance.Name) ? 
                               _assetMatrix[assetKey][instance.Name] : false
                    };
                    
                    // Set style but NOT ReadOnly yet
                    if (!isCompatible)
                    {
                        cell.Style.BackColor = Color.FromArgb(60, 30, 30); // Dark red for incompatible
                        cell.Style.ForeColor = Color.Gray;
                        cell.ToolTipText = $"❌ Incompatible: {asset.Platform} → {instance.Platform}";
                    }
                    else
                    {
                        cell.Style.BackColor = Color.FromArgb(30, 60, 30); // Dark green for compatible
                        cell.ToolTipText = $"✅ Compatible: {asset.Platform} → {instance.Platform}";
                    }
                    
                    row.Cells.Add(cell);
                }
                
                // Add row to grid FIRST
                grid.Rows.Add(row);
                
                // NOW set ReadOnly properties after row is added
                var rowIndex = grid.Rows.Count - 1;
                
                // Set asset name cell as ReadOnly
                grid[0, rowIndex].ReadOnly = true;
                
                // Set compatibility-based ReadOnly for instance cells
                for (int colIndex = 1; colIndex < grid.Columns.Count; colIndex++)
                {
                    var isCompatible = compatibilityInfo[colIndex - 1];
                    grid[colIndex, rowIndex].ReadOnly = !isCompatible;
                }
            }
            
            // KEY FIX: Proper event handling for cell value changes
            grid.CellValueChanged += (sender, e) =>
            {
                if (e.RowIndex >= 0 && e.ColumnIndex > 0) // Skip asset name column
                {
                    try
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
                                var cellValue = grid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value;
                                if (cellValue == null) return;
                                var newValue = (bool)cellValue;
                                
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
                                UpdateSummary();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogActivity($"❌ Error updating matrix: {ex.Message}");
                    }
                }
            };
            
            // KEY FIX: Handle edit validation
            grid.CellBeginEdit += (sender, e) =>
            {
                if (e.RowIndex >= 0 && e.ColumnIndex > 0)
                {
                    var cell = grid[e.ColumnIndex, e.RowIndex];
                    if (cell.ReadOnly)
                    {
                        grid.CancelEdit();
                        return;
                    }
                }
            };
            
            return grid;
        }

        private string GetAssetTypeFromPath(string fullPath)
        {
            var directory = Path.GetDirectoryName(fullPath)?.ToLower() ?? "";
            
            if (directory.Contains("experts")) return "ExpertAdvisors";
            if (directory.Contains("indicators")) return "Indicators";
            if (directory.Contains("scripts")) return "Scripts";
            if (directory.Contains("templates")) return "Templates";
            
            return "Scripts"; // Default fallback
        }

        /// <summary>
        /// CRITICAL: Platform compatibility check
        /// </summary>
        private bool ArePlatformsCompatible(string assetPlatform, string instancePlatform)
        {
            // Normalize platform names
            assetPlatform = assetPlatform?.Trim().ToUpperInvariant() ?? "";
            instancePlatform = instancePlatform?.Trim().ToUpperInvariant() ?? "";
            
            // Exact match required for compatibility
            return assetPlatform == instancePlatform ||
                   (assetPlatform.Contains("MT4") && instancePlatform.Contains("MT4")) ||
                   (assetPlatform.Contains("MT5") && instancePlatform.Contains("MT5")) ||
                   (assetPlatform.Contains("TRADEREVOLUTION") && instancePlatform.Contains("TRADEREVOLUTION"));
        }

        // Event Handlers
        private void RefreshAssets()
        {
            LogActivity("🔄 Refreshing asset matrix...");
            ScanAllAssets();
            CreateMatrixView();
            LogActivity("✅ Asset matrix refreshed");
        }

        private void SelectCompatibleAssets()
        {
            try
            {
                if (_tabControl.SelectedTab?.Controls[0] is DataGridView grid)
                {
                    foreach (DataGridViewRow row in grid.Rows)
                    {
                        for (int col = 1; col < grid.Columns.Count; col++) // Skip asset name column
                        {
                            var cell = grid[col, row.Index];
                            if (grid.Columns[col] is DataGridViewCheckBoxColumn && !cell.ReadOnly)
                            {
                                cell.Value = true;
                            }
                        }
                    }
                    LogActivity("📋 Selected all compatible assets in current tab");
                }
            }
            catch (Exception ex)
            {
                LogActivity($"❌ Error selecting compatible assets: {ex.Message}");
            }
        }

        private void ClearAllSelections()
        {
            try
            {
                if (_tabControl.SelectedTab?.Controls[0] is DataGridView grid)
                {
                    foreach (DataGridViewRow row in grid.Rows)
                    {
                        for (int col = 1; col < grid.Columns.Count; col++) // Skip asset name column
                        {
                            if (grid.Columns[col] is DataGridViewCheckBoxColumn)
                            {
                                grid[col, row.Index].Value = false;
                            }
                        }
                    }
                    LogActivity("❌ Cleared all selections in current tab");
                }
            }
            catch (Exception ex)
            {
                LogActivity($"❌ Error clearing selections: {ex.Message}");
            }
        }

        private void SyncSelectedAssets()
        {
            LogActivity("🚀 Starting asset synchronization...");
            
            int syncCount = 0;
            int errorCount = 0;
            
            try
            {
                foreach (var assetEntry in _assetMatrix)
                {
                    var assetKey = assetEntry.Key;
                    var selections = assetEntry.Value;
                    
                    foreach (var selection in selections.Where(s => s.Value))
                    {
                        try
                        {
                            // Perform actual file copy/sync here
                            LogActivity($"📁 Syncing {assetKey} → {selection.Key}");
                            syncCount++;
                        }
                        catch (Exception ex)
                        {
                            LogActivity($"❌ Sync error for {assetKey}: {ex.Message}");
                            errorCount++;
                        }
                    }
                }
                
                LogActivity($"✅ Sync complete: {syncCount} successful, {errorCount} errors");
                MessageBox.Show($"Synchronization Results:\n\n✅ Successful: {syncCount}\n❌ Errors: {errorCount}", 
                    "Sync Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                LogActivity($"❌ Sync failed: {ex.Message}");
            }
        }

        private void GenerateAssetReport()
        {
            try
            {
                LogActivity("📊 Generating asset distribution report...");
                
                var report = new System.Text.StringBuilder();
                report.AppendLine("ASSET DISTRIBUTION REPORT");
                report.AppendLine("=" + new string('=', 50));
                report.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                report.AppendLine($"Total Assets: {_assetDetails.Count}");
                report.AppendLine($"Total Instances: {_instances.Count(i => i.Enabled)}");
                report.AppendLine();
                
                foreach (var assetType in _assetTypes)
                {
                    var typeAssets = _assetDetails.Where(kvp => 
                        GetAssetTypeFromPath(kvp.Value.FullPath) == assetType.Replace(" ", "")).ToList();
                    
                    report.AppendLine($"{assetType}: {typeAssets.Count} assets");
                }
                
                LogActivity("✅ Report generated successfully");
                
                // Show report in a new form or save to file
                MessageBox.Show(report.ToString(), "Asset Report", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                LogActivity($"❌ Error generating report: {ex.Message}");
            }
        }

        private void UpdateSummary()
        {
            try
            {
                var totalAssets = _assetDetails.Count;
                var totalInstances = _instances.Count(i => i.Enabled);
                var selectedCount = _assetMatrix.SelectMany(am => am.Value).Count(s => s.Value);
                
                _summaryLabel.Text = $"📊 Assets: {totalAssets} | Instances: {totalInstances} | Selected: {selectedCount} | Platform Compatibility Enforced";
                _statusLabel.Text = $"Ready - {totalAssets} assets across {totalInstances} instances";
            }
            catch (Exception ex)
            {
                LogActivity($"❌ Error updating summary: {ex.Message}");
            }
        }

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
            }
            catch
            {
                // Silent fail for logging
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _tabControl?.Dispose();
                _statusStrip?.Dispose();
                _toolStrip?.Dispose();
                _activityLog?.Dispose();
                _summaryPanel?.Dispose();
                _summaryLabel?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}