# Complete Trading Environment Use Cases & Scenarios

## Overview
This document covers all possible use cases, configurations, and scenarios for the trading environment setup. Use this as a complete reference for any situation you might encounter.

---

## Use Case Categories

### 1. Basic Single Trader Setup
### 2. Multi-Broker Professional Setup  
### 3. Demo vs Live Account Management
### 4. Development & Testing Scenarios
### 5. Team/Multi-User Scenarios
### 6. Advanced Automation Scenarios
### 7. Backup & Recovery Scenarios
### 8. Troubleshooting Scenarios
### 9. Clean Up and Starting Afresh Scenarios

---

## 1. Basic Single Trader Setup

### 1.1 Single Broker, Single Platform
**Scenario**: New trader with one MT4 demo account from AfterPrime

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
      "startupSettings": {
        "autoStart": true,
        "startupDelay": 0,
        "executable": "terminal.exe",
        "arguments": "/portable"
      }
    }
  ]
}
```

**Setup Steps**:
1. Run Level 1 (admin, one-time)
2. Extract AfterPrime MT4 demo to `PlatformInstallations\AfterPrime_MT4_Demo\`
3. Create config file above
4. Run Level 2: `.\Setup\'2 Level2-Clean.ps1'`
5. Install automation: `.\Setup\'3 SimpleTradingManager.ps1' -Action Install`

### 1.2 Single Broker, Multiple Platforms
**Scenario**: Trader wants both MT4 and MT5 from same broker

```json
{
  "instances": [
    {
      "name": "AfterPrime_MT4_Demo_Instance",
      "platform": "MT4",
      "source": "AfterPrime_MT4_Demo",
      "enabled": true,
      "startupSettings": {
        "autoStart": true,
        "startupDelay": 0,
        "executable": "terminal.exe",
        "arguments": "/portable"
      }
    },
    {
      "name": "AfterPrime_MT5_Demo_Instance", 
      "platform": "MT5",
      "source": "AfterPrime_MT5_Demo",
      "enabled": true,
      "startupSettings": {
        "autoStart": true,
        "startupDelay": 15,
        "executable": "terminal64.exe",
        "arguments": "/portable"
      }
    }
  ]
}
```

**Key Points**:
- Different `startupDelay` to stagger startup
- Different executables for MT4 vs MT5
- Same broker, different platform installations needed

---

## 2. Multi-Broker Professional Setup

### 2.1 Multiple Brokers for Diversification
**Scenario**: Professional trader using 3 different brokers

```json
{
  "instances": [
    {
      "name": "AfterPrime_MT4_Live_Instance",
      "broker": "AfterPrime", 
      "platform": "MT4",
      "source": "AfterPrime_MT4_Live",
      "accountType": "live",
      "enabled": true,
      "startupSettings": {
        "autoStart": true,
        "startupDelay": 0,
        "priority": "high"
      }
    },
    {
      "name": "ICMarkets_MT5_Live_Instance",
      "broker": "ICMarkets",
      "platform": "MT5", 
      "source": "ICMarkets_MT5_Live",
      "accountType": "live",
      "enabled": true,
      "startupSettings": {
        "autoStart": true,
        "startupDelay": 20,
        "priority": "high"
      }
    },
    {
      "name": "FXCM_TraderEvolution_Live_Instance",
      "broker": "FXCM",
      "platform": "TraderEvolution",
      "source": "FXCM_TraderEvolution_Live", 
      "accountType": "live",
      "enabled": true,
      "startupSettings": {
        "autoStart": true,
        "startupDelay": 40,
        "executable": "TradeTerminal.exe",
        "priority": "normal"
      }
    }
  ]
}
```

**Directory Structure Needed**:
```
PlatformInstallations\
├── AfterPrime_MT4_Live\
├── ICMarkets_MT5_Live\
└── FXCM_TraderEvolution_Live\
```

---

## 3. Demo vs Live Account Management

### 3.1 Parallel Demo and Live Testing
**Scenario**: Trader running same EA on demo and live simultaneously

```json
{
  "instances": [
    {
      "name": "AfterPrime_MT4_Demo_Instance",
      "accountType": "demo",
      "source": "AfterPrime_MT4_Demo",
      "enabled": true,
      "startupSettings": {
        "autoStart": true,
        "startupDelay": 0,
        "priority": "normal"
      }
    },
    {
      "name": "AfterPrime_MT4_Live_Instance",
      "accountType": "live", 
      "source": "AfterPrime_MT4_Live",
      "enabled": true,
      "startupSettings": {
        "autoStart": true,
        "startupDelay": 10,
        "priority": "high"
      }
    }
  ]
}
```

### 3.2 Demo-Only Development Environment
**Scenario**: Developer testing EAs, only wants demo accounts

```json
{
  "instances": [
    {
      "name": "Dev_MT4_Demo1_Instance",
      "accountType": "demo",
      "source": "AfterPrime_MT4_Demo", 
      "enabled": true,
      "startupSettings": {
        "autoStart": false,
        "startupDelay": 0
      }
    },
    {
      "name": "Dev_MT4_Demo2_Instance",
      "accountType": "demo",
      "source": "AfterPrime_MT4_Demo",
      "enabled": true, 
      "startupSettings": {
        "autoStart": false,
        "startupDelay": 0
      }
    }
  ]
}
```

**Usage**:
- `autoStart: false` - manual control only
- Start individually: `.\Setup\'3 SimpleTradingManager.ps1' -Action Start`

### 3.3 Graduated Live Trading
**Scenario**: Trader moving from demo to live gradually

**Phase 1 Config** (Demo only):
```json
{
  "instances": [
    {
      "name": "AfterPrime_MT4_Demo_Instance",
      "accountType": "demo",
      "enabled": true,
      "startupSettings": { "autoStart": true }
    },
    {
      "name": "AfterPrime_MT4_Live_Instance", 
      "accountType": "live",
      "enabled": false,  // Disabled initially
      "startupSettings": { "autoStart": false }
    }
  ]
}
```

**Phase 2 Config** (Both demo and live):
```json
{
  "instances": [
    {
      "name": "AfterPrime_MT4_Demo_Instance",
      "enabled": true,
      "startupSettings": { "autoStart": true }
    },
    {
      "name": "AfterPrime_MT4_Live_Instance",
      "enabled": true,  // Now enabled
      "startupSettings": { "autoStart": true }
    }
  ]
}
```

**Transition Steps**:
1. Edit config file
2. Run Level 2: `.\Setup\'2 Level2-Clean.ps1'`
3. Update automation: `.\Setup\'3 SimpleTradingManager.ps1' -Action Install`

---

## 4. Development & Testing Scenarios

### 4.1 EA Development Environment
**Scenario**: Developer needs multiple MT4 instances for EA testing

```json
{
  "instances": [
    {
      "name": "Dev_Primary_MT4_Instance",
      "source": "AfterPrime_MT4_Demo",
      "enabled": true,
      "startupSettings": {
        "autoStart": true,
        "startupDelay": 0
      }
    },
    {
      "name": "Dev_Testing_MT4_Instance",
      "source": "AfterPrime_MT4_Demo", 
      "enabled": true,
      "startupSettings": {
        "autoStart": false,  // Manual start for testing
        "startupDelay": 0
      }
    },
    {
      "name": "Dev_Backup_MT4_Instance",
      "source": "AfterPrime_MT4_Demo",
      "enabled": false,  // Only enable when needed
      "startupSettings": {
        "autoStart": false,
        "startupDelay": 0
      }
    }
  ]
}
```

**Development Workflow**:
1. Primary instance always running
2. Testing instance: `.\Setup\'3 SimpleTradingManager.ps1' -Action Start` (manual)
3. Backup instance enabled only when needed

---

## 5. Team/Multi-User Scenarios

### 5.1 Shared Computer Setup
**Scenario**: Multiple traders sharing one computer with different shifts

**Switching Workflow**:
```powershell
# Morning trader stops everything
.\Setup\'3 SimpleTradingManager.ps1' -Action Stop

