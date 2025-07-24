# Simple Trading Platform Startup Manager
# Works with standard user privileges - no admin needed after initial setup

param(
    [ValidateSet("Install", "Start", "Stop", "Status", "Remove")]
    [string]$Action = "Start"
)

# Read configuration from existing instances-config.json
$ConfigFile = "instances-config.json"
$StartupFolder = "$env:APPDATA\Microsoft\Windows\Start Menu\Programs\Startup"
$StartupScript = "StartTradingPlatforms.bat"

# Load configuration
$configPath = Join-Path (Get-Location) $ConfigFile
if (-not (Test-Path $configPath)) {
    Write-Host "Configuration file not found: $configPath" -ForegroundColor Red
    Write-Host "Please ensure instances-config.json exists in the current directory" -ForegroundColor Yellow
    exit 1
}

try {
    $config = Get-Content $configPath -Raw | ConvertFrom-Json
    $TradingRoot = $config.tradingRoot
    Write-Host "Configuration loaded from: $configPath" -ForegroundColor Green
}
catch {
    Write-Host "Failed to parse configuration file: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Convert instances from config file format to startup format
$Instances = @()
$delayCounter = 0
foreach ($instance in $config.instances) {
    if ($instance.enabled) {
        # Determine executable based on platform
        $executable = switch ($instance.platform.ToUpper()) {
            "MT4" { "terminal.exe" }
            "MT5" { "terminal64.exe" }
            "TRADEREVOLUTION" { "TraderEvolution.exe" }
            default { "terminal.exe" }
        }
        
        $Instances += @{
            Name = $instance.destination
            Executable = $executable
            Delay = $delayCounter
            Enabled = $instance.enabled
        }
        
        $delayCounter += 10  # 10-second stagger between platforms
    }
}

Write-Host "=== Simple Trading Platform Manager ===" -ForegroundColor Green
Write-Host "Action: $Action" -ForegroundColor Cyan

switch ($Action) {
    "Install" {
        Write-Host "Installing automatic startup..." -ForegroundColor Yellow
        
        # Create the startup batch file
        $batchContent = @"
@echo off
echo Starting Trading Platforms...
timeout /t 5 /nobreak >nul

"@
        
        # Add each enabled instance to the batch file
        foreach ($instance in $Instances) {
            if ($instance.Enabled) {
                $exePath = Join-Path $TradingRoot "PlatformInstances\$($instance.Name)\$($instance.Executable)"
                
                if (Test-Path $exePath) {
                    $batchContent += @"
echo Starting $($instance.Name)...
timeout /t $($instance.Delay) /nobreak >nul
start "" "$exePath"

"@
                    Write-Host "Added to startup: $($instance.Name)" -ForegroundColor Green
                } else {
                    Write-Host "Executable not found: $exePath" -ForegroundColor Red
                }
            }
        }
        
        $batchContent += @"
echo All trading platforms started.
timeout /t 3 /nobreak >nul
"@
        
        # Save to startup folder
        $startupPath = Join-Path $StartupFolder $StartupScript
        $batchContent | Out-File -FilePath $startupPath -Encoding ASCII
        
        Write-Host "Startup script installed: $startupPath" -ForegroundColor Green
        Write-Host "Trading platforms will start automatically when you log in." -ForegroundColor Cyan
    }
    
    "Start" {
        Write-Host "Starting trading platforms manually..." -ForegroundColor Yellow
        
        foreach ($instance in $Instances) {
            if ($instance.Enabled) {
                $exePath = Join-Path $TradingRoot "PlatformInstances\$($instance.Name)\$($instance.Executable)"
                
                if (Test-Path $exePath) {
                    Write-Host "Starting $($instance.Name)..." -ForegroundColor Cyan
                    Start-Sleep $instance.Delay
                    Start-Process $exePath
                    Write-Host "Started: $($instance.Name)" -ForegroundColor Green
                } else {
                    Write-Host "Executable not found: $($instance.Name)" -ForegroundColor Red
                }
            } else {
                Write-Host "Skipped (disabled): $($instance.Name)" -ForegroundColor Gray
            }
        }
        
        Write-Host "Manual startup complete." -ForegroundColor Green
    }
    
    "Stop" {
        Write-Host "Stopping trading platforms..." -ForegroundColor Yellow
        
        foreach ($instance in $Instances) {
            $processName = [System.IO.Path]::GetFileNameWithoutExtension($instance.Executable)
            $processes = Get-Process -Name $processName -ErrorAction SilentlyContinue
            
            if ($processes) {
                $processes | Stop-Process -Force
                Write-Host "Stopped: $processName ($($processes.Count) processes)" -ForegroundColor Yellow
            }
        }
        
        Write-Host "Stop complete." -ForegroundColor Green
    }
    
    "Status" {
        Write-Host "Trading Platform Status:" -ForegroundColor Yellow
        Write-Host ("{0,-30} {1,-15} {2,-10}" -f "Platform", "Process", "Status") -ForegroundColor White
        Write-Host ("-" * 55) -ForegroundColor Gray
        
        foreach ($instance in $Instances) {
            $processName = [System.IO.Path]::GetFileNameWithoutExtension($instance.Executable)
            $processes = Get-Process -Name $processName -ErrorAction SilentlyContinue
            
            if ($processes) {
                $status = "Running ($($processes.Count))"
                $color = "Green"
            } else {
                $status = "Stopped"
                $color = "Red"
            }
            
            Write-Host ("{0,-30} {1,-15} {2,-10}" -f $instance.Name, $processName, $status) -ForegroundColor $color
        }
        
        # Check if startup is installed
        $startupPath = Join-Path $StartupFolder $StartupScript
        if (Test-Path $startupPath) {
            Write-Host "`nAutomatic startup: ENABLED" -ForegroundColor Green
        } else {
            Write-Host "`nAutomatic startup: DISABLED" -ForegroundColor Red
        }
    }
    
    "Remove" {
        Write-Host "Removing automatic startup..." -ForegroundColor Yellow
        
        $startupPath = Join-Path $StartupFolder $StartupScript
        if (Test-Path $startupPath) {
            Remove-Item $startupPath -Force
            Write-Host "Startup script removed: $startupPath" -ForegroundColor Green
        } else {
            Write-Host "Startup script not found." -ForegroundColor Gray
        }
    }
}

Write-Host "`n=== Usage Instructions ===" -ForegroundColor Cyan
Write-Host ".\SimpleTradingManager.ps1 -Action Install   # Set up automatic startup" 
Write-Host ".\SimpleTradingManager.ps1 -Action Start     # Start platforms now"
Write-Host ".\SimpleTradingManager.ps1 -Action Stop      # Stop all platforms"
Write-Host ".\SimpleTradingManager.ps1 -Action Status    # Check what's running"
Write-Host ".\SimpleTradingManager.ps1 -Action Remove    # Remove automatic startup"