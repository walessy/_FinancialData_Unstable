# 3 trading_manager.ps1
# Unified Trading Platform Startup Manager
# Handles all trading platform automation - no separate startup script needed
# Works with standard user privileges after initial setup

param(
    [ValidateSet("Install", "Start", "Stop", "Status", "Remove", "Help")]
    [string]$Action = "Start",
    [switch]$CreateShortcuts = $true,
    [switch]$Quiet = $false
)

# Configuration
$ConfigFile = "instances-config.json"
$StartupFolder = "$env:APPDATA\Microsoft\Windows\Start Menu\Programs\Startup"
$StartupScript = "StartTradingPlatforms.bat"

if (-not $Quiet) {
    Write-Host "=== Simple Trading Platform Manager ===" -ForegroundColor Green
    Write-Host "Action: $Action" -ForegroundColor Cyan
}

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
    if (-not $Quiet) {
        Write-Host "Configuration loaded from: $configPath" -ForegroundColor Green
    }
}
catch {
    Write-Host "Failed to parse configuration file: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Function to find custom icon for platform
function Find-CustomIcon {
    param(
        [string]$InstanceName,
        [string]$TradingRoot
    )
    
    # Extract platform info from instance name
    $parts = $InstanceName -split "_"
    if ($parts.Length -ge 3) {
        $broker = $parts[0]
        $platform = $parts[1]
        $accountType = $parts[2]
        
        # Try different naming patterns
        $possibleSources = @(
            "${broker}_${platform}_${accountType}",
            "${broker}_${platform}-${accountType}",
            "${broker}_${platform}",
            $platform
        )
        
        foreach ($source in $possibleSources) {
            $platformPath = Join-Path $TradingRoot "PlatformInstallations\$source"
            if (Test-Path $platformPath) {
                # Look specifically in ShortCutImage subfolder first
                $shortcutImagePath = Join-Path $platformPath "ShortCutImage"
                
                if (Test-Path $shortcutImagePath) {
                    $imageExtensions = @("*.ico", "*.png", "*.jpg", "*.jpeg", "*.bmp", "*.gif")
                    
                    foreach ($ext in $imageExtensions) {
                        $iconFiles = Get-ChildItem $shortcutImagePath -Filter $ext -ErrorAction SilentlyContinue
                        if ($iconFiles) {
                            # Prefer .ico files
                            $iconFile = $iconFiles | Where-Object { $_.Extension -eq ".ico" } | Select-Object -First 1
                            if (-not $iconFile) {
                                $iconFile = $iconFiles | Select-Object -First 1
                            }
                            return $iconFile.FullName
                        }
                    }
                }
                
                # Fallback: look for icon files in main platform folder
                $imageExtensions = @("*.ico")
                foreach ($ext in $imageExtensions) {
                    $iconFiles = Get-ChildItem $platformPath -Filter $ext -ErrorAction SilentlyContinue
                    if ($iconFiles) {
                        $iconFile = $iconFiles | Select-Object -First 1
                        return $iconFile.FullName
                    }
                }
                break  # Found the platform folder, no need to check other patterns
            }
        }
    }
    
    return $null
}

# Function to create desktop shortcuts with custom icons
function Create-TradingShortcuts {
    param(
        [array]$Instances,
        [string]$TradingRoot
    )
    
    $desktop = [Environment]::GetFolderPath("Desktop")
    $shell = New-Object -ComObject WScript.Shell
    
    if (-not $Quiet) {
        Write-Host "`nCreating desktop shortcuts..." -ForegroundColor Cyan
    }
    
    $shortcutsCreated = 0
    
    foreach ($instance in $Instances) {
        if ($instance.Enabled) {
            $exePath = Join-Path $TradingRoot "PlatformInstances\$($instance.Name)\$($instance.Executable)"
            
            if (Test-Path $exePath) {
                # Create shortcut
                $shortcutPath = "$desktop\$($instance.Name).lnk"
                $shortcut = $shell.CreateShortcut($shortcutPath)
                $shortcut.TargetPath = $exePath
                $shortcut.WorkingDirectory = Split-Path $exePath -Parent
                $shortcut.Description = "Start $($instance.Name)"
                
                # Add arguments if specified
                if ($instance.Arguments) {
                    $shortcut.Arguments = $instance.Arguments
                }
                
                # Look for custom icon
                $iconPath = Find-CustomIcon -InstanceName $instance.Name -TradingRoot $TradingRoot
                if ($iconPath) {
                    $shortcut.IconLocation = $iconPath
                    if (-not $Quiet) {
                        Write-Host "  ✓ Created shortcut with custom icon: $($instance.Name)" -ForegroundColor Green
                    }
                } else {
                    # Use the executable as icon
                    $shortcut.IconLocation = $exePath
                    if (-not $Quiet) {
                        Write-Host "  ✓ Created shortcut: $($instance.Name)" -ForegroundColor Yellow
                    }
                }
                
                $shortcut.Save()
                [System.Runtime.Interopservices.Marshal]::ReleaseComObject($shortcut) | Out-Null
                $shortcutsCreated++
            }
        }
    }
    
    [System.Runtime.Interopservices.Marshal]::ReleaseComObject($shell) | Out-Null
    
    if (-not $Quiet -and $shortcutsCreated -gt 0) {
        Write-Host "Created $shortcutsCreated desktop shortcuts" -ForegroundColor Green
    }
}

# Function to remove desktop shortcuts
function Remove-TradingShortcuts {
    param([array]$Instances)
    
    $desktop = [Environment]::GetFolderPath("Desktop")
    $shortcutsRemoved = 0
    
    foreach ($instance in $Instances) {
        $shortcutPath = "$desktop\$($instance.Name).lnk"
        if (Test-Path $shortcutPath) {
            Remove-Item $shortcutPath -Force
            $shortcutsRemoved++
        }
    }
    
    if (-not $Quiet -and $shortcutsRemoved -gt 0) {
        Write-Host "Removed $shortcutsRemoved desktop shortcuts" -ForegroundColor Yellow
    }
}

# Convert instances from merged config file format to startup format
$Instances = @()
foreach ($instance in $config.instances) {
    if ($instance.enabled) {
        # Use executable from startupSettings if available, otherwise determine from platform
        $executable = if ($instance.startupSettings -and $instance.startupSettings.executable) {
            $instance.startupSettings.executable
        } else {
            switch ($instance.platform.ToUpper()) {
                "MT4" { "terminal.exe" }
                "MT5" { "terminal64.exe" }
                "TRADEREVOLUTION" { "TradeTerminal.exe" }
                default { "terminal.exe" }
            }
        }
        
        $delay = if ($instance.startupSettings -and $instance.startupSettings.startupDelay) {
            $instance.startupSettings.startupDelay
        } else {
            0
        }
        
        $autoStart = if ($instance.startupSettings -and $instance.startupSettings.PSObject.Properties["autoStart"]) {
            $instance.startupSettings.autoStart
        } else {
            $true  # Default to auto-start if not specified
        }
        
        $arguments = if ($instance.startupSettings -and $instance.startupSettings.arguments) {
            $instance.startupSettings.arguments
        } else {
            ""
        }
        
        $Instances += @{
            Name = $instance.destination
            Executable = $executable
            Delay = $delay
            Enabled = $instance.enabled
            AutoStart = $autoStart
            Arguments = $arguments
        }
    }
}

# Main action processing
switch ($Action) {
    "Install" {
        if (-not $Quiet) {
            Write-Host "Installing automatic startup..." -ForegroundColor Yellow
        }
        
        # Create the startup batch file
        $batchContent = @"
@echo off
echo Starting Trading Platforms...
timeout /t 5 /nobreak >nul

"@
        
        $addedToStartup = 0
        
        # Add each enabled auto-start instance to the batch file
        foreach ($instance in $Instances) {
            if ($instance.Enabled -and $instance.AutoStart) {
                $exePath = Join-Path $TradingRoot "PlatformInstances\$($instance.Name)\$($instance.Executable)"
                
                if (Test-Path $exePath) {
                    $args = if ($instance.Arguments) { " $($instance.Arguments)" } else { "" }
                    $batchContent += @"
echo Starting $($instance.Name)...
timeout /t $($instance.Delay) /nobreak >nul
start "" "$exePath"$args

"@
                    if (-not $Quiet) {
                        Write-Host "Added to startup: $($instance.Name)" -ForegroundColor Green
                    }
                    $addedToStartup++
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
        
        if (-not $Quiet) {
            Write-Host "Startup script installed: $startupPath" -ForegroundColor Green
            Write-Host "Added $addedToStartup platforms to automatic startup" -ForegroundColor Cyan
        }
        
        # Create desktop shortcuts if requested
        if ($CreateShortcuts) {
            Create-TradingShortcuts -Instances $Instances -TradingRoot $TradingRoot
        }
        
        if (-not $Quiet) {
            Write-Host "Trading platforms will start automatically when you log in." -ForegroundColor Cyan
        }
    }
    
    "Start" {
        if (-not $Quiet) {
            Write-Host "Starting trading platforms manually..." -ForegroundColor Yellow
        }
        
        $startedCount = 0
        
        foreach ($instance in $Instances) {
            if ($instance.Enabled) {
                $exePath = Join-Path $TradingRoot "PlatformInstances\$($instance.Name)\$($instance.Executable)"
                
                if (Test-Path $exePath) {
                    if (-not $Quiet) {
                        Write-Host "Starting $($instance.Name)..." -ForegroundColor Cyan
                    }
                    Start-Sleep $instance.Delay
                    
                    if ($instance.Arguments) {
                        Start-Process $exePath -ArgumentList $instance.Arguments
                    } else {
                        Start-Process $exePath
                    }
                    
                    if (-not $Quiet) {
                        Write-Host "Started: $($instance.Name)" -ForegroundColor Green
                    }
                    $startedCount++
                } else {
                    Write-Host "Executable not found: $($instance.Name)" -ForegroundColor Red
                }
            }
        }
        
        if (-not $Quiet) {
            Write-Host "Manual startup complete - started $startedCount platforms." -ForegroundColor Green
        }
    }
    
    "Stop" {
        if (-not $Quiet) {
            Write-Host "Stopping trading platforms..." -ForegroundColor Yellow
        }
        
        $stoppedCount = 0
        
        foreach ($instance in $Instances) {
            $processName = [System.IO.Path]::GetFileNameWithoutExtension($instance.Executable)
            $processes = Get-Process -Name $processName -ErrorAction SilentlyContinue
            
            if ($processes) {
                $processes | Stop-Process -Force
                if (-not $Quiet) {
                    Write-Host "Stopped: $processName ($($processes.Count) processes)" -ForegroundColor Yellow
                }
                $stoppedCount += $processes.Count
            }
        }
        
        if (-not $Quiet) {
            Write-Host "Stop complete - stopped $stoppedCount processes." -ForegroundColor Green
        }
    }
    
    "Status" {
        Write-Host "Trading Platform Status:" -ForegroundColor Yellow
        Write-Host ("{0,-35} {1,-15} {2,-10} {3,-10}" -f "Platform", "Process", "Status", "Auto-Start") -ForegroundColor White
        Write-Host ("-" * 75) -ForegroundColor Gray
        
        $runningCount = 0
        $totalCount = 0
        
        foreach ($instance in $Instances) {
            $processName = [System.IO.Path]::GetFileNameWithoutExtension($instance.Executable)
            $processes = Get-Process -Name $processName -ErrorAction SilentlyContinue
            
            if ($processes) {
                $status = "Running ($($processes.Count))"
                $color = "Green"
                $runningCount += $processes.Count
            } else {
                $status = "Stopped"
                $color = "Red"
            }
            
            $autoStartStatus = if ($instance.AutoStart) { "Yes" } else { "No" }
            $totalCount++
            
            Write-Host ("{0,-35} {1,-15} {2,-10} {3,-10}" -f 
                $instance.Name.Substring(0, [Math]::Min(34, $instance.Name.Length)), 
                $processName, 
                $status, 
                $autoStartStatus
            ) -ForegroundColor $color
        }
        
        # Summary
        Write-Host "`nSummary:" -ForegroundColor Cyan
        Write-Host "  Total platforms configured: $totalCount" -ForegroundColor White
        Write-Host "  Currently running: $runningCount" -ForegroundColor $(if ($runningCount -gt 0) { "Green" } else { "Yellow" })
        
        # Check if startup is installed
        $startupPath = Join-Path $StartupFolder $StartupScript
        if (Test-Path $startupPath) {
            Write-Host "  Automatic startup: ENABLED" -ForegroundColor Green
        } else {
            Write-Host "  Automatic startup: DISABLED" -ForegroundColor Red
        }
        
        # Check desktop shortcuts
        $desktop = [Environment]::GetFolderPath("Desktop")
        $shortcutCount = 0
        foreach ($instance in $Instances) {
            $shortcutPath = "$desktop\$($instance.Name).lnk"
            if (Test-Path $shortcutPath) {
                $shortcutCount++
            }
        }
        Write-Host "  Desktop shortcuts: $shortcutCount created" -ForegroundColor $(if ($shortcutCount -gt 0) { "Green" } else { "Yellow" })
    }
    
    "Remove" {
        if (-not $Quiet) {
            Write-Host "Removing trading platform automation..." -ForegroundColor Yellow
        }
        
        # Remove startup script
        $startupPath = Join-Path $StartupFolder $StartupScript
        if (Test-Path $startupPath) {
            Remove-Item $startupPath -Force
            if (-not $Quiet) {
                Write-Host "Startup script removed: $startupPath" -ForegroundColor Green
            }
        } else {
            if (-not $Quiet) {
                Write-Host "Startup script not found." -ForegroundColor Gray
            }
        }
        
        # Remove desktop shortcuts
        Remove-TradingShortcuts -Instances $Instances
        
        if (-not $Quiet) {
            Write-Host "Trading platform automation removed." -ForegroundColor Green
        }
    }
    
    "Help" {
        Write-Host "`n=== Trading Platform Manager Help ===" -ForegroundColor Green
        Write-Host ""
        Write-Host "USAGE:" -ForegroundColor Cyan
        Write-Host "  .\Setup\'3 trading_manager.ps1' [-Action] <Action> [Options]"
        Write-Host ""
        Write-Host "ACTIONS:" -ForegroundColor Cyan
        Write-Host "  Install   Set up automatic startup and create desktop shortcuts" -ForegroundColor White
        Write-Host "  Start     Start all enabled trading platforms now" -ForegroundColor White
        Write-Host "  Stop      Stop all running trading platforms" -ForegroundColor White
        Write-Host "  Status    Show status of all platforms and automation" -ForegroundColor White
        Write-Host "  Remove    Remove automatic startup and desktop shortcuts" -ForegroundColor White
        Write-Host "  Help      Show this help message" -ForegroundColor White
        Write-Host ""
        Write-Host "OPTIONS:" -ForegroundColor Cyan
        Write-Host "  -CreateShortcuts    Create/skip desktop shortcuts (default: true)" -ForegroundColor White
        Write-Host "  -Quiet             Suppress non-essential output" -ForegroundColor White
        Write-Host ""
        Write-Host "EXAMPLES:" -ForegroundColor Cyan
        Write-Host "  .\Setup\'3 trading_manager.ps1' -Action Install" -ForegroundColor Gray
        Write-Host "  .\Setup\'3 trading_manager.ps1' -Action Start -Quiet" -ForegroundColor Gray
        Write-Host "  .\Setup\'3 trading_manager.ps1' -Action Install -CreateShortcuts:$false" -ForegroundColor Gray
        Write-Host ""
        Write-Host "CONFIGURATION:" -ForegroundColor Cyan
        Write-Host "  Edit instances-config.json to add/remove platforms" -ForegroundColor White
        Write-Host "  Set 'autoStart: false' to exclude from automatic startup" -ForegroundColor White
        Write-Host "  Set 'enabled: false' to completely disable a platform" -ForegroundColor White
        return
    }
}

# Show usage instructions (unless Help was called or Quiet mode)
if ($Action -ne "Help" -and -not $Quiet) {
    Write-Host "`n=== Quick Usage ===" -ForegroundColor Cyan
    Write-Host ".\Setup\'3 trading_manager.ps1' -Action Install   # Set up automatic startup" 
    Write-Host ".\Setup\'3 trading_manager.ps1' -Action Start     # Start platforms now"
    Write-Host ".\Setup\'3 trading_manager.ps1' -Action Stop      # Stop all platforms"
    Write-Host ".\Setup\'3 trading_manager.ps1' -Action Status    # Check what's running"
    Write-Host ".\Setup\'3 trading_manager.ps1' -Action Remove    # Remove automation"
    Write-Host ".\Setup\'3 trading_manager.ps1' -Action Help      # Show detailed help"
}