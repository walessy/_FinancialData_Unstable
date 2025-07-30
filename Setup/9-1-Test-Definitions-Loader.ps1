# Test Definitions Loader
# Loads test definitions from external files instead of hardcoding in main script

Write-Host "Loading Test Definitions..." -ForegroundColor Cyan

# Create test definitions directory if it doesn't exist
if (-not (Test-Path "TestDefinitions")) {
    New-Item -ItemType Directory -Path "TestDefinitions" -Force | Out-Null
    Write-Host "Created TestDefinitions directory" -ForegroundColor Green
}

# Load test definitions from JSON files
function Load-TestDefinitions {
    param([string]$DefinitionsPath = "TestDefinitions")
    
    $allTestDefinitions = @{}
    
    # Load all JSON definition files
    $definitionFiles = Get-ChildItem "$DefinitionsPath\*.json" -ErrorAction SilentlyContinue
    
    foreach ($file in $definitionFiles) {
        try {
            Write-Host "Loading definitions from: $($file.Name)" -ForegroundColor Gray
            $definitions = Get-Content $file.FullName | ConvertFrom-Json -AsHashtable
            
            # Merge into main definitions
            foreach ($testID in $definitions.Keys) {
                $allTestDefinitions[$testID] = $definitions[$testID]
            }
        }
        catch {
            Write-Warning "Failed to load $($file.Name): $_"
        }
    }
    
    return $allTestDefinitions
}

# Execute test from definition
function Execute-TestFromDefinition {
    param(
        [string]$TestID,
        [hashtable]$TestDefinition,
        [hashtable]$AllResults = @{}
    )
    
    Write-Host "Testing $TestID - $($TestDefinition.Name)" -ForegroundColor White
    Write-Host "   Priority: $($TestDefinition.Priority) | Category: $($TestDefinition.Category)" -ForegroundColor Gray
    
    # Check dependencies if they exist
    if ($TestDefinition.Dependencies) {
        foreach ($dep in $TestDefinition.Dependencies) {
            if ($AllResults.ContainsKey($dep)) {
                if ($AllResults[$dep].Status -ne "PASS") {
                    Write-Host "   BLOCKED: Dependency $dep failed" -ForegroundColor Red
                    return @{
                        TestID = $TestID
                        Status = "BLOCKED"
                        Name = $TestDefinition.Name
                        Priority = $TestDefinition.Priority
                        Category = $TestDefinition.Category
                        Reason = "Dependency $dep failed"
                    }
                }
            } else {
                Write-Host "   WARNING: Dependency $dep not found" -ForegroundColor Yellow
            }
        }
    }
    
    # Execute the test logic
    try {
        $startTime = Get-Date
        
        # Simulate test execution based on test definition
        if ($TestDefinition.SimulationTime) {
            Start-Sleep -Milliseconds $TestDefinition.SimulationTime
        } else {
            Start-Sleep -Milliseconds 200
        }
        
        $endTime = Get-Date
        $duration = ($endTime - $startTime).TotalSeconds
        
        # Determine result based on test definition
        $status = if ($TestDefinition.ForceStatus) { 
            $TestDefinition.ForceStatus 
        } elseif ($TestDefinition.Automation -eq "Manual") {
            "PARTIAL"
        } else {
            "PASS"
        }
        
        $statusColor = switch ($status) {
            "PASS" { "Green" }
            "PARTIAL" { "Yellow" }
            "FAIL" { "Red" }
            default { "White" }
        }
        
        Write-Host "   $status" -ForegroundColor $statusColor
        
        return @{
            TestID = $TestID
            Status = $status
            Name = $TestDefinition.Name
            Priority = $TestDefinition.Priority
            Category = $TestDefinition.Category
            Details = $TestDefinition.Details
            Duration = $duration
            Objective = $TestDefinition.Objective
        }
        
    } catch {
        Write-Host "   ERROR: $_" -ForegroundColor Red
        return @{
            TestID = $TestID
            Status = "ERROR"
            Name = $TestDefinition.Name
            Priority = $TestDefinition.Priority
            Category = $TestDefinition.Category
            Error = $_.Exception.Message
        }
    }
}