# Switch config files (rename or copy different versions)
Copy-Item "morning-config.json" "instances-config.json" -Force

# Evening trader starts
.\Setup\'3 SimpleTradingManager.ps1' -Action Start
```

### 5.2 Master-Slave EA Setup
**Scenario**: One master account controlling multiple slave accounts

```json
{
  "instances": [
    {
      "name": "Master_Controller_Instance",
      "source": "AfterPrime_MT4_Live",
      "dataFolder": "Master_Data",
      "enabled": true,
      "startupSettings": {
        "autoStart": true,
        "startupDelay": 0,
        "priority": "high"
      }
    },
    {
      "name": "Slave_Account1_Instance",
      "source": "Broker2_MT4_Live", 
      "dataFolder": "Slave1_Data",
      "enabled": true,
      "startupSettings": {
        "autoStart": true,
        "startupDelay": 30,  // Wait for master
        "priority": "normal"
      }
    }
  ]
}
```

---

## 6. Advanced Automation Scenarios

### 6.1 Time-Based Trading Setup
**Scenario**: Different strategies for different market sessions

```json
{
  "instances": [
    {
      "name": "Asian_Session_Instance",
      "source": "AfterPrime_MT4_Live",
      "dataFolder": "Asian_Session_Data",
      "enabled": true,
      "startupSettings": {
        "autoStart": false,  // Manual control based on time
        "priority": "high"
      }
    },
    {
      "name": "London_Session_Instance",
      "source": "AfterPrime_MT4_Live",
      "dataFolder": "London_Session_Data",
      "enabled": true,
      "startupSettings": {
        "autoStart": false,
        "priority": "high"
      }
    }
  ]
}
```

**Manual Session Control**:
```powershell
# Start Asian session platforms (manual timing)
.\Setup\'3 SimpleTradingManager.ps1' -Action Start

