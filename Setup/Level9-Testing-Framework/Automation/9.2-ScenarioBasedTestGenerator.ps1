# 9.2-ScenarioBasedTestGenerator.ps1
# Clean, properly formatted scenario-driven test generator

param(
    [string]$OutputPath = "Enhanced-Scenario-Tests",
    [switch]$GenerateAllScenarios = $true
)

Write-Host "🎯 SCENARIO-DRIVEN TEST GENERATOR" -ForegroundColor Cyan
Write-Host "Creating test definitions based on real-world use cases..." -ForegroundColor White

# Create output directory
New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null

# Real-world scenario-based test definitions
$ScenarioBasedTests = @{
    
    "SCEN-001" = @{
        Name = "Single Broker Demo Account Setup"
        Scenario = "New trader with one MT4 demo account from AfterPrime"
        Category = "Single Trader Setup"
        Priority = "P0"
        Automation = "Full"
        RealWorldContext = "User Story: As a new trader, I want to set up a single demo trading account so I can learn trading without risking real money. Business Value: This is the most common entry point for new traders. Getting this right means smooth onboarding for 80% of new users."
        TestSteps = @(
            "Verify system meets minimum requirements (8GB RAM, 50GB disk)",
            "Create trading root directory structure",
            "Extract AfterPrime MT4 demo to PlatformInstallations\AfterPrime_MT4_Demo\",
            "Create instances-config.json with single demo instance",
            "Run Level 2 instance creation: .\Setup\2 Level2-Clean.ps1",
            "Install automation: .\Setup\3 SimpleTradingManager.ps1 -Action Install",
            "Verify platform starts automatically with Windows",
            "Test platform connects to AfterPrime demo server",
            "Verify demo account loads correctly",
            "Test basic trading functions (market watch, charts, orders)"
        )
        PassCriteria = @(
            "Platform starts within 60 seconds of Windows boot",
            "Demo account connects without manual intervention", 
            "All basic trading functions operational",
            "No error messages in startup process",
            "Memory usage less than 500MB for single instance"
        )
        ConfigExample = '{
  "tradingRoot": "C:\\Projects\\FinancialData",
  "instances": [
    {
      "name": "AfterPrime_MT4_Demo_Instance",
      "broker": "AfterPrime",
      "platform": "MT4",
      "accountType": "demo",
      "enabled": true,
      "startupSettings": {
        "autoStart": true,
        "startupDelay": 0,
        "executable": "terminal.exe"
      }
    }
  ]
}'
    }
    
    "SCEN-010" = @{
        Name = "Professional Multi-Broker Environment"
        Scenario = "Professional trader with live accounts across multiple brokers"
        Category = "Multi-Broker Professional"
        Priority = "P0"
        Automation = "Partial"
        RealWorldContext = "User Story: As a professional trader, I need multiple live accounts across different brokers for diversification and strategy specialization. Business Value: Professional traders manage significant capital and need reliable, automated multi-broker management for business continuity."
        TestSteps = @(
            "Configure three different broker platforms",
            "Set high priority for live accounts, normal for demo", 
            "Implement staggered startup to prevent resource conflicts",
            "Test simultaneous operation of all three platforms",
            "Verify broker-specific settings are isolated",
            "Test failure recovery - if one platform crashes, others continue",
            "Validate live account connections are stable",
            "Test resource allocation under full load"
        )
        PassCriteria = @(
            "All three platforms start reliably in sequence",
            "Live accounts connect before demo accounts (priority)",
            "Total system memory usage less than 2GB",
            "No cross-broker configuration interference", 
            "Platform failures are isolated (do not cascade)"
        )
        ConfigExample = '{
  "instances": [
    {
      "name": "ICMarkets_MT4_Live",
      "broker": "ICMarkets",
      "accountType": "live",
      "priority": "high",
      "startupDelay": 0
    },
    {
      "name": "AfterPrime_MT5_Live", 
      "broker": "AfterPrime",
      "accountType": "live",
      "priority": "high",
      "startupDelay": 30
    },
    {
      "name": "FTMO_MT4_Demo",
      "broker": "FTMO", 
      "accountType": "demo",
      "priority": "normal",
      "startupDelay": 60
    }
  ]
}'
    }
    
    "SCEN-020" = @{
        Name = "Demo to Live Account Transition" 
        Scenario = "Trader transitioning from demo to live account"
        Category = "Account Management"
        Priority = "P0"
        Automation = "Full"
        RealWorldContext = "User Story: As a trader who has proven successful on demo, I want to seamlessly transition to live trading while maintaining my demo environment for continued testing. Business Value: Smooth demo-to-live transition reduces trader friction and maintains confidence during the critical transition phase."
        TestSteps = @(
            "Backup existing demo configuration and data",
            "Create new live instance while preserving demo instance",
            "Copy proven EA settings from demo to live", 
            "Configure different startup priorities (live=high, demo=low)",
            "Test both accounts can run simultaneously",
            "Verify live account has proper risk management settings",
            "Test that demo testing does not interfere with live trading",
            "Validate account separation and data isolation"
        )
        PassCriteria = @(
            "Both demo and live accounts operational simultaneously",
            "Live account gets higher system priority",
            "Configuration settings properly copied but isolated",
            "No accidental cross-account trading",
            "Live account startup time less than 30 seconds"
        )
        ConfigExample = '{
  "instances": [
    {
      "name": "Broker_MT4_Demo_Instance",
      "accountType": "demo",
      "priority": "low",
      "autoStart": false
    },
    {
      "name": "Broker_MT4_Live_Instance",
      "accountType": "live", 
      "priority": "high",
      "autoStart": true
    }
  ]
}'
    }
    
    "SCEN-030" = @{
        Name = "EA Development and Backtesting Environment"
        Scenario = "Developer creating and testing Expert Advisors across multiple timeframes"
        Category = "Development & Testing"
        Priority = "P1"
        Automation = "Partial"
        RealWorldContext = "User Story: As an EA developer, I need isolated testing environments where I can backtest strategies without affecting live trading operations. Business Value: Proper EA development environments are crucial for algorithmic trading success and risk management."
        TestSteps = @(
            "Create three separate MT4 instances for different purposes",
            "Configure backtesting instance with historical data",
            "Setup forward testing with demo account",
            "Configure production instance with live account",
            "Test isolation between development and production",
            "Verify backtesting performance and resource usage",
            "Test strategy migration from backtest to forward test to live",
            "Validate that development work does not impact live trading"
        )
        PassCriteria = @(
            "Three instances operate independently",
            "Backtesting completes without timeout",
            "Development changes do not affect production",
            "Resource usage manageable across all instances",
            "Strategy deployment pipeline works smoothly"
        )
        ConfigExample = '{
  "devEnvironment": {
    "backtestingInstance": {
      "name": "Development_MT4_Backtest",
      "purpose": "Strategy backtesting",
      "autoStart": false,
      "memoryLimit": "1GB"
    },
    "forwardTestInstance": {
      "name": "Development_MT4_Forward",
      "purpose": "Forward testing", 
      "autoStart": false,
      "memoryLimit": "1GB"
    },
    "productionInstance": {
      "name": "Production_MT4_Live",
      "purpose": "Live trading",
      "autoStart": true,
      "priority": "high"
    }
  }
}'
    }
    
    "SCEN-060" = @{
        Name = "Trading System Disaster Recovery"
        Scenario = "Recovery from complete system failure during live trading"
        Category = "Backup & Recovery"
        Priority = "P0"
        Automation = "Partial"
        RealWorldContext = "User Story: As a professional trader, I need to quickly recover from system failures to minimize trading downtime and protect open positions. Business Value: Fast disaster recovery prevents trading losses and maintains business continuity during critical market events."
        TestSteps = @(
            "Simulate complete trading system failure",
            "Execute emergency backup restoration",
            "Verify all platform configurations restored",
            "Test connectivity to all broker servers",
            "Validate open positions are visible and manageable",
            "Test trading functionality immediately after recovery",
            "Verify account balances and equity calculations",
            "Test that EAs resume operation correctly"
        )
        PassCriteria = @(
            "System restoration completed within 5 minutes",
            "All open positions visible and manageable",
            "Trading functionality fully operational",
            "No data loss or configuration corruption",
            "EAs resume without manual intervention"
        )
        ConfigExample = '{
  "disasterScenario": {
    "failureType": "complete_system_crash",
    "openPositions": ["3 EUR/USD longs", "2 GBP/USD shorts"],
    "marketCondition": "high_volatility",
    "recoveryTimeObjective": "5 minutes"
  }
}'
    }
}

