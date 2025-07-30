# Level 9 Smart Test Runner
# Intelligent test execution with dependency management

function Execute-TestSmart {
    param(
        [Parameter(Mandatory=$true)]
        [string]$TestID,
        
        [Parameter(Mandatory=$true)]
        [scriptblock]$TestLogic,
        
        [string[]]$Dependencies = @(),
        [string]$Description = "",
        [int]$ExpectedDuration = 5
    )
    
    Write-Host ""
    Write-Host "?? Analyzing $TestID..." -ForegroundColor Cyan
    if ($Description) {
        Write-Host "   $Description" -ForegroundColor Gray
    }
    
    # Check dependencies
    foreach ($dep in $Dependencies) {
        $depResult = Get-TestResultCache -TestID $dep
        if (-not $depResult -or $depResult.Status -ne "PASS") {
            Write-Host "?? BLOCKED: $TestID requires $dep to pass first" -ForegroundColor Red
            Set-TestResultCache -TestID $TestID -Status "BLOCKED"
            return @{ TestID = $TestID; Status = "BLOCKED"; Reason = "Missing dependency: $dep" }
        }
    }
    
    # Check cache
    $cached = Get-TestResultCache -TestID $TestID
    if ($cached -and $cached.Status -eq "PASS") {
        Write-Host "?? SKIPPING $TestID - Already passed at $($cached.Timestamp)" -ForegroundColor Green
        return $cached
    }
    
    # Execute test
    Write-Host "?? EXECUTING $TestID" -ForegroundColor White
    $startTime = Get-Date
    
    try {
        $result = & $TestLogic
        $endTime = Get-Date
        $duration = ($endTime - $startTime).TotalSeconds
        
        # Validate result
        if ($result -notin @("PASS", "FAIL", "BLOCKED", "SKIPPED")) {
            $result = "ERROR"
        }
        
        Set-TestResultCache -TestID $TestID -Status $result
        
        $color = switch ($result) {
            "PASS" { "Green" }
            "FAIL" { "Red" }
            "BLOCKED" { "Yellow" }
            "SKIPPED" { "Cyan" }
            default { "Magenta" }
        }
        
        $durationText = "$([math]::Round($duration, 2))s"
        $slowText = if ($duration -gt $ExpectedDuration) { " (SLOW)" } else { "" }
        
        Write-Host "? $TestID completed: $result in $durationText$slowText" -ForegroundColor $color
        
        return @{ 
            TestID = $TestID
            Status = $result
            Duration = $duration
            StartTime = $startTime
            EndTime = $endTime
        }
        
    } catch {
        Write-Host "? $TestID ERROR: $_" -ForegroundColor Red
        Set-TestResultCache -TestID $TestID -Status "ERROR"
        return @{ TestID = $TestID; Status = "ERROR"; Error = $_.Exception.Message }
    }
}

function Test-SmartRunner {
    Write-Host "?? Testing Smart Test Runner..." -ForegroundColor Cyan
    
    # Test 1: Basic execution
    $result1 = Execute-TestSmart -TestID "TEST-RUNNER-001" -TestLogic {
        Write-Host "   Running basic test..." -ForegroundColor Gray
        Start-Sleep -Milliseconds 500
        return "PASS"
    } -Description "Basic execution test"
    
    # Test 2: Cache behavior
    $result2 = Execute-TestSmart -TestID "TEST-RUNNER-001" -TestLogic {
        Write-Host "   This should not run..." -ForegroundColor Gray
        return "PASS"
    } -Description "Cache test"
    
    # Test 3: Dependency test
    $result3 = Execute-TestSmart -TestID "TEST-RUNNER-002" -Dependencies @("TEST-RUNNER-001") -TestLogic {
        Write-Host "   Dependency test..." -ForegroundColor Gray
        return "PASS"
    } -Description "Dependency validation"
    
    # Test 4: Blocked test
    $result4 = Execute-TestSmart -TestID "TEST-RUNNER-003" -Dependencies @("MISSING-TEST") -TestLogic {
        Write-Host "   Should be blocked..." -ForegroundColor Gray
        return "PASS"
    } -Description "Blocked test"
    
    Write-Host ""
    Write-Host "?? Smart Runner Test Results:" -ForegroundColor Cyan
    Write-Host "  Basic Test: $($result1.Status)" -ForegroundColor $(if ($result1.Status -eq "PASS") { "Green" } else { "Red" })
    Write-Host "  Cache Test: $($result2.Status)" -ForegroundColor $(if ($result2.Status -eq "SKIPPED") { "Green" } else { "Red" })
    Write-Host "  Dependency Test: $($result3.Status)" -ForegroundColor $(if ($result3.Status -eq "PASS") { "Green" } else { "Red" })
    Write-Host "  Blocked Test: $($result4.Status)" -ForegroundColor $(if ($result4.Status -eq "BLOCKED") { "Green" } else { "Red" })
    
    $success = ($result1.Status -eq "PASS") -and ($result2.Status -eq "SKIPPED") -and 
               ($result3.Status -eq "PASS") -and ($result4.Status -eq "BLOCKED")
    
    if ($success) {
        Write-Host "?? Smart Test Runner working correctly!" -ForegroundColor Green
    } else {
        Write-Host "?? Smart Test Runner has issues" -ForegroundColor Yellow
    }
    
    return $success
}

Write-Host "? Level 9 Smart Test Runner loaded!" -ForegroundColor Green