# Later: Stop Asian, prepare for London
.\Setup\'3 SimpleTradingManager.ps1' -Action Stop
# Edit config to enable London session instances
.\Setup\'3 SimpleTradingManager.ps1' -Action Start
```

### 6.2 VPS/Remote Desktop Setup
**Scenario**: Trading setup for VPS deployment

```json
{
  "tradingRoot": "D:\\TradingEnvironment",
  "defaultDataRoot": "D:\\TradingData",
  "instances": [
    {
      "name": "VPS_Primary_Instance",
      "source": "AfterPrime_MT4_Live",
      "enabled": true,
      "startupSettings": {
        "autoStart": true,
        "startupDelay": 60,  // Extra delay for VPS startup
        "priority": "high",
        "memoryLimit": "512MB"
      }
    }
  ]
}
```

**VPS Deployment**:
```powershell
# Silent installation (no interactive prompts)
.\Setup\'3 SimpleTradingManager.ps1' -Action Install -Quiet

# Check status remotely
.\Setup\'3 SimpleTradingManager.ps1' -Action Status -Quiet
```

---

## 7. Backup & Recovery Scenarios

### 7.1 Full Environment Backup
**Backup Strategy**:
```powershell
# Create backup directory
$BackupDir = "C:\TradingBackup\$(Get-Date -Format 'yyyy-MM-dd')"
New-Item -ItemType Directory -Path $BackupDir -Force

# Stop all platforms first
.\Setup\'3 SimpleTradingManager.ps1' -Action Stop

# Backup configuration
Copy-Item "instances-config.json" "$BackupDir\"

# Backup platform instances (settings, EAs, etc.)
Copy-Item "PlatformInstances" "$BackupDir\" -Recurse

# Backup trading data
Copy-Item "TradingData" "$BackupDir\" -Recurse

# Backup setup scripts
Copy-Item "Setup" "$BackupDir\" -Recurse
```

### 7.2 Disaster Recovery
**Recovery Steps**:
```powershell
# 1. Restore files to new location
Copy-Item "C:\TradingBackup\2025-01-15\*" "C:\TradingRecovery\" -Recurse

# 2. Navigate to restored location
cd "C:\TradingRecovery"

# 3. Update paths in config if needed
# Edit instances-config.json to update tradingRoot and defaultDataRoot

# 4. Recreate instances
.\Setup\'2 Level2-Clean.ps1'

# 5. Set up automation
.\Setup\'3 SimpleTradingManager.ps1' -Action Install

# 6. Verify everything works
.\Setup\'3 SimpleTradingManager.ps1' -Action Status
```

---

## 8. Troubleshooting Scenarios

### 8.1 Platform Won't Start
**Diagnostic Steps**:
```powershell
# Check detailed status
.\Setup\'3 SimpleTradingManager.ps1' -Action Status

