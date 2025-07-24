# Simple Trading Environment Setup Guide

## Overview
Set up multiple trading platform instances with simple startup automation. Works with standard user privileges after initial setup.

## File Structure
```
C:\Projects\FinancialData\
├── instances-config.json          # Main configuration file
├── PlatformInstallations\         # Extract platform installers here
├── PlatformInstances\             # Created by Level 2
├── InstanceData\                  # Data junctions
├── TradingData\                   # Actual data storage
└── Setup\
    ├── 1 Level 1 Trading Environment Setup.ps1
    ├── 2 Level2-Clean.ps1
    └── 3 SimpleTradingManager.ps1  # ← UNIFIED SCRIPT (replaces all startup scripts)
```

---

## Step 1: Initial Setup (Admin Required - One Time Only)

```powershell
# Run PowerShell as Administrator
cd "C:\"

# Run Level 1 - installs software and creates directories
.\1\ Level\ 1\ Trading\ Environment\ Setup.ps1
```

**After this step:** Switch back to standard user - no more admin needed!

---

## Step 2: Extract Platform Files (Manual)

1. Download platform installers from your brokers
2. Extract to `PlatformInstallations\` folder:
   ```
   PlatformInstallations\
   ├── AfterPrime_MT4_Demo\
   │   ├── terminal.exe
   │   └── ShortCutImage\          # ← Custom icons (optional)
   │       └── icon.ico
   ├── AfterPrime_MT4_Live\
   └── AfterPrime_MT5-Live\
   ```
3. Each folder should contain the platform executable (`terminal.exe`, `terminal64.exe`, etc.)

---

## Step 3: Configure Instances

Create `instances-config.json` in your trading root:

```json
{
  "tradingRoot": "C:\\Projects\\FinancialData",
  "defaultDataRoot": "C:\\Projects\\FinancialData\\TradingData",
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
    }
  ]
}
```

---

## Step 4: Create Platform Instances (Level 2)

```powershell
# Navigate to trading directory
cd "C:\Projects\FinancialData"

# Set execution policy
Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope Process

# Create instances
.\Setup\'2 Level2-Clean.ps1'
```

This creates separate, runnable copies of each platform.

---

## Step 5: Setup Complete Automation (Level 3 - Unified)

```powershell
# Install complete automation (startup + shortcuts + icons)
.\Setup\'3 SimpleTradingManager.ps1' -Action Install
```

**What this does:**
- ✅ Sets up automatic startup when you log in
- ✅ Creates desktop shortcuts with custom icons
- ✅ Configures all enabled platforms
- ✅ Applies startup delays to prevent conflicts

**That's it!** Platforms will now start automatically when you log in, and you'll have desktop shortcuts with custom icons.

---

## Daily Usage

### Check Everything
```powershell
.\Setup\'3 SimpleTradingManager.ps1' -Action Status
```
**Shows:** Running platforms, auto-start status, shortcut count

### Manual Control
```powershell
# Start all platforms now
.\Setup\'3 SimpleTradingManager.ps1' -Action Start

# Stop all platforms
.\Setup\'3 SimpleTradingManager.ps1' -Action Stop

# Remove all automation
.\Setup\'3 SimpleTradingManager.ps1' -Action Remove

# Get detailed help
.\Setup\'3 SimpleTradingManager.ps1' -Action Help
```

### Advanced Options
```powershell
# Install without desktop shortcuts
.\Setup\'3 SimpleTradingManager.ps1' -Action Install -CreateShortcuts:$false

# Silent operation (for scripts)
.\Setup\'3 SimpleTradingManager.ps1' -Action Start -Quiet

# Reinstall automation (updates changes)
.\Setup\'3 SimpleTradingManager.ps1' -Action Remove
.\Setup\'3 SimpleTradingManager.ps1' -Action Install
```

---

## Custom Icons

**Automatic Icon Detection:**
1. Place `.ico` files in `PlatformInstallations\[BrokerName]\ShortCutImage\` folder
2. Script automatically finds and uses them for shortcuts
3. Fallback to platform executable icon if none found

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

## Adding New Platforms

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
       "startupDelay": 15
     }
   }
   ```
