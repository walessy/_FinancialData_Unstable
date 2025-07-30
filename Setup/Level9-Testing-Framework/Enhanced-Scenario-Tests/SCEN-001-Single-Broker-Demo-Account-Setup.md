# Scenario-Based Test: SCEN-001
## Single Broker Demo Account Setup

**Test ID**: SCEN-001  
**Scenario**: New trader with one MT4 demo account from AfterPrime  
**Category**: Single Trader Setup  
**Priority**: P0  
**Automation Level**: Full  
**Generated**: 2025-07-30 02:16

---

## Real-World Context

User Story: As a new trader, I want to set up a single demo trading account so I can learn trading without risking real money. Business Value: This is the most common entry point for new traders. Getting this right means smooth onboarding for 80% of new users.

---

## Configuration Example

```json
{
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
}
```

---

## Test Steps

1. Verify system meets minimum requirements (8GB RAM, 50GB disk)
2. Create trading root directory structure
3. Extract AfterPrime MT4 demo to PlatformInstallations\AfterPrime_MT4_Demo\
4. Create instances-config.json with single demo instance
5. Run Level 2 instance creation: .\Setup\2 Level2-Clean.ps1
6. Install automation: .\Setup\3 SimpleTradingManager.ps1 -Action Install
7. Verify platform starts automatically with Windows
8. Test platform connects to AfterPrime demo server
9. Verify demo account loads correctly
10. Test basic trading functions (market watch, charts, orders)


---

## Pass Criteria

✅ Platform starts within 60 seconds of Windows boot
✅ Demo account connects without manual intervention
✅ All basic trading functions operational
✅ No error messages in startup process
✅ Memory usage less than 500MB for single instance


---

## Test Execution Commands

```powershell
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
```

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
