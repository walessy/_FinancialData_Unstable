# Trading Platform Test Case: IM-001
## Single Instance Basic Startup

**Test ID**: IM-001  
**Category**: Instance Management  
**Priority**: P0  
**Automation**: Full  
**Created**: 2025-07-29  
**Framework**: Level 9 Trading Platform Testing

---

## Test Objective
Validate single trading platform instance starts correctly with various parameter combinations

## Prerequisites
- [ ] Platform installation validated (SETUP-002)
- [ ] Configuration file available
- [ ] Test data folders prepared

## Test Steps
1. Configure instance with autoStart=true, enabled=true, delay=0, priority=high
2. Start trading platform management system
3. Monitor instance startup process
4. Validate process priority assignment
5. Check memory allocation and network connectivity
6. Test manual instance control (start/stop)

## Expected Results
- Instance starts immediately (within 5 seconds)
- High priority assigned correctly
- Memory allocation within expected range (< 500MB)
- Network connectivity established to trading servers
- Manual controls respond correctly

## Pass Criteria
- [ ] Instance startup within expected timeframe
- [ ] Correct priority assignment verified
- [ ] Memory and network within limits
- [ ] Manual controls functional

## Test Matrix
Test Scenarios:
autoStart=true,  enabled=true,  delay=0,  priority=high     → Expected: Immediate startup
autoStart=true,  enabled=true,  delay=15, priority=normal   → Expected: 15s delayed startup  
autoStart=false, enabled=true,  delay=0,  priority=low      → Expected: Manual start only
autoStart=true,  enabled=false, delay=0,  priority=high     → Expected: No startup







---

## Test Execution

**Executed By**: ________________  
**Date**: ________________  
**Environment**: ________________  
**Trading Platform Version**: ________________  

### Execution Results
- [ ] **PASS** - All criteria met, system ready for next phase
- [ ] **FAIL** - One or more criteria not met, issues identified
- [ ] **BLOCKED** - Cannot execute due to dependency failure

### Performance Metrics
| Metric | Expected | Actual | Status |
|--------|----------|--------|--------|
| Startup Time | [Expected] | _____ | [ ] Pass [ ] Fail |
| Memory Usage | [Expected] | _____ | [ ] Pass [ ] Fail |
| CPU Usage | [Expected] | _____ | [ ] Pass [ ] Fail |
| Network Latency | [Expected] | _____ | [ ] Pass [ ] Fail |

### Issues Found
| Issue ID | Severity | Description | Status | Resolution |
|----------|----------|-------------|--------|------------|
| ISS-001 | [High/Med/Low] | | [ ] Open [ ] Resolved | |
| ISS-002 | [High/Med/Low] | | [ ] Open [ ] Resolved | |

### Trading Platform Specific Notes
**Instance Configuration**: ________________  
**Market Data Status**: ________________  
**EA Status**: ________________  
**Network Connectivity**: ________________  

### Automation Assessment
**Current Automation Level**: Full  
**Can be fully automated**: [ ] Yes [ ] No [ ] Partially  
**Automation priority for next phase**: [ ] High [ ] Medium [ ] Low  
**Automation complexity**: [ ] Simple [ ] Medium [ ] Complex

### Dependencies Verified
- [ ] Previous test phases completed successfully
- [ ] Required software components installed
- [ ] Network connectivity established
- [ ] Test data and configurations prepared

---

## Related Tests
**Prerequisites**: [Link to prerequisite tests]  
**Dependent Tests**: [Link to tests that depend on this one]  
**Related Documentation**: [Link to relevant documentation]

**Navigation**:  
[← Back to Test Registry](../01-Test-ID-Registry-Framework.md) | [Master Checklist](../Checklists/Master-Checklist.md) | [Next Test →]

---

**Trading Platform Testing Framework - Level 9**  
*Comprehensive validation for production deployment*
