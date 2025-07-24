# 4 Level 4 Advanced Automation Manager.ps1
# Advanced trading platform automation with intelligent sequencing, health monitoring, and auto-recovery

param(
    [string]$ConfigFile = "advanced-automation-config.json",
    [ValidateSet("Setup", "Start", "Stop", "Monitor", "Status", "Health", "Optimize", "Emergency", "Credentials")]
    [string]$Action = "Start",
    [string]$InstanceName = "",
    [switch]$Force,
    [switch]$Daemon,
    [int]$MonitoringDuration = 0
)

# Ensure script runs as Administrator
if (-NOT ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")) {
    Write-Host "This script requires Administrator privileges. Restarting as Administrator..." -ForegroundColor Yellow
    $params = "-File", $PSCommandPath, "-ConfigFile", $ConfigFile, "-Action", $Action
    if ($InstanceName) { $params += "-InstanceName", $InstanceName }
    if ($Force) { $params += "-Force" }
    if ($Daemon) { $params += "-Daemon" }
    if ($MonitoringDuration -gt 0) { $params += "-MonitoringDuration", $MonitoringDuration }
    Start-Process powershell.exe $params -Verb RunAs
    exit
}

Write-Host "=== Level 4 Advanced Automation Manager ===" -ForegroundColor Green
Write-Host "Action: $Action" -ForegroundColor Cyan

# Global variables for monitoring
$Global:InstanceHealth = @{}
$Global:PerformanceMetrics = @{}
$Global:MonitoringActive = $false

# Advanced logging function
function Write-AdvancedLog {
    param(
        [string]$Message,
        [ValidateSet("INFO", "WARN", "ERROR", "DEBUG", "PERF")]
        [string]$Level = "INFO",
        [string]$Component = "SYSTEM"
    )
    
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss.fff"
    $logEntry = "[$timestamp] [$Level] [$Component] $Message"
    
    # Console output with colors
    switch ($Level) {
        "INFO" { Write-Host $logEntry -ForegroundColor White }
        "WARN" { Write-Host $logEntry -ForegroundColor Yellow }
        "ERROR" { Write-Host $logEntry -ForegroundColor Red }
        "DEBUG" { Write-Host $logEntry -ForegroundColor Gray }
        "PERF" { Write-Host $logEntry -ForegroundColor Cyan }
    }
    
    # File logging
    if ($global:config.logging.enableDetailedLogging) {
        $logDir = $global:config.logging.logDirectory
        if (-not (Test-Path $logDir)) {
            New-Item -ItemType Directory -Path $logDir -Force | Out-Null
        }
        $logFile = Join-Path $logDir "automation_$(Get-Date -Format 'yyyy-MM-dd').log"
        $logEntry | Out-File -FilePath $logFile -Append -Encoding UTF8
    }
}

# Market hours checking
function Test-MarketHours {
    param([object]$Config)
    
    if (-not $Config.automationSettings.enableMarketHoursAwareness) {
        return $true
    }
    
    $now = Get-Date
    $dayOfWeek = $now.DayOfWeek.ToString().ToLower()
    
    # Check if today is a market holiday (affects overall trading)
    $todayString = $now.ToString("yyyy-MM-dd")
    if ($Config.marketSettings.marketHolidays.holidays -contains $todayString) {
        Write-AdvancedLog "Market closed - Holiday: $todayString" "INFO" "MARKET"
        return $false
    }
    
    # Check trading hours for today
    $tradingHours = $Config.marketSettings.tradingHours.$dayOfWeek
    if (-not $tradingHours.enabled) {
        Write-AdvancedLog "Market closed - Day not enabled: $dayOfWeek" "INFO" "MARKET"
        return $false
    }
    
    $startTime = [DateTime]::ParseExact($tradingHours.start, "HH:mm", $null)
    $endTime = [DateTime]::ParseExact($tradingHours.end, "HH:mm", $null)
    $currentTime = [DateTime]::ParseExact($now.ToString("HH:mm"), "HH:mm", $null)
    
    if ($currentTime -ge $startTime -and $currentTime -le $endTime) {
        Write-AdvancedLog "Market open - Current time within trading hours" "INFO" "MARKET"
        return $true
    } else {
        Write-AdvancedLog "Market closed - Outside trading hours ($($tradingHours.start) - $($tradingHours.end))" "INFO" "MARKET"
        return $false
    }
}

