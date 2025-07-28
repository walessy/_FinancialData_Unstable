# 6 Level6-InstanceManager.ps1
# PowerShell script to build and launch Level 6 Instance Manager

param(
    [Parameter(Position=0)]
    [ValidateSet("Help", "Launch", "Build", "Status", "Install", "Remove")]
    [string]$Action = "Help",
    
    [switch]$CreateShortcuts,
    [switch]$Quiet
)

function Write-Header {
    if (-not $Quiet) {
        Write-Host ""
        Write-Host "==========================================================" -ForegroundColor Blue
        Write-Host "            Level 6 - Instance Manager" -ForegroundColor Blue  
        Write-Host "==========================================================" -ForegroundColor Blue
        Write-Host "Advanced GUI for managing instances and groups" -ForegroundColor White
        Write-Host ""
    }
}

function Get-TradingRoot {
    $currentDir = Get-Location
    
    if (Test-Path "instances-config.json") {
        return $currentDir.Path
    }
    
    if ($currentDir.Path -like "*\Setup") {
        return Split-Path -Parent $currentDir.Path
    }
    
    if (Test-Path "PlatformInstances") {
        return $currentDir.Path
    }
    
    return $null
}

function Test-DotNet {
    try {
        $version = & dotnet --version 2>$null
        if ($version) {
            Write-Host "[OK] .NET SDK found: $version" -ForegroundColor Green
            return $true
        }
    }
    catch {
        Write-Host "[ERROR] .NET SDK not found" -ForegroundColor Red
        return $false
    }
}

