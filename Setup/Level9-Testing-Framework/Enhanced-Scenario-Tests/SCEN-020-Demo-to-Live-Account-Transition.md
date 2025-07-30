# Scenario-Based Test: SCEN-020
## Demo to Live Account Transition

**Test ID**: SCEN-020  
**Scenario**: Trader transitioning from demo to live account  
**Category**: Account Management  
**Priority**: P0  
**Automation Level**: Full  
**Generated**: 2025-07-30 02:16

---

## Real-World Context

User Story: As a trader who has proven successful on demo, I want to seamlessly transition to live trading while maintaining my demo environment for continued testing. Business Value: Smooth demo-to-live transition reduces trader friction and maintains confidence during the critical transition phase.

---

## Configuration Example

```json
{
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
}
```

---

## Test Steps

1. Backup existing demo configuration and data
2. Create new live instance while preserving demo instance
3. Copy proven EA settings from demo to live
4. Configure different startup priorities (live=high, demo=low)
5. Test both accounts can run simultaneously
6. Verify live account has proper risk management settings
7. Test that demo testing does not interfere with live trading
8. Validate account separation and data isolation


---

## Pass Criteria

✅ Both demo and live accounts operational simultaneously
✅ Live account gets higher system priority
✅ Configuration settings properly copied but isolated
✅ No accidental cross-account trading
✅ Live account startup time less than 30 seconds


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