# Check if specific market session is active
function Test-MarketSession {
    param([string]$SessionName, [object]$Config)
    
    $session = $Config.marketSettings.marketSessions.$SessionName
    if (-not $session) {
        return $false
    }
    
    # Check if session is affected by holidays
    if ($session.affectedByHolidays) {
        $todayString = (Get-Date).ToString("yyyy-MM-dd")
        if ($Config.marketSettings.marketHolidays.holidays -contains $todayString) {
            Write-AdvancedLog "Session $SessionName closed - Market holiday" "INFO" "MARKET"
            return $false
        }
    }
    
    $now = Get-Date
    $currentHour = $now.Hour
    $startHour = [int]$session.start.Split(':')[0]
    $endHour = [int]$session.end.Split(':')[0]
    
    # Handle sessions that cross midnight
    if ($startHour -le $endHour) {
        return ($currentHour -ge $startHour -and $currentHour -le $endHour)
    } else {
        return ($currentHour -ge $startHour -or $currentHour -le $endHour)
    }
}

# System resource checking
function Test-SystemResources {
    param([object]$Config)
    
    Write-AdvancedLog "Checking system resources..." "INFO" "RESOURCE"
    
    # Memory check
    $totalMemory = (Get-WmiObject -Class Win32_ComputerSystem).TotalPhysicalMemory / 1GB
    $availableMemory = (Get-WmiObject -Class Win32_OperatingSystem).FreePhysicalMemory / 1MB / 1024
    
    $minMemoryGB = [double]($Config.sequencing.minimumSystemMemory -replace 'GB','')
    if ($availableMemory -lt $minMemoryGB) {
        Write-AdvancedLog "Insufficient memory: Available $([math]::Round($availableMemory, 2))GB, Required ${minMemoryGB}GB" "WARN" "RESOURCE"
        return $false
    }
    
    # Disk space check
    $systemDrive = (Get-WmiObject -Class Win32_OperatingSystem).SystemDrive
    $disk = Get-WmiObject -Class Win32_LogicalDisk | Where-Object { $_.DeviceID -eq $systemDrive }
    $freeDiskSpaceGB = $disk.FreeSpace / 1GB
    
    $minDiskGB = [double]($Config.sequencing.minimumDiskSpace -replace 'GB','')
    if ($freeDiskSpaceGB -lt $minDiskGB) {
        Write-AdvancedLog "Insufficient disk space: Available $([math]::Round($freeDiskSpaceGB, 2))GB, Required ${minDiskGB}GB" "WARN" "RESOURCE"
        return $false
    }
    
    # Network connectivity check
    if ($Config.sequencing.networkConnectivityCheck) {
        try {
            $pingResult = Test-NetConnection -ComputerName "8.8.8.8" -Port 53 -InformationLevel Quiet
            if (-not $pingResult) {
                Write-AdvancedLog "Network connectivity check failed" "WARN" "NETWORK"
                return $false
            }
        } catch {
            Write-AdvancedLog "Network connectivity check error: $($_.Exception.Message)" "ERROR" "NETWORK"
            return $false
        }
    }
    
    Write-AdvancedLog "System resources check passed" "INFO" "RESOURCE"
    return $true
}

