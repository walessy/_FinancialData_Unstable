# Trading Environment Installation Guide

## Overview
This guide walks you through setting up a complete multi-platform trading environment with automated instance management, portable configurations, and data junction management using a unified three-level approach.

## Architecture
```
FinancialData/
├── PlatformInstallations/     # Raw platform setup files
├── InstanceData/             # Junction access points to isolated data
├── Setup/                    # Configuration files and scripts
└── PlatformInstances/        # Running platform instances
```

---

## Step 1: Environment Setup (Level 1)

### Prerequisites
- Windows 10/11 with Administrator privileges
- Internet connection for package downloads

### What Level 1 Does
- Installs Chocolatey package manager
- Installs R statistical computing environment
- Ensures WPF (Windows Presentation Foundation) dependencies
- Creates the complete directory structure
- Generates environment configuration file

### Running Level 1
1. Download `1 Level 1 Trading Environment Setup.ps1`
2. Open PowerShell as Administrator
3. Navigate to your desired location (e.g., `C:\`)
4. Run the script:

```powershell
# Default installation to C:\Projects\FinancialData
.\1\ Level\ 1\ Trading\ Environment\ Setup.ps1

# Custom location
.\1\ Level\ 1\ Trading\ Environment\ Setup.ps1 -TradingRootPath "D:\MyTradingEnvironment"
```

### Level 1 Results
After completion you'll have:
- ✅ Complete directory structure
- ✅ Chocolatey, R, and WPF installed
- ✅ Environment configuration file in `Setup\environment.config`
- ✅ Ready for platform installations

**Important**: After Level 1, you can switch back to standard user privileges - no more admin rights needed!

---

## Step 2: Extract Platform Installations (Manual)

### Purpose
Extract your trading platform setup files into organized folders for instance creation.

### Process
1. **Obtain Platform Installers**: Download MT4, MT5, and TraderEvolution installers from your broker(s)

2. **Extract to PlatformInstallations**: 
   - Create broker-specific folders with platform type naming
   - Extract each installer to its designated folder
   - Optionally add custom icons

### Folder Structure Example
```
PlatformInstallations/
├── AfterPrime_MT4_Demo/          # AfterPrime MT4 Demo setup files
│   ├── terminal.exe
│   └── ShortCutImage/            # ← Optional custom icons
│       └── mt4_icon.ico
├── AfterPrime_MT4_Live/          # AfterPrime MT4 Live setup files
├── AfterPrime_MT5-Live/          # AfterPrime MT5 Live setup files
├── AfterPrime_TraderEvolution-Live/  # AfterPrime TE Live setup files
└── AfterPrime_TraderEvolution-Demo/  # AfterPrime TE Demo setup files
```

### Platform Extraction Notes
- **MT4/MT5**: Extract installer contents (not just the .exe)
- **TraderEvolution**: Extract full application folder
- **Naming Convention**: Match your actual folder names exactly
  - Use hyphens for Live/Demo distinctions: `AfterPrime_MT5-Live`
  - Use underscores for platform separations: `AfterPrime_MT4_Demo`
  - Separate installations for Live vs Demo accounts
- **Custom Icons**: Place `.ico` files in `ShortCutImage\` subfolder

### Verification
After extraction, each folder should contain:
- **MT4**: `terminal.exe`, `metaeditor.exe`, config folders
- **MT5**: `terminal64.exe`, `metaeditor64.exe`, config folders  
- **TraderEvolution**: Main executable and supporting files

---

## Step 3: Configure Instances (Level 2)

### Overview
Level 2 creates configured, runnable instances from your platform installations using a JSON configuration file.

### Configuration File Setup

1. **Create instances-config.json** in your TradingRoot directory:

```json
{
  "tradingRoot": "C:\\Projects\\FinancialData",
  "defaultDataRoot": "C:\\Projects\\FinancialData\\Data",
  "instances": [
    {
      "name": "AfterPrime_MT4_Demo_Instance",
      "broker": "AfterPrime",
      "platform": "MT4",
      "source": "AfterPrime_MT4_Demo",
      "destination": "AfterPrime_MT4_Demo_Instance",
      "dataFolder": "AfterPrime_MT4_Demo",
      "junctionName": "AfterPrime_MT4_Demo_Data",
      "portableMode": true,
      "accountType": "demo",
      "enabled": true,
      "serverSettings": {
        "server": "AfterPrime-Demo",
        "autoLogin": false
      },
      "startupSettings": {
        "autoStart": true,
        "startupDelay": 0,
        "executable": "terminal.exe",
        "arguments": "/portable",
        "priority": "normal"
      }
    },
    {
      "name": "AfterPrime_MT5_Live_Instance",
      "broker": "AfterPrime",
      "platform": "MT5",
      "source": "AfterPrime_MT5-Live",
      "destination": "AfterPrime_MT5_Live_Instance",
      "dataFolder": "AfterPrime_MT5_Live",
      "junctionName": "AfterPrime_MT5_Live_Data",
      "portableMode": true,
      "accountType": "live",
      "enabled": true,
      "serverSettings": {
        "server": "AfterPrime-Live",
        "autoLogin": false
      },
      "startupSettings": {
        "autoStart": true,
        "startupDelay": 15,
        "executable": "terminal64.exe",
        "arguments": "/portable",
        "priority": "high"
      }
    }
  ]
}
```

### Running Level 2

```powershell
# Navigate to trading directory
cd "C:\Projects\FinancialData"

# Set execution policy (if needed)
Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope Process

# Create instances from config
.\Setup\'2 Level2-Clean.ps1'

# Test what would happen (dry run)
.\Setup\'2 Level2-Clean.ps1' -WhatIf

# Force recreation of existing instances
.\Setup\'2 Level2-Clean.ps1' -Force
```

#### What Level 2 Does
- ✅ Reads configuration file
- ✅ Copies platform installations to PlatformInstances
- ✅ Configures portable mode (creates portable.txt files)
- ✅ Creates data folder junctions in InstanceData
- ✅ Generates instance-specific configuration files
- ✅ Handles change detection (adds/removes/updates instances)

### Change Detection Features
The script automatically detects:
- ✅ **New instances**: Creates them when added to config
- ✅ **Disabled instances**: Removes them when `enabled: false`
- ✅ **Deleted instances**: Removes them when deleted from config
- ✅ **Existing instances**: Skips unless `-Force` is used

---

## Step 4: Unified Automation Management (Level 3)

### Overview
Level 3 provides complete automation management through a single unified script that handles automatic startup, desktop shortcuts, and manual control.

### Unified Script Features

The `3 SimpleTradingManager.ps1` script replaces all separate automation scripts and provides:
- ✅ Automatic startup with Windows login
- ✅ Desktop shortcuts with custom icons
- ✅ Manual start/stop control
- ✅ Status monitoring
- ✅ Silent operation modes
- ✅ Comprehensive help system

### Basic Operations

#### Initial Setup
```powershell
# Install complete automation (startup + shortcuts + icons)
.\Setup\'3 SimpleTradingManager.ps1' -Action Install
```

**What this does:**
- Sets up automatic startup when you log in
- Creates desktop shortcuts with custom icons
- Configures all enabled platforms
- Applies startup delays to prevent conflicts

#### Daily Control
```powershell
# Check status of all platforms
.\Setup\'3 SimpleTradingManager.ps1' -Action Status

# Start all enabled platforms manually
.\Setup\'3 SimpleTradingManager.ps1' -Action Start

# Stop all running platforms
.\Setup\'3 SimpleTradingManager.ps1' -Action Stop

# Remove all automation
.\Setup\'3 SimpleTradingManager.ps1' -Action Remove

# Get comprehensive help
.\Setup\'3 SimpleTradingManager.ps1' -Action Help
```

### Advanced Options

#### Installation Options
```powershell
# Install without desktop shortcuts
.\Setup\'3 SimpleTradingManager.ps1' -Action Install -CreateShortcuts:$false

# Silent operation (for scripts/automation)
.\Setup\'3 SimpleTradingManager.ps1' -Action Install -Quiet

# Reinstall automation (updates changes)
.\Setup\'3 SimpleTradingManager.ps1' -Action Remove
.\Setup\'3 SimpleTradingManager.ps1' -Action Install
```

#### Status and Monitoring
```powershell
# Detailed status (shows running platforms, auto-start status, shortcuts)
.\Setup\'3 SimpleTradingManager.ps1' -Action Status

# Silent status check (for scripts)
.\Setup\'3 SimpleTradingManager.ps1' -Action Status -Quiet
```

### Custom Icons Support

**Automatic Icon Detection:**
1. Place `.ico` files in `PlatformInstallations\[BrokerName]\ShortCutImage\` folder
2. Script automatically finds and uses them for shortcuts
3. Supports multiple image formats (prefers .ico)
4. Fallback to platform executable icon if none found

**Example Structure:**
```
PlatformInstallations\
├── AfterPrime_MT4_Demo\
│   ├── terminal.exe
│   └── ShortCutImage\
│       └── mt4_icon.ico        # ← Custom icon
└── AfterPrime_MT5-Live\
    ├── terminal64.exe
    └── ShortCutImage\
        └── mt5_icon.ico        # ← Custom icon
```

---

## Step 5: Configuration Management

### Instance Control Patterns

#### Enable/Disable Platforms
```json
{
  "enabled": true,        // Platform exists and can be used
  "startupSettings": {
    "autoStart": true     // Starts automatically with Windows
  }
}
```

**Control Combinations:**
- `enabled: false` = Platform completely disabled
- `enabled: true, autoStart: false` = Platform available but manual start only
- `enabled: true, autoStart: true` = Platform starts automatically

#### Platform Types and Executables
The script automatically detects executables based on platform type:
- **MT4**: Uses `terminal.exe`
- **MT5**: Uses `terminal64.exe`  
- **TraderEvolution**: Uses `TradeTerminal.exe`

Override with custom executable in `startupSettings.executable` if needed.

### Adding New Platforms

1. **Extract new platform** to `PlatformInstallations\`
2. **Add to instances-config.json**:
   ```json
   {
     "name": "NewBroker_MT4_Instance",
     "platform": "MT4",
     "source": "NewBroker_MT4",
     "destination": "NewBroker_MT4_Instance",
     "enabled": true,
     "startupSettings": {
       "autoStart": true,
       "startupDelay": 20
     }
   }
   ```
3. **Update instances**: `.\Setup\'2 Level2-Clean.ps1'`
4. **Update automation**: `.\Setup\'3 SimpleTradingManager.ps1' -Action Install`

### Configuration Update Workflow

When you modify `instances-config.json`:

```powershell
# 1. Update instances to reflect config changes
.\Setup\'2 Level2-Clean.ps1'

# 2. Update automation to reflect new instances
.\Setup\'3 SimpleTradingManager.ps1' -Action Install

# 3. Verify everything is working
.\Setup\'3 SimpleTradingManager.ps1' -Action Status
```

---

## Step 6: Data Management

### Data Architecture
- **Data Isolation**: Each instance has completely separate data through junction system
- **Junction Access**: Platforms access their data through `InstanceData\` junction points
- **Instance Settings**: Platform-specific settings isolated in `PlatformInstances\`
- **Custom Icons**: Store in `PlatformInstallations\[Broker]\ShortCutImage\`

### Backup Strategy
```powershell
# Stop all platforms
.\Setup\'3 SimpleTradingManager.ps1' -Action Stop

# Create backup
$BackupDir = "C:\TradingBackup\$(Get-Date -Format 'yyyy-MM-dd')"
New-Item -ItemType Directory -Path $BackupDir -Force

# Backup critical components
Copy-Item "instances-config.json" "$BackupDir\"
Copy-Item "PlatformInstances" "$BackupDir\" -Recurse    # Settings, EAs, data
Copy-Item "Setup" "$BackupDir\" -Recurse               # Setup scripts
```

### Data Isolation
Each instance has:
- **Separate data storage**: Prevents cross-contamination through junction system
- **Independent settings**: EAs, indicators, templates
- **Isolated histories**: Price data and account histories  
- **Individual configurations**: Server settings, login info

---

## Troubleshooting

### Common Issues and Solutions

#### Scripts Won't Run
```powershell
# Set execution policy
Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope Process

# Or unblock specific files
Unblock-File ".\Setup\*.ps1"
```

#### Level 2 Fails - Source Not Found
- Verify platform files were extracted to `PlatformInstallations\`
- Check folder names match your `instances-config.json` exactly
- Ensure each folder contains the platform executable

#### Junction Creation Fails
```powershell
# Clean up existing junctions
Get-ChildItem "InstanceData" -Directory | ForEach-Object { 
    cmd /c rmdir "$($_.FullName)" 2>$null 
}

# Recreate instances
.\Setup\'2 Level2-Clean.ps1' -Force
```

#### Platforms Don't Start
```powershell
# Check detailed status
.\Setup\'3 SimpleTradingManager.ps1' -Action Status

# Check if instances were created
Get-ChildItem "PlatformInstances" -Directory

# Try manual start
.\Setup\'3 SimpleTradingManager.ps1' -Action Start
```

#### Automatic Startup Not Working
```powershell
# Check if startup file was created
Get-ChildItem "$env:APPDATA\Microsoft\Windows\Start Menu\Programs\Startup" -Filter "*.bat"

# Reinstall automation
.\Setup\'3 SimpleTradingManager.ps1' -Action Remove
.\Setup\'3 SimpleTradingManager.ps1' -Action Install

# Verify
.\Setup\'3 SimpleTradingManager.ps1' -Action Status
```

### Complete Environment Reset

If you need to start fresh:

```powershell
# Stop everything and remove automation
.\Setup\'3 SimpleTradingManager.ps1' -Action Stop
.\Setup\'3 SimpleTradingManager.ps1' -Action Remove

# Remove generated directories (keeps platform files)
Remove-Item "PlatformInstances" -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item "InstanceData" -Recurse -Force -ErrorAction SilentlyContinue

# Backup and remove config
Copy-Item "instances-config.json" "instances-config-backup.json"
Remove-Item "instances-config.json"

# Now create new config and start fresh
# 1. Create new instances-config.json
# 2. Run Level 2: .\Setup\'2 Level2-Clean.ps1'
# 3. Run Level 3: .\Setup\'3 SimpleTradingManager.ps1' -Action Install
```

---

## Performance Optimization

### Resource Management
- **Priority Settings**: Set `priority: "high"` for live trading, `"normal"` for demo
- **Startup Delays**: Use `startupDelay` to stagger platform starts (15-30 seconds apart)
- **Memory Limits**: Configure appropriate limits for your system
- **Disable Unused**: Set `enabled: false` for platforms you don't need

### High-Performance Configuration Example
```json
{
  "instances": [
    {
      "name": "Live_Trading_Instance",
      "enabled": true,
      "startupSettings": {
        "autoStart": true,
        "priority": "high",
        "startupDelay": 0,
        "memoryLimit": "2GB"
      }
    },
    {
      "name": "Demo_Testing_Instance",
      "enabled": true,
      "startupSettings": {
        "autoStart": false,  // Manual start only
        "priority": "normal",
        "startupDelay": 30
      }
    }
  ]
}
```

---

## Advanced Use Cases

### Multiple Broker Setup
```json
{
  "instances": [
    {
      "name": "AfterPrime_MT4_Live",
      "broker": "AfterPrime",
      "source": "AfterPrime_MT4_Live",
      "enabled": true
    },
    {
      "name": "ICMarkets_MT5_Live",
      "broker": "ICMarkets",
      "source": "ICMarkets_MT5_Live",
      "enabled": true
    }
  ]
}
```

### Development Environment
```json
{
  "instances": [
    {
      "name": "Development_MT4",
      "enabled": true,
      "accountType": "demo",
      "startupSettings": {
        "autoStart": false,  // Manual control
        "priority": "low"
      }
    },
    {
      "name": "Production_MT4",
      "enabled": true,
      "accountType": "live",
      "startupSettings": {
        "autoStart": true,
        "priority": "high"
      }
    }
  ]
}
```

### VPS Deployment
```json
{
  "tradingRoot": "D:\\TradingEnvironment",
  "instances": [
    {
      "name": "VPS_Instance",
      "enabled": true,
      "startupSettings": {
        "autoStart": true,
        "startupDelay": 60,    // Extra delay for VPS
        "memoryLimit": "512MB" // Conservative for VPS
      }
    }
  ]
}
```

---

## Migration Guide

### Moving to New Computer

**On Old Computer:**
```powershell
# Create migration package
.\Setup\'3 SimpleTradingManager.ps1' -Action Stop
$ExportPath = "C:\TradingExport"
Copy-Item "instances-config.json" "$ExportPath\"
Copy-Item "PlatformInstallations" "$ExportPath\" -Recurse
Copy-Item "TradingData" "$ExportPath\" -Recurse
Copy-Item "Setup" "$ExportPath\" -Recurse
```

**On New Computer:**
```powershell
# 1. Run Level 1 setup (admin required)
.\Setup\'1 Level 1 Trading Environment Setup.ps1'

# 2. Copy migration files
# 3. Update paths in instances-config.json if needed
# 4. Run Level 2
.\Setup\'2 Level2-Clean.ps1'

# 5. Run Level 3
.\Setup\'3 SimpleTradingManager.ps1' -Action Install

# 6. Verify
.\Setup\'3 SimpleTradingManager.ps1' -Action Status
```

---

## Next Steps

After completing this setup:

1. **Configure Trading Accounts**: Set up accounts in each platform instance
2. **Install Trading Tools**: Add EAs, indicators, and scripts as needed
3. **Set Up Monitoring**: Use the status command to monitor platform health
4. **Implement Backup Strategy**: Regular backups of configurations and data
5. **Optimize Performance**: Adjust priority and delay settings as needed
6. **Scale Up**: Add more brokers and instances as your trading grows

Your trading environment is now ready for professional multi-platform trading operations with unified, simple management through a single powerful script!