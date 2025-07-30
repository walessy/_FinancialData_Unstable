# Level 9 Cache System
# Core caching functions for smart test execution

function Set-TestResultCache {
    param([string]$TestID, [string]$Status = "PASS")
    if (-not (Test-Path "TestResults\Cache")) {
        New-Item -Path "TestResults\Cache" -ItemType Directory -Force | Out-Null
    }
    $cacheData = @{ 
        TestID = $TestID
        Status = $Status
        CachedAt = Get-Date
        Timestamp = (Get-Date).ToString('yyyy-MM-dd HH:mm:ss')
    }
    $cacheData | ConvertTo-Json | Set-Content "TestResults\Cache\$TestID.json"
    Write-Host "? Cached: $TestID = $Status" -ForegroundColor Green
}

function Get-TestResultCache {
    param([string]$TestID)
    $cacheFile = "TestResults\Cache\$TestID.json"
    if (Test-Path $cacheFile) {
        $data = Get-Content $cacheFile | ConvertFrom-Json
        return $data
    }
    return $null
}

function Show-TestCacheStats {
    Write-Host "=== TEST CACHE STATISTICS ===" -ForegroundColor Cyan
    $cachePath = "TestResults\Cache"
    $count = if (Test-Path $cachePath) { (Get-ChildItem $cachePath -File).Count } else { 0 }
    Write-Host "?? Total Cached Tests: $count" -ForegroundColor Green
    
    if ($count -gt 0) {
        $passCount = 0
        $failCount = 0
        $blockedCount = 0
        
        Get-ChildItem $cachePath -File | ForEach-Object {
            $cache = Get-Content $_.FullName | ConvertFrom-Json
            switch ($cache.Status) {
                "PASS" { $passCount++ }
                "FAIL" { $failCount++ }
                "BLOCKED" { $blockedCount++ }
            }
        }
        
        Write-Host "  ? Passed: $passCount" -ForegroundColor Green
        Write-Host "  ? Failed: $failCount" -ForegroundColor Red
        Write-Host "  ?? Blocked: $blockedCount" -ForegroundColor Yellow
    }
}

function Clear-TestCache {
    param([switch]$Confirm = $false)
    
    if (-not $Confirm) {
        Write-Host "??  This will clear all cached test results!" -ForegroundColor Yellow
        Write-Host "Use -Confirm to proceed" -ForegroundColor Yellow
        return
    }
    
    $cachePath = "TestResults\Cache"
    if (Test-Path $cachePath) {
        Remove-Item "$cachePath\*" -Force
        Write-Host "?? Test cache cleared!" -ForegroundColor Green
    }
}

Write-Host "? Level 9 Cache System loaded!" -ForegroundColor Green
