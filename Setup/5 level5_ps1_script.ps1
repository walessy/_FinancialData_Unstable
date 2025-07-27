# ============================================================================
# Level 5: Trading Platform Control Dashboard
# ============================================================================
# This is a C# WinForms application, not a PowerShell script
# The actual dashboard executable is located in the Level4Dashboard folder
# ============================================================================

param(
    [Parameter(Mandatory=$true, Position=0)]
    [ValidateSet("Help", "Launch", "Build", "Status", "Install")]
    [string]$Action,
    
    [switch]$Release,
    [switch]$Quiet
)

# Script metadata
$ScriptVersion = "1.0.0"
$ScriptName = "Level 5: Trading Platform Control Dashboard"

function Write-Header {
    if (-not $Quiet) {
        Write-Host ""
        Write-Host "════════════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
        Write-Host "  $ScriptName" -ForegroundColor Yellow
        Write-Host "  Version: $ScriptVersion" -ForegroundColor Gray
        Write-Host "════════════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
        Write-Host ""
    }
}

function Get-TradingRoot {
    # Try to find the trading root directory
    $currentPath = Get-Location
    $configFile = Join-Path $currentPath "instances-config.json"
    
    if (Test-Path $configFile) {
        return $currentPath
    }
    
    # Check parent directory
    $parentPath = Split-Path $currentPath -Parent
    $parentConfigFile = Join-Path $parentPath "instances-config.json"
    
    if (Test-Path $parentConfigFile) {
        return $parentPath
    }
    
    return $null
}

function Get-DashboardPath {
    $tradingRoot = Get-TradingRoot
    if (-not $tradingRoot) {
        Write-Host "Error: Could not locate trading root directory with instances-config.json" -ForegroundColor Red
        return $null
    }
    
    $dashboardPath = Join-Path $tradingRoot "Level4Dashboard"
    return $dashboardPath
}

function Test-DotNetInstalled {
    try {
        $dotnetVersion = & dotnet --version 2>$null
        return $true
    }
    catch {
        return $false
    }
}

function Show-Help {
    Write-Header
    
    Write-Host "OVERVIEW:" -ForegroundColor Green
    Write-Host "  Level 5 provides a visual C# WinForms control dashboard for managing" -ForegroundColor White
    Write-Host "  your trading platform instances with advanced features including:" -ForegroundColor White
    Write-Host ""
    Write-Host "  • Visual instance management with icons and grouping" -ForegroundColor Gray
    Write-Host "  • Group operations (start/stop all instances in a group)" -ForegroundColor Gray
    Write-Host "  • Context menus for individual instance control" -ForegroundColor Gray
    Write-Host "  • Integration with Level 3/4 PowerShell automation" -ForegroundColor Gray
    Write-Host "  • Real-time performance monitoring" -ForegroundColor Gray
    Write-Host "  • Advanced batch operations" -ForegroundColor Gray
    Write-Host "  • Configuration editor" -ForegroundColor Gray
    Write-Host "  • Dark/Light theme support" -ForegroundColor Gray
    Write-Host ""
    
    Write-Host "USAGE:" -ForegroundColor Cyan
    Write-Host "  .\Setup\'5 Level5-TradingControlDashboard.ps1' [-Action] <Action> [Options]" -ForegroundColor White
    Write-Host ""
    
    Write-Host "ACTIONS:" -ForegroundColor Cyan
    Write-Host "  Help      Show this help information" -ForegroundColor White
    Write-Host "  Launch    Launch the Level 5 dashboard application" -ForegroundColor White
    Write-Host "  Build     Build the dashboard (Debug or Release)" -ForegroundColor White
    Write-Host "  Status    Show dashboard and .NET status" -ForegroundColor White
    Write-Host "  Install   Install .NET SDK if not present (requires admin)" -ForegroundColor White
    Write-Host ""
    
    Write-Host "OPTIONS:" -ForegroundColor Cyan
    Write-Host "  -Release  Build in Release mode (default: Debug)" -ForegroundColor White
    Write-Host "  -Quiet    Suppress non-essential output" -ForegroundColor White
    Write-Host ""
    
    Write-Host "EXAMPLES:" -ForegroundColor Green
    Write-Host "  .\Setup\'5 Level5-TradingControlDashboard.ps1' -Action Launch" -ForegroundColor Gray
    Write-Host "  .\Setup\'5 Level5-TradingControlDashboard.ps1' -Action Build -Release" -ForegroundColor Gray
    Write-Host "  .\Setup\'5 Level5-TradingControlDashboard.ps1' -Action Status" -ForegroundColor Gray
    Write-Host ""
    
    Write-Host "TECHNICAL DETAILS:" -ForegroundColor Cyan
    Write-Host "  • Platform: .NET 9.0 with Windows Forms" -ForegroundColor White
    Write-Host "  • Language: C# with modern nullable reference types" -ForegroundColor White
    Write-Host "  • Architecture: Self-contained WinForms application" -ForegroundColor White
    Write-Host "  • Location: Level4Dashboard/ subdirectory" -ForegroundColor White
    Write-Host "  • Integration: Reads instances-config.json and calls Level 3/4 scripts" -ForegroundColor White
    Write-Host ""
    
    Write-Host "PREREQUISITES:" -ForegroundColor Cyan
    Write-Host "  • Windows 10/11" -ForegroundColor White
    Write-Host "  • .NET 9.0 SDK (will prompt to install if missing)" -ForegroundColor White
    Write-Host "  • Completed Level 1-4 setup" -ForegroundColor White
    Write-Host "  • Valid instances-config.json file" -ForegroundColor White
    Write-Host ""
}

