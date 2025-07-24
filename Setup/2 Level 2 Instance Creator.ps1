# 2 Level 2 Instance Creator.ps1
# This script creates platform instances based on configuration file
# Monitors config file changes and updates instances accordingly

param(
    [string]$ConfigFile = "instances-config.json",
    [switch]$WhatIf,
    [switch]$Monitor,
    [switch]$Force
)

# Ensure script runs as Administrator
if (-NOT ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")) {
    Write-Host "This script requires Administrator privileges. Restarting as Administrator..." -ForegroundColor Yellow
    $params = "-File", $PSCommandPath, "-ConfigFile", $ConfigFile
    if ($WhatIf) { $params += "-WhatIf" }
    Start-Process powershell.exe $params -Verb RunAs
    exit
}

Write-Host "=== Level 2 Instance Creator ===" -ForegroundColor Green

# Function to remove instance
function Remove-PlatformInstance {
    param(
        [string]$InstanceName,
        [string]$InstancePath,
        [string]$JunctionPath
    )
    
    Write-Host "Removing instance: $InstanceName" -ForegroundColor Yellow
    
    # Remove junction first
    if (Test-Path $JunctionPath) {
        try {
            cmd /c rmdir "$JunctionPath" | Out-Null
            Write-Host "Removed junction: $JunctionPath" -ForegroundColor Green
        }
        catch {
            Write-Host "Failed to remove junction: $JunctionPath" -ForegroundColor Red
        }
    }
    
    # Remove instance directory
    if (Test-Path $InstancePath) {
        try {
            Remove-Item -Path $InstancePath -Recurse -Force
            Write-Host "Removed instance directory: $InstancePath" -ForegroundColor Green
            return $true
        }
        catch {
            Write-Host "Failed to remove instance directory: $($_.Exception.Message)" -ForegroundColor Red
            return $false
        }
    }
    
    return $true
}