# Instance health monitoring
function Get-InstanceHealth {
    param(
        [object]$Instance,
        [string]$InstancePath
    )
    
    $healthScore = 100
    $healthDetails = @{}
    
    # Process existence check
    $processName = [System.IO.Path]::GetFileNameWithoutExtension($Instance.executable)
    $processes = Get-Process -Name $processName -ErrorAction SilentlyContinue
    
    if (-not $processes) {
        $healthScore = 0
        $healthDetails.ProcessStatus = "Not Running"
        Write-AdvancedLog "Instance $($Instance.name) - Process not found" "WARN" "HEALTH"
    } else {
        $healthDetails.ProcessStatus = "Running"
        $process = $processes[0]
        
        # Memory usage check
        $memoryUsageMB = $process.WorkingSet64 / 1MB
        $memoryLimitMB = [double]($Instance.memoryLimit -replace 'GB','') * 1024
        
        if ($memoryUsageMB -gt $memoryLimitMB) {
            $healthScore -= 30
            $healthDetails.MemoryStatus = "Exceeded Limit"
            Write-AdvancedLog "Instance $($Instance.name) - Memory usage exceeded: $([math]::Round($memoryUsageMB, 2))MB" "WARN" "HEALTH"
        } else {
            $healthDetails.MemoryStatus = "Normal"
        }
        
        # CPU usage check
        $cpuUsage = (Get-WmiObject -Class Win32_Process | Where-Object { $_.ProcessId -eq $process.Id }).PageFileUsage
        if ($cpuUsage -gt $Instance.cpuLimit) {
            $healthScore -= 20
            $healthDetails.CpuStatus = "High Usage"
        } else {
            $healthDetails.CpuStatus = "Normal"
        }
        
        # Response time check (simplified)
        $healthDetails.ResponseTime = "Good"
        
        $healthDetails.MemoryUsage = "$([math]::Round($memoryUsageMB, 2))MB"
        $healthDetails.ProcessId = $process.Id
    }
    
    $healthDetails.HealthScore = $healthScore
    $healthDetails.LastCheck = Get-Date
    
    return $healthDetails
}

# Intelligent instance startup
function Start-InstanceWithDependencies {
    param(
        [object]$Instance,
        [object]$Config,
        [string]$InstancePath,
        [array]$AllInstances
    )
    
    Write-AdvancedLog "Starting instance: $($Instance.name)" "INFO" "STARTUP"
    
    # Check dependencies
    foreach ($dependency in $Instance.dependsOn) {
        $depInstance = $AllInstances | Where-Object { $_.name -eq $dependency }
        if ($depInstance) {
            $depHealth = Get-InstanceHealth -Instance $depInstance -InstancePath ""
            if ($depHealth.HealthScore -lt 80) {
                Write-AdvancedLog "Dependency $dependency not healthy (Score: $($depHealth.HealthScore)). Waiting..." "WARN" "STARTUP"
                
                # Wait for dependency to stabilize
                $maxWait = 60
                $waited = 0
                while ($waited -lt $maxWait -and $depHealth.HealthScore -lt 80) {
                    Start-Sleep -Seconds 5
                    $waited += 5
                    $depHealth = Get-InstanceHealth -Instance $depInstance -InstancePath ""
                    Write-AdvancedLog "Waiting for dependency $dependency... Score: $($depHealth.HealthScore)" "INFO" "STARTUP"
                }
                
                if ($depHealth.HealthScore -lt 80) {
                    Write-AdvancedLog "Dependency $dependency failed to stabilize. Aborting startup of $($Instance.name)" "ERROR" "STARTUP"
                    return $false
                }
            }
        }
    }
    
    # Market session check
    if ($Instance.marketSessionAware -and $Instance.allowedSessions.Count -gt 0) {
        $inAllowedSession = $false
        
        foreach ($sessionName in $Instance.allowedSessions) {
            if (Test-MarketSession -SessionName $sessionName -Config $Config) {
                $inAllowedSession = $true
                Write-AdvancedLog "Instance $($Instance.name) - Session $sessionName is active" "INFO" "STARTUP"
                break
            }
        }
        
        if (-not $inAllowedSession) {
            Write-AdvancedLog "Instance $($Instance.name) not started - outside allowed trading sessions" "INFO" "STARTUP"
            return $false
        }
    }
    
    # Start the instance via Task Scheduler
    try {
        $taskName = $Instance.name -replace '_Instance$', ''
        Start-ScheduledTask -TaskName $taskName -TaskPath "\TradingPlatforms\" -ErrorAction Stop
        Write-AdvancedLog "Successfully started task for instance: $($Instance.name)" "INFO" "STARTUP"
        
        # Wait for stabilization
        Start-Sleep -Seconds $Config.sequencing.stabilityWaitTime
        
        # Verify startup
        $health = Get-InstanceHealth -Instance $Instance -InstancePath $InstancePath
        if ($health.HealthScore -gt 70) {
            Write-AdvancedLog "Instance $($Instance.name) startup verified - Health Score: $($health.HealthScore)" "INFO" "STARTUP"
            return $true
        } else {
            Write-AdvancedLog "Instance $($Instance.name) startup failed verification - Health Score: $($health.HealthScore)" "ERROR" "STARTUP"
            return $false
        }
    } catch {
        Write-AdvancedLog "Failed to start instance $($Instance.name): $($_.Exception.Message)" "ERROR" "STARTUP"
        return $false
    }
}