# Check if executable exists
$Instance = "AfterPrime_MT4_Demo_Instance"
$ExePath = "PlatformInstances\$Instance\terminal.exe"
Test-Path $ExePath

# Check data junction
$JunctionPath = "InstanceData\AfterPrime_MT4_Demo_Data"
Test-Path $JunctionPath

# Check actual data folder
$DataPath = "TradingData\AfterPrime_MT4_Demo"
Test-Path $DataPath

# Try manual start
.\Setup\'3 SimpleTradingManager.ps1' -Action Start
```

### 8.2 Level 2 Junction Creation Failures
**Common Issue**: "Junction target already exists" or "Failed to create junction"

**Complete Fix Process**:
```powershell
# Step 1: Stop all platforms
.\Setup\'3 SimpleTradingManager.ps1' -Action Stop

# Step 2: Clean up existing junctions
Get-ChildItem "InstanceData" -Directory | ForEach-Object { 
    cmd /c rmdir "$($_.FullName)" 2>$null 
    Write-Host "Removed junction: $($_.Name)" -ForegroundColor Yellow
}

# Step 3: Remove automation to prevent conflicts
.\Setup\'3 SimpleTradingManager.ps1' -Action Remove

# Step 4: Recreate instances
.\Setup\'2 Level2-Clean.ps1' -Force

# Step 5: Reinstall automation
.\Setup\'3 SimpleTradingManager.ps1' -Action Install

# Step 6: Verify success
.\Setup\'3 SimpleTradingManager.ps1' -Action Status
```

### 8.3 Desktop Shortcuts Issues
**Troubleshooting**:
```powershell
# Check if shortcuts exist
$desktop = [Environment]::GetFolderPath("Desktop")
Get-ChildItem $desktop -Filter "*Instance*.lnk"

# Check for custom icons
.\Setup\'3 SimpleTradingManager.ps1' -Action Status

# Recreate shortcuts only
.\Setup\'3 SimpleTradingManager.ps1' -Action Remove
.\Setup\'3 SimpleTradingManager.ps1' -Action Install -CreateShortcuts:$true

# Install without shortcuts (if having issues)
.\Setup\'3 SimpleTradingManager.ps1' -Action Install -CreateShortcuts:$false
```

### 8.4 Automatic Startup Not Working
**Fix Steps**:
```powershell
# Check if startup file was created
Get-ChildItem "$env:APPDATA\Microsoft\Windows\Start Menu\Programs\Startup" -Filter "*.bat"

# Test startup file manually
& "$env:APPDATA\Microsoft\Windows\Start Menu\Programs\Startup\StartTradingPlatforms.bat"

# Reinstall automation completely
.\Setup\'3 SimpleTradingManager.ps1' -Action Remove
.\Setup\'3 SimpleTradingManager.ps1' -Action Install

# Check final status
.\Setup\'3 SimpleTradingManager.ps1' -Action Status
```

---

## 9. Clean Up and Starting Afresh Scenarios

### 9.1 Complete Environment Reset
**Scenario**: Starting completely over while preserving platform files

```powershell
# Navigate to trading directory
cd "C:\Projects\FinancialData"

# Stop everything and remove automation
.\Setup\'3 SimpleTradingManager.ps1' -Action Stop
.\Setup\'3 SimpleTradingManager.ps1' -Action Remove

# Remove generated directories (keeps your extracted platforms and data)
Remove-Item "PlatformInstances" -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item "InstanceData" -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item "Logs" -Recurse -Force -ErrorAction SilentlyContinue

# Backup current config before removing (optional)
if (Test-Path "instances-config.json") {
    Copy-Item "instances-config.json" "instances-config-backup-$(Get-Date -Format 'yyyy-MM-dd-HHmm').json"
    Remove-Item "instances-config.json"
}