# Generate test definitions for each scenario
foreach ($TestID in $ScenarioBasedTests.Keys) {
    $Test = $ScenarioBasedTests[$TestID]
    
    # Build test steps section
    $TestStepsSection = ""
    $StepNumber = 1
    foreach ($Step in $Test.TestSteps) {
        $TestStepsSection += "$StepNumber. $Step`n"
        $StepNumber++
    }
    
    # Build pass criteria section
    $PassCriteriaSection = ""
    foreach ($Criteria in $Test.PassCriteria) {
        $PassCriteriaSection += "✅ $Criteria`n"
    }
    
    $TestDocument = @"
# Scenario-Based Test: $TestID
## $($Test.Name)

**Test ID**: $TestID  
**Scenario**: $($Test.Scenario)  
**Category**: $($Test.Category)  
**Priority**: $($Test.Priority)  
**Automation Level**: $($Test.Automation)  
**Generated**: $(Get-Date -Format 'yyyy-MM-dd HH:mm')

---

## Real-World Context

$($Test.RealWorldContext)

---

## Configuration Example

``````json
$($Test.ConfigExample)
``````

---

## Test Steps

$TestStepsSection

---

## Pass Criteria

$PassCriteriaSection

---

## Test Execution Commands

``````powershell
# Setup Phase (Run as Administrator, one-time)
.\Setup\1 Level 1 Trading Environment Setup.ps1

# Configuration Phase
# Create instances-config.json based on scenario configuration above

# Instance Creation
.\Setup\2 Level2-Clean.ps1

# Automation Setup  
.\Setup\3 SimpleTradingManager.ps1 -Action Install

# Validation
.\Setup\3 SimpleTradingManager.ps1 -Action Status
``````

---

## Success Metrics

| Metric | Target | Measurement Method |
|--------|--------|-------------------|
| Startup Time | Less than 60 seconds | Timestamp measurement |
| Memory Usage | Less than 1GB per instance | Task Manager monitoring |
| Connection Success | 100% | Broker connection logs |
| Error Rate | 0% | System event logs |
| User Satisfaction | High | Functional requirements met |

---

## Related Test IDs

**Prerequisites**: SETUP-001, SETUP-002  
**Dependencies**: IM-001, AS-001  
**Follow-up Tests**: IE-001, IE-030  

---

**Navigation**:  
[← Back to Scenario Index](Scenario-Test-Index.md) | [Level 9 Master Plan](../00-Master-Test-Plan-Overview.md)

---

**Scenario-Driven Testing Framework - Level 9**  
*Real-world context drives comprehensive test coverage*
"@

    $FileName = "$TestID-$($Test.Name -replace '[^a-zA-Z0-9]', '-').md"
    $FilePath = Join-Path $OutputPath $FileName
    
    $TestDocument | Out-File $FilePath -Encoding UTF8
    Write-Host "✅ Created: $FileName" -ForegroundColor Green
}

