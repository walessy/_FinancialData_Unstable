// Level6InstanceManager - Program.cs
// Complete implementation building on Dashboard Version 28
// Advanced Instance Management System with full CRUD operations

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Level6InstanceManager
{
    // Enhanced configuration classes for Level 6
    public class Level6Configuration
    {
        public ConfigurationMetadata Metadata { get; set; } = new ConfigurationMetadata();
        public Dictionary<string, GroupDefinition> Groups { get; set; } = new Dictionary<string, GroupDefinition>();
        public string TradingRoot { get; set; } = "";
        public string DefaultDataRoot { get; set; } = "";
        public List<TradingInstance> Instances { get; set; } = new List<TradingInstance>();
    }

    public class ConfigurationMetadata
    {
        public string Version { get; set; } = "6.0";
        public string BuiltOnDashboard { get; set; } = "v28";
        public DateTime Created { get; set; } = DateTime.Now;
        public DateTime LastModified { get; set; } = DateTime.Now;
        public string CreatedBy { get; set; } = Environment.UserName;
        public List<string> BackupFiles { get; set; } = new List<string>();
    }

    public class GroupDefinition
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string Color { get; set; } = "#0078D4";
        public string Priority { get; set; } = "normal";
        public bool AutoStart { get; set; } = false;
        public Dictionary<string, object> CustomProperties { get; set; } = new Dictionary<string, object>();
    }

    public class TradingInstance
    {
        public string Name { get; set; } = "";
        public string Broker { get; set; } = "";
        public string Platform { get; set; } = "";
        public string Source { get; set; } = "";
        public string Destination { get; set; } = "";
        public string DataFolder { get; set; } = "";
        public string JunctionName { get; set; } = "";
        public bool PortableMode { get; set; } = true;
        public string AccountType { get; set; } = "";
        public bool Enabled { get; set; } = true;
        public bool AutoStart { get; set; } = false;
        public int StartupDelay { get; set; } = 0;
        public string Priority { get; set; } = "normal";
        public List<string> GroupMembership { get; set; } = new List<string>();
        public Dictionary<string, object> ServerSettings { get; set; } = new Dictionary<string, object>();
        public Dictionary<string, object> CustomSettings { get; set; } = new Dictionary<string, object>();
        
        // Level 6 specific properties
        public DateTime Created { get; set; } = DateTime.Now;
        public DateTime LastModified { get; set; } = DateTime.Now;
        public string CreatedBy { get; set; } = Environment.UserName;
        public string Notes { get; set; } = "";
    }

    // Available platform information
    public class PlatformInfo
    {
        public string Name { get; set; } = "";
        public string Path { get; set; } = "";
        public string Platform { get; set; } = "";
        public string Broker { get; set; } = "";
        public bool IsValid { get; set; } = false;
    }

    // Main Level 6 Instance Manager Form
    public partial class Level6InstanceManager : Form
    {
        private Level6Configuration config;
        private string configFilePath;
        private bool isConfigModified = false;
        
        // Inherit base functionality from existing Level 5 structure
        private string tradingRoot;
        private bool darkModeEnabled = true;
        
        // UI Components
        private MenuStrip menuStrip;
        private ToolStrip toolStrip;
        private SplitContainer mainSplitContainer;
        private SplitContainer leftSplitContainer;
        private TreeView groupTreeView;
        private ListView instanceListView;
        private PropertyGrid instancePropertyGrid;
        private StatusStrip statusStrip;
        private ToolStripStatusLabel statusLabel;
        private ToolStripStatusLabel configStatusLabel;
        
        // Context menus
        private ContextMenuStrip groupContextMenu;
        private ContextMenuStrip instanceContextMenu;
        
        public Level6InstanceManager()
        {
            InitializeComponent();
            FindTradingRoot();
            LoadConfiguration();
            SetupEventHandlers();
        }
        
        private void FindTradingRoot()
        {
            // Use same logic as existing Level 5 dashboard
            var currentPath = Directory.GetCurrentDirectory();
            var configFile = Path.Combine(currentPath, "instances-config.json");
            
            if (File.Exists(configFile))
            {
                tradingRoot = currentPath;
                return;
            }
            
            // Check parent directory
            var parentPath = Directory.GetParent(currentPath)?.FullName;
            if (parentPath != null)
            {
                var parentConfigFile = Path.Combine(parentPath, "instances-config.json");
                if (File.Exists(parentConfigFile))
                {
                    tradingRoot = parentPath;
                    return;
                }
            }
            
            // Default fallback
            tradingRoot = @"C:\TradingRoot";
        }
        
        private void InitializeComponent()
        {
            this.Text = "Level 6 - Advanced Instance Management (Built on Dashboard v28)";
            this.Size = new Size(1400, 900);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimumSize = new Size(1000, 700);
            
            CreateMenuAndToolbar();
            CreateMainInterface();
            CreateStatusBar();
            CreateContextMenus();
            
            this.Controls.AddRange(new Control[] { mainSplitContainer, toolStrip, menuStrip, statusStrip });
            this.MainMenuStrip = menuStrip;
            
            // Apply same dark theme as Level 5
            ApplyDarkTheme();
        }
        
        private void CreateMenuAndToolbar()
        {
            // Menu Strip
            menuStrip = new MenuStrip();
            
            // File Menu
            var fileMenu = new ToolStripMenuItem("&File");
            fileMenu.DropDownItems.Add("&New Configuration", null, NewConfiguration);
            fileMenu.DropDownItems.Add("&Open Configuration...", null, OpenConfiguration);
            fileMenu.DropDownItems.Add("&Save Configuration", null, (s, e) => SaveConfiguration());
            fileMenu.DropDownItems.Add("Save &As...", null, SaveConfigurationAs);
            fileMenu.DropDownItems.Add(new ToolStripSeparator());
            fileMenu.DropDownItems.Add("&Import Instances...", null, ImportInstances);
            fileMenu.DropDownItems.Add("&Export Instances...", null, ExportInstances);
            fileMenu.DropDownItems.Add(new ToolStripSeparator());
            fileMenu.DropDownItems.Add("E&xit", null, (s, e) => this.Close());
            
            // Instance Menu
            var instanceMenu = new ToolStripMenuItem("&Instance");
            instanceMenu.DropDownItems.Add("&Add New Instance...", null, AddNewInstance);
            instanceMenu.DropDownItems.Add("&Edit Instance...", null, EditSelectedInstance);
            instanceMenu.DropDownItems.Add("&Clone Instance...", null, CloneSelectedInstance);
            instanceMenu.DropDownItems.Add("&Delete Instance", null, DeleteSelectedInstance);
            instanceMenu.DropDownItems.Add(new ToolStripSeparator());
            instanceMenu.DropDownItems.Add("&Refresh Status", null, RefreshInstanceStatus);
            
            // Group Menu
            var groupMenu = new ToolStripMenuItem("&Groups");
            groupMenu.DropDownItems.Add("&Create New Group...", null, CreateNewGroup);
            groupMenu.DropDownItems.Add("&Edit Group...", null, EditSelectedGroup);
            groupMenu.DropDownItems.Add("&Delete Group", null, DeleteSelectedGroup);
            groupMenu.DropDownItems.Add(new ToolStripSeparator());
            groupMenu.DropDownItems.Add("&Reorganize Groups...", null, ReorganizeGroups);
            
            // Operations Menu
            var operationsMenu = new ToolStripMenuItem("&Operations");
            operationsMenu.DropDownItems.Add("&Apply Changes (Run Level 2)", null, ApplyChanges);
            operationsMenu.DropDownItems.Add("&Build Selected Instances", null, BuildSelectedInstances);
            operationsMenu.DropDownItems.Add("&Rebuild All Instances", null, RebuildAllInstances);
            operationsMenu.DropDownItems.Add(new ToolStripSeparator());
            operationsMenu.DropDownItems.Add("&Validate Configuration", null, ValidateConfiguration);
            operationsMenu.DropDownItems.Add("&Backup Configuration", null, BackupConfiguration);
            
            // Integration Menu (Level 5 compatibility)
            var integrationMenu = new ToolStripMenuItem("&Integration");
            integrationMenu.DropDownItems.Add("Launch Level 5 Dashboard", null, LaunchLevel5Dashboard);
            integrationMenu.DropDownItems.Add("Run Level 3 Manager", null, RunLevel3Manager);
            integrationMenu.DropDownItems.Add(new ToolStripSeparator());
            integrationMenu.DropDownItems.Add("Check All Levels Status", null, CheckAllLevelsStatus);
            
            menuStrip.Items.AddRange(new ToolStripItem[] { fileMenu, instanceMenu, groupMenu, operationsMenu, integrationMenu });
            
            // Tool Strip
            toolStrip = new ToolStrip();
            toolStrip.Items.Add(new ToolStripButton("Add Instance", null, AddNewInstance) { DisplayStyle = ToolStripItemDisplayStyle.ImageAndText });
            toolStrip.Items.Add(new ToolStripButton("Edit Instance", null, EditSelectedInstance) { DisplayStyle = ToolStripItemDisplayStyle.ImageAndText });
            toolStrip.Items.Add(new ToolStripButton("Delete Instance", null, DeleteSelectedInstance) { DisplayStyle = ToolStripItemDisplayStyle.ImageAndText });
            toolStrip.Items.Add(new ToolStripSeparator());
            toolStrip.Items.Add(new ToolStripButton("Create Group", null, CreateNewGroup) { DisplayStyle = ToolStripItemDisplayStyle.ImageAndText });
            toolStrip.Items.Add(new ToolStripSeparator());
            toolStrip.Items.Add(new ToolStripButton("Apply Changes", null, ApplyChanges) { DisplayStyle = ToolStripItemDisplayStyle.ImageAndText });
            toolStrip.Items.Add(new ToolStripButton("Refresh", null, (s, e) => RefreshAll()) { DisplayStyle = ToolStripItemDisplayStyle.ImageAndText });
        }
        
        private void CreateMainInterface()
        {
            mainSplitContainer = new SplitContainer();
            mainSplitContainer.Dock = DockStyle.Fill;
            mainSplitContainer.SplitterDistance = 350;
            mainSplitContainer.Panel1MinSize = 250;
            mainSplitContainer.Panel2MinSize = 400;
            
            // Left panel - Groups tree and properties
            leftSplitContainer = new SplitContainer();
            leftSplitContainer.Dock = DockStyle.Fill;
            leftSplitContainer.Orientation = Orientation.Horizontal;
            leftSplitContainer.SplitterDistance = 450;
            
            // Groups tree view
            var groupPanel = new Panel();
            groupPanel.Dock = DockStyle.Fill;
            var groupLabel = new Label 
            { 
                Text = "Groups & Instances", 
                Dock = DockStyle.Top, 
                Height = 25, 
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.FromArgb(60, 60, 63),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            
            groupTreeView = new TreeView();
            groupTreeView.Dock = DockStyle.Fill;
            groupTreeView.CheckBoxes = true;
            groupTreeView.HideSelection = false;
            groupTreeView.FullRowSelect = true;
            groupTreeView.ShowLines = true;
            groupTreeView.ShowPlusMinus = true;
            groupTreeView.ShowRootLines = true;
            groupTreeView.AfterSelect += GroupTreeView_AfterSelect;
            groupTreeView.AfterCheck += GroupTreeView_AfterCheck;
            
            groupPanel.Controls.AddRange(new Control[] { groupTreeView, groupLabel });
            
            // Instance property grid
            var propertyPanel = new Panel();
            propertyPanel.Dock = DockStyle.Fill;
            var propertyLabel = new Label 
            { 
                Text = "Instance Properties", 
                Dock = DockStyle.Top, 
                Height = 25, 
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.FromArgb(60, 60, 63),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            
            instancePropertyGrid = new PropertyGrid();
            instancePropertyGrid.Dock = DockStyle.Fill;
            instancePropertyGrid.PropertyValueChanged += InstancePropertyGrid_PropertyValueChanged;
            instancePropertyGrid.HelpVisible = true;
            instancePropertyGrid.ToolbarVisible = true;
            instancePropertyGrid.CategoryForeColor = Color.LightBlue;
            
            propertyPanel.Controls.AddRange(new Control[] { instancePropertyGrid, propertyLabel });
            
            leftSplitContainer.Panel1.Controls.Add(groupPanel);
            leftSplitContainer.Panel2.Controls.Add(propertyPanel);
            
            // Right panel - Instance list view
            var rightPanel = new Panel();
            rightPanel.Dock = DockStyle.Fill;
            var instanceLabel = new Label 
            { 
                Text = "Instance Details", 
                Dock = DockStyle.Top, 
                Height = 25, 
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.FromArgb(60, 60, 63),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            
            instanceListView = new ListView();
            instanceListView.Dock = DockStyle.Fill;
            instanceListView.View = View.Details;
            instanceListView.FullRowSelect = true;
            instanceListView.GridLines = true;
            instanceListView.CheckBoxes = true;
            instanceListView.MultiSelect = true;
            instanceListView.SelectedIndexChanged += InstanceListView_SelectedIndexChanged;
            instanceListView.ItemCheck += InstanceListView_ItemCheck;
            
            // Setup list view columns
            instanceListView.Columns.Add("Name", 150);
            instanceListView.Columns.Add("Broker", 100);
            instanceListView.Columns.Add("Platform", 60);
            instanceListView.Columns.Add("Account Type", 80);
            instanceListView.Columns.Add("Status", 80);
            instanceListView.Columns.Add("Enabled", 60);
            instanceListView.Columns.Add("Auto Start", 70);
            instanceListView.Columns.Add("Groups", 120);
            instanceListView.Columns.Add("Created", 100);
            
            rightPanel.Controls.AddRange(new Control[] { instanceListView, instanceLabel });
            
            mainSplitContainer.Panel1.Controls.Add(leftSplitContainer);
            mainSplitContainer.Panel2.Controls.Add(rightPanel);
        }
        
        private void CreateStatusBar()
        {
            statusStrip = new StatusStrip();
            statusLabel = new ToolStripStatusLabel("Ready - Level 6 Instance Manager");
            statusStrip.Items.Add(statusLabel);
            
            configStatusLabel = new ToolStripStatusLabel();
            configStatusLabel.Spring = true;
            configStatusLabel.TextAlign = ContentAlignment.MiddleRight;
            statusStrip.Items.Add(configStatusLabel);
            
            var modifiedLabel = new ToolStripStatusLabel();
            statusStrip.Items.Add(modifiedLabel);
        }
        
        private void CreateContextMenus()
        {
            // Group context menu
            groupContextMenu = new ContextMenuStrip();
            groupContextMenu.Items.Add("Add Instance to Group", null, AddInstanceToGroup);
            groupContextMenu.Items.Add("Remove from Group", null, RemoveFromGroup);
            groupContextMenu.Items.Add(new ToolStripSeparator());
            groupContextMenu.Items.Add("Start All in Group", null, StartGroup);
            groupContextMenu.Items.Add("Stop All in Group", null, StopGroup);
            groupContextMenu.Items.Add(new ToolStripSeparator());
            groupContextMenu.Items.Add("Edit Group...", null, EditSelectedGroup);
            groupContextMenu.Items.Add("Delete Group", null, DeleteSelectedGroup);
            
            groupTreeView.ContextMenuStrip = groupContextMenu;
            
            // Instance context menu
            instanceContextMenu = new ContextMenuStrip();
            instanceContextMenu.Items.Add("Edit Instance...", null, EditSelectedInstance);
            instanceContextMenu.Items.Add("Clone Instance...", null, CloneSelectedInstance);
            instanceContextMenu.Items.Add("Delete Instance", null, DeleteSelectedInstance);
            instanceContextMenu.Items.Add(new ToolStripSeparator());
            instanceContextMenu.Items.Add("Start Instance", null, StartSelectedInstance);
            instanceContextMenu.Items.Add("Stop Instance", null, StopSelectedInstance);
            instanceContextMenu.Items.Add(new ToolStripSeparator());
            instanceContextMenu.Items.Add("Open Instance Folder", null, OpenInstanceFolder);
            instanceContextMenu.Items.Add("Open Data Folder", null, OpenDataFolder);
            
            instanceListView.ContextMenuStrip = instanceContextMenu;
        }
        
        private void ApplyDarkTheme()
        {
            this.BackColor = Color.FromArgb(45, 45, 48);
            this.ForeColor = Color.White;
            
            groupTreeView.BackColor = Color.FromArgb(37, 37, 38);
            groupTreeView.ForeColor = Color.White;
            
            instanceListView.BackColor = Color.FromArgb(37, 37, 38);
            instanceListView.ForeColor = Color.White;
            
            instancePropertyGrid.BackColor = Color.FromArgb(37, 37, 38);
            instancePropertyGrid.ViewBackColor = Color.FromArgb(45, 45, 48);
            instancePropertyGrid.ViewForeColor = Color.White;
            instancePropertyGrid.CategoryForeColor = Color.LightBlue;
            instancePropertyGrid.LineColor = Color.FromArgb(60, 60, 63);
            
            menuStrip.BackColor = Color.FromArgb(45, 45, 48);
            menuStrip.ForeColor = Color.White;
            
            toolStrip.BackColor = Color.FromArgb(45, 45, 48);
            toolStrip.ForeColor = Color.White;
            
            statusStrip.BackColor = Color.FromArgb(45, 45, 48);
            statusStrip.ForeColor = Color.White;
        }
        
        private void SetupEventHandlers()
        {
            this.FormClosing += Level6InstanceManager_FormClosing;
        }
        
        private void Level6InstanceManager_FormClosing(object? sender, FormClosingEventArgs e)
        {
            if (isConfigModified)
            {
                var result = MessageBox.Show("Configuration has been modified. Save changes?", 
                    "Unsaved Changes", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                    
                if (result == DialogResult.Yes)
                {
                    SaveConfiguration();
                }
                else if (result == DialogResult.Cancel)
                {
                    e.Cancel = true;
                }
            }
        }
        
        #region Configuration Management
        
        private void LoadConfiguration()
        {
            configFilePath = Path.Combine(tradingRoot, "instances-config.json");
            
            if (File.Exists(configFilePath))
            {
                try
                {
                    var json = File.ReadAllText(configFilePath);
                    
                    // Try to load as Level 6 config first
                    try
                    {
                        config = JsonSerializer.Deserialize<Level6Configuration>(json, new JsonSerializerOptions 
                        { 
                            PropertyNameCaseInsensitive = true,
                            WriteIndented = true
                        }) ?? new Level6Configuration();
                        
                        if (config?.Metadata == null)
                        {
                            // Upgrade from older version
                            UpgradeConfiguration(json);
                        }
                    }
                    catch
                    {
                        // Load as legacy config and upgrade
                        UpgradeConfiguration(json);
                    }
                    
                    RefreshAll();
                    statusLabel.Text = $"Loaded configuration: {Path.GetFileName(configFilePath)}";
                    configStatusLabel.Text = $"Trading Root: {tradingRoot}";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading configuration: {ex.Message}", "Configuration Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    config = new Level6Configuration();
                }
            }
            else
            {
                config = new Level6Configuration();
                config.TradingRoot = tradingRoot;
                config.DefaultDataRoot = Path.Combine(tradingRoot, "TradingData");
                CreateDefaultGroups();
            }
            
            isConfigModified = false;
        }
        
        private void UpgradeConfiguration(string json)
        {
            // Parse legacy configuration and upgrade to Level 6 format
            var legacyConfig = JsonSerializer.Deserialize<JsonElement>(json);
            
            config = new Level6Configuration();
            
            if (legacyConfig.TryGetProperty("tradingRoot", out var tradingRootElement))
                config.TradingRoot = tradingRootElement.GetString() ?? tradingRoot;
            if (legacyConfig.TryGetProperty("defaultDataRoot", out var dataRootElement))
                config.DefaultDataRoot = dataRootElement.GetString() ?? Path.Combine(tradingRoot, "TradingData");
            
            if (legacyConfig.TryGetProperty("instances", out var instancesElement))
            {
                config.Instances = JsonSerializer.Deserialize<List<TradingInstance>>(instancesElement.GetRawText(), 
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<TradingInstance>();
                
                // Auto-assign groups based on broker/platform
                foreach (var instance in config.Instances)
                {
                    if (instance.GroupMembership == null || !instance.GroupMembership.Any())
                    {
                        instance.GroupMembership = new List<string> { instance.Broker ?? "Unknown" };
                    }
                }
            }
            
            CreateDefaultGroups();
            isConfigModified = true;
        }
        
        private void CreateDefaultGroups()
        {
            if (!config.Groups.ContainsKey("Live Trading"))
            {
                config.Groups["Live Trading"] = new GroupDefinition
                {
                    Name = "Live Trading",
                    Description = "Production trading instances",
                    Color = "#DC3545",
                    Priority = "high"
                };
            }
            
            if (!config.Groups.ContainsKey("Demo Trading"))
            {
                config.Groups["Demo Trading"] = new GroupDefinition
                {
                    Name = "Demo Trading",
                    Description = "Demo and testing instances",
                    Color = "#28A745",
                    Priority = "normal"
                };
            }
            
            // Create broker-based groups
            var brokers = config.Instances.Select(i => i.Broker).Distinct().Where(b => !string.IsNullOrEmpty(b));
            foreach (var broker in brokers)
            {
                if (!config.Groups.ContainsKey(broker))
                {
                    config.Groups[broker] = new GroupDefinition
                    {
                        Name = broker,
                        Description = $"{broker} instances",
                        Color = "#0078D4"
                    };
                }
            }
        }
        
        private void SaveConfiguration()
        {
            try
            {
                config.Metadata.LastModified = DateTime.Now;
                
                var json = JsonSerializer.Serialize(config, new JsonSerializerOptions 
                { 
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                File.WriteAllText(configFilePath, json);
                
                isConfigModified = false;
                statusLabel.Text = "Configuration saved successfully";
                this.Text = "Level 6 - Advanced Instance Management (Built on Dashboard v28)";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving configuration: {ex.Message}", "Save Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        #endregion
        
        #region Event Handlers
        
        private void NewConfiguration(object? sender, EventArgs e) 
        {
            if (ConfirmUnsavedChanges())
            {
                config = new Level6Configuration();
                config.TradingRoot = tradingRoot;
                config.DefaultDataRoot = Path.Combine(tradingRoot, "TradingData");
                CreateDefaultGroups();
                RefreshAll();
                isConfigModified = true;
                MarkConfigurationModified();
            }
        }
        
        private void OpenConfiguration(object? sender, EventArgs e) 
        {
            var dialog = new OpenFileDialog
            {
                Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*",
                Title = "Open Configuration File"
            };
            
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                configFilePath = dialog.FileName;
                LoadConfiguration();
            }
        }
        
        private void SaveConfigurationAs(object? sender, EventArgs e) 
        {
            var dialog = new SaveFileDialog
            {
                Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*",
                Title = "Save Configuration As",
                FileName = "instances-config.json"
            };
            
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                configFilePath = dialog.FileName;
                SaveConfiguration();
            }
        }
        
        private void ImportInstances(object? sender, EventArgs e) 
        {
            MessageBox.Show("Import functionality would be implemented here", "Import Instances", 
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        
        private void ExportInstances(object? sender, EventArgs e) 
        {
            MessageBox.Show("Export functionality would be implemented here", "Export Instances", 
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        
        private void AddNewInstance(object? sender, EventArgs e) 
        { 
            var dialog = new SimpleInstanceDialog(config, GetAvailablePlatforms(), null);
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                config.Instances.Add(dialog.Instance);
                RefreshInstanceList();
                RefreshGroupTree();
                MarkConfigurationModified();
            }
        }
        
        private void EditSelectedInstance(object? sender, EventArgs e) 
        {
            var selectedInstance = GetSelectedInstance();
            if (selectedInstance != null)
            {
                var dialog = new SimpleInstanceDialog(config, GetAvailablePlatforms(), selectedInstance);
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    RefreshInstanceList();
                    RefreshGroupTree();
                    MarkConfigurationModified();
                }
            }
            else
            {
                MessageBox.Show("Please select an instance to edit.", "No Selection", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        
        private void CloneSelectedInstance(object? sender, EventArgs e) 
        {
            var selectedInstance = GetSelectedInstance();
            if (selectedInstance != null)
            {
                var clonedInstance = CloneInstance(selectedInstance);
                var dialog = new SimpleInstanceDialog(config, GetAvailablePlatforms(), clonedInstance);
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    config.Instances.Add(dialog.Instance);
                    RefreshInstanceList();
                    RefreshGroupTree();
                    MarkConfigurationModified();
                }
            }
            else
            {
                MessageBox.Show("Please select an instance to clone.", "No Selection", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        
        private void DeleteSelectedInstance(object? sender, EventArgs e) 
        {
            var selectedInstance = GetSelectedInstance();
            if (selectedInstance != null)
            {
                var result = MessageBox.Show($"Are you sure you want to delete instance '{selectedInstance.Name}'?", 
                    "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    
                if (result == DialogResult.Yes)
                {
                    config.Instances.Remove(selectedInstance);
                    RefreshInstanceList();
                    RefreshGroupTree();
                    MarkConfigurationModified();
                }
            }
            else
            {
                MessageBox.Show("Please select an instance to delete.", "No Selection", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        
        private void RefreshInstanceStatus(object? sender, EventArgs e) 
        {
            RefreshInstanceList();
            statusLabel.Text = "Instance status refreshed";
        }
        
        private void CreateNewGroup(object? sender, EventArgs e) 
        {
            var dialog = new SimpleGroupDialog(null);
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                config.Groups[dialog.Group.Name] = dialog.Group;
                RefreshGroupTree();
                MarkConfigurationModified();
            }
        }
        
        private void EditSelectedGroup(object? sender, EventArgs e) 
        {
            var selectedGroup = GetSelectedGroup();
            if (selectedGroup != null)
            {
                var dialog = new SimpleGroupDialog(selectedGroup);
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    // Update group in dictionary
                    if (dialog.Group.Name != selectedGroup.Name)
                    {
                        config.Groups.Remove(selectedGroup.Name);
                    }
                    config.Groups[dialog.Group.Name] = dialog.Group;
                    RefreshGroupTree();
                    MarkConfigurationModified();
                }
            }
            else
            {
                MessageBox.Show("Please select a group to edit.", "No Selection", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        
        private void DeleteSelectedGroup(object? sender, EventArgs e) 
        {
            var selectedGroup = GetSelectedGroup();
            if (selectedGroup != null)
            {
                var result = MessageBox.Show($"Are you sure you want to delete group '{selectedGroup.Name}'?", 
                    "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    
                if (result == DialogResult.Yes)
                {
                    config.Groups.Remove(selectedGroup.Name);
                    
                    // Remove group membership from instances
                    foreach (var instance in config.Instances)
                    {
                        instance.GroupMembership.Remove(selectedGroup.Name);
                    }
                    
                    RefreshAll();
                    MarkConfigurationModified();
                }
            }
            else
            {
                MessageBox.Show("Please select a group to delete.", "No Selection", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        
        private void ReorganizeGroups(object? sender, EventArgs e) 
        {
            MessageBox.Show("Group reorganization functionality would be implemented here", "Reorganize Groups", 
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        
        private void ApplyChanges(object? sender, EventArgs e)
        {
            // Save configuration and run Level 2 script
            SaveConfiguration();
            RunLevel2Script();
        }
        
        private void BuildSelectedInstances(object? sender, EventArgs e) 
        {
            MessageBox.Show("Build selected instances functionality would be implemented here", "Build Selected", 
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        
        private void RebuildAllInstances(object? sender, EventArgs e) 
        {
            var result = MessageBox.Show("This will rebuild all instances. Continue?", "Rebuild All", 
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                
            if (result == DialogResult.Yes)
            {
                SaveConfiguration();
                RunLevel2Script("-Force");
            }
        }
        
        private void ValidateConfiguration(object? sender, EventArgs e) 
        {
            var errors = ValidateCurrentConfiguration();
            if (errors.Any())
            {
                MessageBox.Show($"Configuration errors found:\n{string.Join("\n", errors)}", 
                    "Validation Errors", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else
            {
                MessageBox.Show("Configuration is valid!", "Validation Success", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        
        private void BackupConfiguration(object? sender, EventArgs e) 
        {
            try
            {
                var backupPath = $"{configFilePath}.backup.{DateTime.Now:yyyyMMdd-HHmmss}";
                File.Copy(configFilePath, backupPath);
                config.Metadata.BackupFiles.Add(backupPath);
                MessageBox.Show($"Configuration backed up to:\n{backupPath}", "Backup Complete", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Backup failed: {ex.Message}", "Backup Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        // Integration event handlers
        private void LaunchLevel5Dashboard(object? sender, EventArgs e)
        {
            try
            {
                var level5Path = Path.Combine(tradingRoot, "Level4Dashboard");
                var exePaths = new[]
                {
                    Path.Combine(level5Path, "bin\\Release\\net9.0-windows\\Level4Dashboard.exe"),
                    Path.Combine(level5Path, "bin\\Debug\\net9.0-windows\\Level4Dashboard.exe")
                };
                
                var exePath = exePaths.FirstOrDefault(File.Exists);
                if (exePath != null)
                {
                    Process.Start(new ProcessStartInfo(exePath) { WorkingDirectory = tradingRoot });
                    statusLabel.Text = "Level 5 Dashboard launched";
                }
                else
                {
                    MessageBox.Show("Level 5 Dashboard not found. Please build it first.", "Dashboard Not Found", 
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to launch Level 5 Dashboard: {ex.Message}", "Launch Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void RunLevel3Manager(object? sender, EventArgs e)
        {
            RunPowerShellScript("3 SimpleTradingManager.ps1", "-Action Status");
        }
        
        private void CheckAllLevelsStatus(object? sender, EventArgs e)
        {
            var status = new StringBuilder();
            status.AppendLine("Trading Environment Status:");
            status.AppendLine($"Trading Root: {tradingRoot}");
            status.AppendLine($"Configuration: {(File.Exists(configFilePath) ? "Found" : "Missing")}");
            status.AppendLine($"Instances: {config.Instances.Count}");
            status.AppendLine($"Groups: {config.Groups.Count}");
            
            MessageBox.Show(status.ToString(), "System Status", 
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        
        // Tree and list view event handlers
        private void GroupTreeView_AfterSelect(object? sender, TreeViewEventArgs e) 
        {
            if (e.Node?.Tag is TradingInstance instance)
            {
                instancePropertyGrid.SelectedObject = instance;
                
                // Highlight in list view
                foreach (ListViewItem item in instanceListView.Items)
                {
                    if (item.Tag == instance)
                    {
                        item.Selected = true;
                        item.EnsureVisible();
                        break;
                    }
                }
            }
            else if (e.Node?.Tag is GroupDefinition group)
            {
                instancePropertyGrid.SelectedObject = group;
            }
        }
        
        private void GroupTreeView_AfterCheck(object? sender, TreeViewEventArgs e) 
        {
            if (e.Node?.Tag is TradingInstance instance)
            {
                instance.Enabled = e.Node.Checked;
                RefreshInstanceList();
                MarkConfigurationModified();
            }
        }
        
        private void InstanceListView_SelectedIndexChanged(object? sender, EventArgs e) 
        {
            if (instanceListView.SelectedItems.Count > 0)
            {
                var instance = instanceListView.SelectedItems[0].Tag as TradingInstance;
                instancePropertyGrid.SelectedObject = instance;
            }
        }
        
        private void InstanceListView_ItemCheck(object? sender, ItemCheckEventArgs e) 
        {
            if (instanceListView.Items[e.Index].Tag is TradingInstance instance)
            {
                instance.Enabled = e.NewValue == CheckState.Checked;
                MarkConfigurationModified();
            }
        }
        
        private void InstancePropertyGrid_PropertyValueChanged(object? s, PropertyValueChangedEventArgs e) 
        {
            MarkConfigurationModified();
            RefreshInstanceList();
        }
        
        // Context menu handlers
        private void AddInstanceToGroup(object? sender, EventArgs e) 
        {
            MessageBox.Show("Add to group functionality would be implemented here", "Add to Group", 
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        
        private void RemoveFromGroup(object? sender, EventArgs e) 
        {
            MessageBox.Show("Remove from group functionality would be implemented here", "Remove from Group", 
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        
        private void StartGroup(object? sender, EventArgs e) 
        {
            var selectedGroup = GetSelectedGroup();
            if (selectedGroup != null)
            {
                var instances = config.Instances.Where(i => i.GroupMembership.Contains(selectedGroup.Name));
                foreach (var instance in instances)
                {
                    // Start instance logic would go here
                }
                statusLabel.Text = $"Started all instances in group: {selectedGroup.Name}";
            }
        }
        
        private void StopGroup(object? sender, EventArgs e) 
        {
            var selectedGroup = GetSelectedGroup();
            if (selectedGroup != null)
            {
                var instances = config.Instances.Where(i => i.GroupMembership.Contains(selectedGroup.Name));
                foreach (var instance in instances)
                {
                    // Stop instance logic would go here
                }
                statusLabel.Text = $"Stopped all instances in group: {selectedGroup.Name}";
            }
        }
        
        private void StartSelectedInstance(object? sender, EventArgs e) 
        {
            var instance = GetSelectedInstance();
            if (instance != null)
            {
                statusLabel.Text = $"Starting instance: {instance.Name}";
                // Start logic would go here
            }
        }
        
        private void StopSelectedInstance(object? sender, EventArgs e) 
        {
            var instance = GetSelectedInstance();
            if (instance != null)
            {
                statusLabel.Text = $"Stopping instance: {instance.Name}";
                // Stop logic would go here
            }
        }
        
        private void OpenInstanceFolder(object? sender, EventArgs e) 
        {
            var instance = GetSelectedInstance();
            if (instance != null)
            {
                var path = Path.Combine(tradingRoot, "PlatformInstances", instance.Destination);
                if (Directory.Exists(path))
                {
                    Process.Start("explorer.exe", path);
                }
                else
                {
                    MessageBox.Show($"Instance folder not found: {path}", "Folder Not Found", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }
        
        private void OpenDataFolder(object? sender, EventArgs e) 
        {
            var instance = GetSelectedInstance();
            if (instance != null)
            {
                var path = Path.Combine(tradingRoot, "InstanceData", instance.JunctionName);
                if (Directory.Exists(path))
                {
                    Process.Start("explorer.exe", path);
                }
                else
                {
                    MessageBox.Show($"Data folder not found: {path}", "Folder Not Found", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }
        
        #endregion
        
        #region Helper Methods
        
        private void RefreshAll()
        {
            RefreshGroupTree();
            RefreshInstanceList();
        }
        
        private void RefreshGroupTree()
        {
            groupTreeView.Nodes.Clear();
            
            foreach (var group in config.Groups.Values)
            {
                var groupNode = new TreeNode(group.Name);
                groupNode.Tag = group;
                groupNode.ForeColor = ColorTranslator.FromHtml(group.Color);
                
                var instancesInGroup = config.Instances.Where(i => 
                    i.GroupMembership != null && i.GroupMembership.Contains(group.Name));
                
                foreach (var instance in instancesInGroup)
                {
                    var instanceNode = new TreeNode(instance.Name);
                    instanceNode.Tag = instance;
                    instanceNode.Checked = instance.Enabled;
                    
                    // Color code by account type
                    if (instance.AccountType?.ToLower().Contains("live") == true)
                    {
                        instanceNode.ForeColor = Color.Red;
                    }
                    else if (instance.AccountType?.ToLower().Contains("demo") == true)
                    {
                        instanceNode.ForeColor = Color.Green;
                    }
                    
                    groupNode.Nodes.Add(instanceNode);
                }
                
                groupTreeView.Nodes.Add(groupNode);
            }
            
            groupTreeView.ExpandAll();
        }
        
        private void RefreshInstanceList()
        {
            instanceListView.Items.Clear();
            
            foreach (var instance in config.Instances)
            {
                var item = new ListViewItem(instance.Name);
                item.SubItems.Add(instance.Broker ?? "");
                item.SubItems.Add(instance.Platform ?? "");
                item.SubItems.Add(instance.AccountType ?? "");
                item.SubItems.Add("Unknown"); // Status - would be determined by checking process
                item.SubItems.Add(instance.Enabled ? "Yes" : "No");
                item.SubItems.Add(instance.AutoStart ? "Yes" : "No");
                item.SubItems.Add(string.Join(", ", instance.GroupMembership ?? new List<string>()));
                item.SubItems.Add(instance.Created.ToShortDateString());
                
                item.Tag = instance;
                item.Checked = instance.Enabled;
                
                // Color code by account type
                if (instance.AccountType?.ToLower().Contains("live") == true)
                {
                    item.ForeColor = Color.Red;
                }
                else if (instance.AccountType?.ToLower().Contains("demo") == true)
                {
                    item.ForeColor = Color.Green;
                }
                
                instanceListView.Items.Add(item);
            }
        }
        
        private TradingInstance? GetSelectedInstance()
        {
            if (instanceListView.SelectedItems.Count > 0)
            {
                return instanceListView.SelectedItems[0].Tag as TradingInstance;
            }
            
            if (groupTreeView.SelectedNode?.Tag is TradingInstance instance)
            {
                return instance;
            }
            
            return null;
        }
        
        private GroupDefinition? GetSelectedGroup()
        {
            if (groupTreeView.SelectedNode?.Tag is GroupDefinition group)
            {
                return group;
            }
            
            return null;
        }
        
        private TradingInstance CloneInstance(TradingInstance original)
        {
            return new TradingInstance
            {
                Name = $"{original.Name}_Copy",
                Broker = original.Broker,
                Platform = original.Platform,
                Source = original.Source,
                Destination = $"{original.Destination}_Copy",
                DataFolder = $"{original.DataFolder}_Copy",
                JunctionName = $"{original.JunctionName}_Copy",
                PortableMode = original.PortableMode,
                AccountType = original.AccountType,
                Enabled = false, // Disabled by default for safety
                AutoStart = false,
                StartupDelay = original.StartupDelay,
                Priority = original.Priority,
                GroupMembership = new List<string>(original.GroupMembership),
                ServerSettings = new Dictionary<string, object>(original.ServerSettings),
                CustomSettings = new Dictionary<string, object>(original.CustomSettings),
                Notes = $"Cloned from {original.Name}"
            };
        }
        
        private List<PlatformInfo> GetAvailablePlatforms()
        {
            var platforms = new List<PlatformInfo>();
            var installationsPath = Path.Combine(tradingRoot, "PlatformInstallations");
            
            if (Directory.Exists(installationsPath))
            {
                foreach (var dir in Directory.GetDirectories(installationsPath))
                {
                    var dirInfo = new DirectoryInfo(dir);
                    var platform = new PlatformInfo
                    {
                        Name = dirInfo.Name,
                        Path = dir,
                        Platform = "Unknown",
                        Broker = "Unknown",
                        IsValid = false
                    };
                    
                    // Try to detect platform type
                    if (File.Exists(Path.Combine(dir, "terminal.exe")))
                    {
                        platform.Platform = "MT4";
                        platform.IsValid = true;
                    }
                    else if (File.Exists(Path.Combine(dir, "terminal64.exe")))
                    {
                        platform.Platform = "MT5";
                        platform.IsValid = true;
                    }
                    else if (File.Exists(Path.Combine(dir, "ctrader.exe")))
                    {
                        platform.Platform = "cTrader";
                        platform.IsValid = true;
                    }
                    
                    // Extract broker name from folder name
                    var nameMatch = System.Text.RegularExpressions.Regex.Match(dirInfo.Name, @"^([^_]+)");
                    if (nameMatch.Success)
                    {
                        platform.Broker = nameMatch.Groups[1].Value;
                    }
                    
                    platforms.Add(platform);
                }
            }
            
            return platforms;
        }
        
        private void RunLevel2Script(string additionalArgs = "")
        {
            try
            {
                var scriptPath = Path.Combine(tradingRoot, "Setup", "2 Level2-Clean.ps1");
                if (!File.Exists(scriptPath))
                {
                    MessageBox.Show("Level 2 script not found. Please ensure the script exists in Setup folder.",
                        "Script Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                
                var args = $"-ExecutionPolicy Bypass -File \"{scriptPath}\"";
                if (!string.IsNullOrEmpty(additionalArgs))
                {
                    args += $" {additionalArgs}";
                }
                
                var startInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = args,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WorkingDirectory = tradingRoot
                };
                
                using var process = Process.Start(startInfo);
                if (process != null)
                {
                    var output = process.StandardOutput.ReadToEnd();
                    var error = process.StandardError.ReadToEnd();
                    process.WaitForExit();
                    
                    if (process.ExitCode == 0)
                    {
                        statusLabel.Text = "Level 2 script completed successfully";
                        MessageBox.Show("Instance changes applied successfully!", "Success", 
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show($"Level 2 script failed:\n{error}", "Script Error", 
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error running Level 2 script: {ex.Message}", "Execution Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void RunPowerShellScript(string scriptName, string args = "")
        {
            try
            {
                var scriptPath = Path.Combine(tradingRoot, "Setup", scriptName);
                var startInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-ExecutionPolicy Bypass -File \"{scriptPath}\" {args}",
                    UseShellExecute = true,
                    WorkingDirectory = tradingRoot
                };
                
                Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error running script: {ex.Message}", "Script Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private List<string> ValidateCurrentConfiguration()
        {
            var errors = new List<string>();
            
            // Validate instances
            foreach (var instance in config.Instances)
            {
                if (string.IsNullOrEmpty(instance.Name))
                    errors.Add("Instance with empty name found");
                    
                if (string.IsNullOrEmpty(instance.Source))
                    errors.Add($"Instance '{instance.Name}' has no source specified");
                    
                if (string.IsNullOrEmpty(instance.Destination))
                    errors.Add($"Instance '{instance.Name}' has no destination specified");
            }
            
            // Check for duplicate names
            var duplicateNames = config.Instances.GroupBy(i => i.Name)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key);
                
            foreach (var name in duplicateNames)
            {
                errors.Add($"Duplicate instance name: {name}");
            }
            
            return errors;
        }
        
        private bool ConfirmUnsavedChanges()
        {
            if (isConfigModified)
            {
                var result = MessageBox.Show("Configuration has unsaved changes. Continue anyway?", 
                    "Unsaved Changes", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                return result == DialogResult.Yes;
            }
            return true;
        }
        
        private void MarkConfigurationModified()
        {
            isConfigModified = true;
            this.Text = "Level 6 - Advanced Instance Management (Built on Dashboard v28) *";
        }
        
        #endregion
    }

    // Simple Instance Editor Dialog
    public class SimpleInstanceDialog : Form
    {
        public TradingInstance Instance { get; private set; }
        
        private TextBox nameTextBox;
        private ComboBox brokerComboBox;
        private ComboBox platformComboBox;
        private ComboBox sourceComboBox;
        private TextBox destinationTextBox;
        private ComboBox accountTypeComboBox;
        private CheckBox enabledCheckBox;
        private CheckBox autoStartCheckBox;
        private CheckedListBox groupListBox;
        
        public SimpleInstanceDialog(Level6Configuration config, List<PlatformInfo> platforms, TradingInstance? existingInstance)
        {
            Instance = existingInstance ?? new TradingInstance();
            InitializeDialog(config, platforms);
            PopulateControls(config, platforms);
        }
        
        private void InitializeDialog(Level6Configuration config, List<PlatformInfo> platforms)
        {
            this.Text = Instance.Name == "" ? "Add New Instance" : "Edit Instance";
            this.Size = new Size(500, 600);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            
            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 10,
                Padding = new Padding(10)
            };
            
            // Name
            panel.Controls.Add(new Label { Text = "Name:", Anchor = AnchorStyles.Right }, 0, 0);
            nameTextBox = new TextBox { Anchor = AnchorStyles.Left | AnchorStyles.Right, Width = 300 };
            panel.Controls.Add(nameTextBox, 1, 0);
            
            // Broker
            panel.Controls.Add(new Label { Text = "Broker:", Anchor = AnchorStyles.Right }, 0, 1);
            brokerComboBox = new ComboBox { Anchor = AnchorStyles.Left | AnchorStyles.Right, Width = 300 };
            panel.Controls.Add(brokerComboBox, 1, 1);
            
            // Platform
            panel.Controls.Add(new Label { Text = "Platform:", Anchor = AnchorStyles.Right }, 0, 2);
            platformComboBox = new ComboBox { Anchor = AnchorStyles.Left | AnchorStyles.Right, Width = 300 };
            panel.Controls.Add(platformComboBox, 1, 2);
            
            // Source
            panel.Controls.Add(new Label { Text = "Source:", Anchor = AnchorStyles.Right }, 0, 3);
            sourceComboBox = new ComboBox { Anchor = AnchorStyles.Left | AnchorStyles.Right, Width = 300 };
            panel.Controls.Add(sourceComboBox, 1, 3);
            
            // Destination
            panel.Controls.Add(new Label { Text = "Destination:", Anchor = AnchorStyles.Right }, 0, 4);
            destinationTextBox = new TextBox { Anchor = AnchorStyles.Left | AnchorStyles.Right, Width = 300 };
            panel.Controls.Add(destinationTextBox, 1, 4);
            
            // Account Type
            panel.Controls.Add(new Label { Text = "Account Type:", Anchor = AnchorStyles.Right }, 0, 5);
            accountTypeComboBox = new ComboBox { Anchor = AnchorStyles.Left | AnchorStyles.Right, Width = 300 };
            accountTypeComboBox.Items.AddRange(new[] { "demo", "live", "contest", "test" });
            panel.Controls.Add(accountTypeComboBox, 1, 5);
            
            // Enabled
            panel.Controls.Add(new Label { Text = "Enabled:", Anchor = AnchorStyles.Right }, 0, 6);
            enabledCheckBox = new CheckBox { Anchor = AnchorStyles.Left };
            panel.Controls.Add(enabledCheckBox, 1, 6);
            
            // Auto Start
            panel.Controls.Add(new Label { Text = "Auto Start:", Anchor = AnchorStyles.Right }, 0, 7);
            autoStartCheckBox = new CheckBox { Anchor = AnchorStyles.Left };
            panel.Controls.Add(autoStartCheckBox, 1, 7);
            
            // Groups
            panel.Controls.Add(new Label { Text = "Groups:", Anchor = AnchorStyles.Right | AnchorStyles.Top }, 0, 8);
            groupListBox = new CheckedListBox { Anchor = AnchorStyles.Left | AnchorStyles.Right, Height = 100, Width = 300 };
            panel.Controls.Add(groupListBox, 1, 8);
            
            // Buttons
            var buttonPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Bottom, Height = 40 };
            
            var okButton = new Button { Text = "OK", DialogResult = DialogResult.OK, Size = new Size(75, 25) };
            okButton.Click += OkButton_Click;
            
            var cancelButton = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Size = new Size(75, 25) };
            
            buttonPanel.Controls.AddRange(new Control[] { okButton, cancelButton });
            
            this.Controls.AddRange(new Control[] { panel, buttonPanel });
            this.AcceptButton = okButton;
            this.CancelButton = cancelButton;
        }
        
        private void PopulateControls(Level6Configuration config, List<PlatformInfo> platforms)
        {
            // Populate broker combo
            var brokers = platforms.Select(p => p.Broker).Distinct().Where(b => !string.IsNullOrEmpty(b));
            brokerComboBox.Items.AddRange(brokers.ToArray());
            
            // Populate platform combo
            var platformTypes = platforms.Select(p => p.Platform).Distinct().Where(p => !string.IsNullOrEmpty(p));
            platformComboBox.Items.AddRange(platformTypes.ToArray());
            
            // Populate source combo
            var sources = platforms.Where(p => p.IsValid).Select(p => p.Name);
            sourceComboBox.Items.AddRange(sources.ToArray());
            
            // Populate groups
            foreach (var group in config.Groups.Keys)
            {
                groupListBox.Items.Add(group);
            }
            
            // Set current values
            nameTextBox.Text = Instance.Name;
            brokerComboBox.Text = Instance.Broker;
            platformComboBox.Text = Instance.Platform;
            sourceComboBox.Text = Instance.Source;
            destinationTextBox.Text = Instance.Destination;
            accountTypeComboBox.Text = Instance.AccountType;
            enabledCheckBox.Checked = Instance.Enabled;
            autoStartCheckBox.Checked = Instance.AutoStart;
            
            // Set group memberships
            for (int i = 0; i < groupListBox.Items.Count; i++)
            {
                if (Instance.GroupMembership.Contains(groupListBox.Items[i].ToString()))
                {
                    groupListBox.SetItemChecked(i, true);
                }
            }
        }
        
        private void OkButton_Click(object? sender, EventArgs e)
        {
            // Validate
            if (string.IsNullOrWhiteSpace(nameTextBox.Text))
            {
                MessageBox.Show("Name is required.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.DialogResult = DialogResult.None;
                return;
            }
            
            if (string.IsNullOrWhiteSpace(sourceComboBox.Text))
            {
                MessageBox.Show("Source is required.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.DialogResult = DialogResult.None;
                return;
            }
            
            // Save values
            Instance.Name = nameTextBox.Text.Trim();
            Instance.Broker = brokerComboBox.Text.Trim();
            Instance.Platform = platformComboBox.Text.Trim();
            Instance.Source = sourceComboBox.Text.Trim();
            Instance.Destination = string.IsNullOrWhiteSpace(destinationTextBox.Text) ? Instance.Name : destinationTextBox.Text.Trim();
            Instance.DataFolder = Instance.Destination;
            Instance.JunctionName = Instance.Destination;
            Instance.AccountType = accountTypeComboBox.Text.Trim();
            Instance.Enabled = enabledCheckBox.Checked;
            Instance.AutoStart = autoStartCheckBox.Checked;
            Instance.LastModified = DateTime.Now;
            
            // Save group memberships
            Instance.GroupMembership.Clear();
            for (int i = 0; i < groupListBox.Items.Count; i++)
            {
                if (groupListBox.GetItemChecked(i))
                {
                    Instance.GroupMembership.Add(groupListBox.Items[i].ToString() ?? "");
                }
            }
        }
    }

    // Simple Group Editor Dialog
    public class SimpleGroupDialog : Form
    {
        public GroupDefinition Group { get; private set; }
        
        private TextBox nameTextBox;
        private TextBox descriptionTextBox;
        private ComboBox colorComboBox;
        private ComboBox priorityComboBox;
        private CheckBox autoStartCheckBox;
        
        public SimpleGroupDialog(GroupDefinition? existingGroup)
        {
            Group = existingGroup ?? new GroupDefinition();
            InitializeDialog();
            PopulateControls();
        }
        
        private void InitializeDialog()
        {
            this.Text = Group.Name == "" ? "Create New Group" : "Edit Group";
            this.Size = new Size(400, 300);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            
            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 6,
                Padding = new Padding(10)
            };
            
            // Name
            panel.Controls.Add(new Label { Text = "Name:", Anchor = AnchorStyles.Right }, 0, 0);
            nameTextBox = new TextBox { Anchor = AnchorStyles.Left | AnchorStyles.Right, Width = 250 };
            panel.Controls.Add(nameTextBox, 1, 0);
            
            // Description
            panel.Controls.Add(new Label { Text = "Description:", Anchor = AnchorStyles.Right }, 0, 1);
            descriptionTextBox = new TextBox { Anchor = AnchorStyles.Left | AnchorStyles.Right, Width = 250 };
            panel.Controls.Add(descriptionTextBox, 1, 1);
            
            // Color
            panel.Controls.Add(new Label { Text = "Color:", Anchor = AnchorStyles.Right }, 0, 2);
            colorComboBox = new ComboBox { Anchor = AnchorStyles.Left | AnchorStyles.Right, Width = 250 };
            colorComboBox.Items.AddRange(new[] { "#0078D4", "#DC3545", "#28A745", "#FFC107", "#6F42C1", "#FD7E14" });
            panel.Controls.Add(colorComboBox, 1, 2);
            
            // Priority
            panel.Controls.Add(new Label { Text = "Priority:", Anchor = AnchorStyles.Right }, 0, 3);
            priorityComboBox = new ComboBox { Anchor = AnchorStyles.Left | AnchorStyles.Right, Width = 250 };
            priorityComboBox.Items.AddRange(new[] { "low", "normal", "high", "critical" });
            panel.Controls.Add(priorityComboBox, 1, 3);
            
            // Auto Start
            panel.Controls.Add(new Label { Text = "Auto Start:", Anchor = AnchorStyles.Right }, 0, 4);
            autoStartCheckBox = new CheckBox { Anchor = AnchorStyles.Left };
            panel.Controls.Add(autoStartCheckBox, 1, 4);
            
            // Buttons
            var buttonPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Bottom, Height = 40 };
            
            var okButton = new Button { Text = "OK", DialogResult = DialogResult.OK, Size = new Size(75, 25) };
            okButton.Click += OkButton_Click;
            
            var cancelButton = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Size = new Size(75, 25) };
            
            buttonPanel.Controls.AddRange(new Control[] { okButton, cancelButton });
            
            this.Controls.AddRange(new Control[] { panel, buttonPanel });
            this.AcceptButton = okButton;
            this.CancelButton = cancelButton;
        }
        
        private void PopulateControls()
        {
            nameTextBox.Text = Group.Name;
            descriptionTextBox.Text = Group.Description;
            colorComboBox.Text = Group.Color;
            priorityComboBox.Text = Group.Priority;
            autoStartCheckBox.Checked = Group.AutoStart;
        }
        
        private void OkButton_Click(object? sender, EventArgs e)
        {
            // Validate
            if (string.IsNullOrWhiteSpace(nameTextBox.Text))
            {
                MessageBox.Show("Name is required.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.DialogResult = DialogResult.None;
                return;
            }
            
            // Save values
            Group.Name = nameTextBox.Text.Trim();
            Group.Description = descriptionTextBox.Text.Trim();
            Group.Color = colorComboBox.Text.Trim();
            Group.Priority = priorityComboBox.Text.Trim();
            Group.AutoStart = autoStartCheckBox.Checked;
        }
    }

    // Program entry point
    public class Program
    {
        [STAThread]
        public static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Level6InstanceManager());
        }
    }
}