3. **Create the instance**: `.\Setup\'2 Level2-Clean.ps1'`
4. **Update automation**: `.\Setup\'3 SimpleTradingManager.ps1' -Action Install`

---

## Configuration Control

### Enable/Disable Platforms
```json
{
  "enabled": true,        // Platform exists and can be used
  "startupSettings": {
    "autoStart": true     // Starts automatically with Windows
  }
}
```

**Combinations:**
- `enabled: false` = Platform completely disabled
- `enabled: true, autoStart: false` = Platform available but manual start only
- `enabled: true, autoStart: true` = Platform starts automatically

### Platform Types
- **MT4**: Uses `terminal.exe`
- **MT5**: Uses `terminal64.exe`  
- **TraderEvolution**: Uses `TradeTerminal.exe`

Script automatically detects correct executable based on `platform` field.

---

## Troubleshooting

### Scripts Won't Run
```powershell
# Set execution policy
Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope Process

# Or unblock files
Unblock-File ".\Setup\*.ps1"
```

### Platforms Don't Start
```powershell
# Check detailed status
.\Setup\'3 SimpleTradingManager.ps1' -Action Status

# Check if instances were created
Get-ChildItem "PlatformInstances" -Directory

# Test manual start
.\Setup\'3 SimpleTradingManager.ps1' -Action Start
```

### Level 2 Fails - Source Not Found
- Verify platform files were extracted to `PlatformInstallations\`
- Check folder names match your `instances-config.json`
- Ensure each folder contains the platform executable

### Automatic Startup Not Working
```powershell
# Check if startup file was created
Get-ChildItem "$env:APPDATA\Microsoft\Windows\Start Menu\Programs\Startup" -Filter "*.bat"

# Test startup file manually
& "$env:APPDATA\Microsoft\Windows\Start Menu\Programs\Startup\StartTradingPlatforms.bat"

# Reinstall automation
.\Setup\'3 SimpleTradingManager.ps1' -Action Remove
.\Setup\'3 SimpleTradingManager.ps1' -Action Install
```

### Desktop Shortcuts Issues
```powershell
# Check if shortcuts exist
$desktop = [Environment]::GetFolderPath("Desktop")
Get-ChildItem $desktop -Filter "*Instance*.lnk"

# Recreate shortcuts only
.\Setup\'3 SimpleTradingManager.ps1' -Action Remove
.\Setup\'3 SimpleTradingManager.ps1' -Action Install -CreateShortcuts:$true
```

---

## Quick Recovery

### Reset Everything
```powershell
# Stop and remove all automation
.\Setup\'3 SimpleTradingManager.ps1' -Action Stop
.\Setup\'3 SimpleTradingManager.ps1' -Action Remove

# Remove instances (keeps data)
Remove-Item "PlatformInstances" -Recurse -Force
Remove-Item "InstanceData" -Recurse -Force

# Recreate instances
.\Setup\'2 Level2-Clean.ps1'

# Reinstall automation
.\Setup\'3 SimpleTradingManager.ps1' -Action Install
```

### Update Configuration
```powershell
# After editing instances-config.json:
.\Setup\'2 Level2-Clean.ps1'                              # Recreate instances
.\Setup\'3 SimpleTradingManager.ps1' -Action Install      # Update automation
```

---

## Data Management
- **Actual data**: Stored in `TradingData\` folder
- **Access data**: Through junctions in `InstanceData\`
- **Backup**: Backup both `PlatformInstances\` and `TradingData\` folders
- **Custom icons**: Store in `PlatformInstallations\[Broker]\ShortCutImage\`

---

## Summary

Your unified trading environment provides:
- ✅ Multiple isolated platform instances
- ✅ Automatic startup when you log in  
- ✅ Desktop shortcuts with custom icons
- ✅ Simple manual control
- ✅ Easy to add new platforms
- ✅ Works with standard user privileges
- ✅ One script handles everything

Just configure your trading accounts in each platform instance and you're ready to trade!