# Create master scenario index
$ScenarioIndex = @"
# Scenario-Driven Test Index
## Real-World Use Case Testing Framework

**Generated**: $(Get-Date -Format 'yyyy-MM-dd HH:mm')  
**Total Scenarios**: $($ScenarioBasedTests.Keys.Count)  
**Coverage**: Complete real-world trading scenarios  

---

## Scenario Categories

### 🔰 Basic Single Trader Setup
Real-world entry scenarios for new traders

| Test ID | Scenario | Priority | Context |
|---------|----------|----------|---------|
| SCEN-001 | Single Broker Demo Account Setup | P0 | Most common trader onboarding |

### 💼 Multi-Broker Professional Setup
Enterprise-grade multi-broker environments

| Test ID | Scenario | Priority | Context |
|---------|----------|----------|---------|
| SCEN-010 | Professional Multi-Broker Environment | P0 | Professional trader diversification |

### 📊 Account Management
Demo-to-live transitions and account handling

| Test ID | Scenario | Priority | Context |
|---------|----------|----------|---------|
| SCEN-020 | Demo to Live Account Transition | P0 | Critical trader progression path |

### 🔬 Development & Testing
EA development and strategy testing scenarios

| Test ID | Scenario | Priority | Context |
|---------|----------|----------|---------|
| SCEN-030 | EA Development and Backtesting Environment | P1 | Algorithm development workflow |

