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
    └── 3 SimpleTradingManager.ps1
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

## Step 5: Setup Automatic Startup (Level 3)

```powershell
# Install automatic startup (uses Windows startup folder)
.\Setup\'3 SimpleTradingManager.ps1' -Action Install
```

**That's it!** Platforms will now start automatically when you log in.

---

## Daily Usage

### Check Status
```powershell
.\Setup\'3 SimpleTradingManager.ps1' -Action Status
```

### Manual Control
```powershell
# Start all platforms now
.\Setup\'3 SimpleTradingManager.ps1' -Action Start

# Stop all platforms
.\Setup\'3 SimpleTradingManager.ps1' -Action Stop

# Remove automatic startup
.\Setup\'3 SimpleTradingManager.ps1' -Action Remove
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
     "enabled": true
   }
   ```
3. **Create the instance**: `.\Setup\'2 Level2-Clean.ps1'`
4. **Update startup**: `.\Setup\'3 SimpleTradingManager.ps1' -Action Install`

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
# Check if instances were created
Get-ChildItem "PlatformInstances" -Directory

# Check if executables exist
.\Setup\'3 SimpleTradingManager.ps1' -Action Status
```

### Level 2 Fails - Source Not Found
- Verify platform files were extracted to `PlatformInstallations\`
- Check folder names match your `instances-config.json`
- Ensure each folder contains the platform executable

### Automatic Startup Not Working
```powershell
# Check if startup file was created
Get-ChildItem "$env:APPDATA\Microsoft\Windows\Start Menu\Programs\Startup" -Filter "*.bat"

# Test manually
& "$env:APPDATA\Microsoft\Windows\Start Menu\Programs\Startup\StartTradingPlatforms.bat"
```

---

## Quick Recovery

### Reset Everything
```powershell
# Remove instances (keeps data)
Remove-Item "PlatformInstances" -Recurse -Force
Remove-Item "InstanceData" -Recurse -Force

# Recreate instances
.\Setup\'2 Level2-Clean.ps1'

# Reinstall startup
.\Setup\'3 SimpleTradingManager.ps1' -Action Install
```

### Reset Just Startup
```powershell
.\Setup\'3 SimpleTradingManager.ps1' -Action Remove
.\Setup\'3 SimpleTradingManager.ps1' -Action Install
```

---

## Configuration Tips

### Enable/Disable Platforms
Edit `instances-config.json` - set `"enabled": true` or `"enabled": false`, then reinstall startup.

### Platform Types
- **MT4**: Uses `terminal.exe`
- **MT5**: Uses `terminal64.exe`  
- **TraderEvolution**: Uses `TraderEvolution.exe`

Level 3 automatically determines the correct executable based on the `"platform"` field.

### Data Management
- **Actual data**: Stored in `TradingData\` folder
- **Access data**: Through junctions in `InstanceData\`
- **Backup**: Backup both `PlatformInstances\` and `TradingData\` folders

---

## That's It!

Your trading environment is now set up with:
- ✅ Multiple isolated platform instances
- ✅ Automatic startup when you log in  
- ✅ Simple manual control
- ✅ Easy to add new platforms
- ✅ Works with standard user privileges

Just configure your trading accounts in each platform instance and you're ready to trade!