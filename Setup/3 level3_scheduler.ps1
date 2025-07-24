# 3 Level 3 Task Scheduler Manager.ps1
# This script manages trading platform instances via Windows Task Scheduler

param(
    [string]$ConfigFile = "startup-config.json",
    [ValidateSet("Setup", "Start", "Stop", "Restart", "Status", "Remove", "WhatIf")]
    [string]$Action = "Setup",
    [string]$InstanceName = "",
    [switch]$Force
)

# Ensure script runs as Administrator
if (-NOT ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")) {
    Write-Host "This script requires Administrator privileges. Restarting as Administrator..." -ForegroundColor Yellow
    $params = "-File", $PSCommandPath, "-ConfigFile", $ConfigFile, "-Action", $Action
    if ($InstanceName) { $params += "-InstanceName", $InstanceName }
    if ($Force) { $params += "-Force" }
    Start-Process powershell.exe $params -Verb RunAs
    exit
}

Write-Host "=== Level 3 Task Scheduler Manager ===" -ForegroundColor Green
Write-Host "Action: $Action" -ForegroundColor Cyan

# Function to create scheduled task
function New-TradingTask {
    param(
        [object]$Instance,
        [object]$Settings,
        [string]$InstancePath
    )
    
    $taskName = "\$($Settings.taskFolder)\$($Instance.taskName)"
    $executable = Join-Path $InstancePath $Instance.executable
    
    if (-not (Test-Path $executable)) {
        Write-Host "Executable not found: $executable" -ForegroundColor Red
        return $false
    }
    
    try {
        # Remove existing task if it exists
        $existingTask = Get-ScheduledTask -TaskName $Instance.taskName -TaskPath "\$($Settings.taskFolder)\" -ErrorAction SilentlyContinue
        if ($existingTask) {
            Unregister-ScheduledTask -TaskName $Instance.taskName -TaskPath "\$($Settings.taskFolder)\" -Confirm:$false
            Write-Host "Removed existing task: $($Instance.taskName)" -ForegroundColor Yellow
        }
        
        # Create task folder if it doesn't exist
        $taskFolderPath = "\$($Settings.taskFolder)"
        try {
            Get-ScheduledTask -TaskPath $taskFolderPath -ErrorAction Stop | Out-Null
        }
        catch {
            $taskService = New-Object -ComObject Schedule.Service
            $taskService.Connect()
            $rootFolder = $taskService.GetFolder("\")
            $rootFolder.CreateFolder($Settings.taskFolder)
            Write-Host "Created task folder: $($Settings.taskFolder)" -ForegroundColor Green
        }
        
        # Create task action
        $action = New-ScheduledTaskAction -Execute $executable -Argument $Instance.arguments -WorkingDirectory $InstancePath
        
        # Create triggers
        $triggers = @()
        
        # Boot trigger with delay
        if ($Instance.autoStart -and $Settings.autoStart) {
            $bootDelay = [TimeSpan]::FromSeconds($Settings.globalStartupDelay + $Instance.startupDelay)
            $bootTrigger = New-ScheduledTaskTrigger -AtStartup
            $bootTrigger.Delay = $bootDelay
            $triggers += $bootTrigger
        }
        
        # Create task settings
        $taskSettings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -StartWhenAvailable
        $taskSettings.RestartCount = $Instance.maxRestarts
        $taskSettings.RestartInterval = [TimeSpan]::FromSeconds($Instance.restartDelay)
        $taskSettings.ExecutionTimeLimit = [TimeSpan]::Zero  # No time limit
        
        # Set priority
        switch ($Instance.priority.ToLower()) {
            "high" { $taskSettings.Priority = 4 }
            "normal" { $taskSettings.Priority = 5 }
            "low" { $taskSettings.Priority = 6 }
            default { $taskSettings.Priority = 5 }
        }
        
        # Create task principal
        $principal = New-ScheduledTaskPrincipal -UserId $env:USERNAME -LogonType Interactive -RunLevel Highest
        
        # Register the task
        Register-ScheduledTask -TaskName $Instance.taskName -TaskPath "\$($Settings.taskFolder)\" -Action $action -Trigger $triggers -Settings $taskSettings -Principal $principal -Description "Trading Platform: $($Instance.name)"
        
        Write-Host "Created scheduled task: $($Instance.taskName)" -ForegroundColor Green
        return $true
    }
    catch {
        Write-Host "Failed to create task for $($Instance.name): $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

# Function to create desktop shortcuts
function New-TradingShortcuts {
    param(
        [object]$Instance,
        [string]$InstancePath
    )
    
    $desktop = [Environment]::GetFolderPath("Desktop")
    $startMenu = [Environment]::GetFolderPath("StartMenu")
    
    # Create shortcut objects
    $shell = New-Object -ComObject WScript.Shell
    
    # Desktop shortcut
    $desktopShortcut = $shell.CreateShortcut("$desktop\$($Instance.taskName).lnk")
    $desktopShortcut.TargetPath = "schtasks.exe"
    $desktopShortcut.Arguments = "/run /tn `"\TradingPlatforms\$($Instance.taskName)`""
    $desktopShortcut.WorkingDirectory = $InstancePath
    $desktopShortcut.IconLocation = Join-Path $InstancePath $Instance.executable
    $desktopShortcut.Description = "Start $($Instance.name)"
    $desktopShortcut.Save()
    
    Write-Host "Created desktop shortcut: $($Instance.taskName).lnk" -ForegroundColor Green
}

# Function to get task status
function Get-TaskStatus {
    param([string]$TaskName, [string]$TaskPath)
    
    try {
        $task = Get-ScheduledTask -TaskName $TaskName -TaskPath $TaskPath -ErrorAction Stop
        $taskInfo = Get-ScheduledTaskInfo -TaskName $TaskName -TaskPath $TaskPath
        
        return @{
            Name = $TaskName
            State = $task.State
            LastRunTime = $taskInfo.LastRunTime
            NextRunTime = $taskInfo.NextRunTime
            LastTaskResult = $taskInfo.LastTaskResult
            NumberOfMissedRuns = $taskInfo.NumberOfMissedRuns
        }
    }
    catch {
        return @{
            Name = $TaskName
            State = "NotFound"
            LastRunTime = $null
            NextRunTime = $null
            LastTaskResult = $null
            NumberOfMissedRuns = $null
        }
    }
}

# Function to start/stop tasks
function Set-TaskState {
    param(
        [string]$TaskName,
        [string]$TaskPath,
        [ValidateSet("Start", "Stop")]
        [string]$State
    )
    
    try {
        switch ($State) {
            "Start" {
                Start-ScheduledTask -TaskName $TaskName -TaskPath $TaskPath
                Write-Host "Started task: $TaskName" -ForegroundColor Green
            }
            "Stop" {
                Stop-ScheduledTask -TaskName $TaskName -TaskPath $TaskPath
                Write-Host "Stopped task: $TaskName" -ForegroundColor Yellow
            }
        }
        return $true
    }
    catch {
        Write-Host "Failed to $State task $TaskName`: $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

# Read configuration files
$configPath = Join-Path (Get-Location) $ConfigFile
if (-not (Test-Path $configPath)) {
    Write-Host "Startup configuration file not found: $configPath" -ForegroundColor Red
    exit 1
}

try {
    $startupConfig = Get-Content $configPath -Raw | ConvertFrom-Json
    Write-Host "Startup configuration loaded from: $configPath" -ForegroundColor Green
}
catch {
    Write-Host "Failed to parse startup configuration: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Also read instances config for paths
$instancesConfigPath = Join-Path (Get-Location) "instances-config.json"
if (Test-Path $instancesConfigPath) {
    try {
        $instancesConfig = Get-Content $instancesConfigPath -Raw | ConvertFrom-Json
        Write-Host "Instance configuration loaded from: $instancesConfigPath" -ForegroundColor Green
    }
    catch {
        Write-Host "Failed to parse instance configuration: $($_.Exception.Message)" -ForegroundColor Red
        exit 1
    }
}

# Create logs directory if specified
if ($startupConfig.startupSettings.logDirectory) {
    if (-not (Test-Path $startupConfig.startupSettings.logDirectory)) {
        New-Item -ItemType Directory -Path $startupConfig.startupSettings.logDirectory -Force | Out-Null
        Write-Host "Created log directory: $($startupConfig.startupSettings.logDirectory)" -ForegroundColor Green
    }
}

# Main action processing
switch ($Action) {
    "Setup" {
        Write-Host "`n--- Setting Up Scheduled Tasks ---" -ForegroundColor Yellow
        $successCount = 0
        $skipCount = 0
        
        foreach ($instance in $startupConfig.instances) {
            if (-not $instance.enabled) {
                Write-Host "Skipping disabled instance: $($instance.name)" -ForegroundColor Gray
                $skipCount++
                continue
            }
            
            Write-Host "`nProcessing: $($instance.name)" -ForegroundColor Cyan
            
            # Find instance path
            $instancePath = Join-Path $instancesConfig.tradingRoot "PlatformInstances\$($instance.name)"
            
            if (-not (Test-Path $instancePath)) {
                Write-Host "Instance path not found: $instancePath" -ForegroundColor Red
                continue
            }
            
            # Create scheduled task
            if (New-TradingTask -Instance $instance -Settings $startupConfig.startupSettings -InstancePath $instancePath) {
                $successCount++
                
                # Create shortcuts if enabled
                if ($startupConfig.startupSettings.createShortcuts) {
                    New-TradingShortcuts -Instance $instance -InstancePath $instancePath
                }
            }
        }
        
        Write-Host "`n=== Setup Complete ===" -ForegroundColor Green
        Write-Host "Tasks created: $successCount" -ForegroundColor Green
        Write-Host "Tasks skipped: $skipCount" -ForegroundColor Yellow
    }
    
    "Start" {
        Write-Host "`n--- Starting Trading Platforms ---" -ForegroundColor Yellow
        
        $instancesToProcess = if ($InstanceName) { 
            $startupConfig.instances | Where-Object { $_.name -eq $InstanceName -or $_.taskName -eq $InstanceName }
        } else { 
            $startupConfig.instances | Where-Object { $_.enabled }
        }
        
        foreach ($instance in $instancesToProcess) {
            Set-TaskState -TaskName $instance.taskName -TaskPath "\$($startupConfig.startupSettings.taskFolder)\" -State "Start"
            Start-Sleep -Seconds 2
        }
    }
    
    "Stop" {
        Write-Host "`n--- Stopping Trading Platforms ---" -ForegroundColor Yellow
        
        $instancesToProcess = if ($InstanceName) { 
            $startupConfig.instances | Where-Object { $_.name -eq $InstanceName -or $_.taskName -eq $InstanceName }
        } else { 
            $startupConfig.instances | Where-Object { $_.enabled }
        }
        
        foreach ($instance in $instancesToProcess) {
            Set-TaskState -TaskName $instance.taskName -TaskPath "\$($startupConfig.startupSettings.taskFolder)\" -State "Stop"
        }
    }
    
    "Restart" {
        Write-Host "`n--- Restarting Trading Platforms ---" -ForegroundColor Yellow
        
        $instancesToProcess = if ($InstanceName) { 
            $startupConfig.instances | Where-Object { $_.name -eq $InstanceName -or $_.taskName -eq $InstanceName }
        } else { 
            $startupConfig.instances | Where-Object { $_.enabled }
        }
        
        foreach ($instance in $instancesToProcess) {
            Write-Host "Restarting: $($instance.taskName)" -ForegroundColor Cyan
            Set-TaskState -TaskName $instance.taskName -TaskPath "\$($startupConfig.startupSettings.taskFolder)\" -State "Stop"
            Start-Sleep -Seconds 3
            Set-TaskState -TaskName $instance.taskName -TaskPath "\$($startupConfig.startupSettings.taskFolder)\" -State "Start"
            Start-Sleep -Seconds 2
        }
    }
    
    "Status" {
        Write-Host "`n--- Trading Platform Status ---" -ForegroundColor Yellow
        Write-Host ("{0,-25} {1,-10} {2,-20} {3,-10}" -f "Task Name", "State", "Last Run", "Result") -ForegroundColor White
        Write-Host ("-" * 75) -ForegroundColor Gray
        
        foreach ($instance in $startupConfig.instances) {
            if (-not $instance.enabled) { continue }
            
            $status = Get-TaskStatus -TaskName $instance.taskName -TaskPath "\$($startupConfig.startupSettings.taskFolder)\"
            $lastRun = if ($status.LastRunTime) { $status.LastRunTime.ToString("MM/dd HH:mm") } else { "Never" }
            $result = if ($status.LastTaskResult -eq 0) { "Success" } elseif ($status.LastTaskResult) { "Error" } else { "N/A" }
            
            $color = switch ($status.State) {
                "Running" { "Green" }
                "Ready" { "Yellow" }
                "Disabled" { "Gray" }
                default { "Red" }
            }
            
            Write-Host ("{0,-25} {1,-10} {2,-20} {3,-10}" -f $status.Name, $status.State, $lastRun, $result) -ForegroundColor $color
        }
    }
    
    "Remove" {
        Write-Host "`n--- Removing Scheduled Tasks ---" -ForegroundColor Yellow
        
        foreach ($instance in $startupConfig.instances) {
            try {
                Unregister-ScheduledTask -TaskName $instance.taskName -TaskPath "\$($startupConfig.startupSettings.taskFolder)\" -Confirm:$false -ErrorAction SilentlyContinue
                Write-Host "Removed task: $($instance.taskName)" -ForegroundColor Green
            }
            catch {
                Write-Host "Task not found or already removed: $($instance.taskName)" -ForegroundColor Gray
            }
        }
    }
    
    "WhatIf" {
        Write-Host "`n--- What-If Analysis ---" -ForegroundColor Magenta
        
        foreach ($instance in $startupConfig.instances) {
            if (-not $instance.enabled) {
                Write-Host "[SKIP] Disabled: $($instance.name)" -ForegroundColor Gray
                continue
            }
            
            Write-Host "[CREATE] Task: $($instance.taskName)" -ForegroundColor Cyan
            Write-Host "  - Executable: $($instance.executable)" -ForegroundColor White
            Write-Host "  - Auto-start: $($instance.autoStart)" -ForegroundColor White
            Write-Host "  - Startup delay: $($instance.startupDelay)s" -ForegroundColor White
            Write-Host "  - Priority: $($instance.priority)" -ForegroundColor White
        }
    }
}

Write-Host "`nLevel 3 Task Scheduler Management complete!" -ForegroundColor Green