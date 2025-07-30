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
    └── 3 trading_manager.ps1       # ← UNIFIED SCRIPT
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
   │   └── ShortCutImage\          # ← Custom icons go here
   │       └── mt4_demo.ico
   ├── AfterPrime_MT4_Live\
   │   ├── terminal.exe
   │   └── ShortCutImage\
   │       └── mt4_live.ico        # ← Different icon for live
   └── AfterPrime_MT5-Live\
       ├── terminal64.exe
       └── ShortCutImage\
           └── mt5_live.ico
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

## Step 5: Setup Complete Automation (Level 3)

```powershell
# Install complete automation (startup + shortcuts + icons)
.\Setup\'3 trading_manager.ps1' -Action Install
```

**What this does:**
- ✅ Sets up automatic startup when you log in
- ✅ Creates "Trading Platforms" folder on desktop
- ✅ Creates desktop shortcuts with custom icons
- ✅ Automatically creates ShortCutImage folders
- ✅ Configures all enabled platforms with /portable arguments

**That's it!** Platforms will now start automatically when you log in with organized shortcuts.

---

## 🎯 **IMPORTANT: Custom Icons for Application Identification**

### Why Custom Icons Matter
**While custom icons are not compulsory, they are HIGHLY RECOMMENDED** for differentiating applications running in the taskbar. This is especially critical when running multiple instances of the same platform:

**⚠️ Problem Without Custom Icons:**
- Multiple MT4 instances (Demo + Live) all show the same icon
- Difficult to distinguish between Demo and Live accounts in taskbar
- Risk of accidentally trading on wrong account type
- Confusion when Alt+Tab switching between applications

**✅ Solution With Custom Icons:**
- Each instance has a unique, recognizable icon
- Instant visual identification in taskbar and Alt+Tab
- Reduced risk of account mix-ups
- Professional, organized appearance

### Recommended Icon Strategy
```
Demo Accounts:    Use cooler colors (blue, green, gray)
Live Accounts:    Use warmer colors (red, orange, gold)
Different Brokers: Use distinct shapes or symbols
```

### Icon Implementation
1. **Place custom `.ico` files** in each platform's `ShortCutImage\` folder
2. **Script automatically detects** and applies them during setup
3. **Icons appear in:**
   - Desktop shortcuts
   - Taskbar when applications are running
   - Alt+Tab application switcher
   - Windows Start Menu

### Example Icon Organization
```
PlatformInstallations\
├── AfterPrime_MT4_Demo\ShortCutImage\mt4_demo_blue.ico     # Blue for demo
├── AfterPrime_MT4_Live\ShortCutImage\mt4_live_red.ico      # Red for live
├── AfterPrime_MT5_Demo\ShortCutImage\mt5_demo_green.ico    # Green for demo
└── AfterPrime_MT5_Live\ShortCutImage\mt5_live_orange.ico   # Orange for live
```

**💡 Pro Tip:** Create a consistent color scheme across all your trading platforms for instant recognition!

---

## Daily Usage

### Check Everything
```powershell
.\Setup\'3 trading_manager.ps1' -Action Status
```
**Shows:** Running platforms, auto-start status, shortcut count, icon detection

### Manual Control
```powershell
# Start all platforms now
.\Setup\'3 trading_manager.ps1' -Action Start

# Stop all platforms
.\Setup\'3 trading_manager.ps1' -Action Stop

# Remove all automation
.\Setup\'3 trading_manager.ps1' -Action Remove

# Get detailed help
.\Setup\'3 trading_manager.ps1' -Action Help
```

### Advanced Options
```powershell
# Install without desktop shortcuts
.\Setup\'3 trading_manager.ps1' -Action Install -CreateShortcuts:$false

# Silent operation (for scripts)
.\Setup\'3 trading_manager.ps1' -Action Start -Quiet

# Reinstall automation (updates changes including new icons)
.\Setup\'3 trading_manager.ps1' -Action Remove
.\Setup\'3 trading_manager.ps1' -Action Install
```

---

## Adding New Platforms

1. **Extract new platform** to `PlatformInstallations\`
2. **Add custom icon** to `ShortCutImage\` folder (recommended)
3. **Add to instances-config.json**:
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
4. **Create the instance**: `.\Setup\'2 Level2-Clean.ps1'`
5. **Update automation**: `.\Setup\'3 trading_manager.ps1' -Action Install`

---

## Troubleshooting

### Icons Not Showing
- Check that `.ico` files are in `ShortCutImage\` folders
- Verify icon files are not corrupted
- Re-run: `.\Setup\'3 trading_manager.ps1' -Action Install`

### Platforms Not Starting
```powershell
# Check detailed status
.\Setup\'3 trading_manager.ps1' -Action Status

# Manual start test
.\Setup\'3 trading_manager.ps1' -Action Start
```

### Reset Everything
```powershell
.\Setup\'3 trading_manager.ps1' -Action Remove
.\Setup\'2 Level2-Clean.ps1' -Force
.\Setup\'3 trading_manager.ps1' -Action Install
```

---

## Summary

Your unified trading environment provides:
- ✅ Multiple isolated platform instances
- ✅ Automatic startup when you log in  
- ✅ Organized desktop shortcuts with custom icons
- ✅ Visual differentiation between Demo/Live accounts
- ✅ Simple manual control
- ✅ Easy to add new platforms
- ✅ Works with standard user privileges
- ✅ Professional taskbar organization

**Configure your trading accounts in each platform instance and you're ready to trade safely with clear visual identification!**