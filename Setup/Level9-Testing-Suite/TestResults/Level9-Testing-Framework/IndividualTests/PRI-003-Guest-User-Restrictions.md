# Trading Platform Test Case: PRI-003
## Guest User Restrictions

**Test ID**: PRI-003  
**Category**: User Privilege  
**Priority**: P1  
**Automation**: Partial  
**Created**: 2025-07-29  
**Framework**: Level 9 Trading Platform Testing

---

## Test Objective
Ensure guest users have minimal read-only access with appropriate restrictions

## Prerequisites
- [ ] Standard and admin user testing completed
- [ ] Guest user account configured
- [ ] Guest access policies defined

## Test Steps
1. Login with guest user credentials
2. Attempt to read configuration files (should be restricted)
3. Attempt to modify any system settings (should fail)
4. Test access to trading interfaces (read-only expected)
5. Attempt file operations in system directories (should fail)
6. Verify access to public documentation and help files
7. Test session timeout and automatic logout

## Expected Results
- Configuration files access denied or read-only
- System settings modification attempts blocked
- Trading interfaces accessible in read-only mode only
- File operations in system directories denied
- Public documentation accessible
- Session timeout enforced for guest users

## Pass Criteria
- [ ] All write operations properly restricted
- [ ] Read access limited to appropriate files
- [ ] Session management enforced for guest accounts







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
| Execution Time | [Expected] | _____ | [ ] Pass [ ] Fail |
| Memory Usage | [Expected] | _____ | [ ] Pass [ ] Fail |
| CPU Usage | [Expected] | _____ | [ ] Pass [ ] Fail |
| Success Rate | [Expected] | _____ | [ ] Pass [ ] Fail |

### Issues Found
| Issue ID | Severity | Description | Status | Resolution |
|----------|----------|-------------|--------|------------|
| ISS-001 | [High/Med/Low] | | [ ] Open [ ] Resolved | |
| ISS-002 | [High/Med/Low] | | [ ] Open [ ] Resolved | |

### Trading Platform Specific Notes
**Platform Tested**: [ ] MT4 [ ] MT5 [ ] TraderEvolution [ ] All  
**Instance Configuration**: ________________  
**Data Folder**: ________________  
**Network Status**: ________________  

### Automation Assessment
**Current Automation Level**: Partial  
**Automation Feasibility**: [ ] High [ ] Medium [ ] Low  
**Implementation Complexity**: [ ] Simple [ ] Medium [ ] Complex  

---

**Navigation**:  
[← Back to Test Registry](../01-Test-ID-Registry-Framework.md) | [Trading Platform Index](../02-Trading-Platform-Tests-Index.md) | [Master Checklist](../Checklists/Master-Checklist.md)

---

**Trading Platform Testing Framework - Level 9**  
*Enhanced comprehensive validation for production deployment*
