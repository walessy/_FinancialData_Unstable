# Trading Environment Installation Guide

## Overview
This guide walks you through setting up a complete multi-platform trading environment with automated instance management, portable configurations, and data junction management.

## Architecture
```
TradingRoot/
├── PlatformInstallations/     # Raw platform setup files
├── InstanceData/             # Junction points to data folders
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
# Default installation to C:\TradingRoot
.\1\ Level\ 1\ Trading\ Environment\ Setup.ps1

# Custom location
.\1\ Level\ 1\ Trading\ Environment\ Setup.ps1 -TradingRootPath "D:\MyTradingSetup"
```

### Level 1 Results
After completion you'll have:
- ✅ Complete directory structure
- ✅ Chocolatey, R, and WPF installed
- ✅ Environment configuration file in `Setup\environment.config`
- ✅ Ready for platform installations

---

## Step 2: Extract Platform Installations (Manual)

### Purpose
Extract your trading platform setup files into organized folders for instance creation.

### Process
1. **Obtain Platform Installers**: Download MT4, MT5, and TraderEvolution installers from your broker(s)

2. **Extract to PlatformInstallations**: 
   - Create broker-specific folders with platform type naming
   - Extract each installer to its designated folder

### Folder Structure Example
```
PlatformInstallations/
├── AfterPrime_MT4_Demo/          # AfterPrime MT4 Demo setup files
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
- **Portable Mode**: Most modern installers support portable extraction

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
  "tradingRoot": "C:\\TradingRoot",
  "defaultDataRoot": "D:\\TradingData",
  "instances": [
    {
      "name": "AfterPrime_MT4_Live_Instance",
      "broker": "AfterPrime",
      "platform": "MT4",
      "source": "AfterPrime_MT4_Live",
      "destination": "AfterPrime_MT4_Live_Instance",
      "dataFolder": "AfterPrime_MT4_Live",
      "junctionName": "AfterPrime_MT4_Live_Data",
      "portableMode": true,
      "accountType": "live",
      "enabled": true,
      "serverSettings": {
        "server": "AfterPrime-Live",
        "autoLogin": false
      }
    }
  ]
}
```

### Basic Level 2 Features

#### Standard Instance Creation
```powershell
# Create instances from config
.\2\ Level\ 2\ Instance\ Creator.ps1

# Test what would happen (dry run)
.\2\ Level\ 2\ Instance\ Creator.ps1 -WhatIf

# Use custom config file
.\2\ Level\ 2\ Instance\ Creator.ps1 -ConfigFile "my-config.json"
```

#### What Level 2 Does (Basic)
- ✅ Reads configuration file
- ✅ Copies platform installations to PlatformInstances
- ✅ Configures portable mode (creates portable.txt files)
- ✅ Creates data folder junctions in InstanceData
- ✅ Generates instance-specific configuration files
- ✅ Skips disabled instances (`"enabled": false`)

---

## Step 4: Advanced Instance Management

### Force Recreation
```powershell
# Recreate existing instances
.\2\ Level\ 2\ Instance\ Creator.ps1 -Force
```
- Removes and recreates existing instances
- Useful when updating platform installations
- Preserves data folders (only recreates instance folders)

### Change Detection
The script automatically detects:
- ✅ **New instances**: Creates them when added to config
- ✅ **Disabled instances**: Removes them when `enabled: false`
- ✅ **Deleted instances**: Removes them when deleted from config
- ✅ **Existing instances**: Skips unless `-Force` is used

### Monitoring Mode
```powershell
# Start continuous monitoring
.\2\ Level\ 2\ Instance\ Creator.ps1 -Monitor
```

#### How Monitoring Works
- 🔍 **File Watcher**: Monitors instances-config.json for changes
- ⚡ **Instant Updates**: Automatically processes changes when file is saved
- 🔄 **Live Management**: Add/remove/modify instances without restarting
- 🛑 **Stop Monitoring**: Press `Ctrl+C` to exit

#### Monitoring Benefits
- **Dynamic Configuration**: Edit config file, changes apply immediately
- **Zero Downtime**: Modify instances while others keep running
- **Development Friendly**: Perfect for testing different configurations
- **Production Ready**: Reliable for live trading environment management

---

## Step 5: Using Your Trading Environment

### Starting Platforms
1. Navigate to `PlatformInstances\[InstanceName]`
2. Run the platform executable:
   - **MT4**: `terminal.exe`
   - **MT5**: `terminal64.exe`
   - **TraderEvolution**: Main application executable

### Data Management
- **Data Location**: Actual data stored in configured data folders
- **Junction Access**: Access data through `InstanceData\[JunctionName]`
- **Backup Strategy**: Backup both instance folders and data folders
- **Multiple Accounts**: Each instance can run different accounts simultaneously

### Configuration Management
- **Instance Settings**: Modify `instance.config` in each instance folder
- **Global Changes**: Update `instances-config.json` and rerun Level 2
- **Platform Settings**: Configure within each platform as normal

---

## Configuration Examples

### Multiple Broker Setup
```json
{
  "tradingRoot": "C:\\TradingRoot",
  "defaultDataRoot": "D:\\TradingData",
  "instances": [
    {
      "name": "AfterPrime_MT4_Live",
      "broker": "AfterPrime",
      "platform": "MT4",
      "source": "AfterPrime_MT4",
      "destination": "AfterPrime_MT4_Live",
      "enabled": true
    },
    {
      "name": "ICMarkets_MT5_Demo", 
      "broker": "ICMarkets",
      "platform": "MT5",
      "source": "ICMarkets_MT5",
      "destination": "ICMarkets_MT5_Demo",
      "enabled": true
    }
  ]
}
```

### Development vs Production
```json
{
  "instances": [
    {
      "name": "Development_MT4",
      "enabled": true,
      "accountType": "demo"
    },
    {
      "name": "Production_MT4",
      "enabled": false,
      "accountType": "live"
    }
  ]
}
```

---

## Troubleshooting

### Common Issues

**Level 1 Package Installation Fails**
- Ensure running as Administrator
- Check internet connection
- Verify Windows version compatibility

**Level 2 Source Not Found**
- Verify platform installations are extracted to PlatformInstallations
- Check folder naming matches config file
- Ensure source folders contain actual platform files

**Junction Creation Fails**
- Confirm Administrator privileges
- Check if target junction already exists
- Verify data folder path is accessible

**Monitoring Not Working**
- Ensure config file path is correct
- Check file permissions
- Verify JSON syntax is valid

### Best Practices

**Security**
- Run scripts as Administrator only when necessary
- Keep trading data on separate drive from OS
- Regular backups of both instances and data

**Organization**
- Use consistent naming conventions
- Document custom configurations
- Keep config files in version control

**Performance**  
- Use SSDs for platform instances
- Separate data storage for heavy history data
- Monitor disk space usage

---

## Next Steps

After completing this setup:

1. **Install Trading Tools**: Add EAs, indicators, and trading scripts
2. **Configure Accounts**: Set up live/demo accounts in each instance  
3. **Backup Strategy**: Implement regular backup of configurations and data
4. **Monitoring Setup**: Consider automated monitoring of trading activities
5. **Scale Up**: Add more brokers and instances as needed

Your trading environment is now ready for professional multi-platform trading operations!