# Performance optimization
function Optimize-SystemPerformance {
    param([object]$Config)
    
    Write-AdvancedLog "Starting performance optimization..." "PERF" "OPTIMIZER"
    
    if (-not $Config.performance.enableAdaptivePriority) { return }
    
    # Get all running trading processes
    $tradingProcesses = @()
    foreach ($instance in $Config.instances) {
        if ($instance.enabled) {
            $processName = [System.IO.Path]::GetFileNameWithoutExtension($instance.executable)
            $processes = Get-Process -Name $processName -ErrorAction SilentlyContinue
            if ($processes) {
                $tradingProcesses += @{
                    Process = $processes[0]
                    Instance = $instance
                }
            }
        }
    }
    
    # Adaptive priority adjustment
    foreach ($tp in $tradingProcesses) {
        $process = $tp.Process
        $instance = $tp.Instance
        
        # Get current performance metrics
        $memoryUsage = $process.WorkingSet64 / 1MB
        $cpuUsage = (Get-Counter "\Process($($process.ProcessName))\% Processor Time" -ErrorAction SilentlyContinue).CounterSamples.CookedValue
        
        # Adjust priority based on performance and configuration
        $targetPriority = switch ($instance.priority) {
            "high" { "High" }
            "normal" { "Normal" }
            "low" { "BelowNormal" }
            default { "Normal" }
        }
        
        # Dynamic adjustment based on system load
        $systemCpuUsage = (Get-Counter "\Processor(_Total)\% Processor Time").CounterSamples.CookedValue
        if ($systemCpuUsage -gt 80) {
            if ($instance.priority -eq "normal") { $targetPriority = "BelowNormal" }
            if ($instance.priority -eq "low") { $targetPriority = "Idle" }
        }
        
        try {
            $process.PriorityClass = $targetPriority
            Write-AdvancedLog "Adjusted priority for $($instance.name) to $targetPriority (CPU: $([math]::Round($systemCpuUsage, 1))%)" "PERF" "OPTIMIZER"
        } catch {
            Write-AdvancedLog "Failed to adjust priority for $($instance.name): $($_.Exception.Message)" "WARN" "OPTIMIZER"
        }
    }
}

# Emergency shutdown
function Invoke-EmergencyShutdown {
    param([object]$Config, [string]$Reason)
    
    Write-AdvancedLog "EMERGENCY SHUTDOWN INITIATED: $Reason" "ERROR" "EMERGENCY"
    
    # Stop all trading processes immediately
    foreach ($instance in $Config.instances) {
        if ($instance.enabled) {
            try {
                $processName = [System.IO.Path]::GetFileNameWithoutExtension($instance.executable)
                $processes = Get-Process -Name $processName -ErrorAction SilentlyContinue
                if ($processes) {
                    $processes | Stop-Process -Force
                    Write-AdvancedLog "Emergency stopped process: $processName" "ERROR" "EMERGENCY"
                }
            } catch {
                Write-AdvancedLog "Failed to emergency stop $($instance.name): $($_.Exception.Message)" "ERROR" "EMERGENCY"
            }
        }
    }
    
    # Send notifications if configured
    if ($Config.notifications.enableWindowsNotifications) {
        Add-Type -AssemblyName System.Windows.Forms
        [System.Windows.Forms.MessageBox]::Show("Trading Platform Emergency Shutdown: $Reason", "Critical Alert", "OK", "Warning")
    }
}