function Install-Level6 {
    Write-Host "Installing Level 6 Instance Manager..." -ForegroundColor Cyan
    
    $tradingRoot = Get-TradingRoot
    if (-not $tradingRoot) {
        Write-Host "[ERROR] Cannot find trading root directory" -ForegroundColor Red
        return $false
    }
    
    Write-Host "[OK] Trading root: $tradingRoot" -ForegroundColor Green
    
    if (-not (Test-DotNet)) {
        Write-Host "[ERROR] .NET SDK required. Please install .NET 9.0 SDK" -ForegroundColor Red
        Write-Host "Download from: https://dotnet.microsoft.com/download" -ForegroundColor Yellow
        return $false
    }
    
    # Create Level 6 project folder
    $level6Path = Join-Path $tradingRoot "Level6InstanceManager"
    if (-not (Test-Path $level6Path)) {
        New-Item -ItemType Directory -Path $level6Path -Force | Out-Null
        Write-Host "[OK] Created project folder: $level6Path" -ForegroundColor Green
    }
    
    # Create project file
    $projectFile = Join-Path $level6Path "Level6InstanceManager.csproj"
    $projectContent = @'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net9.0-windows</TargetFramework>
    <UseWindowsForms>true</UseWindowsForms>
    <Nullable>enable</Nullable>
    <AssemblyTitle>Level 6 - Advanced Instance Manager</AssemblyTitle>
    <AssemblyVersion>6.0.0.0</AssemblyVersion>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="System.Text.Json" Version="8.0.4" />
  </ItemGroup>
</Project>
'@
    
    Set-Content -Path $projectFile -Value $projectContent -Encoding UTF8
    Write-Host "[OK] Created project file" -ForegroundColor Green
    
    # Create placeholder Program.cs if it doesn't exist
    $sourceFile = Join-Path $level6Path "Program.cs"
    if (-not (Test-Path $sourceFile)) {
        $placeholderContent = @'
using System;
using System.Windows.Forms;

namespace Level6InstanceManager
{
    public class Program
    {
        [STAThread]
        public static void Main()
        {
            MessageBox.Show("Level 6 placeholder - replace Program.cs with full source code", 
                "Level 6 Instance Manager", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
'@
        Set-Content -Path $sourceFile -Value $placeholderContent -Encoding UTF8
        Write-Host "[WARNING] Created placeholder Program.cs - REPLACE WITH FULL SOURCE CODE" -ForegroundColor Yellow
    }
    
    Write-Host ""
    Write-Host "NEXT STEPS:" -ForegroundColor Cyan
    Write-Host "1. Replace Program.cs with the complete C# source code" -ForegroundColor White
    Write-Host "2. Run: .\Setup\'6 Level6-InstanceManager.ps1' -Action Build" -ForegroundColor White
    Write-Host "3. Run: .\Setup\'6 Level6-InstanceManager.ps1' -Action Launch" -ForegroundColor White
    Write-Host ""
    Write-Host "Program.cs location: $sourceFile" -ForegroundColor Yellow
    
    return $true
}

function Build-Level6 {
    Write-Host "Building Level 6 Instance Manager..." -ForegroundColor Cyan
    
    $tradingRoot = Get-TradingRoot
    if (-not $tradingRoot) {
        Write-Host "[ERROR] Cannot find trading root directory" -ForegroundColor Red
        return $false
    }
    
    $level6Path = Join-Path $tradingRoot "Level6InstanceManager"
    $projectFile = Join-Path $level6Path "Level6InstanceManager.csproj"
    
    if (-not (Test-Path $projectFile)) {
        Write-Host "[ERROR] Project not found. Run -Action Install first" -ForegroundColor Red
        return $false
    }
    
    try {
        Push-Location $level6Path
        
        Write-Host "Building project..." -ForegroundColor Gray
        & dotnet build --configuration Release --verbosity quiet
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "[OK] Build successful!" -ForegroundColor Green
            
            $exePath = "bin\Release\net9.0-windows\Level6InstanceManager.exe"
            if (Test-Path $exePath) {
                Write-Host "[OK] Executable created: $exePath" -ForegroundColor Green
                return $true
            } else {
                Write-Host "[ERROR] Executable not found after build" -ForegroundColor Red
                return $false
            }
        } else {
            Write-Host "[ERROR] Build failed" -ForegroundColor Red
            return $false
        }
    }
    finally {
        Pop-Location
    }
}

function Launch-Level6 {
    Write-Host "Launching Level 6 Instance Manager..." -ForegroundColor Cyan
    
    $tradingRoot = Get-TradingRoot
    if (-not $tradingRoot) {
        Write-Host "[ERROR] Cannot find trading root directory" -ForegroundColor Red
        return $false
    }
    
    $level6Path = Join-Path $tradingRoot "Level6InstanceManager"
    $exePath = Join-Path $level6Path "bin\Release\net9.0-windows\Level6InstanceManager.exe"
    
    if (-not (Test-Path $exePath)) {
        Write-Host "[ERROR] Executable not found. Run -Action Build first" -ForegroundColor Red
        return $false
    }
    
    try {
        Start-Process -FilePath $exePath -WorkingDirectory $tradingRoot
        Write-Host "[OK] Level 6 Instance Manager started!" -ForegroundColor Green
        return $true
    }
    catch {
        Write-Host "[ERROR] Failed to start: $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

function Show-Status {
    Write-Host "=== Level 6 Instance Manager Status ===" -ForegroundColor Green
    
    $tradingRoot = Get-TradingRoot
    if ($tradingRoot) {
        Write-Host "Trading Root: $tradingRoot" -ForegroundColor Cyan
        
        $level6Path = Join-Path $tradingRoot "Level6InstanceManager"
        if (Test-Path $level6Path) {
            Write-Host "[OK] Project folder exists" -ForegroundColor Green
            
            $projectFile = Join-Path $level6Path "Level6InstanceManager.csproj"
            $sourceFile = Join-Path $level6Path "Program.cs"
            $exeFile = Join-Path $level6Path "bin\Release\net9.0-windows\Level6InstanceManager.exe"
            
            if (Test-Path $projectFile) {
                Write-Host "[OK] Project file found" -ForegroundColor Green
            } else {
                Write-Host "[ERROR] Project file missing" -ForegroundColor Red
            }
            
            if (Test-Path $sourceFile) {
                Write-Host "[OK] Source code found" -ForegroundColor Green
            } else {
                Write-Host "[ERROR] Source code missing" -ForegroundColor Red
            }
            
            if (Test-Path $exeFile) {
                Write-Host "[OK] Executable built" -ForegroundColor Green
            } else {
                Write-Host "[WARNING] Executable not built" -ForegroundColor Yellow
            }
            
            if (Test-Path $sourceFile) {
                $content = Get-Content $sourceFile -Raw
                if ($content -like "*placeholder*") {
                    Write-Host "[WARNING] Source code is placeholder - needs replacement" -ForegroundColor Yellow
                } else {
                    Write-Host "[OK] Source code looks complete" -ForegroundColor Green
                }
            }
        } else {
            Write-Host "[ERROR] Project not installed" -ForegroundColor Red
        }
        
        $configFile = Join-Path $tradingRoot "instances-config.json"
        if (Test-Path $configFile) {
            Write-Host "[OK] Configuration file found" -ForegroundColor Green
        } else {
            Write-Host "[ERROR] Configuration file missing" -ForegroundColor Red
        }
    } else {
        Write-Host "[ERROR] Cannot find trading root directory" -ForegroundColor Red
    }
    
    Test-DotNet | Out-Null
}

function Remove-Level6 {
    $tradingRoot = Get-TradingRoot
    if (-not $tradingRoot) {
        Write-Host "[ERROR] Cannot find trading root directory" -ForegroundColor Red
        return
    }
    
    $level6Path = Join-Path $tradingRoot "Level6InstanceManager"
    
    if (Test-Path $level6Path) {
        Write-Host "Removing Level 6 Instance Manager..." -ForegroundColor Yellow
        
        try {
            Remove-Item -Path $level6Path -Recurse -Force
            Write-Host "[OK] Level 6 removed" -ForegroundColor Green
        }
        catch {
            Write-Host "[ERROR] Error removing Level 6: $($_.Exception.Message)" -ForegroundColor Red
        }
    } else {
        Write-Host "[OK] Level 6 not installed" -ForegroundColor Green
    }
}

function Show-Help {
    Write-Header
    
    Write-Host "WHAT IS LEVEL 6?" -ForegroundColor Green
    Write-Host "  Level 6 is a Windows GUI application (C# WinForms) that provides" -ForegroundColor White
    Write-Host "  advanced management for your trading instances and groups." -ForegroundColor White
    Write-Host ""
    Write-Host "  Features:" -ForegroundColor Yellow
    Write-Host "  * Add/Edit/Delete instances through easy GUI forms" -ForegroundColor Gray
    Write-Host "  * Create custom groups and organize instances visually" -ForegroundColor Gray
    Write-Host "  * Automatically calls Level 2 scripts when you apply changes" -ForegroundColor Gray
    Write-Host "  * Works alongside your existing Level 5 dashboard" -ForegroundColor Gray
    Write-Host ""
    
    Write-Host "USAGE:" -ForegroundColor Cyan
    Write-Host "  .\Setup\'6 Level6-InstanceManager.ps1' [-Action] <Action>" -ForegroundColor White
    Write-Host ""
    
    Write-Host "ACTIONS:" -ForegroundColor Cyan
    Write-Host "  Help      Show this help information" -ForegroundColor White
    Write-Host "  Install   Create Level 6 project structure" -ForegroundColor White
    Write-Host "  Build     Build the C# application" -ForegroundColor White
    Write-Host "  Launch    Start the Level 6 GUI application" -ForegroundColor White
    Write-Host "  Status    Show installation and build status" -ForegroundColor White
    Write-Host "  Remove    Remove Level 6 installation" -ForegroundColor White
    Write-Host ""
    
    Write-Host "SIMPLE SETUP PROCESS:" -ForegroundColor Yellow
    Write-Host "  1. .\Setup\'6 Level6-InstanceManager.ps1' -Action Install" -ForegroundColor Gray
    Write-Host "  2. Replace Program.cs with complete C# source code" -ForegroundColor Gray
    Write-Host "  3. .\Setup\'6 Level6-InstanceManager.ps1' -Action Build" -ForegroundColor Gray
    Write-Host "  4. .\Setup\'6 Level6-InstanceManager.ps1' -Action Launch" -ForegroundColor Gray
    Write-Host ""
    
    Write-Host "REQUIREMENTS:" -ForegroundColor Cyan
    Write-Host "  * .NET 9.0 SDK (download from dotnet.microsoft.com)" -ForegroundColor White
    Write-Host "  * Existing trading environment (Level 1-5 setup)" -ForegroundColor White
    Write-Host "  * instances-config.json file" -ForegroundColor White
    Write-Host ""
}

# Main execution
Write-Header

switch ($Action.ToLower()) {
    "help" { 
        Show-Help 
    }
    "install" { 
        Install-Level6
    }
    "build" { 
        Build-Level6
    }
    "launch" { 
        Launch-Level6
    }
    "status" { 
        Show-Status
    }
    "remove" { 
        Remove-Level6
    }
    default { 
        Write-Host "[ERROR] Unknown action: $Action" -ForegroundColor Red
        Write-Host "Use -Action Help for usage information" -ForegroundColor Yellow
    }
}