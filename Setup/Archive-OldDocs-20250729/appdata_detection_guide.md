# AppData Trading Platform Detection & Integration Guide

## Overview

Most trading platforms install to AppData by default, not portable mode. This guide provides methods to detect, identify, and integrate existing AppData installations into your testing and management framework.

---

## Common AppData Installation Paths

### MetaTrader 4
```
# Standard Installation Paths
%APPDATA%\MetaQuotes\Terminal\<InstanceID>\
%APPDATA%\MetaQuotes\Terminal\Common\Files\

# Broker-Specific Paths
%APPDATA%\MetaQuotes\Terminal\<BrokerName>\
%LOCALAPPDATA%\<BrokerName>\

# Common Broker Examples
%APPDATA%\MetaQuotes\Terminal\4C2245EC2E09E2A08C96A7BFE1F0F3F2\  # Random hash
%APPDATA%\MetaQuotes\Terminal\OANDA - MetaTrader 4\
%APPDATA%\MetaQuotes\Terminal\FXCM - MetaTrader 4\
```

### MetaTrader 5
```
# Standard Installation Paths
%APPDATA%\MetaQuotes\Terminal\<InstanceID>\
%APPDATA%\MetaQuotes\Terminal\Common\Files\

# Program Files Installations
%PROGRAMFILES%\<BrokerName> MetaTrader 5\
%PROGRAMFILES(X86)%\<BrokerName> MetaTrader 5\

# Common Examples
%APPDATA%\MetaQuotes\Terminal\D0E8209F77C8CF37AD8BF550E51FF075\  # Random hash
%PROGRAMFILES%\MetaTrader 5\
```

### cTrader
```
%APPDATA%\cTrader\
%LOCALAPPDATA%\cTrader\
%APPDATA%\<BrokerName> cTrader\
```

### TradeStation / Other Platforms
```
%APPDATA%\TradeStation\
%APPDATA%\NinjaTrader 8\
%APPDATA%\TradingView\
%LOCALAPPDATA%\<BrokerName>\
```

---

## PowerShell Detection Scripts

### 1. Comprehensive AppData Scanner