function Show-Status {
    Write-Header
    
    $tradingRoot = Get-TradingRoot
    $dashboardPath = Get-DashboardPath
    
    Write-Host "TRADING ENVIRONMENT STATUS:" -ForegroundColor Green
    Write-Host "  Trading Root: " -NoNewline -ForegroundColor White
    if ($tradingRoot) {
        Write-Host $tradingRoot -ForegroundColor Green
    } else {
        Write-Host "Not Found" -ForegroundColor Red
    }
    
    Write-Host "  Dashboard Path: " -NoNewline -ForegroundColor White
    if ($dashboardPath -and (Test-Path $dashboardPath)) {
        Write-Host $dashboardPath -ForegroundColor Green
    } else {
        Write-Host "Not Found" -ForegroundColor Red
    }
    
    Write-Host ""
    Write-Host ".NET ENVIRONMENT STATUS:" -ForegroundColor Green
    
    if (Test-DotNetInstalled) {
        try {
            $dotnetVersion = & dotnet --version 2>$null
            Write-Host "  .NET SDK: " -NoNewline -ForegroundColor White
            Write-Host "v$dotnetVersion" -ForegroundColor Green
            
            # Check if project exists
            $projectFile = Join-Path $dashboardPath "Level4Dashboard.csproj"
            Write-Host "  Project File: " -NoNewline -ForegroundColor White
            if (Test-Path $projectFile) {
                Write-Host "Found" -ForegroundColor Green
            } else {
                Write-Host "Not Found" -ForegroundColor Red
            }
            
            # Check if executable exists
            $debugExe = Join-Path $dashboardPath "bin\Debug\net9.0-windows\Level4Dashboard.exe"
            $releaseExe = Join-Path $dashboardPath "bin\Release\net9.0-windows\Level4Dashboard.exe"
            
            Write-Host "  Debug Build: " -NoNewline -ForegroundColor White
            if (Test-Path $debugExe) {
                Write-Host "Available" -ForegroundColor Green
            } else {
                Write-Host "Not Built" -ForegroundColor Yellow
            }
            
            Write-Host "  Release Build: " -NoNewline -ForegroundColor White
            if (Test-Path $releaseExe) {
                Write-Host "Available" -ForegroundColor Green
            } else {
                Write-Host "Not Built" -ForegroundColor Yellow
            }
        }
        catch {
            Write-Host "  Status: Error checking .NET details" -ForegroundColor Red
        }
    } else {
        Write-Host "  .NET SDK: " -NoNewline -ForegroundColor White
        Write-Host "Not Installed" -ForegroundColor Red
        Write-Host "  Run: .\Setup\'5 Level5-TradingControlDashboard.ps1' -Action Install" -ForegroundColor Yellow
    }
    
    Write-Host ""
    Write-Host "CONFIGURATION STATUS:" -ForegroundColor Green
    
    if ($tradingRoot) {
        $configFile = Join-Path $tradingRoot "instances-config.json"
        if (Test-Path $configFile) {
            try {
                $config = Get-Content $configFile -Raw | ConvertFrom-Json
                $instanceCount = $config.instances.Count
                $enabledCount = ($config.instances | Where-Object { $_.enabled }).Count
                
                Write-Host "  Configuration: " -NoNewline -ForegroundColor White
                Write-Host "Valid" -ForegroundColor Green
                Write-Host "  Total Instances: " -NoNewline -ForegroundColor White
                Write-Host $instanceCount -ForegroundColor Cyan
                Write-Host "  Enabled Instances: " -NoNewline -ForegroundColor White
                Write-Host $enabledCount -ForegroundColor Green
            }
            catch {
                Write-Host "  Configuration: " -NoNewline -ForegroundColor White
                Write-Host "Invalid JSON" -ForegroundColor Red
            }
        } else {
            Write-Host "  Configuration: " -NoNewline -ForegroundColor White
            Write-Host "Missing" -ForegroundColor Red
        }
    }
    
    Write-Host ""
}