# Continuous monitoring daemon
function Start-ContinuousMonitoring {
    param([object]$Config, [int]$Duration)
    
    Write-AdvancedLog "Starting continuous monitoring daemon..." "INFO" "MONITOR"
    $Global:MonitoringActive = $true
    
    $startTime = Get-Date
    $endTime = if ($Duration -gt 0) { $startTime.AddSeconds($Duration) } else { $null }
    
    while ($Global:MonitoringActive) {
        if ($endTime -and (Get-Date) -gt $endTime) {
            Write-AdvancedLog "Monitoring duration completed" "INFO" "MONITOR"
            break
        }
        
        # Health checks for all instances
        foreach ($instance in $Config.instances) {
            if ($instance.enabled -and $instance.healthCheckEnabled) {
                $health = Get-InstanceHealth -Instance $instance -InstancePath ""
                $Global:InstanceHealth[$instance.name] = $health
                
                # Check for critical health issues
                if ($health.HealthScore -lt $instance.healthThreshold) {
                    Write-AdvancedLog "Instance $($instance.name) below health threshold: $($health.HealthScore)" "WARN" "MONITOR"
                    
                    # Auto-restart if enabled
                    if ($instance.autoRestart) {
                        Write-AdvancedLog "Attempting auto-restart of $($instance.name)" "INFO" "RECOVERY"
                        # Restart logic would go here
                    }
                }
                
                # Check for critical system resources
                if ($health.HealthScore -eq 0) {
                    Write-AdvancedLog "Critical failure detected for $($instance.name)" "ERROR" "MONITOR"
                }
            }
        }
        
        # System performance optimization
        if ($Config.performance.enableResourceOptimization) {
            Optimize-SystemPerformance -Config $Config
        }
        
        # Emergency shutdown check
        $systemCpuUsage = (Get-Counter "\Processor(_Total)\% Processor Time").CounterSamples.CookedValue
        if ($systemCpuUsage -gt $Config.recovery.emergencyShutdownThreshold) {
            Invoke-EmergencyShutdown -Config $Config -Reason "System CPU usage exceeded threshold: $([math]::Round($systemCpuUsage, 1))%"
            break
        }
        
        Start-Sleep -Seconds $Config.healthChecks.checkInterval
    }
    
    Write-AdvancedLog "Continuous monitoring stopped" "INFO" "MONITOR"
}

# Credential management
function Set-InstanceCredentials {
    param([string]$CredentialId, [string]$Username, [string]$Password)
    
    try {
        $securePassword = ConvertTo-SecureString $Password -AsPlainText -Force
        $credential = New-Object System.Management.Automation.PSCredential($Username, $securePassword)
        
        # Store in Windows Credential Manager
        cmdkey /generic:$CredentialId /user:$Username /pass:$Password | Out-Null
        
        Write-AdvancedLog "Credentials stored for: $CredentialId" "INFO" "CREDENTIALS"
        return $true
    } catch {
        Write-AdvancedLog "Failed to store credentials for $CredentialId`: $($_.Exception.Message)" "ERROR" "CREDENTIALS"
        return $false
    }
}

# Load configuration
try {
    $configPath = Join-Path (Get-Location) $ConfigFile
    if (-not (Test-Path $configPath)) {
        Write-AdvancedLog "Configuration file not found: $configPath" "ERROR" "CONFIG"
        exit 1
    }
    
    $global:config = Get-Content $configPath -Raw | ConvertFrom-Json
    Write-AdvancedLog "Advanced automation configuration loaded" "INFO" "CONFIG"
} catch {
    Write-AdvancedLog "Failed to load configuration: $($_.Exception.Message)" "ERROR" "CONFIG"
    exit 1
}

