# Level 9 Master Loader
# Loads all Level 9 testing components in proper order

Write-Host "?? Loading Level 9 Testing Suite..." -ForegroundColor Magenta
Write-Host "====================================" -ForegroundColor Magenta

# Load components in order
Write-Host "Loading Cache System..." -ForegroundColor Cyan
. .\Core\CacheSystem\Level9-CacheSystem.ps1

Write-Host "Loading Smart Runner..." -ForegroundColor Cyan
. .\Core\SmartRunner\Level9-SmartRunner.ps1

Write-Host "Loading Test Functions..." -ForegroundColor Cyan
. .\Core\TestFunctions\Level9-TestFunctions.ps1

Write-Host ""
Write-Host "? Level 9 Testing Suite Ready!" -ForegroundColor Green
Write-Host ""
Write-Host "Available Commands:" -ForegroundColor Cyan
Write-Host "  Test-SmartRunner      # Test the smart runner" -ForegroundColor White
Write-Host "  Start-FoundationTests # Run foundation tests" -ForegroundColor White
Write-Host "  Start-InstanceTests   # Run instance tests" -ForegroundColor White
Write-Host "  Start-ProcessTests    # Run process tests" -ForegroundColor White
Write-Host "  Start-IntegrationTests# Run integration tests" -ForegroundColor White
Write-Host "  Start-AllTests        # Run complete test suite" -ForegroundColor White
Write-Host "  Show-TestCacheStats   # Show test results" -ForegroundColor White
Write-Host "  Clear-TestCache       # Clear cached results" -ForegroundColor White
Write-Host ""
Write-Host "Quick Start:" -ForegroundColor Yellow
Write-Host "  Test-SmartRunner      # Verify everything works" -ForegroundColor White
Write-Host "  Start-AllTests        # Run the full test suite" -ForegroundColor White
Write-Host ""
