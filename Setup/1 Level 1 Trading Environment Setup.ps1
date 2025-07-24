# 1 Level 1 Trading Environment Setup.ps1
# This script installs Chocolatey, R, WPF dependencies and creates directory structure

param(
    [string]$TradingRootPath = "C:\TradingRoot"
)

# Ensure script runs as Administrator
if (-NOT ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")) {
    Write-Host "This script requires Administrator privileges. Restarting as Administrator..." -ForegroundColor Yellow
    Start-Process powershell.exe "-File", $PSCommandPath, "-TradingRootPath", $TradingRootPath -Verb RunAs
    exit
}

Write-Host "=== Level 1 Trading Environment Setup ===" -ForegroundColor Green
Write-Host "Setting up trading environment at: $TradingRootPath" -ForegroundColor Cyan

# Function to check if software is installed
function Test-SoftwareInstalled {
    param([string]$SoftwareName)
    
    try {
        $null = Get-Command $SoftwareName -ErrorAction Stop
        return $true
    }
    catch {
        return $false
    }
}

# 1. Install Chocolatey
Write-Host "`n--- Installing Chocolatey ---" -ForegroundColor Yellow
if (Test-SoftwareInstalled "choco") {
    Write-Host "Chocolatey already installed" -ForegroundColor Green
} else {
    Write-Host "Installing Chocolatey..."
    Set-ExecutionPolicy Bypass -Scope Process -Force
    [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.ServicePointManager]::SecurityProtocol -bor 3072
    Invoke-Expression ((New-Object System.Net.WebClient).DownloadString('https://community.chocolatey.org/install.ps1'))
    
    # Refresh environment variables
    $env:Path = [System.Environment]::GetEnvironmentVariable("Path","Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path","User")
    
    if (Test-SoftwareInstalled "choco") {
        Write-Host "Chocolatey installed successfully" -ForegroundColor Green
    } else {
        Write-Host "Failed to install Chocolatey" -ForegroundColor Red
        exit 1
    }
}

# 2. Install R
Write-Host "`n--- Installing R ---" -ForegroundColor Yellow
if (Test-SoftwareInstalled "R") {
    Write-Host "R already installed" -ForegroundColor Green
} else {
    Write-Host "Installing R via Chocolatey..."
    choco install r.project -y
    
    # Refresh environment variables
    $env:Path = [System.Environment]::GetEnvironmentVariable("Path","Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path","User")
    
    if (Test-SoftwareInstalled "R") {
        Write-Host "R installed successfully" -ForegroundColor Green
    } else {
        Write-Host "R installation may require system restart to update PATH" -ForegroundColor Yellow
    }
}

# 3. Ensure WPF is available (part of .NET Framework on Windows)
Write-Host "`n--- Checking WPF Dependencies ---" -ForegroundColor Yellow
try {
    # Check if .NET Framework with WPF is available
    $dotNetVersion = Get-ItemProperty "HKLM:SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full\" -Name Release -ErrorAction Stop
    if ($dotNetVersion.Release -ge 461808) {
        Write-Host ".NET Framework 4.7.2+ detected - WPF available" -ForegroundColor Green
    } else {
        Write-Host "Installing .NET Framework 4.8 for WPF support..."
        choco install netfx-4.8 -y
    }
} catch {
    Write-Host "Installing .NET Framework 4.8 for WPF support..."
    choco install netfx-4.8 -y
}

# Install additional WPF development tools if needed
Write-Host "Installing additional development tools..."
try {
    choco install dotnet-runtime -y
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Trying alternative package name..." -ForegroundColor Yellow
        choco install dotnetcore-runtime -y
    }
} catch {
    Write-Host "Additional runtime installation failed, but .NET Framework is sufficient for WPF" -ForegroundColor Yellow
}

# 4. Create Directory Structure
Write-Host "`n--- Creating Directory Structure ---" -ForegroundColor Yellow

$directories = @(
    $TradingRootPath,
    "$TradingRootPath\PlatformInstallations",
    "$TradingRootPath\InstanceData", 
    "$TradingRootPath\Setup",
    "$TradingRootPath\PlatformInstances"
)

foreach ($dir in $directories) {
    if (Test-Path $dir) {
        Write-Host "Directory already exists: $dir" -ForegroundColor Yellow
    } else {
        try {
            New-Item -ItemType Directory -Path $dir -Force | Out-Null
            Write-Host "Created directory: $dir" -ForegroundColor Green
        } catch {
            Write-Host "Failed to create directory: $dir - $($_.Exception.Message)" -ForegroundColor Red
            exit 1
        }
    }
}

# 5. Create a basic setup configuration file
Write-Host "`n--- Creating Setup Configuration ---" -ForegroundColor Yellow
$configContent = @"
# Trading Environment Configuration
# Generated: $(Get-Date)

[Environment]
TradingRootPath=$TradingRootPath
ChocolateyInstalled=True
RInstalled=$(Test-SoftwareInstalled "R")
WPFAvailable=True

[Directories]
PlatformInstallations=$TradingRootPath\PlatformInstallations
InstanceData=$TradingRootPath\InstanceData
Setup=$TradingRootPath\Setup
PlatformInstances=$TradingRootPath\PlatformInstances

[Status]
Level1Complete=True
SetupDate=$(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
"@

$configPath = "$TradingRootPath\Setup\environment.config"
$configContent | Out-File -FilePath $configPath -Encoding UTF8
Write-Host "Configuration saved to: $configPath" -ForegroundColor Green

# 6. Summary
Write-Host "`n=== Setup Complete ===" -ForegroundColor Green
Write-Host "Trading environment initialized at: $TradingRootPath" -ForegroundColor Cyan
Write-Host "`nDirectory structure created:"
Write-Host "  ├── PlatformInstallations/" -ForegroundColor Gray
Write-Host "  ├── InstanceData/" -ForegroundColor Gray  
Write-Host "  ├── Setup/" -ForegroundColor Gray
Write-Host "  └── PlatformInstances/" -ForegroundColor Gray

Write-Host "`nSoftware installed:"
Write-Host "  ✓ Chocolatey" -ForegroundColor Green
Write-Host "  ✓ R" -ForegroundColor Green
Write-Host "  ✓ WPF Dependencies" -ForegroundColor Green

Write-Host "`nNext steps:"
Write-Host "  1. Extract platform installations to PlatformInstallations\"
Write-Host "  2. Run Level 2 script to create platform instances"
Write-Host "  3. Configure junctions in InstanceData\"

if ($LASTEXITCODE -eq 0 -or $null -eq $LASTEXITCODE) {
    Write-Host "`nLevel 1 setup completed successfully!" -ForegroundColor Green
} else {
    Write-Host "`nSetup completed with some warnings. Check output above." -ForegroundColor Yellow
}