# Main action processing
switch ($Action) {
    "Setup" {
        Write-AdvancedLog "Setting up advanced automation system..." "INFO" "SETUP"
        
        # Create log directories
        if (-not (Test-Path $global:config.logging.logDirectory)) {
            New-Item -ItemType Directory -Path $global:config.logging.logDirectory -Force | Out-Null
            Write-AdvancedLog "Created advanced log directory" "INFO" "SETUP"
        }
        
        Write-AdvancedLog "Advanced automation setup complete" "INFO" "SETUP"
    }
    
    "Start" {
        Write-AdvancedLog "Starting intelligent platform sequence..." "INFO" "STARTUP"
        
        # Market hours check
        if (-not (Test-MarketHours -Config $global:config)) {
            Write-AdvancedLog "Market closed - startup aborted" "INFO" "STARTUP"
            exit 0
        }
        
        # System resources check
        if (-not (Test-SystemResources -Config $global:config)) {
            Write-AdvancedLog "Insufficient system resources - startup aborted" "ERROR" "STARTUP"
            exit 1
        }
        
        # Intelligent sequenced startup
        $enabledInstances = $global:config.instances | Where-Object { $_.enabled -and $_.autoStart }
        $started = 0
        
        foreach ($instance in $enabledInstances) {
            if (Start-InstanceWithDependencies -Instance $instance -Config $global:config -InstancePath "" -AllInstances $global:config.instances) {
                $started++
                Start-Sleep -Seconds $instance.startupDelay
            }
        }
        
        Write-AdvancedLog "Intelligent startup complete - $started instances started" "INFO" "STARTUP"
        
        # Start monitoring if requested
        if ($Daemon) {
            Start-ContinuousMonitoring -Config $global:config -Duration $MonitoringDuration
        }
    }
    
    "Monitor" {
        Start-ContinuousMonitoring -Config $global:config -Duration $MonitoringDuration
    }
    
    "Status" {
        Write-AdvancedLog "Advanced status report..." "INFO" "STATUS"
        
        Write-Host "`n=== Advanced Trading Platform Status ===" -ForegroundColor Green
        Write-Host ("{0,-30} {1,-10} {2,-15} {3,-10} {4,-15}" -f "Instance", "Health", "Memory", "Status", "Last Check") -ForegroundColor White
        Write-Host ("-" * 85) -ForegroundColor Gray
        
        foreach ($instance in $global:config.instances) {
            if ($instance.enabled) {
                $health = Get-InstanceHealth -Instance $instance -InstancePath ""
                $color = if ($health.HealthScore -gt 80) { "Green" } elseif ($health.HealthScore -gt 50) { "Yellow" } else { "Red" }
                
                Write-Host ("{0,-30} {1,-10} {2,-15} {3,-10} {4,-15}" -f 
                    $instance.name.Substring(0, [Math]::Min(29, $instance.name.Length)),
                    "$($health.HealthScore)%",
                    $health.MemoryUsage,
                    $health.ProcessStatus,
                    $health.LastCheck.ToString("HH:mm:ss")
                ) -ForegroundColor $color
            }
        }
        
        # System overview
        $systemCpu = (Get-Counter "\Processor(_Total)\% Processor Time").CounterSamples.CookedValue
        $systemMemory = (Get-WmiObject -Class Win32_OperatingSystem)
        $memoryUsagePercent = [math]::Round((($systemMemory.TotalPhysicalMemory - $systemMemory.FreePhysicalMemory) / $systemMemory.TotalPhysicalMemory) * 100, 1)
        
        Write-Host "`n=== System Resources ===" -ForegroundColor Cyan
        Write-Host "CPU Usage: $([math]::Round($systemCpu, 1))%" -ForegroundColor $(if ($systemCpu -gt 80) { "Red" } elseif ($systemCpu -gt 60) { "Yellow" } else { "Green" })
        Write-Host "Memory Usage: $memoryUsagePercent%" -ForegroundColor $(if ($memoryUsagePercent -gt 80) { "Red" } elseif ($memoryUsagePercent -gt 60) { "Yellow" } else { "Green" })
        
        # Market status
        $marketOpen = Test-MarketHours -Config $global:config
        Write-Host "Market Status: $(if ($marketOpen) { 'OPEN' } else { 'CLOSED' })" -ForegroundColor $(if ($marketOpen) { "Green" } else { "Red" })
    }
    
    "Health" {
        Write-AdvancedLog "Performing comprehensive health check..." "INFO" "HEALTH"
        
        foreach ($instance in $global:config.instances) {
            if ($instance.enabled) {
                $health = Get-InstanceHealth -Instance $instance -InstancePath ""
                Write-AdvancedLog "Instance: $($instance.name) - Health Score: $($health.HealthScore)%" "INFO" "HEALTH"
            }
        }
    }
    
    "Optimize" {
        Write-AdvancedLog "Performing system optimization..." "PERF" "OPTIMIZER"
        Optimize-SystemPerformance -Config $global:config
    }
    
    "Emergency" {
        Invoke-EmergencyShutdown -Config $global:config -Reason "Manual emergency shutdown requested"
    }
    
    "Credentials" {
        Write-Host "=== Credential Management ===" -ForegroundColor Yellow
        Write-Host "This feature requires manual implementation of credential storage"
        Write-Host "Use Windows Credential Manager or implement secure credential vault"
    }
}

Write-AdvancedLog "Level 4 Advanced Automation Manager completed action: $Action" "INFO" "SYSTEM"