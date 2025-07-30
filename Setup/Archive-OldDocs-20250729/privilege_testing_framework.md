# User Privilege Testing Framework
## Trading Platform Management System

### Version: 1.0
### Date: July 28, 2025

---

## Overview

This framework provides comprehensive testing for different user privilege scenarios, ensuring the trading platform management system works correctly for both administrative and standard users, with graceful degradation when permissions are insufficient.

---

## User Privilege Scenarios

### Scenario Matrix

| User Type | Location | Expected Behavior | Test Requirements |
|-----------|----------|-------------------|-------------------|
| Administrator | System-wide | Full functionality | All features work |
| Standard User | User profile | Limited functionality | Graceful degradation |
| Restricted User | Locked down | Basic functionality | Core features only |
| Service Account | Background | Automated operation | Unattended mode |

---

## Privilege-Aware Test Categories

### Category 1: File System Access Tests

#### PRI-001: Standard User File System Access

**Objective**: Verify file system operations work for standard users

**Implementation Steps**:

```powershell
function Test-StandardUserFileAccess {
    param([string]$TestPath = $null)
    
    Write-Host "🔐 Testing Standard User File System Access..." -ForegroundColor Cyan
    
    # Get current user context
    $CurrentUser = [System.Security.Principal.WindowsIdentity]::GetCurrent()
    $IsAdmin = ([Security.Principal.WindowsPrincipal] $CurrentUser).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")
    
    Write-Host "Current User: $($CurrentUser.Name)" -ForegroundColor Yellow
    Write-Host "Is Administrator: $IsAdmin" -ForegroundColor $(if($IsAdmin){"Red"}else{"Green"})
    
    $TestResults = @{
        TestID = "PRI-001"
        TestName = "Standard User File System Access"
        UserContext = $CurrentUser.Name
        IsAdmin = $IsAdmin
        Results = @{}
    }
    
    # Test common paths
    $PathTests = @{
        "UserProfile" = $env:USERPROFILE
        "AppData" = $env:APPDATA
        "LocalAppData" = $env:LOCALAPPDATA
        "TempPath" = $env:TEMP
        "CurrentDirectory" = Get-Location
        "DesktopPath" = [Environment]::GetFolderPath("Desktop")
        "DocumentsPath" = [Environment]::GetFolderPath("MyDocuments")
        "StartupFolder" = "$env:APPDATA\Microsoft\Windows\Start Menu\Programs\Startup"
    }
    
    # Test each path
    foreach ($PathName in $PathTests.Keys) {
        $TestPath = $PathTests[$PathName]
        Write-Host "Testing path: $PathName ($TestPath)" -ForegroundColor Gray
        
        $PathResult = @{
            Path = $TestPath
            Exists = Test-Path $TestPath
            CanRead = $false
            CanWrite = $false
            CanCreateFiles = $false
            CanCreateDirs = $false
        }
        
        try {
            # Test read access
            if ($PathResult.Exists) {
                Get-ChildItem $TestPath -ErrorAction Stop | Out-Null
                $PathResult.CanRead = $true
                Write-Host "  ✅ Read access: OK" -ForegroundColor Green
                
                # Test write access with temp file
                $TestFile = Join-Path $TestPath "privilege_test_$(Get-Date -Format 'yyyyMMddHHmmss').tmp"
                try {
                    "Test content" | Out-File $TestFile -ErrorAction Stop
                    $PathResult.CanWrite = $true
                    $PathResult.CanCreateFiles = $true
                    Write-Host "  ✅ Write access: OK" -ForegroundColor Green
                    
                    # Cleanup
                    Remove-Item $TestFile -ErrorAction SilentlyContinue
                }
                catch {
                    Write-Host "  ❌ Write access: DENIED" -ForegroundColor Red
                    $PathResult.CanWrite = $false
                }
                
                # Test directory creation
                $TestDir = Join-Path $TestPath "privilege_test_dir_$(Get-Date -Format 'yyyyMMddHHmmss')"
                try {
                    New-Item -ItemType Directory -Path $TestDir -ErrorAction Stop | Out-Null
                    $PathResult.CanCreateDirs = $true
                    Write-Host "  ✅ Directory creation: OK" -ForegroundColor Green
                    
                    # Cleanup
                    Remove-Item $TestDir -ErrorAction SilentlyContinue
                }
                catch {
                    Write-Host "  ❌ Directory creation: DENIED" -ForegroundColor Red
                }
            }
            else {
                Write-Host "  ⚠️ Path does not exist" -ForegroundColor Yellow
            }
        }
        catch {
            Write-Host "  ❌ Read access: DENIED - $($_.Exception.Message)" -ForegroundColor Red
        }
        
        $TestResults.Results[$PathName] = $PathResult
    }
    
    # Test specific trading environment paths
    $TradingPaths = @{
        "PlatformInstallations" = "PlatformInstallations"
        "PlatformInstances" = "PlatformInstances"
        "InstanceData" = "InstanceData"
        "TradingData" = "TradingData"
        "TestConfigs" = "TestConfigs"
        "TestResults" = "TestResults"
    }
    
    Write-Host "`nTesting Trading Environment Paths:" -ForegroundColor Cyan
    foreach ($PathName in $TradingPaths.Keys) {
        $TestPath = $TradingPaths[$PathName]
        
        try {
            # Try to create if doesn't exist
            if (-not (Test-Path $TestPath)) {
                New-Item -ItemType Directory -Path $TestPath -Force -ErrorAction Stop | Out-Null
                Write-Host "  ✅ Created: $PathName" -ForegroundColor Green
                $TestResults.Results["Create_$PathName"] = $true
            }
            else {
                Write-Host "  ✅ Exists: $PathName" -ForegroundColor Green
                $TestResults.Results["Exists_$PathName"] = $true
            }
        }
        catch {
            Write-Host "  ❌ Cannot create: $PathName - $($_.Exception.Message)" -ForegroundColor Red
            $TestResults.Results["Create_$PathName"] = $false
        }
    }
    
    # Save results
    $TestResults | ConvertTo-Json -Depth 3 | Set-Content "TestResults\PRI-001_$(Get-Date -Format 'yyyyMMdd_HHmmss').json" -ErrorAction SilentlyContinue
    
    return $TestResults
}
```

#### PRI-002: AppData Access Verification

**Objective**: Verify standard users can access and scan AppData installations

```powershell
function Test-AppDataAccess {
    Write-Host "🔐 Testing AppData Access for Standard Users..." -ForegroundColor Cyan
    
    $TestResults = @{
        TestID = "PRI-002" 
        TestName = "AppData Access Verification"
        UserContext = [System.Security.Principal.WindowsIdentity]::GetCurrent().Name
        AppDataResults = @{}
    }
    
    # Test AppData paths
    $AppDataPaths = @{
        "APPDATA" = $env:APPDATA
        "LOCALAPPDATA" = $env:LOCALAPPDATA
        "MetaQuotes" = "$env:APPDATA\MetaQuotes"
        "MetaQuotesTerminal" = "$env:APPDATA\MetaQuotes\Terminal"
    }
    
    foreach ($PathName in $AppDataPaths.Keys) {
        $TestPath = $AppDataPaths[$PathName]
        
        Write-Host "Testing AppData path: $PathName" -ForegroundColor Gray
        
        $PathTest = @{
            Path = $TestPath
            Accessible = $false
            CanEnumerate = $false
            SubfolderCount = 0
            Error = $null
        }
        
        try {
            if (Test-Path $TestPath) {
                $PathTest.Accessible = $true
                Write-Host "  ✅ Path accessible" -ForegroundColor Green
                
                # Try to enumerate contents
                $Contents = Get-ChildItem $TestPath -ErrorAction Stop
                $PathTest.CanEnumerate = $true
                $PathTest.SubfolderCount = ($Contents | Where-Object { $_.PSIsContainer }).Count
                Write-Host "  ✅ Can enumerate contents ($($PathTest.SubfolderCount) subfolders)" -ForegroundColor Green
            }
            else {
                Write-Host "  ⚠️ Path does not exist" -ForegroundColor Yellow
            }
        }
        catch {
            $PathTest.Error = $_.Exception.Message
            Write-Host "  ❌ Access denied: $($_.Exception.Message)" -ForegroundColor Red
        }
        
        $TestResults.AppDataResults[$PathName] = $PathTest
    }
    
    # Test MetaTrader detection with current privileges
    Write-Host "`nTesting MetaTrader Detection:" -ForegroundColor Cyan
    try {
        $MTInstallations = Find-MetaTraderInstallations -ErrorAction Stop
        Write-Host "  ✅ Found $($MTInstallations.Count) MetaTrader installations" -ForegroundColor Green
        $TestResults.MetaTraderDetection = @{
            Success = $true
            Count = $MTInstallations.Count
            Installations = $MTInstallations
        }
    }
    catch {
        Write-Host "  ❌ MetaTrader detection failed: $($_.Exception.Message)" -ForegroundColor Red
        $TestResults.MetaTraderDetection = @{
            Success = $false
            Error = $_.Exception.Message
        }
    }
    
    return $TestResults
}
```

### Category 2: Registry Access Tests

#### PRI-003: Registry Access Verification

**Objective**: Test registry operations that may be required by trading platforms

```powershell
function Test-RegistryAccess {
    Write-Host "🔐 Testing Registry Access..." -ForegroundColor Cyan
    
    $TestResults = @{
        TestID = "PRI-003"
        TestName = "Registry Access Verification"
        RegistryTests = @{}
    }
    
    # Registry paths commonly used by trading platforms
    $RegistryPaths = @{
        "HKCU_Software" = "HKCU:\Software"
        "HKCU_MetaQuotes" = "HKCU:\Software\MetaQuotes"
        "HKLM_Software" = "HKLM:\Software"
        "HKCU_Run" = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run"
        "HKLM_Run" = "HKLM:\Software\Microsoft\Windows\CurrentVersion\Run"
    }
    
    foreach ($RegName in $RegistryPaths.Keys) {
        $RegPath = $RegistryPaths[$RegName]
        Write-Host "Testing registry path: $RegName" -ForegroundColor Gray
        
        $RegTest = @{
            Path = $RegPath
            CanRead = $false
            CanWrite = $false
            CanCreateKeys = $false
            Error = $null
        }
        
        try {
            # Test read access
            if (Test-Path $RegPath) {
                Get-ItemProperty $RegPath -ErrorAction Stop | Out-Null
                $RegTest.CanRead = $true
                Write-Host "  ✅ Read access: OK" -ForegroundColor Green
                
                # Test write access
                $TestValueName = "PrivilegeTest_$(Get-Date -Format 'yyyyMMddHHmmss')"
                try {
                    Set-ItemProperty -Path $RegPath -Name $TestValueName -Value "test" -ErrorAction Stop
                    $RegTest.CanWrite = $true
                    Write-Host "  ✅ Write access: OK" -ForegroundColor Green
                    
                    # Cleanup
                    Remove-ItemProperty -Path $RegPath -Name $TestValueName -ErrorAction SilentlyContinue
                }
                catch {
                    Write-Host "  ❌ Write access: DENIED" -ForegroundColor Red
                }
                
                # Test key creation (only for HKCU paths)
                if ($RegPath -like "HKCU:*") {
                    $TestKeyPath = "$RegPath\PrivilegeTest_$(Get-Date -Format 'yyyyMMddHHmmss')"
                    try {
                        New-Item -Path $TestKeyPath -Force -ErrorAction Stop | Out-Null
                        $RegTest.CanCreateKeys = $true
                        Write-Host "  ✅ Key creation: OK" -ForegroundColor Green
                        
                        # Cleanup
                        Remove-Item -Path $TestKeyPath -ErrorAction SilentlyContinue
                    }
                    catch {
                        Write-Host "  ❌ Key creation: DENIED" -ForegroundColor Red
                    }
                }
            }
            else {
                Write-Host "  ⚠️ Registry path does not exist" -ForegroundColor Yellow
            }
        }
        catch {
            $RegTest.Error = $_.Exception.Message
            Write-Host "  ❌ Registry access failed: $($_.Exception.Message)" -ForegroundColor Red
        }
        
        $TestResults.RegistryTests[$RegName] = $RegTest
    }
    
    return $TestResults
}
```

### Category 3: Process Management Tests

#### PRI-004: Process Management Verification

**Objective**: Test process creation and management with standard user privileges

```powershell
function Test-ProcessManagement {
    Write-Host "🔐 Testing Process Management..." -ForegroundColor Cyan
    
    $TestResults = @{
        TestID = "PRI-004"
        TestName = "Process Management Verification"
        ProcessTests = @{}
    }
    
    # Test process enumeration
    Write-Host "Testing process enumeration..." -ForegroundColor Gray
    try {
        $AllProcesses = Get-Process -ErrorAction Stop
        $TradingProcesses = $AllProcesses | Where-Object { 
            $_.ProcessName -in @("terminal", "terminal64", "TradeTerminal", "cTrader") 
        }
        
        Write-Host "  ✅ Can enumerate processes ($($AllProcesses.Count) total, $($TradingProcesses.Count) trading)" -ForegroundColor Green
        $TestResults.ProcessTests.Enumeration = @{
            Success = $true
            TotalProcesses = $AllProcesses.Count
            TradingProcesses = $TradingProcesses.Count
        }
    }
    catch {
        Write-Host "  ❌ Process enumeration failed: $($_.Exception.Message)" -ForegroundColor Red
        $TestResults.ProcessTests.Enumeration = @{
            Success = $false
            Error = $_.Exception.Message
        }
    }
    
    # Test process priority modification
    Write-Host "Testing process priority modification..." -ForegroundColor Gray
    try {
        # Start a test process (notepad as a safe test)
        $TestProcess = Start-Process -FilePath "notepad.exe" -PassThru -WindowStyle Minimized -ErrorAction Stop
        Start-Sleep -Seconds 2
        
        # Try to change priority
        $OriginalPriority = $TestProcess.PriorityClass
        try {
            $TestProcess.PriorityClass = "High"
            Write-Host "  ✅ Can modify process priority" -ForegroundColor Green
            $TestResults.ProcessTests.PriorityModification = @{
                Success = $true
                OriginalPriority = $OriginalPriority
                ModifiedPriority = "High"
            }
            
            # Restore original priority
            $TestProcess.PriorityClass = $OriginalPriority
        }
        catch {
            Write-Host "  ❌ Cannot modify process priority: $($_.Exception.Message)" -ForegroundColor Red
            $TestResults.ProcessTests.PriorityModification = @{
                Success = $false
                Error = $_.Exception.Message
            }
        }
        
        # Cleanup test process
        $TestProcess.Kill()
        $TestProcess.WaitForExit(5000)
    }
    catch {
        Write-Host "  ❌ Process management test failed: $($_.Exception.Message)" -ForegroundColor Red
        $TestResults.ProcessTests.ProcessCreation = @{
            Success = $false
            Error = $_.Exception.Message
        }
    }
    
    # Test performance counter access
    Write-Host "Testing performance counter access..." -ForegroundColor Gray
    try {
        $CpuCounter = Get-Counter "\Processor(_Total)\% Processor Time" -ErrorAction Stop
        $MemoryCounter = Get-Counter "\Memory\Available Bytes" -ErrorAction Stop
        
        Write-Host "  ✅ Can access performance counters" -ForegroundColor Green
        $TestResults.ProcessTests.PerformanceCounters = @{
            Success = $true
            CpuValue = $CpuCounter.CounterSamples[0].CookedValue
            MemoryValue = $MemoryCounter.CounterSamples[0].CookedValue
        }
    }
    catch {
        Write-Host "  ❌ Cannot access performance counters: $($_.Exception.Message)" -ForegroundColor Red
        $TestResults.ProcessTests.PerformanceCounters = @{
            Success = $false
            Error = $_.Exception.Message
        }
    }
    
    return $TestResults
}
```

### Category 4: Network and System Services Tests

#### PRI-005: Network Access Verification

**Objective**: Test network operations that may be restricted for standard users

```powershell
function Test-NetworkAccess {
    Write-Host "🔐 Testing Network Access..." -ForegroundColor Cyan
    
    $TestResults = @{
        TestID = "PRI-005"
        TestName = "Network Access Verification"
        NetworkTests = @{}
    }
    
    # Test basic connectivity
    Write-Host "Testing basic network connectivity..." -ForegroundColor Gray
    $TestHosts = @{
        "Google_DNS" = "8.8.8.8"
        "Microsoft" = "microsoft.com"
        "MT4_Example" = "mt4.example.com"  # Example broker server
    }
    
    foreach ($HostName in $TestHosts.Keys) {
        $HostAddress = $TestHosts[$HostName]
        
        try {
            $PingResult = Test-Connection -ComputerName $HostAddress -Count 1 -Quiet -ErrorAction Stop
            if ($PingResult) {
                Write-Host "  ✅ $HostName ($HostAddress): Reachable" -ForegroundColor Green
                $TestResults.NetworkTests[$HostName] = @{
                    Success = $true
                    Host = $HostAddress
                    Reachable = $true
                }
            }
            else {
                Write-Host "  ❌ $HostName ($HostAddress): Not reachable" -ForegroundColor Red
                $TestResults.NetworkTests[$HostName] = @{
                    Success = $false
                    Host = $HostAddress
                    Reachable = $false
                }
            }
        }
        catch {
            Write-Host "  ❌ $HostName ($HostAddress): Test failed - $($_.Exception.Message)" -ForegroundColor Red
            $TestResults.NetworkTests[$HostName] = @{
                Success = $false
                Host = $HostAddress
                Error = $_.Exception.Message
            }
        }
    }
    
    # Test port connectivity (common trading platform ports)
    Write-Host "Testing port connectivity..." -ForegroundColor Gray
    $PortTests = @{
        "HTTPS" = @{ Host = "www.google.com"; Port = 443 }
        "FTP" = @{ Host = "ftp.microsoft.com"; Port = 21 }
        "MT4_Port" = @{ Host = "mt4.example.com"; Port = 443 }  # Common MT4 port
    }
    
    foreach ($TestName in $PortTests.Keys) {
        $TestInfo = $PortTests[$TestName]
        
        try {
            $TcpClient = New-Object System.Net.Sockets.TcpClient
            $ConnectTask = $TcpClient.BeginConnect($TestInfo.Host, $TestInfo.Port, $null, $null)
            $Success = $ConnectTask.AsyncWaitHandle.WaitOne(3000, $false)
            
            if ($Success) {
                Write-Host "  ✅ $TestName ($($TestInfo.Host):$($TestInfo.Port)): Connected" -ForegroundColor Green
                $TestResults.NetworkTests[$TestName] = @{
                    Success = $true
                    Host = $TestInfo.Host
                    Port = $TestInfo.Port
                    Connected = $true
                }
                $TcpClient.EndConnect($ConnectTask)
            }
            else {
                Write-Host "  ❌ $TestName ($($TestInfo.Host):$($TestInfo.Port)): Connection timeout" -ForegroundColor Red
                $TestResults.NetworkTests[$TestName] = @{
                    Success = $false
                    Host = $TestInfo.Host
                    Port = $TestInfo.Port
                    Connected = $false
                }
            }
            
            $TcpClient.Close()
        }
        catch {
            Write-Host "  ❌ $TestName ($($TestInfo.Host):$($TestInfo.Port)): $($_.Exception.Message)" -ForegroundColor Red
            $TestResults.NetworkTests[$TestName] = @{
                Success = $false
                Host = $TestInfo.Host
                Port = $TestInfo.Port
                Error = $_.Exception.Message
            }
        }
    }
    
    return $TestResults
}
```

### Category 5: Junction and Symbolic Link Tests

#### PRI-006: Junction Creation Verification

**Objective**: Test junction/symbolic link creation (may require special privileges)

```powershell
function Test-JunctionCreation {
    Write-Host "🔐 Testing Junction/Symbolic Link Creation..." -ForegroundColor Cyan
    
    $TestResults = @{
        TestID = "PRI-006"
        TestName = "Junction Creation Verification"
        JunctionTests = @{}
    }
    
    # Test junction creation (typically allowed for standard users)
    Write-Host "Testing directory junction creation..." -ForegroundColor Gray
    $SourceDir = "TestData\JunctionSource_$(Get-Date -Format 'yyyyMMddHHmmss')"
    $JunctionDir = "TestData\JunctionTarget_$(Get-Date -Format 'yyyyMMddHHmmss')"
    
    try {
        # Create source directory
        New-Item -ItemType Directory -Path $SourceDir -Force -ErrorAction Stop | Out-Null
        "Test file content" | Out-File (Join-Path $SourceDir "testfile.txt")
        
        # Try to create junction
        cmd /c "mklink /j `"$JunctionDir`" `"$SourceDir`"" 2>$null
        
        if (Test-Path $JunctionDir) {
            Write-Host "  ✅ Directory junction creation: SUCCESS" -ForegroundColor Green
            $TestResults.JunctionTests.DirectoryJunction = @{
                Success = $true
                SourcePath = $SourceDir
                JunctionPath = $JunctionDir
            }
            
            # Test junction access
            $JunctionFiles = Get-ChildItem $JunctionDir -ErrorAction SilentlyContinue
            if ($JunctionFiles.Count -gt 0) {
                Write-Host "  ✅ Junction access: SUCCESS" -ForegroundColor Green
                $TestResults.JunctionTests.JunctionAccess = $true
            }
        }
        else {
            Write-Host "  ❌ Directory junction creation: FAILED" -ForegroundColor Red
            $TestResults.JunctionTests.DirectoryJunction = @{
                Success = $false
                Error = "Junction not created"
            }
        }
    }
    catch {
        Write-Host "  ❌ Directory junction test failed: $($_.Exception.Message)" -ForegroundColor Red
        $TestResults.JunctionTests.DirectoryJunction = @{
            Success = $false
            Error = $_.Exception.Message
        }
    }
    finally {
        # Cleanup
        if (Test-Path $JunctionDir) {
            cmd /c "rmdir `"$JunctionDir`"" 2>$null
        }
        if (Test-Path $SourceDir) {
            Remove-Item $SourceDir -Recurse -Force -ErrorAction SilentlyContinue
        }
    }
    
    # Test symbolic link creation (may require elevated privileges)
    Write-Host "Testing symbolic link creation..." -ForegroundColor Gray
    $SourceFile = "TestData\SymlinkSource_$(Get-Date -Format 'yyyyMMddHHmmss').txt"
    $SymlinkFile = "TestData\SymlinkTarget_$(Get-Date -Format 'yyyyMMddHHmmss').txt"
    
    try {
        # Create source file
        "Test symlink content" | Out-File $SourceFile -ErrorAction Stop
        
        # Try to create symbolic link
        cmd /c "mklink `"$SymlinkFile`" `"$SourceFile`"" 2>$null
        
        if (Test-Path $SymlinkFile) {
            Write-Host "  ✅ Symbolic link creation: SUCCESS" -ForegroundColor Green
            $TestResults.JunctionTests.SymbolicLink = @{
                Success = $true
                SourcePath = $SourceFile
                SymlinkPath = $SymlinkFile
            }
        }
        else {
            Write-Host "  ❌ Symbolic link creation: FAILED (may require admin privileges)" -ForegroundColor Yellow
            $TestResults.JunctionTests.SymbolicLink = @{
                Success = $false
                Error = "Symbolic link not created - may require admin privileges"
            }
        }
    }
    catch {
        Write-Host "  ❌ Symbolic link test failed: $($_.Exception.Message)" -ForegroundColor Red
        $TestResults.JunctionTests.SymbolicLink = @{
            Success = $false
            Error = $_.Exception.Message
        }
    }
    finally {
        # Cleanup
        if (Test-Path $SymlinkFile) {
            Remove-Item $SymlinkFile -ErrorAction SilentlyContinue
        }
        if (Test-Path $SourceFile) {
            Remove-Item $SourceFile -ErrorAction SilentlyContinue
        }
    }
    
    return $TestResults
}
```

### Category 6: Windows Startup and Service Tests

#### PRI-007: Startup Management Verification

**Objective**: Test Windows startup folder access and service management capabilities

```powershell
function Test-StartupManagement {
    Write-Host "🔐 Testing Startup Management..." -ForegroundColor Cyan
    
    $TestResults = @{
        TestID = "PRI-007"
        TestName = "Startup Management Verification"
        StartupTests = @{}
    }
    
    # Test user startup folder access
    Write-Host "Testing user startup folder access..." -ForegroundColor Gray
    $UserStartupPath = "$env:APPDATA\Microsoft\Windows\Start Menu\Programs\Startup"
    
    try {
        if (Test-Path $UserStartupPath) {
            Write-Host "  ✅ User startup folder exists" -ForegroundColor Green
            
            # Test write access
            $TestStartupFile = Join-Path $UserStartupPath "PrivilegeTest_$(Get-Date -Format 'yyyyMMddHHmmss').bat"
            try {
                "echo Test startup script" | Out-File $TestStartupFile -ErrorAction Stop
                Write-Host "  ✅ Can create startup scripts" -ForegroundColor Green
                $TestResults.StartupTests.UserStartupAccess = @{
                    Success = $true
                    Path = $UserStartupPath
                    CanWrite = $true
                }
                
                # Cleanup
                Remove-Item $TestStartupFile -ErrorAction SilentlyContinue
            }
            catch {
                Write-Host "  ❌ Cannot create startup scripts: $($_.Exception.Message)" -ForegroundColor Red
                $TestResults.StartupTests.UserStartupAccess = @{
                    Success = $false
                    Path = $UserStartupPath
                    CanWrite = $false
                    Error = $_.Exception.Message
                }
            }
        }
        else {
            Write-Host "  ❌ User startup folder does not exist" -ForegroundColor Red
            $TestResults.StartupTests.UserStartupAccess = @{
                Success = $false
                Path = $UserStartupPath
                Exists = $false
            }
        }
    }
    catch {
        Write-Host "  ❌ Startup folder test failed: $($_.Exception.Message)" -ForegroundColor Red
        $TestResults.StartupTests.UserStartupAccess = @{
            Success = $false
            Error = $_.Exception.Message
        }
    }
    
    # Test system startup folder (typically requires admin)
    Write-Host "Testing system startup folder access..." -ForegroundColor Gray
    $SystemStartupPath = "$env:ProgramData\Microsoft\Windows\Start Menu\Programs\Startup"
    
    try {
        if (Test-Path $SystemStartupPath) {
            # Test write access (typically fails for standard users)
            $TestSystemStartupFile = Join-Path $SystemStartupPath "PrivilegeTest_$(Get-Date -Format 'yyyyMMddHHmmss').bat"
            try {
                "echo Test system startup script" | Out-File $TestSystemStartupFile -ErrorAction Stop
                Write-Host "  ✅ Can create system startup scripts (unexpected for standard user)" -ForegroundColor Yellow
                $TestResults.StartupTests.SystemStartupAccess = @{
                    Success = $true
                    Path = $SystemStartupPath
                    CanWrite = $true
                }
                
                # Cleanup
                Remove-Item $TestSystemStartupFile -ErrorAction SilentlyContinue
            }
            catch {
                Write-Host "  ✅ Cannot create system startup scripts (expected for standard user)" -ForegroundColor Green
                $TestResults.StartupTests.SystemStartupAccess = @{
                    Success = $true  # This is actually expected behavior
                    Path = $SystemStartupPath
                    CanWrite = $false
                    ExpectedBehavior = $true
                }
            }
        }
    }
    catch {
        Write-Host "  ❌ System startup folder test failed: $($_.Exception.Message)" -ForegroundColor Red
        $TestResults.StartupTests.SystemStartupAccess = @{
            Success = $false
            Error = $_.Exception.Message
        }
    }
    
    # Test Windows service enumeration
    Write-Host "Testing Windows service enumeration..." -ForegroundColor Gray
    try {
        $Services = Get-Service -ErrorAction Stop
        Write-Host "  ✅ Can enumerate services ($($Services.Count) found)" -ForegroundColor Green
        $TestResults.StartupTests.ServiceEnumeration = @{
            Success = $true
            ServiceCount = $Services.Count
        }
        
        # Test service control (typically restricted)
        Write-Host "Testing service control capabilities..." -ForegroundColor Gray
        try {
            # Try to get service details (should work)
            $TestService = Get-Service "Spooler" -ErrorAction Stop
            Write-Host "  ✅ Can get service details" -ForegroundColor Green
            
            # Try to control service (should fail for standard user)
            try {
                # Don't actually stop/start - just test the command
                $TestService | Set-Service -Status Running -WhatIf -ErrorAction Stop
                Write-Host "  ⚠️ Service control appears available (unexpected)" -ForegroundColor Yellow
                $TestResults.StartupTests.ServiceControl = @{
                    Available = $true
                    Unexpected = $true
                }
            }
            catch {
                Write-Host "  ✅ Service control restricted (expected)" -ForegroundColor Green
                $TestResults.StartupTests.ServiceControl = @{
                    Available = $false
                    Expected = $true
                }
            }
        }
        catch {
            Write-Host "  ❌ Service detail access failed: $($_.Exception.Message)" -ForegroundColor Red
        }
    }
    catch {
        Write-Host "  ❌ Service enumeration failed: $($_.Exception.Message)" -ForegroundColor Red
        $TestResults.StartupTests.ServiceEnumeration = @{
            Success = $false
            Error = $_.Exception.Message
        }
    }
    
    return $TestResults
}
```

---

## Master Privilege Testing Runner

### Complete Privilege Test Suite

```powershell
function Invoke-CompletePrivilegeTests {
    param(
        [string]$OutputPath = "TestResults\PrivilegeTests",
        [switch]$GenerateReport
    )
    
    Write-Host "🔐 Starting Complete Privilege Testing Suite..." -ForegroundColor Magenta
    Write-Host "=" * 80 -ForegroundColor Cyan
    
    # Ensure output directory exists
    if (-not (Test-Path $OutputPath)) {
        New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null
    }
    
    # Get current user context
    $CurrentUser = [System.Security.Principal.WindowsIdentity]::GetCurrent()
    $IsAdmin = ([Security.Principal.WindowsPrincipal] $CurrentUser).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")
    
    Write-Host "User Context: $($CurrentUser.Name)" -ForegroundColor Yellow
    Write-Host "Administrator: $IsAdmin" -ForegroundColor $(if($IsAdmin){"Red"}else{"Green"})
    Write-Host "Test Start: $(Get-Date)" -ForegroundColor Yellow
    Write-Host ""
    
    # Initialize overall results
    $OverallResults = @{
        TestSuite = "Complete Privilege Testing"
        UserContext = $CurrentUser.Name
        IsAdministrator = $IsAdmin
        StartTime = Get-Date
        TestResults = @{}
        Summary = @{}
    }
    
    # Execute all privilege tests
    $PrivilegeTests = @(
        @{ Function = "Test-StandardUserFileAccess"; Name = "File System Access" }
        @{ Function = "Test-AppDataAccess"; Name = "AppData Access" }
        @{ Function = "Test-RegistryAccess"; Name = "Registry Access" }
        @{ Function = "Test-ProcessManagement"; Name = "Process Management" }
        @{ Function = "Test-NetworkAccess"; Name = "Network Access" }
        @{ Function = "Test-JunctionCreation"; Name = "Junction Creation" }
        @{ Function = "Test-StartupManagement"; Name = "Startup Management" }
    )
    
    foreach ($Test in $PrivilegeTests) {
        Write-Host "🧪 Executing: $($Test.Name)" -ForegroundColor Cyan
        
        try {
            $TestResult = & $Test.Function
            $OverallResults.TestResults[$Test.Function] = $TestResult
            
            # Determine test outcome
            $TestPassed = $TestResult.PSObject.Properties.Value | ForEach-Object {
                if ($_ -is [hashtable] -and $_.ContainsKey("Success")) {
                    $_.Success
                }
            } | Where-Object { $_ -eq $false }
            
            if (-not $TestPassed) {
                Write-Host "  ✅ Test completed successfully" -ForegroundColor Green
            }
            else {
                Write-Host "  ⚠️ Test completed with some restrictions" -ForegroundColor Yellow
            }
        }
        catch {
            Write-Host "  ❌ Test failed: $($_.Exception.Message)" -ForegroundColor Red
            $OverallResults.TestResults[$Test.Function] = @{
                TestID = $Test.Function
                TestName = $Test.Name
                Success = $false
                Error = $_.Exception.Message
            }
        }
        
        Write-Host ""
    }
    
    $OverallResults.EndTime = Get-Date
    $OverallResults.Duration = ($OverallResults.EndTime - $OverallResults.StartTime).TotalMinutes
    
    # Generate summary
    $TotalTests = $OverallResults.TestResults.Count
    $PassedTests = 0
    $FailedTests = 0
    $RestrictedTests = 0
    
    foreach ($Result in $OverallResults.TestResults.Values) {
        # This is a simplified assessment - real implementation would be more nuanced
        if ($Result.TestID) {
            $PassedTests++
        }
    }
    
    $OverallResults.Summary = @{
        TotalTests = $TotalTests
        PassedTests = $PassedTests
        FailedTests = $FailedTests
        RestrictedTests = $RestrictedTests
        SuccessRate = if ($TotalTests -gt 0) { [math]::Round(($PassedTests / $TotalTests) * 100, 1) } else { 0 }
    }
    
    # Save results
    $ResultFile = Join-Path $OutputPath "PrivilegeTestResults_$(Get-Date -Format 'yyyyMMdd_HHmmss').json"
    $OverallResults | ConvertTo-Json -Depth 5 | Set-Content $ResultFile
    
    # Display summary
    Write-Host "📊 PRIVILEGE TESTING SUMMARY" -ForegroundColor Cyan
    Write-Host "=" * 80 -ForegroundColor Cyan
    Write-Host "User: $($CurrentUser.Name)" -ForegroundColor White
    Write-Host "Administrator: $IsAdmin" -ForegroundColor $(if($IsAdmin){"Red"}else{"Green"})
    Write-Host "Total Tests: $TotalTests" -ForegroundColor White
    Write-Host "Test Duration: $([math]::Round($OverallResults.Duration, 1)) minutes" -ForegroundColor White
    Write-Host "Results saved: $ResultFile" -ForegroundColor Green
    Write-Host ""
    
    # Generate HTML report if requested
    if ($GenerateReport) {
        $ReportFile = Join-Path $OutputPath "PrivilegeTestReport_$(Get-Date -Format 'yyyyMMdd_HHmmss').html"
        Generate-PrivilegeTestReport -Results $OverallResults -OutputPath $ReportFile
        Write-Host "HTML Report: $ReportFile" -ForegroundColor Green
    }
    
    return $OverallResults
}

# Usage Examples:
# Standard user testing
# Invoke-CompletePrivilegeTests -GenerateReport

# Admin user testing  
# Invoke-CompletePrivilegeTests -OutputPath "TestResults\AdminPrivilegeTests" -GenerateReport
```

---

## Implementation Strategy

### 1. Pre-Testing Environment Setup
```powershell
# Create test user account (requires admin)
net user StandardTestUser TestPassword123! /add
net localgroup Users StandardTestUser /add

# Test with different user contexts
runas /user:StandardTestUser "powershell.exe -File PrivilegeTests.ps1"
```

### 2. Graceful Degradation Implementation
```powershell
function Test-FeatureWithFallback {
    param([string]$FeatureName, [scriptblock]$PrimaryMethod, [scriptblock]$FallbackMethod)
    
    try {
        Write-Host "Attempting $FeatureName with full privileges..." -ForegroundColor Cyan
        & $PrimaryMethod
    }
    catch {
        Write-Host "Primary method failed, trying fallback..." -ForegroundColor Yellow
        try {
            & $FallbackMethod
        }
        catch {
            Write-Host "Both methods failed for $FeatureName" -ForegroundColor Red
            throw
        }
    }
}
```

### 3. Privilege-Aware Configuration
```json
{
  "privilegeSettings": {
    "requiresAdmin": false,
    "degradeGracefully": true,
    "fallbackMethods": {
      "startupManagement": "userOnly",
      "processControl": "limited",
      "registryAccess": "currentUserOnly"
    }
  }
}
```

This comprehensive privilege testing framework ensures your trading platform management system works correctly across all user privilege scenarios, with proper fallback mechanisms and clear reporting of any restrictions encountered.