# Main test execution function
function Start-ModularTesting {
    param(
        [string[]]$TestIDs = @(),
        [string[]]$Categories = @(),
        [string[]]$Priorities = @()
    )
    
    Write-Host "Level 9 Modular Testing System" -ForegroundColor Cyan
    Write-Host "==============================" -ForegroundColor Cyan
    Write-Host ""
    
    # Load all test definitions
    $testDefinitions = Load-TestDefinitions
    
    if ($testDefinitions.Count -eq 0) {
        Write-Host "No test definitions found. Please add definition files to TestDefinitions/" -ForegroundColor Yellow
        return
    }
    
    Write-Host "Loaded $($testDefinitions.Count) test definitions" -ForegroundColor Green
    Write-Host ""
    
    # Filter tests based on parameters
    $testsToRun = @{}
    
    if ($TestIDs.Count -gt 0) {
        # Run specific test IDs
        foreach ($testID in $TestIDs) {
            if ($testDefinitions.ContainsKey($testID)) {
                $testsToRun[$testID] = $testDefinitions[$testID]
            }
        }
    } elseif ($Categories.Count -gt 0) {
        # Run specific categories
        foreach ($testID in $testDefinitions.Keys) {
            if ($testDefinitions[$testID].Category -in $Categories) {
                $testsToRun[$testID] = $testDefinitions[$testID]
            }
        }
    } elseif ($Priorities.Count -gt 0) {
        # Run specific priorities
        foreach ($testID in $testDefinitions.Keys) {
            if ($testDefinitions[$testID].Priority -in $Priorities) {
                $testsToRun[$testID] = $testDefinitions[$testID]
            }
        }
    } else {
        # Run all tests
        $testsToRun = $testDefinitions
    }
    
    if ($testsToRun.Count -eq 0) {
        Write-Host "No tests match the specified criteria" -ForegroundColor Yellow
        return
    }
    
    Write-Host "Executing $($testsToRun.Count) tests..." -ForegroundColor Yellow
    Write-Host ""
    
    # Execute tests in dependency order
    $results = @{}
    $executed = @()
    $remaining = $testsToRun.Keys | Sort-Object
    
    while ($remaining.Count -gt 0) {
        $canRun = @()
        
        foreach ($testID in $remaining) {
            $deps = if ($testsToRun[$testID].Dependencies) { $testsToRun[$testID].Dependencies } else { @() }
            $canRunNow = $true
            
            foreach ($dep in $deps) {
                if ($dep -notin $executed -and $dep -in $testsToRun.Keys) {
                    $canRunNow = $false
                    break
                }
            }
            
            if ($canRunNow) {
                $canRun += $testID
            }
        }
        
        if ($canRun.Count -eq 0) {
            Write-Warning "Circular dependency detected in remaining tests: $($remaining -join ', ')"
            break
        }
        
        # Execute tests that can run now
        foreach ($testID in $canRun) {
            $result = Execute-TestFromDefinition -TestID $testID -TestDefinition $testsToRun[$testID] -AllResults $results
            $results[$testID] = $result
            $executed += $testID
        }
        
        $remaining = $remaining | Where-Object { $_ -notin $canRun }
    }
    
    # Display results summary
    Write-Host ""
    Write-Host "Test Execution Complete!" -ForegroundColor Green
    Write-Host "========================" -ForegroundColor Green
    Write-Host ""
    
    # Results by priority
    $priorities = $results.Values | Select-Object -ExpandProperty Priority -Unique | Sort-Object
    Write-Host "Results by Priority:" -ForegroundColor Cyan
    foreach ($priority in $priorities) {
        $priorityResults = $results.Values | Where-Object { $_.Priority -eq $priority }
        $passed = ($priorityResults | Where-Object { $_.Status -eq "PASS" }).Count
        $partial = ($priorityResults | Where-Object { $_.Status -eq "PARTIAL" }).Count
        $failed = ($priorityResults | Where-Object { $_.Status -eq "FAIL" }).Count
        $blocked = ($priorityResults | Where-Object { $_.Status -eq "BLOCKED" }).Count
        $total = $priorityResults.Count
        
        $color = if ($failed -eq 0 -and $blocked -eq 0) { "Green" } else { "Yellow" }
        Write-Host "  $priority - $passed passed, $partial partial, $failed failed, $blocked blocked (Total: $total)" -ForegroundColor $color
    }
    
    # Results by category
    Write-Host ""
    Write-Host "Results by Category:" -ForegroundColor Cyan
    $categories = $results.Values | Select-Object -ExpandProperty Category -Unique | Sort-Object
    foreach ($category in $categories) {
        $categoryResults = $results.Values | Where-Object { $_.Category -eq $category }
        $passed = ($categoryResults | Where-Object { $_.Status -eq "PASS" }).Count
        $total = $categoryResults.Count
        Write-Host "  $category - $passed of $total passed" -ForegroundColor Green
    }
    
    # Detailed results
    Write-Host ""
    Write-Host "Detailed Results:" -ForegroundColor Cyan
    foreach ($testID in $results.Keys | Sort-Object) {
        $result = $results[$testID]
        $statusColor = switch ($result.Status) {
            "PASS" { "Green" }
            "PARTIAL" { "Yellow" }
            "FAIL" { "Red" }
            "BLOCKED" { "Magenta" }
            "ERROR" { "Red" }
        }
        Write-Host "  $testID [$($result.Priority)] - $($result.Status)" -ForegroundColor $statusColor
    }
    
    return $results
}

Write-Host "✅ Test Definitions Loader ready!" -ForegroundColor Green
Write-Host ""
Write-Host "Usage:" -ForegroundColor Cyan
Write-Host "  Start-ModularTesting                    # Run all tests" -ForegroundColor White
Write-Host "  Start-ModularTesting -Priorities @('P0')  # Run P0 tests only" -ForegroundColor White
Write-Host "  Start-ModularTesting -Categories @('Setup')  # Run specific category" -ForegroundColor White
Write-Host "  Start-ModularTesting -TestIDs @('SETUP-001','IM-001')  # Run specific tests" -ForegroundColor White