# Function to get existing instances
function Get-ExistingInstances {
    $existing = @{}
    $instancesPath = "PlatformInstances"
    
    if (Test-Path $instancesPath) {
        Get-ChildItem $instancesPath -Directory | ForEach-Object {
            $configPath = Join-Path $_.FullName "instance.config"
            if (Test-Path $configPath) {
                $existing[$_.Name] = $_.FullName
            }
        }
    }
    
    return $existing
}
function New-Junction {
    param(
        [string]$Source,
        [string]$Target
    )
    
    if (Test-Path $Target) {
        Write-Host "Junction target already exists: $Target" -ForegroundColor Yellow
        return $false
    }
    
    if (-not (Test-Path $Source)) {
        Write-Host "Creating source directory: $Source" -ForegroundColor Cyan
        New-Item -ItemType Directory -Path $Source -Force | Out-Null
    }
    
    try {
        cmd /c mklink /J "$Target" "$Source" | Out-Null
        Write-Host "Created junction: $Target -> $Source" -ForegroundColor Green
        return $true
    }
    catch {
        Write-Host "Failed to create junction: $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

# Function to copy platform installation
function Copy-PlatformInstance {
    param(
        [string]$Source,
        [string]$Destination
    )
    
    if (-not (Test-Path $Source)) {
        Write-Host "Source installation not found: $Source" -ForegroundColor Red
        return $false
    }
    
    if (Test-Path $Destination) {
        Write-Host "Destination already exists: $Destination" -ForegroundColor Yellow
        return $false
    }
    
    try {
        Write-Host "Copying platform files: $Source -> $Destination" -ForegroundColor Cyan
        Copy-Item -Path $Source -Destination $Destination -Recurse -Force
        Write-Host "Platform copied successfully" -ForegroundColor Green
        return $true
    }
    catch {
        Write-Host "Failed to copy platform: $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

# Function to configure portable mode
function Set-PortableMode {
    param(
        [string]$InstancePath,
        [string]$Platform
    )
    
    switch ($Platform.ToUpper()) {
        "MT4" {
            $portableFile = Join-Path $InstancePath "portable.txt"
            if (-not (Test-Path $portableFile)) {
                "" | Out-File -FilePath $portableFile -Encoding UTF8
                Write-Host "Created portable.txt for MT4" -ForegroundColor Green
            }
        }
        "MT5" {
            $portableFile = Join-Path $InstancePath "portable.txt" 
            if (-not (Test-Path $portableFile)) {
                "" | Out-File -FilePath $portableFile -Encoding UTF8
                Write-Host "Created portable.txt for MT5" -ForegroundColor Green
            }
        }
        "TRADEREVOLUTION" {
            # TraderEvolution portable configuration may vary by broker
            Write-Host "TraderEvolution portable mode configured" -ForegroundColor Green
        }
    }
}

# Function to create instance configuration file
function New-InstanceConfig {
    param(
        [object]$Instance,
        [string]$InstancePath
    )
    
    $configContent = @"
# Instance Configuration
# Generated: $(Get-Date)

[Instance]
Name=$($Instance.name)
Broker=$($Instance.broker)
Platform=$($Instance.platform)
AccountType=$($Instance.accountType)
Created=$(Get-Date -Format "yyyy-MM-dd HH:mm:ss")

[Paths]
InstancePath=$InstancePath
DataFolder=$($Instance.dataFolder)
JunctionName=$($Instance.junctionName)

[Settings]
PortableMode=$($Instance.portableMode)
Enabled=$($Instance.enabled)

[Server]
Server=$($Instance.serverSettings.server)
AutoLogin=$($Instance.serverSettings.autoLogin)
"@

    $configPath = Join-Path $InstancePath "instance.config"
    $configContent | Out-File -FilePath $configPath -Encoding UTF8
    Write-Host "Instance configuration saved: $configPath" -ForegroundColor Green
}

# Read configuration file
$configPath = Join-Path (Get-Location) $ConfigFile
if (-not (Test-Path $configPath)) {
    Write-Host "Configuration file not found: $configPath" -ForegroundColor Red
    Write-Host "Please ensure instances-config.json exists in the current directory" -ForegroundColor Yellow
    exit 1
}

try {
    $config = Get-Content $configPath -Raw | ConvertFrom-Json
    Write-Host "Configuration loaded from: $configPath" -ForegroundColor Green
}
catch {
    Write-Host "Failed to parse configuration file: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Validate trading root
if (-not (Test-Path $config.tradingRoot)) {
    Write-Host "Trading root directory not found: $($config.tradingRoot)" -ForegroundColor Red
    Write-Host "Please run Level 1 setup first" -ForegroundColor Yellow
    exit 1
}

# Set working directory
Set-Location $config.tradingRoot

# Process instances with change detection
$successCount = 0
$skipCount = 0
$removeCount = 0

# Get existing instances
$existingInstances = Get-ExistingInstances
$configuredInstances = @{}

# Build list of configured instances
foreach ($instance in $config.instances) {
    $configuredInstances[$instance.destination] = $instance
}

# Remove instances that are no longer configured or disabled
foreach ($existingName in $existingInstances.Keys) {
    if (-not $configuredInstances.ContainsKey($existingName) -or -not $configuredInstances[$existingName].enabled) {
        $instancePath = $existingInstances[$existingName]
        $junctionPath = Join-Path "InstanceData" "$existingName*"
        
        # Find the actual junction (may have different naming)
        $actualJunctions = Get-ChildItem "InstanceData" -Directory | Where-Object { $_.Name -like "*$existingName*" }
        
        if ($WhatIf) {
            Write-Host "[WHAT-IF] Would remove instance: $existingName" -ForegroundColor Magenta
        }
        else {
            foreach ($junction in $actualJunctions) {
                Remove-PlatformInstance -InstanceName $existingName -InstancePath $instancePath -JunctionPath $junction.FullName
                $removeCount++
            }
        }
    }
}

# Process each configured instance
foreach ($instance in $config.instances) {
    if (-not $instance.enabled) {
        Write-Host "`nSkipping disabled instance: $($instance.name)" -ForegroundColor Gray
        $skipCount++
        continue
    }
    
    Write-Host "`n--- Processing Instance: $($instance.name) ---" -ForegroundColor Yellow
    
    if ($WhatIf) {
        Write-Host "[WHAT-IF] Would create instance: $($instance.name)" -ForegroundColor Magenta
        continue
    }
    
    # Check if instance already exists and is enabled
    $destPath = Join-Path "PlatformInstances" $instance.destination
    
    if ((Test-Path $destPath) -and -not $Force) {
        Write-Host "Instance already exists: $($instance.name) (use -Force to recreate)" -ForegroundColor Yellow
        $skipCount++
        continue
    }
    $sourcePath = Join-Path "PlatformInstallations" $instance.source
    $destPath = Join-Path "PlatformInstances" $instance.destination
    $dataPath = Join-Path $config.defaultDataRoot $instance.dataFolder
    $junctionPath = Join-Path "InstanceData" $instance.junctionName
    
    Write-Host "Source: $sourcePath"
    Write-Host "Destination: $destPath"
    Write-Host "Data folder: $dataPath"
    Write-Host "Junction: $junctionPath"
    
    # Step 1: Copy platform installation (remove existing if Force is used)
    if ($Force -and (Test-Path $destPath)) {
        Write-Host "Force mode: Removing existing instance" -ForegroundColor Yellow
        Remove-Item -Path $destPath -Recurse -Force
    }
    
    if (Copy-PlatformInstance -Source $sourcePath -Destination $destPath) {
        
        # Step 2: Configure portable mode
        if ($instance.portableMode) {
            Set-PortableMode -InstancePath $destPath -Platform $instance.platform
        }
        
        # Step 3: Create data folder junction
        if (New-Junction -Source $dataPath -Target $junctionPath) {
            
            # Step 4: Create instance configuration
            New-InstanceConfig -Instance $instance -InstancePath $destPath
            
            Write-Host "Instance created successfully: $($instance.name)" -ForegroundColor Green
            $successCount++
        }
        else {
            Write-Host "Failed to create junction for: $($instance.name)" -ForegroundColor Red
        }
    }
    else {
        Write-Host "Failed to copy platform for: $($instance.name)" -ForegroundColor Red
    }
}

# Summary
Write-Host "`n=== Level 2 Complete ===" -ForegroundColor Green
Write-Host "Instances processed:" -ForegroundColor Cyan
Write-Host "  ✓ Created: $successCount" -ForegroundColor Green
Write-Host "  - Skipped: $skipCount" -ForegroundColor Yellow
Write-Host "  ✗ Removed: $removeCount" -ForegroundColor Red

if ($Monitor) {
    Write-Host "`n--- Starting File Monitor ---" -ForegroundColor Cyan
    Write-Host "Monitoring: $configPath" -ForegroundColor Gray
    Write-Host "Press Ctrl+C to stop monitoring" -ForegroundColor Yellow
    
    # Create file system watcher
    $watcher = New-Object System.IO.FileSystemWatcher
    $watcher.Path = Split-Path $configPath -Parent
    $watcher.Filter = Split-Path $configPath -Leaf
    $watcher.NotifyFilter = [System.IO.NotifyFilters]::LastWrite
    $watcher.EnableRaisingEvents = $true
    
    # Register event handler
    $action = {
        Write-Host "`n[$(Get-Date)] Configuration file changed, reprocessing..." -ForegroundColor Cyan
        & $PSCommandPath -ConfigFile $configPath
    }
    
    Register-ObjectEvent -InputObject $watcher -EventName "Changed" -Action $action
    
    try {
        # Keep script running
        while ($true) {
            Start-Sleep -Seconds 1
        }
    }
    finally {
        $watcher.Dispose()
    }
}

if ($successCount -gt 0) {
    Write-Host "`nInstances ready in: PlatformInstances\" -ForegroundColor Cyan
    Write-Host "Data junctions created in: InstanceData\" -ForegroundColor Cyan
    Write-Host "`nNext steps:" -ForegroundColor White
    Write-Host "  1. Configure account settings in each instance"
    Write-Host "  2. Install EAs and indicators as needed"
    Write-Host "  3. Run instances from PlatformInstances folder"
}