Write-Host "Environment reset complete. Ready for fresh setup." -ForegroundColor Green
```

**After Reset - Fresh Start Steps**:
1. Create new `instances-config.json`
2. Run Level 2: `.\Setup\'2 Level2-Clean.ps1'`
3. Set up automation: `.\Setup\'3 SimpleTradingManager.ps1' -Action Install`

### 9.2 Configuration Update Workflow
**Scenario**: Adding new platforms or changing settings

```powershell
# 1. Stop current automation
.\Setup\'3 SimpleTradingManager.ps1' -Action Stop
.\Setup\'3 SimpleTradingManager.ps1' -Action Remove

# 2. Edit instances-config.json
# Add/remove/modify instances as needed

# 3. Recreate instances (processes config changes)
.\Setup\'2 Level2-Clean.ps1'

# 4. Reinstall automation with new config
.\Setup\'3 SimpleTradingManager.ps1' -Action Install

# 5. Verify everything is working
.\Setup\'3 SimpleTradingManager.ps1' -Action Status
```

### 9.3 Migration to New Computer
**Preparation on Old Computer**:
```powershell
# Stop everything
.\Setup\'3 SimpleTradingManager.ps1' -Action Stop
.\Setup\'3 SimpleTradingManager.ps1' -Action Remove

# Create migration package
$ExportPath = "C:\TradingExport"
New-Item -ItemType Directory -Path $ExportPath -Force

# Export critical components
Copy-Item "instances-config.json" "$ExportPath\"
Copy-Item "PlatformInstallations" "$ExportPath\" -Recurse
Copy-Item "TradingData" "$ExportPath\" -Recurse
Copy-Item "Setup" "$ExportPath\" -Recurse

Write-Host "Migration package ready at: $ExportPath" -ForegroundColor Green
```

**Setup on New Computer**:
```powershell
# 1. Copy migration package to new system
# 2. Run Level 1 setup (admin required)
.\Setup\'1 Level 1 Trading Environment Setup.ps1'

# 3. Update paths in instances-config.json if needed
# 4. Create instances
.\Setup\'2 Level2-Clean.ps1'

# 5. Set up automation
.\Setup\'3 SimpleTradingManager.ps1' -Action Install

# 6. Verify
.\Setup\'3 SimpleTradingManager.ps1' -Action Status
```

---

## Quick Reference Commands

### Daily Operations
```powershell
# Check what's running
.\Setup\'3 SimpleTradingManager.ps1' -Action Status

# Start all platforms
.\Setup\'3 SimpleTradingManager.ps1' -Action Start

# Stop all platforms  
.\Setup\'3 SimpleTradingManager.ps1' -Action Stop

# Get help
.\Setup\'3 SimpleTradingManager.ps1' -Action Help
```

### Configuration Management
```powershell
# After editing config file
.\Setup\'2 Level2-Clean.ps1'                              # Recreate instances
.\Setup\'3 SimpleTradingManager.ps1' -Action Install      # Update automation

# Silent operations (for scripts)
.\Setup\'3 SimpleTradingManager.ps1' -Action Start -Quiet
.\Setup\'3 SimpleTradingManager.ps1' -Action Status -Quiet
```

### Troubleshooting
```powershell
# Complete reset
.\Setup\'3 SimpleTradingManager.ps1' -Action Stop
.\Setup\'3 SimpleTradingManager.ps1' -Action Remove
.\Setup\'2 Level2-Clean.ps1' -Force
.\Setup\'3 SimpleTradingManager.ps1' -Action Install

# Install without shortcuts
.\Setup\'3 SimpleTradingManager.ps1' -Action Install -CreateShortcuts:$false
```

---

## Common Patterns Summary

### Enable/Disable Patterns:
- **Full Disable**: `"enabled": false`
- **Create but Don't Auto-Start**: `"enabled": true, "autoStart": false` 
- **Temporarily Disable Auto-Start**: `"autoStart": false`

### Priority Patterns:
- **Live Trading**: `"priority": "high"`
- **Demo/Testing**: `"priority": "normal"`
- **Backtesting**: `"priority": "low"`

### Delay Patterns:
- **First Instance**: `"startupDelay": 0`
- **Subsequent Instances**: `"startupDelay": 15, 30, 45...`
- **Network Issues**: `"startupDelay": 60+`

This comprehensive guide covers virtually every scenario you might encounter with the unified trading environment setup. The single script approach makes management much simpler while providing all the functionality you need.