### 🆘 Backup & Recovery
Disaster recovery and business continuity

| Test ID | Scenario | Priority | Context |
|---------|----------|----------|---------|
| SCEN-060 | Trading System Disaster Recovery | P0 | Business continuity protection |

---

## Key Benefits of Scenario-Driven Testing

### 🎯 Real-World Relevance
- Tests based on actual user stories and business needs
- Covers complete user journeys, not just technical functions
- Includes business context and value propositions

### 📈 Comprehensive Coverage
- From beginner single-account setups to enterprise multi-broker environments
- Covers normal operations and disaster scenarios
- Includes team and individual use cases

### 💡 Practical Value
- Each test includes real configuration examples
- Troubleshooting guidance based on common failure patterns
- Success metrics tied to business objectives

### 🔄 Maintainable Framework
- Scenarios can be easily updated as requirements evolve
- Test cases remain relevant to actual user needs
- Framework scales from individual to enterprise use

---

## Usage Recommendations

### For Development Teams
Use scenario-driven tests to validate that technical implementations 
meet real-world user needs and business requirements.

### For QA Teams
Execute scenario tests to ensure complete user journeys work correctly,
not just individual technical components.

### For Product Teams
Use scenarios to validate that product features solve actual user problems
and deliver measurable business value.

### For Operations Teams
Use troubleshooting scenarios to prepare for and practice issue resolution
in controlled environments.

---

## Integration with Level 9 Framework

These scenario-driven tests complement the existing Level 9 testing framework:

- **Technical Tests** (SETUP-xxx, IM-xxx, AS-xxx): Validate individual components
- **Scenario Tests** (SCEN-xxx): Validate complete user journeys and business value
- **Integration Tests** (IE-xxx): Validate system-level integration and performance

**Recommended Execution Order**:
1. Run technical component tests (Level 9 framework)
2. Execute relevant scenario tests for your use case
3. Perform final integration and certification testing

---

**Generated by Scenario-Driven Test Generator**  
*Real-world use cases drive comprehensive test coverage*
"@

$IndexPath = Join-Path $OutputPath "Scenario-Test-Index.md"
$ScenarioIndex | Out-File $IndexPath -Encoding UTF8

Write-Host ""
Write-Host "🎉 SCENARIO-DRIVEN TEST GENERATION COMPLETE!" -ForegroundColor Green
Write-Host "📁 Output Location: $OutputPath" -ForegroundColor Cyan
Write-Host "📊 Scenarios Generated: $($ScenarioBasedTests.Keys.Count)" -ForegroundColor White
Write-Host "📋 Index File: Scenario-Test-Index.md" -ForegroundColor White

Write-Host ""
Write-Host "🎯 Key Benefits:" -ForegroundColor Cyan
Write-Host "✅ Real-world context drives test design" -ForegroundColor Green
Write-Host "✅ Complete user journey coverage" -ForegroundColor Green
Write-Host "✅ Business value validation included" -ForegroundColor Green
Write-Host "✅ Practical configuration examples" -ForegroundColor Green
Write-Host "✅ Clean, consistent formatting" -ForegroundColor Green

Write-Host ""
Write-Host "🚀 Ready to execute comprehensive scenario-based testing!" -ForegroundColor White