function Install-DotNet {
    Write-Header
    
    if (Test-DotNetInstalled) {
        Write-Host ".NET SDK is already installed." -ForegroundColor Green
        return
    }
    
    Write-Host "Installing .NET 9.0 SDK..." -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Note: This requires internet connection and administrator privileges." -ForegroundColor Cyan
    Write-Host ""
    
    $confirmation = Read-Host "Continue with .NET installation? (y/N)"
    if ($confirmation -ne 'y' -and $confirmation -ne 'Y') {
        Write-Host "Installation cancelled." -ForegroundColor Yellow
        return
    }
    
    try {
        # Download and run .NET installer
        $installerUrl = "https://download.microsoft.com/download/7/8/b/78b69c5c-93ad-41bc-aaf6-7b2dc8e32f50/dotnet-sdk-9.0.100-win-x64.exe"
        $installerPath = Join-Path $env:TEMP "dotnet-sdk-installer.exe"
        
        Write-Host "Downloading .NET SDK installer..." -ForegroundColor Cyan
        Invoke-WebRequest -Uri $installerUrl -OutFile $installerPath
        
        Write-Host "Running installer..." -ForegroundColor Cyan
        Start-Process -FilePath $installerPath -ArgumentList "/quiet" -Wait
        
        Remove-Item $installerPath -Force
        
        Write-Host ".NET SDK installation completed!" -ForegroundColor Green
        Write-Host "You may need to restart your terminal for PATH changes to take effect." -ForegroundColor Yellow
    }
    catch {
        Write-Host "Failed to install .NET SDK: $($_.Exception.Message)" -ForegroundColor Red
        Write-Host "Please install manually from: https://dotnet.microsoft.com/download" -ForegroundColor Yellow
    }
}

function Build-Dashboard {
    Write-Header
    
    $dashboardPath = Get-DashboardPath
    if (-not $dashboardPath -or -not (Test-Path $dashboardPath)) {
        Write-Host "Error: Dashboard project not found at expected location." -ForegroundColor Red
        Write-Host "Expected: $dashboardPath" -ForegroundColor Gray
        return
    }
    
    if (-not (Test-DotNetInstalled)) {
        Write-Host "Error: .NET SDK not installed. Run with -Action Install first." -ForegroundColor Red
        return
    }
    
    $buildConfig = if ($Release) { "Release" } else { "Debug" }
    
    Write-Host "Building Level 5 Dashboard ($buildConfig mode)..." -ForegroundColor Cyan
    Write-Host "Location: $dashboardPath" -ForegroundColor Gray
    Write-Host ""
    
    Push-Location $dashboardPath
    try {
        $buildArgs = @("build", "--configuration", $buildConfig)
        if ($Quiet) {
            $buildArgs += "--verbosity", "quiet"
        }
        
        & dotnet @buildArgs
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host ""
            Write-Host "Build completed successfully!" -ForegroundColor Green
            
            $exePath = "bin\$buildConfig\net9.0-windows\Level4Dashboard.exe"
            Write-Host "Executable: $exePath" -ForegroundColor Cyan
        } else {
            Write-Host ""
            Write-Host "Build failed with exit code: $LASTEXITCODE" -ForegroundColor Red
        }
    }
    finally {
        Pop-Location
    }
}

function Launch-Dashboard {
    Write-Header
    
    $dashboardPath = Get-DashboardPath
    if (-not $dashboardPath -or -not (Test-Path $dashboardPath)) {
        Write-Host "Error: Dashboard project not found." -ForegroundColor Red
        return
    }
    
    # Try Release build first, then Debug
    $releaseExe = Join-Path $dashboardPath "bin\Release\net9.0-windows\Level4Dashboard.exe"
    $debugExe = Join-Path $dashboardPath "bin\Debug\net9.0-windows\Level4Dashboard.exe"
    
    $exePath = $null
    if (Test-Path $releaseExe) {
        $exePath = $releaseExe
        $buildType = "Release"
    } elseif (Test-Path $debugExe) {
        $exePath = $debugExe
        $buildType = "Debug"
    }
    
    if (-not $exePath) {
        Write-Host "Dashboard executable not found. Building now..." -ForegroundColor Yellow
        Build-Dashboard
        
        # Check again after build
        if (Test-Path $debugExe) {
            $exePath = $debugExe
            $buildType = "Debug"
        } else {
            Write-Host "Build failed or executable still not found." -ForegroundColor Red
            return
        }
    }
    
    Write-Host "Launching Level 5 Trading Control Dashboard..." -ForegroundColor Green
    Write-Host "Executable: $exePath ($buildType)" -ForegroundColor Cyan
    Write-Host ""
    
    try {
        Start-Process -FilePath $exePath
        Write-Host "Dashboard launched successfully!" -ForegroundColor Green
    }
    catch {
        Write-Host "Failed to launch dashboard: $($_.Exception.Message)" -ForegroundColor Red
    }
}

# Main execution
switch ($Action) {
    "Help" {
        Show-Help
    }
    
    "Status" {
        Show-Status
    }
    
    "Install" {
        Install-DotNet
    }
    
    "Build" {
        Build-Dashboard
    }
    
    "Launch" {
        Launch-Dashboard
    }
}

if (-not $Quiet) {
    Write-Host ""
    Write-Host "Level 5 Trading Control Dashboard - C# WinForms Application" -ForegroundColor Cyan
    Write-Host "For help: .\Setup\'5 Level5-TradingControlDashboard.ps1' -Action Help" -ForegroundColor Gray
    Write-Host ""
}