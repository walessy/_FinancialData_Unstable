using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Forms;

namespace Level6InstanceManager
{
    // Simple configuration classes
    public class TradingInstance
    {
        public string Name { get; set; } = "";
        public string Broker { get; set; } = "";
        public string Platform { get; set; } = "";
        public string Source { get; set; } = "";
        public string Destination { get; set; } = "";
        public string AccountType { get; set; } = "";
        public bool Enabled { get; set; } = true;
        public bool AutoStart { get; set; } = false;
    }

    public class Configuration
    {
        public string TradingRoot { get; set; } = "";
        public string DefaultDataRoot { get; set; } = "";
        public List<TradingInstance> Instances { get; set; } = new List<TradingInstance>();
    }

    // Main Level 6 Form
    public partial class Level6InstanceManager : Form
    {
        private Configuration config = new Configuration();
        private string tradingRoot = "";
        private string configFilePath = "";
        
        // UI Components
        private MenuStrip menuStrip;
        private ListView instanceListView;
        private StatusStrip statusStrip;
        private ToolStripStatusLabel statusLabel;

        public Level6InstanceManager()
        {
            try
            {
                FindTradingRoot();
                LoadConfiguration();
                InitializeComponents();
                RefreshInstanceList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Initialization error: {ex.Message}", "Level 6 Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void FindTradingRoot()
        {
            var currentPath = Directory.GetCurrentDirectory();
            var configFile = Path.Combine(currentPath, "instances-config.json");
            
            if (File.Exists(configFile))
            {
                tradingRoot = currentPath;
                configFilePath = configFile;
                return;
            }

            // Look in parent directories
            var parent = Directory.GetParent(currentPath);
            while (parent != null)
            {
                configFile = Path.Combine(parent.FullName, "instances-config.json");
                if (File.Exists(configFile))
                {
                    tradingRoot = parent.FullName;
                    configFilePath = configFile;
                    return;
                }
                parent = parent.Parent;
            }

            throw new FileNotFoundException("instances-config.json not found in current or parent directories");
        }

        private void LoadConfiguration()
        {
            try
            {
                var json = File.ReadAllText(configFilePath);
                config = JsonSerializer.Deserialize<Configuration>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new Configuration();

                if (string.IsNullOrEmpty(config.TradingRoot))
                    config.TradingRoot = tradingRoot;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading configuration: {ex.Message}", "Configuration Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                config = new Configuration { TradingRoot = tradingRoot };
            }
        }

        private void InitializeComponents()
        {
            // Main form setup
            this.Text = "Level 6 - Trading Instance Manager";
            this.Size = new Size(1000, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(45, 45, 48);
            this.ForeColor = Color.White;

            // Create menu
            CreateMenu();

            // Create main ListView
            CreateInstanceListView();

            // Create status bar
            CreateStatusBar();
        }

        private void CreateMenu()
        {
            menuStrip = new MenuStrip();
            menuStrip.BackColor = Color.FromArgb(60, 60, 63);
            menuStrip.ForeColor = Color.White;

            var fileMenu = new ToolStripMenuItem("File");
            fileMenu.DropDownItems.Add("Save Configuration", null, SaveConfiguration);
            fileMenu.DropDownItems.Add("Apply Changes (Run Level 2)", null, ApplyChanges);
            fileMenu.DropDownItems.Add(new ToolStripSeparator());
            fileMenu.DropDownItems.Add("Exit", null, (s, e) => this.Close());

            var instanceMenu = new ToolStripMenuItem("Instance");
            instanceMenu.DropDownItems.Add("Add New", null, AddNewInstance);
            instanceMenu.DropDownItems.Add("Edit Selected", null, EditSelectedInstance);
            instanceMenu.DropDownItems.Add("Start Selected", null, StartSelectedInstance);
            instanceMenu.DropDownItems.Add("Delete Selected", null, DeleteSelectedInstance);
            instanceMenu.DropDownItems.Add("Delete Files (Remove from Disk)", null, DeleteInstanceFiles);
            instanceMenu.DropDownItems.Add(new ToolStripSeparator());
            instanceMenu.DropDownItems.Add("Refresh List", null, (s, e) => RefreshInstanceList());

            menuStrip.Items.AddRange(new[] { fileMenu, instanceMenu });
            this.MainMenuStrip = menuStrip;
            this.Controls.Add(menuStrip);
        }

        private void CreateInstanceListView()
        {
            instanceListView = new ListView();
            instanceListView.View = View.Details;
            instanceListView.FullRowSelect = true;
            instanceListView.GridLines = true;
            instanceListView.CheckBoxes = true;
            instanceListView.Dock = DockStyle.Fill;
            instanceListView.BackColor = Color.FromArgb(37, 37, 38);
            instanceListView.ForeColor = Color.White;

            // Add columns
            instanceListView.Columns.Add("Name", 200);
            instanceListView.Columns.Add("Broker", 120);
            instanceListView.Columns.Add("Platform", 80);
            instanceListView.Columns.Add("Account Type", 100);
            instanceListView.Columns.Add("Enabled", 70);
            instanceListView.Columns.Add("Auto Start", 80);
            instanceListView.Columns.Add("Source", 150);
            instanceListView.Columns.Add("Destination", 200);

            // Create context menu for right-click
            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("Start Instance", null, StartSelectedInstance);
            contextMenu.Items.Add("Edit Instance", null, EditSelectedInstance);
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add("Delete Instance", null, DeleteSelectedInstance);
            contextMenu.Items.Add("Delete Files (Remove from Disk)", null, DeleteInstanceFiles);
            
            instanceListView.ContextMenuStrip = contextMenu;

            // Double-click to edit (safer than start)
            instanceListView.DoubleClick += EditSelectedInstance;

            this.Controls.Add(instanceListView);
        }

        private void CreateStatusBar()
        {
            statusStrip = new StatusStrip();
            statusStrip.BackColor = Color.FromArgb(60, 60, 63);
            statusLabel = new ToolStripStatusLabel("Ready");
            statusLabel.ForeColor = Color.White;
            statusStrip.Items.Add(statusLabel);
            this.Controls.Add(statusStrip);
        }

        private void RefreshInstanceList()
        {
            instanceListView.Items.Clear();

            foreach (var instance in config.Instances)
            {
                var item = new ListViewItem(instance.Name);
                item.SubItems.Add(instance.Broker);
                item.SubItems.Add(instance.Platform);
                item.SubItems.Add(instance.AccountType);
                item.SubItems.Add(instance.Enabled ? "Yes" : "No");
                item.SubItems.Add(instance.AutoStart ? "Yes" : "No");
                item.SubItems.Add(instance.Source);
                item.SubItems.Add(instance.Destination);

                item.Tag = instance;
                item.Checked = instance.Enabled;

                // Color coding
                if (instance.AccountType?.ToLower().Contains("live") == true)
                {
                    item.ForeColor = Color.LightCoral;
                }
                else if (instance.AccountType?.ToLower().Contains("demo") == true)
                {
                    item.ForeColor = Color.LightGreen;
                }

                instanceListView.Items.Add(item);
            }

            statusLabel.Text = $"Loaded {config.Instances.Count} instances";
        }

        // Event Handlers
        private void SaveConfiguration(object sender, EventArgs e)
        {
            try
            {
                var json = JsonSerializer.Serialize(config, new JsonSerializerOptions 
                { 
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                File.WriteAllText(configFilePath, json);
                statusLabel.Text = "Configuration saved successfully";
                MessageBox.Show("Configuration saved!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving configuration: {ex.Message}", "Save Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void AddNewInstance(object sender, EventArgs e)
        {
            var dialog = new InstanceDialog(null, config);
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                config.Instances.Add(dialog.Instance);
                RefreshInstanceList();
                statusLabel.Text = "New instance added";
            }
        }

        private void EditSelectedInstance(object sender, EventArgs e)
        {
            if (instanceListView.SelectedItems.Count > 0)
            {
                var instance = instanceListView.SelectedItems[0].Tag as TradingInstance;
                if (instance != null)
                {
                    var dialog = new InstanceDialog(instance, config);
                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        RefreshInstanceList();
                        statusLabel.Text = "Instance updated";
                    }
                }
            }
            else
            {
                MessageBox.Show("Please select an instance to edit.", "No Selection", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void DeleteSelectedInstance(object sender, EventArgs e)
        {
            if (instanceListView.SelectedItems.Count > 0)
            {
                var instance = instanceListView.SelectedItems[0].Tag as TradingInstance;
                if (instance != null)
                {
                    if (MessageBox.Show($"Delete instance '{instance.Name}'?", "Confirm Delete", 
                        MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        config.Instances.Remove(instance);
                        RefreshInstanceList();
                        statusLabel.Text = "Instance deleted";
                    }
                }
            }
            else
            {
                MessageBox.Show("Please select an instance to delete.", "No Selection", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void DeleteInstanceFiles(object sender, EventArgs e)
        {
            if (instanceListView.SelectedItems.Count > 0)
            {
                var instance = instanceListView.SelectedItems[0].Tag as TradingInstance;
                if (instance != null)
                {
                    var result = MessageBox.Show(
                        $"WARNING: This will permanently delete ALL files for '{instance.Name}'\n\n" +
                        "This includes:\n" +
                        $"• Platform Instance: PlatformInstances\\{instance.Destination}\n" +
                        $"• Trading Data: TradingData\\{instance.Destination}\n" +
                        $"• Data Junction: InstanceData\\{instance.Destination}_Data\n\n" +
                        "This action CANNOT be undone!\n\nAre you sure?", 
                        "DELETE FILES - Permanent Action", 
                        MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                    
                    if (result == DialogResult.Yes)
                    {
                        try
                        {
                            var deletedItems = new List<string>();
                            
                            // Delete platform instance folder
                            var instancePath = Path.Combine(tradingRoot, "PlatformInstances", instance.Destination);
                            if (Directory.Exists(instancePath))
                            {
                                Directory.Delete(instancePath, true);
                                deletedItems.Add($"Platform Instance: {instancePath}");
                            }
                            
                            // Delete trading data folder
                            var dataPath = Path.Combine(tradingRoot, "TradingData", instance.Destination);
                            if (Directory.Exists(dataPath))
                            {
                                Directory.Delete(dataPath, true);
                                deletedItems.Add($"Trading Data: {dataPath}");
                            }
                            
                            // Remove data junction
                            var junctionPath = Path.Combine(tradingRoot, "InstanceData", $"{instance.Destination}_Data");
                            if (Directory.Exists(junctionPath))
                            {
                                // Use rmdir to remove junction safely
                                var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                                {
                                    FileName = "cmd.exe",
                                    Arguments = $"/c rmdir \"{junctionPath}\"",
                                    UseShellExecute = false,
                                    CreateNoWindow = true
                                });
                                process?.WaitForExit();
                                deletedItems.Add($"Data Junction: {junctionPath}");
                            }
                            
                            // Remove from configuration
                            config.Instances.Remove(instance);
                            
                            // Save configuration immediately
                            try
                            {
                                var json = JsonSerializer.Serialize(config, new JsonSerializerOptions 
                                { 
                                    WriteIndented = true,
                                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                                });
                                File.WriteAllText(configFilePath, json);
                            }
                            catch (Exception saveEx)
                            {
                                MessageBox.Show($"Files deleted but error saving configuration: {saveEx.Message}", 
                                    "Save Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            }
                            
                            RefreshInstanceList();
                            
                            statusLabel.Text = $"Files deleted for: {instance.Name}";
                            
                            var summary = "Successfully deleted:\n" + string.Join("\n", deletedItems);
                            MessageBox.Show(summary, "Files Deleted", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            
                            // Automatically apply changes (run Level 2) to clean up any remaining references
                            try
                            {
                                var scriptPath = Path.Combine(tradingRoot, "Setup", "2 Level2-Clean.ps1");
                                if (File.Exists(scriptPath))
                                {
                                    var startInfo = new System.Diagnostics.ProcessStartInfo
                                    {
                                        FileName = "powershell.exe",
                                        Arguments = $"-ExecutionPolicy Bypass -File \"{scriptPath}\"",
                                        WorkingDirectory = tradingRoot,
                                        UseShellExecute = true,
                                        Verb = "runas"
                                    };

                                    System.Diagnostics.Process.Start(startInfo);
                                    statusLabel.Text = $"Files deleted and Level 2 applied for: {instance.Name}";
                                }
                            }
                            catch (Exception scriptEx)
                            {
                                // Don't show error - files are already deleted successfully
                                statusLabel.Text = $"Files deleted for: {instance.Name} (Level 2 skipped)";
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Error deleting files for {instance.Name}: {ex.Message}", 
                                "Delete Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }
            else
            {
                MessageBox.Show("Please select an instance to delete files for.", "No Selection", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void StartSelectedInstance(object sender, EventArgs e)
        {
            if (instanceListView.SelectedItems.Count > 0)
            {
                var instance = instanceListView.SelectedItems[0].Tag as TradingInstance;
                if (instance != null)
                {
                    try
                    {
                        // Find the platform executable
                        var instancePath = Path.Combine(tradingRoot, "PlatformInstances", instance.Destination);
                        var executable = "";
                        
                        // Determine executable based on platform
                        var platform = instance.Platform?.ToUpper();
                        if (platform == "MT5")
                        {
                            executable = Path.Combine(instancePath, "terminal64.exe");
                        }
                        else if (platform == "MT4")
                        {
                            executable = Path.Combine(instancePath, "terminal.exe");
                        }
                        else if (platform == "TRADEREVOLUTION" || platform?.Contains("TRADER") == true)
                        {
                            // TraderEvolution uses tradeterminal.exe
                            executable = Path.Combine(instancePath, "tradeterminal.exe");
                        }
                        else
                        {
                            // Try to find any known trading platform executable
                            var possibleExes = new[] { "tradeterminal.exe", "terminal64.exe", "terminal.exe", "TradeTerminal.exe" };
                            foreach (var exe in possibleExes)
                            {
                                var testPath = Path.Combine(instancePath, exe);
                                if (File.Exists(testPath))
                                {
                                    executable = testPath;
                                    break;
                                }
                            }
                        }

                        if (File.Exists(executable))
                        {
                            var startInfo = new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = executable,
                                WorkingDirectory = instancePath,
                                UseShellExecute = true
                            };

                            System.Diagnostics.Process.Start(startInfo);
                            statusLabel.Text = $"Started: {instance.Name} ({Path.GetFileName(executable)})";
                        }
                        else
                        {
                            // List available executables for debugging
                            var availableExes = Directory.GetFiles(instancePath, "*.exe")
                                .Select(Path.GetFileName)
                                .ToArray();
                            
                            var exeList = availableExes.Length > 0 ? string.Join(", ", availableExes) : "None found";
                            
                            MessageBox.Show($"Trading platform executable not found for {instance.Name}\n\n" +
                                $"Instance Path: {instancePath}\n" +
                                $"Platform: {instance.Platform}\n" +
                                $"Available executables: {exeList}", 
                                "Start Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error starting {instance.Name}: {ex.Message}", 
                            "Start Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Please select an instance to start.", "No Selection", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void ApplyChanges(object sender, EventArgs e)
        {
            // Save configuration first
            try
            {
                var json = JsonSerializer.Serialize(config, new JsonSerializerOptions 
                { 
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                File.WriteAllText(configFilePath, json);
                statusLabel.Text = "Configuration saved";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving configuration: {ex.Message}", "Save Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Run Level 2 script
            try
            {
                var scriptPath = Path.Combine(tradingRoot, "Setup", "2 Level2-Clean.ps1");
                if (File.Exists(scriptPath))
                {
                    var startInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = $"-ExecutionPolicy Bypass -File \"{scriptPath}\"",
                        WorkingDirectory = tradingRoot,
                        UseShellExecute = true,
                        Verb = "runas" // Run as administrator if needed
                    };

                    System.Diagnostics.Process.Start(startInfo);
                    statusLabel.Text = "Level 2 script started - creating instances...";
                    MessageBox.Show("Configuration saved and Level 2 script started!\n\nInstances are being created in the background.", 
                        "Applied Changes", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show($"Level 2 script not found at: {scriptPath}\n\nConfiguration saved successfully.", 
                        "Script Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error running Level 2 script: {ex.Message}\n\nConfiguration was saved successfully.", 
                    "Script Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }

    // Instance Dialog Form
    public class InstanceDialog : Form
    {
        private Configuration config;
        private List<string> availableSources;
        
        // UI Controls
        private TextBox nameTextBox;
        private ComboBox brokerComboBox;
        private ComboBox platformComboBox;
        private ComboBox sourceComboBox;
        private TextBox destinationTextBox;
        private ComboBox accountTypeComboBox;
        private CheckBox enabledCheckBox;
        private CheckBox autoStartCheckBox;

        public TradingInstance Instance { get; private set; }

        public InstanceDialog(TradingInstance instance, Configuration configuration)
        {
            Instance = instance?.Clone() ?? new TradingInstance();
            config = configuration;
            availableSources = GetAvailableSources();
            InitializeDialog();
            PopulateControls();
            SetupAutoSourceDetection();
        }

        private List<string> GetAvailableSources()
        {
            var sources = new List<string>();
            
            // Get sources from PlatformInstallations folder
            var installationsPath = Path.Combine(config.TradingRoot, "PlatformInstallations");
            if (Directory.Exists(installationsPath))
            {
                foreach (var dir in Directory.GetDirectories(installationsPath))
                {
                    var dirName = Path.GetFileName(dir);
                    sources.Add(dirName);
                }
            }
            
            // Also get sources from existing instances
            foreach (var instance in config.Instances)
            {
                if (!string.IsNullOrEmpty(instance.Source) && !sources.Contains(instance.Source))
                {
                    sources.Add(instance.Source);
                }
            }
            
            return sources.OrderBy(s => s).ToList();
        }

        private void InitializeDialog()
        {
            this.Text = Instance.Name == "" ? "Add New Instance" : $"Edit Instance - {Instance.Name}";
            this.Size = new Size(600, 400);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(45, 45, 48);
            this.ForeColor = Color.White;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // Create main panel with TableLayoutPanel for better organization
            var panel = new TableLayoutPanel();
            panel.Dock = DockStyle.Fill;
            panel.Padding = new Padding(20);
            panel.BackColor = Color.FromArgb(45, 45, 48);
            panel.ColumnCount = 2;
            panel.RowCount = 8;
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            // Name
            panel.Controls.Add(new Label { Text = "Name:", ForeColor = Color.White, Anchor = AnchorStyles.Right }, 0, 0);
            nameTextBox = new TextBox { Width = 300, Anchor = AnchorStyles.Left | AnchorStyles.Right };
            panel.Controls.Add(nameTextBox, 1, 0);

            // Broker
            panel.Controls.Add(new Label { Text = "Broker:", ForeColor = Color.White, Anchor = AnchorStyles.Right }, 0, 1);
            brokerComboBox = new ComboBox 
            { 
                Width = 300, 
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                DropDownStyle = ComboBoxStyle.DropDown,
                AutoCompleteMode = AutoCompleteMode.SuggestAppend,
                AutoCompleteSource = AutoCompleteSource.ListItems
            };
            panel.Controls.Add(brokerComboBox, 1, 1);

            // Platform
            panel.Controls.Add(new Label { Text = "Platform:", ForeColor = Color.White, Anchor = AnchorStyles.Right }, 0, 2);
            platformComboBox = new ComboBox 
            { 
                Width = 300, 
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                DropDownStyle = ComboBoxStyle.DropDown,
                AutoCompleteMode = AutoCompleteMode.SuggestAppend,
                AutoCompleteSource = AutoCompleteSource.ListItems
            };
            panel.Controls.Add(platformComboBox, 1, 2);

            // Source (with auto-detect button)
            panel.Controls.Add(new Label { Text = "Source:", ForeColor = Color.White, Anchor = AnchorStyles.Right }, 0, 3);
            var sourcePanel = new Panel { Height = 25, Anchor = AnchorStyles.Left | AnchorStyles.Right };
            sourceComboBox = new ComboBox 
            { 
                Width = 250, 
                Anchor = AnchorStyles.Left,
                DropDownStyle = ComboBoxStyle.DropDown,
                AutoCompleteMode = AutoCompleteMode.SuggestAppend,
                AutoCompleteSource = AutoCompleteSource.ListItems
            };
            var autoDetectButton = new Button { Text = "Auto-Detect", Width = 85, Height = 23 };
            autoDetectButton.Location = new Point(255, 0);
            autoDetectButton.Click += AutoDetectSource;
            
            sourcePanel.Controls.AddRange(new Control[] { sourceComboBox, autoDetectButton });
            panel.Controls.Add(sourcePanel, 1, 3);

            // Destination
            panel.Controls.Add(new Label { Text = "Destination:", ForeColor = Color.White, Anchor = AnchorStyles.Right }, 0, 4);
            destinationTextBox = new TextBox { Width = 300, Anchor = AnchorStyles.Left | AnchorStyles.Right };
            panel.Controls.Add(destinationTextBox, 1, 4);

            // Account Type
            panel.Controls.Add(new Label { Text = "Account Type:", ForeColor = Color.White, Anchor = AnchorStyles.Right }, 0, 5);
            accountTypeComboBox = new ComboBox { Width = 300, Anchor = AnchorStyles.Left | AnchorStyles.Right };
            accountTypeComboBox.Items.AddRange(new[] { "demo", "live", "contest", "test" });
            panel.Controls.Add(accountTypeComboBox, 1, 5);

            // Enabled
            panel.Controls.Add(new Label { Text = "Enabled:", ForeColor = Color.White, Anchor = AnchorStyles.Right }, 0, 6);
            enabledCheckBox = new CheckBox { ForeColor = Color.White, Anchor = AnchorStyles.Left };
            panel.Controls.Add(enabledCheckBox, 1, 6);

            // Auto Start
            panel.Controls.Add(new Label { Text = "Auto Start:", ForeColor = Color.White, Anchor = AnchorStyles.Right }, 0, 7);
            autoStartCheckBox = new CheckBox { ForeColor = Color.White, Anchor = AnchorStyles.Left };
            panel.Controls.Add(autoStartCheckBox, 1, 7);

            // Buttons
            var buttonPanel = new FlowLayoutPanel();
            buttonPanel.FlowDirection = FlowDirection.RightToLeft;
            buttonPanel.Dock = DockStyle.Bottom;
            buttonPanel.Height = 40;
            buttonPanel.BackColor = Color.FromArgb(45, 45, 48);

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
            // Populate broker dropdown with existing brokers
            var existingBrokers = config.Instances
                .Select(i => i.Broker)
                .Where(b => !string.IsNullOrEmpty(b))
                .Distinct()
                .OrderBy(b => b)
                .ToList();
            
            // Also scan PlatformInstallations folder for brokers
            var installationsPath = Path.Combine(config.TradingRoot, "PlatformInstallations");
            if (Directory.Exists(installationsPath))
            {
                foreach (var dir in Directory.GetDirectories(installationsPath))
                {
                    var dirName = Path.GetFileName(dir);
                    // Extract broker name (assume format: Broker_Platform_Something)
                    var parts = dirName.Split('_');
                    if (parts.Length > 0 && !string.IsNullOrEmpty(parts[0]))
                    {
                        var brokerName = parts[0];
                        if (!existingBrokers.Contains(brokerName))
                        {
                            existingBrokers.Add(brokerName);
                        }
                    }
                }
            }
            
            existingBrokers.Sort();
            brokerComboBox.Items.Clear();
            brokerComboBox.Items.AddRange(existingBrokers.ToArray());

            // Populate platform dropdown with existing platforms
            var existingPlatforms = config.Instances
                .Select(i => i.Platform)
                .Where(p => !string.IsNullOrEmpty(p))
                .Distinct()
                .OrderBy(p => p)
                .ToList();
                
            // Also scan PlatformInstallations folder for platforms
            if (Directory.Exists(installationsPath))
            {
                foreach (var dir in Directory.GetDirectories(installationsPath))
                {
                    var dirName = Path.GetFileName(dir);
                    // Extract platform name (assume format: Broker_Platform_Something)
                    var parts = dirName.Split('_');
                    if (parts.Length > 1 && !string.IsNullOrEmpty(parts[1]))
                    {
                        var platformName = parts[1];
                        if (!existingPlatforms.Contains(platformName))
                        {
                            existingPlatforms.Add(platformName);
                        }
                    }
                }
            }
            
            existingPlatforms.Sort();
            platformComboBox.Items.Clear();
            platformComboBox.Items.AddRange(existingPlatforms.ToArray());

            // Populate source dropdown
            sourceComboBox.Items.Clear();
            sourceComboBox.Items.AddRange(availableSources.ToArray());

            // Set current values
            nameTextBox.Text = Instance.Name;
            brokerComboBox.Text = Instance.Broker;
            platformComboBox.Text = Instance.Platform;
            sourceComboBox.Text = Instance.Source;
            destinationTextBox.Text = Instance.Destination;
            accountTypeComboBox.Text = Instance.AccountType;
            enabledCheckBox.Checked = Instance.Enabled;
            autoStartCheckBox.Checked = Instance.AutoStart;
        }

        private void SetupAutoSourceDetection()
        {
            // Fire when any field is completed (Leave event)
            brokerComboBox.Leave += (s, e) => {
                AutoDetectSourceIfEmpty();
                AutoGenerateDestination();
            };
            
            platformComboBox.Leave += (s, e) => {
                AutoDetectSourceIfEmpty();
                AutoGenerateDestination();
            };

            accountTypeComboBox.Leave += (s, e) => {
                AutoDetectSourceIfEmpty();
                AutoGenerateDestination();
            };
            
            nameTextBox.Leave += (s, e) => {
                AutoGenerateDestination();
            };

            // Also fire when dropdown selections are made
            brokerComboBox.SelectedIndexChanged += (s, e) => {
                AutoDetectSourceIfEmpty();
                AutoGenerateDestination();
            };
            platformComboBox.SelectedIndexChanged += (s, e) => {
                AutoDetectSourceIfEmpty();
                AutoGenerateDestination();
            };
            accountTypeComboBox.SelectedIndexChanged += (s, e) => {
                AutoDetectSourceIfEmpty();
                AutoGenerateDestination();
            };
        }

        private void AutoGenerateDestination()
        {
            var name = nameTextBox.Text.Trim();
            var broker = brokerComboBox.Text.Trim();
            var platform = platformComboBox.Text.Trim();
            var accountType = accountTypeComboBox.Text.Trim();

            // Generate destination as: Name_Broker_Platform_AccountType
            var parts = new List<string>();
            
            if (!string.IsNullOrEmpty(name))
                parts.Add(name);
            if (!string.IsNullOrEmpty(broker))
                parts.Add(broker);
            if (!string.IsNullOrEmpty(platform))
                parts.Add(platform);
            if (!string.IsNullOrEmpty(accountType))
                parts.Add(accountType);

            if (parts.Count > 0)
            {
                string newDestination = string.Join("_", parts);
                destinationTextBox.Text = newDestination;
            }
            else
            {
                destinationTextBox.Text = "";
            }
        }

        private void AutoDetectSourceIfEmpty()
        {
            var broker = brokerComboBox.Text.Trim();
            var platform = platformComboBox.Text.Trim();
            var accountType = accountTypeComboBox.Text.Trim().ToLower();

            // Don't do anything until ALL parts are filled
            if (string.IsNullOrEmpty(broker) || string.IsNullOrEmpty(platform) || string.IsNullOrEmpty(accountType))
            {
                return;
            }

            // Find matching sources with account type preference
            var matchingSources = availableSources.Where(source => 
            {
                var sourceLower = source.ToLower();
                var brokerLower = broker.ToLower();
                var platformLower = platform.ToLower();
                
                // Check if source contains both broker and platform
                return sourceLower.Contains(brokerLower) && sourceLower.Contains(platformLower);
            }).ToList();

            if (matchingSources.Count == 0)
            {
                return; // No matches at all
            }

            // Prefer sources that match the account type
            var accountTypeMatches = matchingSources.Where(source => 
            {
                var sourceLower = source.ToLower();
                return sourceLower.Contains(accountType);
            }).ToList();

            string bestMatch = null;

            if (accountTypeMatches.Count > 0)
            {
                // Found sources matching account type - pick the best one
                bestMatch = accountTypeMatches.OrderBy(s => s.Length).First();
            }
            else
            {
                // No account type matches - pick the shortest general match
                bestMatch = matchingSources.OrderBy(s => s.Length).First();
            }

            if (!string.IsNullOrEmpty(bestMatch))
            {
                sourceComboBox.Text = bestMatch;
            }
        }

        private void AutoDetectSource(object sender, EventArgs e)
        {
            var broker = brokerComboBox.Text.Trim();
            var platform = platformComboBox.Text.Trim();
            var accountType = accountTypeComboBox.Text.Trim().ToLower();

            if (string.IsNullOrEmpty(broker) && string.IsNullOrEmpty(platform))
            {
                MessageBox.Show("Please enter Broker and/or Platform first.", "Auto-Detect Source", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Find matching sources with improved logic including account type
            var allMatches = new List<(string source, int score)>();
            
            foreach (var source in availableSources)
            {
                var sourceLower = source.ToLower();
                var score = 0;
                
                // Score for broker match
                if (!string.IsNullOrEmpty(broker) && sourceLower.Contains(broker.ToLower()))
                    score += 3;
                    
                // Score for platform match  
                if (!string.IsNullOrEmpty(platform) && sourceLower.Contains(platform.ToLower()))
                    score += 3;

                // IMPORTANT: Score for account type match
                if (!string.IsNullOrEmpty(accountType) && sourceLower.Contains(accountType))
                    score += 2;
                
                // Bonus for exact word matches
                if (!string.IsNullOrEmpty(broker) && sourceLower.Split('_').Contains(broker.ToLower()))
                    score += 1;
                    
                if (!string.IsNullOrEmpty(platform) && sourceLower.Split('_').Contains(platform.ToLower()))
                    score += 1;

                if (!string.IsNullOrEmpty(accountType) && sourceLower.Split('_').Contains(accountType))
                    score += 1;
                
                if (score > 0)
                {
                    allMatches.Add((source, score));
                }
            }

            if (allMatches.Count == 0)
            {
                MessageBox.Show("No matching sources found.", "Auto-Detect Source", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Get the best match
            var bestMatch = allMatches.OrderByDescending(m => m.score).ThenBy(m => m.source.Length).First();
            sourceComboBox.Text = bestMatch.source;

            // Show results
            if (allMatches.Count > 1)
            {
                var message = $"Found {allMatches.Count} matches. Selected: {bestMatch.source}\n\nOther matches:\n" +
                    string.Join("\n", allMatches.Take(5).Select(m => $"• {m.source} (score: {m.score})"));
                MessageBox.Show(message, "Auto-Detect Results", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void OkButton_Click(object sender, EventArgs e)
        {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(nameTextBox.Text))
            {
                MessageBox.Show("Please enter a name for the instance.", "Validation Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.DialogResult = DialogResult.None;
                return;
            }

            if (string.IsNullOrWhiteSpace(sourceComboBox.Text))
            {
                MessageBox.Show("Please select or enter a source.", "Validation Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.DialogResult = DialogResult.None;
                return;
            }

            // Save values
            Instance.Name = nameTextBox.Text.Trim();
            Instance.Broker = brokerComboBox.Text.Trim();
            Instance.Platform = platformComboBox.Text.Trim();
            Instance.Source = sourceComboBox.Text.Trim();
            Instance.Destination = string.IsNullOrWhiteSpace(destinationTextBox.Text) ? Instance.Name : destinationTextBox.Text.Trim();
            Instance.AccountType = accountTypeComboBox.Text.Trim();
            Instance.Enabled = enabledCheckBox.Checked;
            Instance.AutoStart = autoStartCheckBox.Checked;
        }
    }

    // Extension method for cloning
    public static class TradingInstanceExtensions
    {
        public static TradingInstance Clone(this TradingInstance instance)
        {
            return new TradingInstance
            {
                Name = instance.Name,
                Broker = instance.Broker,
                Platform = instance.Platform,
                Source = instance.Source,
                Destination = instance.Destination,
                AccountType = instance.AccountType,
                Enabled = instance.Enabled,
                AutoStart = instance.AutoStart
            };
        }
    }

    // Program entry point
    public class Program
    {
        [STAThread]
        public static void Main()
        {
            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new Level6InstanceManager());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fatal error: {ex.Message}\n\nStack trace:\n{ex.StackTrace}", 
                    "Level 6 Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}