```powershell
function Find-TradingPlatformsInAppData {
    param(
        [switch]$IncludeDetails,
        [switch]$ExportToCSV
    )
    
    Write-Host "🔍 Scanning for trading platforms in AppData..." -ForegroundColor Cyan
    
    $Results = @()
    $AppDataPath = $env:APPDATA
    $LocalAppDataPath = $env:LOCALAPPDATA
    $ProgramFilesPath = $env:PROGRAMFILES
    $ProgramFilesX86Path = ${env:PROGRAMFILES(X86)}
    
    # Define platform signatures
    $PlatformSignatures = @{
        "MetaTrader4" = @{
            Paths = @(
                "$AppDataPath\MetaQuotes\Terminal\*",
                "$LocalAppDataPath\MetaQuotes\Terminal\*"
            )
            Executables = @("terminal.exe", "terminal64.exe")
            ConfigFiles = @("config\common.ini", "origin.txt")
            DataFolders = @("MQL4", "history", "logs")
        }
        "MetaTrader5" = @{
            Paths = @(
                "$AppDataPath\MetaQuotes\Terminal\*",
                "$LocalAppDataPath\MetaQuotes\Terminal\*",
                "$ProgramFilesPath\MetaTrader 5*",
                "$ProgramFilesX86Path\MetaTrader 5*"
            )
            Executables = @("terminal64.exe", "terminal.exe")
            ConfigFiles = @("config\common.ini", "origin.txt")
            DataFolders = @("MQL5", "history", "logs")
        }
        "cTrader" = @{
            Paths = @(
                "$AppDataPath\cTrader*",
                "$LocalAppDataPath\cTrader*"
            )
            Executables = @("cTrader.exe")
            ConfigFiles = @("Settings.json")
            DataFolders = @("Robots", "Indicators")
        }
        "TradeStation" = @{
            Paths = @(
                "$AppDataPath\TradeStation*",
                "$LocalAppDataPath\TradeStation*"
            )
            Executables = @("TradeStation.exe")
            ConfigFiles = @("Settings.ini")
            DataFolders = @("Strategies", "Indicators")
        }
    }
    
    foreach ($Platform in $PlatformSignatures.Keys) {
        Write-Host "  Scanning for $Platform..." -ForegroundColor Yellow
        
        $PlatformConfig = $PlatformSignatures[$Platform]
        
        foreach ($SearchPath in $PlatformConfig.Paths) {
            $FoundPaths = Get-ChildItem -Path $SearchPath -Directory -ErrorAction SilentlyContinue
            
            foreach ($Path in $FoundPaths) {
                # Check if this looks like a trading platform installation
                $MatchScore = 0
                $FoundFiles = @()
                
                # Check for executables
                foreach ($ExeName in $PlatformConfig.Executables) {
                    $ExePath = Join-Path $Path.FullName $ExeName
                    if (Test-Path $ExePath) {
                        $MatchScore += 3
                        $FoundFiles += $ExeName
                    }
                }
                
                # Check for config files
                foreach ($ConfigFile in $PlatformConfig.ConfigFiles) {
                    $ConfigPath = Join-Path $Path.FullName $ConfigFile
                    if (Test-Path $ConfigPath) {
                        $MatchScore += 2
                        $FoundFiles += $ConfigFile
                    }
                }
                
                # Check for data folders
                foreach ($DataFolder in $PlatformConfig.DataFolders) {
                    $DataPath = Join-Path $Path.FullName $DataFolder
                    if (Test-Path $DataPath) {
                        $MatchScore += 1
                        $FoundFiles += "$DataFolder/"
                    }
                }
                
                # If we have a reasonable match, record it
                if ($MatchScore -ge 3) {
                    $InstallDetails = @{
                        Platform = $Platform
                        Path = $Path.FullName
                        Name = $Path.Name
                        MatchScore = $MatchScore
                        FoundFiles = $FoundFiles
                        Size = if ($IncludeDetails) { 
                            [math]::Round((Get-ChildItem -Path $Path.FullName -Recurse -File -ErrorAction SilentlyContinue | 
                                         Measure-Object -Property Length -Sum).Sum / 1MB, 2) 
                        } else { $null }
                        LastModified = $Path.LastWriteTime
                        InstallationType = if ($Path.FullName -like "*Program Files*") { "System" } else { "User" }
                    }
                    
                    # Try to identify broker/account type from path or config
                    $BrokerInfo = Get-BrokerInfoFromPath -Path $Path.FullName -Platform $Platform
                    $InstallDetails.Broker = $BrokerInfo.Broker
                    $InstallDetails.AccountType = $BrokerInfo.AccountType
                    
                    $Results += [PSCustomObject]$InstallDetails
                    
                    Write-Host "    ✅ Found: $($Path.Name) (Score: $MatchScore)" -ForegroundColor Green
                }
            }
        }
    }
    
    # Display summary
    Write-Host "`n📊 Discovery Summary:" -ForegroundColor Cyan
    $Results | Group-Object Platform | ForEach-Object {
        Write-Host "  $($_.Name): $($_.Count) installation(s)" -ForegroundColor White
    }
    
    # Export to CSV if requested
    if ($ExportToCSV) {
        $CsvPath = "TradingPlatforms_Discovery_$(Get-Date -Format 'yyyyMMdd_HHmmss').csv"
        $Results | Export-Csv -Path $CsvPath -NoTypeInformation
        Write-Host "`n💾 Results exported to: $CsvPath" -ForegroundColor Green
    }
    
    return $Results
}

function Get-BrokerInfoFromPath {
    param([string]$Path, [string]$Platform)
    
    $Broker = "Unknown"
    $AccountType = "Unknown"
    
    # Try to extract broker name from path
    $PathParts = $Path -split '\\'
    
    # Look for broker identifiers in path
    $BrokerPatterns = @{
        "OANDA" = @("oanda", "oanda-")
        "FXCM" = @("fxcm", "fxcm-")
        "Pepperstone" = @("pepperstone", "pepper")
        "IC Markets" = @("icmarkets", "ic markets", "ic-markets")
        "XM" = @("xm trading", "xm global", "xm-")
        "FXTM" = @("fxtm", "forextime")
        "Admiral Markets" = @("admiral", "admiralmarkets")
        "AvaTrade" = @("avatrade", "ava-")
        "Plus500" = @("plus500")
        "eToro" = @("etoro")
    }
    
    foreach ($BrokerName in $BrokerPatterns.Keys) {
        foreach ($Pattern in $BrokerPatterns[$BrokerName]) {
            if ($Path -like "*$Pattern*") {
                $Broker = $BrokerName
                break
            }
        }
        if ($Broker -ne "Unknown") { break }
    }
    
    # Try to determine account type
    if ($Path -like "*demo*" -or $Path -like "*test*") {
        $AccountType = "Demo"
    } elseif ($Path -like "*live*" -or $Path -like "*real*") {
        $AccountType = "Live"
    } else {
        # Try to read from config files
        $ConfigPaths = @(
            (Join-Path $Path "config\common.ini"),
            (Join-Path $Path "origin.txt"),
            (Join-Path $Path "Settings.json")
        )
        
        foreach ($ConfigPath in $ConfigPaths) {
            if (Test-Path $ConfigPath) {
                $ConfigContent = Get-Content $ConfigPath -ErrorAction SilentlyContinue
                if ($ConfigContent -match "demo|test") {
                    $AccountType = "Demo"
                } elseif ($ConfigContent -match "live|real") {
                    $AccountType = "Live"
                }
                break
            }
        }
    }
    
    return @{ Broker = $Broker; AccountType = $AccountType }
}
```

### 2. Specific MetaTrader Detection

```powershell
function Find-MetaTraderInstallations {
    param([switch]$IncludeInactive)
    
    Write-Host "🔍 Scanning for MetaTrader installations..." -ForegroundColor Cyan
    
    $Results = @()
    $MetaQuotesPath = Join-Path $env:APPDATA "MetaQuotes\Terminal"
    
    if (Test-Path $MetaQuotesPath) {
        $Terminals = Get-ChildItem -Path $MetaQuotesPath -Directory
        
        foreach ($Terminal in $Terminals) {
            $TerminalPath = $Terminal.FullName
            $OriginFile = Join-Path $TerminalPath "origin.txt"
            $CommonIni = Join-Path $TerminalPath "config\common.ini"
            $Terminal64 = Join-Path $TerminalPath "terminal64.exe"
            $Terminal32 = Join-Path $TerminalPath "terminal.exe"
            
            # Determine if it's MT4 or MT5
            $Platform = "Unknown"
            $Executable = $null
            
            if (Test-Path $Terminal64) {
                $Executable = $Terminal64
                # Check for MQL5 folder to distinguish MT5 from MT4
                if (Test-Path (Join-Path $TerminalPath "MQL5")) {
                    $Platform = "MetaTrader5"
                } else {
                    $Platform = "MetaTrader4"
                }
            } elseif (Test-Path $Terminal32) {
                $Executable = $Terminal32
                $Platform = "MetaTrader4"
            }
            
            # Skip if no executable found and not including inactive
            if (-not $Executable -and -not $IncludeInactive) {
                continue
            }
            
            # Get broker information
            $BrokerName = "Unknown"
            $ServerName = "Unknown"
            
            if (Test-Path $OriginFile) {
                $OriginContent = Get-Content $OriginFile -ErrorAction SilentlyContinue
                if ($OriginContent) {
                    # Origin file often contains broker information
                    $BrokerName = ($OriginContent | Select-Object -First 1) -replace '[^a-zA-Z0-9\s]', ''
                }
            }
            
            if (Test-Path $CommonIni) {
                $CommonContent = Get-Content $CommonIni -ErrorAction SilentlyContinue
                $ServerLine = $CommonContent | Where-Object { $_ -like "*Server=*" } | Select-Object -First 1
                if ($ServerLine) {
                    $ServerName = ($ServerLine -split "=")[1]
                }
            }
            
            # Determine if it's demo or live
            $AccountType = "Unknown"
            if ($ServerName -like "*demo*" -or $Terminal.Name -like "*demo*") {
                $AccountType = "Demo"
            } elseif ($ServerName -like "*live*" -or $ServerName -like "*real*") {
                $AccountType = "Live"
            }
            
            # Get last activity
            $LastActivity = $Terminal.LastWriteTime
            $LogsPath = Join-Path $TerminalPath "logs"
            if (Test-Path $LogsPath) {
                $LatestLog = Get-ChildItem -Path $LogsPath -File | Sort-Object LastWriteTime -Descending | Select-Object -First 1
                if ($LatestLog) {
                    $LastActivity = $LatestLog.LastWriteTime
                }
            }
            
            $Installation = [PSCustomObject]@{
                Platform = $Platform
                InstanceID = $Terminal.Name
                Path = $TerminalPath
                Executable = $Executable
                BrokerName = $BrokerName
                ServerName = $ServerName
                AccountType = $AccountType
                LastActivity = $LastActivity
                IsActive = if ($Executable) { Test-Path $Executable } else { $false }
                HasData = Test-Path (Join-Path $TerminalPath "history")
                Size = [math]::Round((Get-ChildItem -Path $TerminalPath -Recurse -File -ErrorAction SilentlyContinue | 
                                     Measure-Object -Property Length -Sum).Sum / 1MB, 2)
            }
            
            $Results += $Installation
            
            $StatusIcon = if ($Installation.IsActive) { "✅" } else { "⚠️" }
            Write-Host "  $StatusIcon $($Installation.Platform): $($Installation.BrokerName) ($($Installation.AccountType))" -ForegroundColor $(if ($Installation.IsActive) { "Green" } else { "Yellow" })
            Write-Host "    Path: $($Installation.Path)" -ForegroundColor Gray
        }
    }
    
    Write-Host "`n📊 Found $($Results.Count) MetaTrader installation(s)" -ForegroundColor Cyan
    return $Results
}
```

### 3. Integration with Existing System

```powershell
function Convert-AppDataToPortable {
    param(
        [Parameter(Mandatory)]
        [string]$AppDataPath,
        [Parameter(Mandatory)]
        [string]$PortableDestination,
        [string]$InstanceName,
        [switch]$PreserveOriginal
    )
    
    Write-Host "🔄 Converting AppData installation to portable..." -ForegroundColor Cyan
    Write-Host "  Source: $AppDataPath" -ForegroundColor Yellow
    Write-Host "  Destination: $PortableDestination" -ForegroundColor Yellow
    
    try {
        # Create destination directory
        if (-not (Test-Path $PortableDestination)) {
            New-Item -ItemType Directory -Path $PortableDestination -Force | Out-Null
        }
        
        # Copy essential files and folders
        $EssentialItems = @(
            "terminal*.exe",
            "*.dll",
            "config\*",
            "MQL4\*",
            "MQL5\*",
            "templates\*",
            "presets\*",
            "sounds\*",
            "history\*"
        )
        
        foreach ($Item in $EssentialItems) {
            $SourcePattern = Join-Path $AppDataPath $Item
            $SourceItems = Get-ChildItem -Path $SourcePattern -Recurse -ErrorAction SilentlyContinue
            
            foreach ($SourceItem in $SourceItems) {
                $RelativePath = $SourceItem.FullName.Substring($AppDataPath.Length + 1)
                $DestPath = Join-Path $PortableDestination $RelativePath
                $DestDir = Split-Path $DestPath -Parent
                
                if (-not (Test-Path $DestDir)) {
                    New-Item -ItemType Directory -Path $DestDir -Force | Out-Null
                }
                
                Copy-Item -Path $SourceItem.FullName -Destination $DestPath -Force
            }
        }
        
        # Create portable.ini to force portable mode
        $PortableIni = Join-Path $PortableDestination "portable.ini"
        "[Common]`nDataPath=$PortableDestination\data" | Out-File -FilePath $PortableIni -Encoding ASCII
        
        # Create data directory
        $DataPath = Join-Path $PortableDestination "data"
        if (-not (Test-Path $DataPath)) {
            New-Item -ItemType Directory -Path $DataPath -Force | Out-Null
        }
        
        Write-Host "✅ Conversion completed successfully" -ForegroundColor Green
        
        # Add to instances config if name provided
        if ($InstanceName) {
            Add-ToInstancesConfig -InstanceName $InstanceName -SourcePath $PortableDestination
        }
        
        return $true
    }
    catch {
        Write-Host "❌ Conversion failed: $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

function Add-ToInstancesConfig {
    param(
        [string]$InstanceName,
        [string]$SourcePath
    )
    
    $ConfigPath = "instances-config.json"
    
    if (Test-Path $ConfigPath) {
        $Config = Get-Content $ConfigPath -Raw | ConvertFrom-Json
    } else {
        $Config = @{ instances = @() }
    }
    
    # Determine platform type
    $Platform = if (Test-Path (Join-Path $SourcePath "MQL5")) { "MT5" } else { "MT4" }
    
    $NewInstance = @{
        name = $InstanceName
        platform = $Platform
        source = Split-Path $SourcePath -Leaf
        destination = $InstanceName
        enabled = $true
        startupSettings = @{
            autoStart = $false  # Default to manual start for converted instances
            startupDelay = 0
            priority = "normal"
        }
    }
    
    # Add to config
    $Config.instances += $NewInstance
    
    # Save updated config
    $Config | ConvertTo-Json -Depth 3 | Set-Content $ConfigPath
    
    Write-Host "✅ Added $InstanceName to instances configuration" -ForegroundColor Green
}
```

### 4. Testing Integration Script

```powershell
function Test-AllTradingInstallations {
    param([switch]$IncludeAppData)
    
    Write-Host "🧪 Testing all trading platform installations..." -ForegroundColor Cyan
    
    $Results = @()
    
    # Test portable installations
    Write-Host "`n📁 Testing Portable Installations:" -ForegroundColor Yellow
    $PortableResults = Test-PortableInstallations
    $Results += $PortableResults
    
    # Test AppData installations if requested
    if ($IncludeAppData) {
        Write-Host "`n💾 Testing AppData Installations:" -ForegroundColor Yellow
        $AppDataInstallations = Find-TradingPlatformsInAppData
        
        foreach ($Installation in $AppDataInstallations) {
            $TestResult = Test-InstallationHealth -Path $Installation.Path -Platform $Installation.Platform
            $TestResult.InstallationType = "AppData"
            $TestResult.Location = $Installation.Path
            $Results += $TestResult
        }
    }
    
    # Generate comprehensive report
    Write-Host "`n📊 Installation Test Summary:" -ForegroundColor Cyan
    Write-Host "  Total Installations: $($Results.Count)" -ForegroundColor White
    Write-Host "  Portable: $(($Results | Where-Object { $_.InstallationType -eq 'Portable' }).Count)" -ForegroundColor Green
    Write-Host "  AppData: $(($Results | Where-Object { $_.InstallationType -eq 'AppData' }).Count)" -ForegroundColor Yellow
    Write-Host "  Healthy: $(($Results | Where-Object { $_.Status -eq 'Healthy' }).Count)" -ForegroundColor Green
    Write-Host "  Issues: $(($Results | Where-Object { $_.Status -ne 'Healthy' }).Count)" -ForegroundColor Red
    
    return $Results
}

function Test-PortableInstallations {
    $PortableResults = @()
    $InstancesPath = "PlatformInstances"
    
    if (Test-Path $InstancesPath) {
        $Instances = Get-ChildItem -Path $InstancesPath -Directory
        
        foreach ($Instance in $Instances) {
            $TestResult = Test-InstallationHealth -Path $Instance.FullName -Platform "Auto-Detect"
            $TestResult.InstallationType = "Portable"
            $TestResult.Location = $Instance.FullName
            $PortableResults += $TestResult
        }
    }
    
    return $PortableResults
}

function Test-InstallationHealth {
    param([string]$Path, [string]$Platform)
    
    $HealthStatus = @{
        Name = Split-Path $Path -Leaf
        Platform = $Platform
        Status = "Unknown"
        Issues = @()
        LastTested = Get-Date
    }
    
    # Check if executable exists
    $Executables = @("terminal64.exe", "terminal.exe", "cTrader.exe")
    $ExecutableFound = $false
    
    foreach ($Exe in $Executables) {
        if (Test-Path (Join-Path $Path $Exe)) {
            $ExecutableFound = $true
            $HealthStatus.Executable = $Exe
            break
        }
    }
    
    if (-not $ExecutableFound) {
        $HealthStatus.Issues += "No executable found"
    }
    
    # Check for essential folders
    $EssentialFolders = @("config", "history", "logs")
    foreach ($Folder in $EssentialFolders) {
        if (-not (Test-Path (Join-Path $Path $Folder))) {
            $HealthStatus.Issues += "Missing folder: $Folder"
        }
    }
    
    # Determine overall health
    if ($HealthStatus.Issues.Count -eq 0) {
        $HealthStatus.Status = "Healthy"
    } elseif ($ExecutableFound) {
        $HealthStatus.Status = "Minor Issues"
    } else {
        $HealthStatus.Status = "Unhealthy"
    }
    
    return [PSCustomObject]$HealthStatus
}
```

---

## Usage Examples

### 1. Discover All Trading Platforms

```powershell
# Basic discovery
$Platforms = Find-TradingPlatformsInAppData

# Detailed discovery with export
$Platforms = Find-TradingPlatformsInAppData -IncludeDetails -ExportToCSV

# Display results
$Platforms | Format-Table Platform, Name, Broker, AccountType, InstallationType, Size -AutoSize
```

### 2. Find MetaTrader Installations

```powershell
# Active installations only
$MT_Installations = Find-MetaTraderInstallations

# Include inactive installations
$All_MT_Installations = Find-MetaTraderInstallations -IncludeInactive

# Show summary
$MT_Installations | Group-Object Platform | Select-Object Name, Count
```

### 3. Convert AppData to Portable

```powershell
# Convert a specific installation
$AppDataPath = "$env:APPDATA\MetaQuotes\Terminal\4C2245EC2E09E2A08C96A7BFE1F0F3F2"
$PortablePath = "PlatformInstallations\OANDA_MT4_Demo"

Convert-AppDataToPortable -AppDataPath $AppDataPath -PortableDestination $PortablePath -InstanceName "OANDA_MT4_Demo_Instance"

# Then create instance
.\Setup\'2 Level2-Clean.ps1'
```

### 4. Test All Installations

```powershell
# Test portable and AppData installations
$TestResults = Test-AllTradingInstallations -IncludeAppData

# Show issues
$TestResults | Where-Object { $_.Status -ne "Healthy" } | Format-Table Name, Status, Issues -Wrap
```

---

## Integration with Testing Framework

### Modified Test Setup Procedure

```powershell
# SETUP-001-EXTENDED: Environment Preparation with AppData Detection

function Initialize-TestEnvironmentExtended {
    param([switch]$IncludeAppData)
    
    Write-Host "🔧 Initializing Extended Test Environment..." -ForegroundColor Cyan
    
    # 1. Standard portable setup
    $PortableInstallations = Test-PortableInstallations
    Write-Host "Found $($PortableInstallations.Count) portable installations" -ForegroundColor Green
    
    # 2. Discover AppData installations
    if ($IncludeAppData) {
        $AppDataInstallations = Find-TradingPlatformsInAppData -IncludeDetails
        Write-Host "Found $($AppDataInstallations.Count) AppData installations" -ForegroundColor Green
        
        # 3. Create test configuration including both types
        $ExtendedConfig = @{
            portableInstallations = $PortableInstallations
            appDataInstallations = $AppDataInstallations
            testingMode = "Extended"
            includeAppData = $true
        }
        
        # 4. Offer conversion options
        Write-Host "`n🔄 AppData Conversion Options:" -ForegroundColor Yellow
        Write-Host "1. Test AppData installations in-place" -ForegroundColor White
        Write-Host "2. Convert AppData to portable for testing" -ForegroundColor White
        Write-Host "3. Skip AppData installations" -ForegroundColor White
        
        $Choice = Read-Host "Choose option (1-3)"
        
        switch ($Choice) {
            "1" { 
                Write-Host "✅ Will test AppData installations in-place" -ForegroundColor Green
                $ExtendedConfig.appDataTestMode = "InPlace"
            }
            "2" { 
                Write-Host "🔄 Converting AppData installations to portable..." -ForegroundColor Yellow
                Convert-AppDataInstallationsToPortable -Installations $AppDataInstallations
                $ExtendedConfig.appDataTestMode = "Converted"
            }
            "3" { 
                Write-Host "⏭️ Skipping AppData installations" -ForegroundColor Yellow
                $ExtendedConfig.includeAppData = $false
            }
        }
        
        # Save extended configuration
        $ExtendedConfig | ConvertTo-Json -Depth 4 | Set-Content "TestConfigs\extended_config.json"
    }
    
    return $ExtendedConfig
}

function Convert-AppDataInstallationsToPortable {
    param([array]$Installations)
    
    foreach ($Installation in $Installations) {
        $PortableName = "$($Installation.Broker)_$($Installation.Platform)_$($Installation.AccountType)".Replace(" ", "")
        $PortablePath = "PlatformInstallations\$PortableName"
        $InstanceName = "${PortableName}_Instance"
        
        Write-Host "Converting: $($Installation.Name) → $PortableName" -ForegroundColor Cyan
        
        $Success = Convert-AppDataToPortable -AppDataPath $Installation.Path -PortableDestination $PortablePath -InstanceName $InstanceName
        
        if ($Success) {
            Write-Host "✅ Converted successfully" -ForegroundColor Green
        } else {
            Write-Host "❌ Conversion failed" -ForegroundColor Red
        }
    }
}
```

### Modified Test Execution

```powershell
# Execute tests on both portable and AppData installations
function Execute-ExtendedTests {
    param([string]$ConfigPath = "TestConfigs\extended_config.json")
    
    if (Test-Path $ConfigPath) {
        $Config = Get-Content $ConfigPath -Raw | ConvertFrom-Json
        
        # Test portable installations
        Write-Host "🧪 Testing Portable Installations..." -ForegroundColor Cyan
        $PortableResults = Execute-PortableTests -Installations $Config.portableInstallations
        
        # Test AppData installations if included
        if ($Config.includeAppData) {
            Write-Host "🧪 Testing AppData Installations..." -ForegroundColor Cyan
            $AppDataResults = Execute-AppDataTests -Installations $Config.appDataInstallations -TestMode $Config.appDataTestMode
        }
        
        # Combine and report results
        $AllResults = @()
        $AllResults += $PortableResults
        if ($Config.includeAppData) {
            $AllResults += $AppDataResults
        }
        
        Generate-ExtendedTestReport -Results $AllResults
    }
}
```

---

## Best Practices

### 1. AppData vs Portable Testing
- **AppData Installations**: Test in-place for existing user scenarios
- **Portable Installations**: Convert for controlled testing environments
- **Mixed Testing**: Test both to ensure compatibility

### 2. Data Preservation
- Always backup AppData installations before conversion
- Use `-PreserveOriginal` flag when converting
- Test conversions on copies first

### 3. Performance Considerations
- AppData scanning can be slow on systems with many installations
- Use filters to focus on specific platforms
- Cache discovery results for repeated testing

### 4. Security Considerations
- Some AppData folders may have restricted access
- Run discovery scripts with appropriate permissions
- Respect user privacy when scanning AppData

This comprehensive approach allows you to work with both portable and standard AppData installations, giving you complete coverage of all trading platform scenarios in your testing framework.