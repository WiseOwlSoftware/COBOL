CobolSharp Operational Runbook — Incident Response, Debugging in Production & Recovery Procedures (CIL‑Only)
===========================================================================================================

Purpose
-------
Define the authoritative operational runbook for CobolSharp production systems:
- Incident response workflow
- Production debugging procedures
- ExceptionState triage
- File I/O corruption recovery
- Indexed file rebuild procedures
- WASM and AOT production diagnostics
- Multi‑tenant isolation failures
- Performance degradation triage
- Memory leak investigation
- Rollback and recovery procedures
- Observability and log analysis
- Safe hotfix and redeploy workflows

This document provides the operational playbook for SREs, operators, and on‑call engineers.

------------------------------------------------------------
SECTION 1 — INCIDENT RESPONSE WORKFLOW
------------------------------------------------------------

1.1 Severity classification
---------------------------
SEV‑1:  
- System down  
- Data corruption  
- Multi‑tenant outage  

SEV‑2:  
- Partial outage  
- Performance degradation  
- File I/O failures  

SEV‑3:  
- Non‑critical errors  
- Single‑tenant issues  

1.2 Response timeline
---------------------
SEV‑1: 5 minutes  
SEV‑2: 15 minutes  
SEV‑3: 1 hour  

1.3 Initial triage
------------------
1. Identify failing subsystem  
2. Capture logs  
3. Capture ExceptionState  
4. Capture ProgramRegistry version  
5. Capture build hash  

1.4 Stabilization
-----------------
- Stop affected workloads  
- Disable new tenant traffic  
- Switch to read‑only mode if needed  

------------------------------------------------------------
SECTION 2 — PRODUCTION DEBUGGING PROCEDURES
------------------------------------------------------------

2.1 Required artifacts
----------------------
- Logs  
- Metrics  
- Traces  
- StorageBlock snapshots  
- FileManager state  
- ProgramRegistry  

2.2 Debugging ExecutionContext
------------------------------
Inspect:
- PERFORM stack  
- CALL stack  
- Declarative history  
- ExceptionState  

2.3 Debugging StorageBlocks
---------------------------
Dump:
- Raw bytes  
- Decoded fields  
- OCCURS tables  
- REDEFINES overlays  

2.4 Debugging FileManager
-------------------------
Inspect:
- File status codes  
- Cursor positions  
- B‑tree node structure  
- Record counts  

------------------------------------------------------------
SECTION 3 — EXCEPTIONSTATE TRIAGE
------------------------------------------------------------

3.1 Exception categories
------------------------
- FILE‑ERROR  
- JSON‑ERROR  
- XML‑ERROR  
- SORT‑ERROR  
- REPORT‑ERROR  
- RUNTIME‑ERROR  
- USER‑ERROR  

3.2 Required fields
-------------------
ExceptionState includes:
- Category  
- Message  
- Source subsystem  
- File name  
- Key value  
- JSON/XML token  

3.3 Triage flow
---------------
1. Identify category  
2. Identify subsystem  
3. Identify failing record  
4. Identify failing field  
5. Identify failing statement  

------------------------------------------------------------
SECTION 4 — FILE I/O CORRUPTION RECOVERY
------------------------------------------------------------

4.1 Sequential file corruption
------------------------------
Symptoms:
- Unexpected EOF  
- Invalid record length  

Recovery:
1. Dump file  
2. Identify corrupt record  
3. Remove or repair  
4. Re‑run batch  

4.2 Indexed file corruption
---------------------------
Symptoms:
- INVALID KEY on valid keys  
- B‑tree imbalance  
- Missing records  

Recovery:
1. Dump B‑tree  
2. Validate node ordering  
3. Rebuild index (SORT → GIVING)  
4. Replace file atomically  

4.3 Relative file corruption
----------------------------
Symptoms:
- Missing RRNs  
- Invalid RRNs  

Recovery:
1. Scan RRNs  
2. Identify gaps  
3. Rebuild file  

------------------------------------------------------------
SECTION 5 — INDEXED FILE REBUILD PROCEDURES
------------------------------------------------------------

5.1 Full rebuild
----------------
SORT TEMP  
    ON ASCENDING KEY KEYFIELD  
    USING ORIGINAL  
    GIVING REBUILT.

5.2 Partial rebuild
-------------------
1. Extract damaged range  
2. Rebuild range  
3. Merge into main file  

5.3 Validation
--------------
- Key ordering  
- Duplicate detection  
- Record count  

------------------------------------------------------------
SECTION 6 — PERFORMANCE DEGRADATION TRIAGE
------------------------------------------------------------

6.1 Symptoms
------------
- Increased latency  
- Slow SORT  
- Slow JSON/XML parsing  
- File I/O bottlenecks  

6.2 Root causes
---------------
- Large OCCURS tables  
- Deep recursion  
- Large JSON/XML documents  
- Fragmented indexed files  

6.3 Remediation
---------------
- Rebuild indexed files  
- Flatten JSON/XML  
- Replace recursion with PERFORM  
- Increase SORT buffer  

------------------------------------------------------------
SECTION 7 — MEMORY LEAK INVESTIGATION
------------------------------------------------------------

7.1 Symptoms
------------
- Increasing memory usage  
- WASM out‑of‑memory  
- AOT memory pressure  

7.2 Root causes
---------------
- Unbounded OCCURS  
- Large NATIONAL fields  
- Large JSON/XML documents  
- ObjectTable growth  

7.3 Remediation
---------------
- Cap OCCURS  
- Use DISPLAY instead of NATIONAL  
- Stream JSON/XML  
- Clear ObjectTable references  

------------------------------------------------------------
SECTION 8 — MULTI‑TENANT INCIDENTS
------------------------------------------------------------

8.1 Tenant isolation failure
----------------------------
Symptoms:
- Cross‑tenant data leakage  
- Shared FileManager state  

Remediation:
1. Identify shared resource  
2. Reset tenant contexts  
3. Rebuild tenant file roots  

8.2 Tenant quota violation
--------------------------
Symptoms:
- Excessive CPU  
- Excessive memory  
- Excessive file handles  

Remediation:
- Throttle tenant  
- Enforce quotas  
- Migrate tenant to dedicated node  

------------------------------------------------------------
SECTION 9 — AOT PRODUCTION INCIDENTS
------------------------------------------------------------

9.1 Native crash
----------------
Symptoms:
- SIGSEGV  
- Access violation  

Remediation:
1. Capture crash dump  
2. Validate trimming  
3. Validate ENTRY points  
4. Rebuild with trimming disabled  

9.2 Missing method
------------------
Symptoms:
- ENTRY not found  

Remediation:
- Add DynamicDependency attribute  
- Rebuild  

------------------------------------------------------------
SECTION 10 — WASM PRODUCTION INCIDENTS
------------------------------------------------------------

10.1 Out‑of‑memory
------------------
Remediation:
- Reduce JSON/XML size  
- Reduce OCCURS  
- Increase WASM memory  

10.2 Host interop failure
-------------------------
Remediation:
- Validate JS bridge  
- Validate console/timer bindings  

------------------------------------------------------------
SECTION 11 — ROLLBACK & RECOVERY
------------------------------------------------------------

11.1 Rollback triggers
----------------------
- Data corruption  
- Performance regression  
- Incorrect ProgramRegistry  

11.2 Rollback procedure
-----------------------
1. Stop traffic  
2. Restore previous build  
3. Restore previous ProgramRegistry  
4. Restore previous file roots  
5. Validate  
6. Resume traffic  

11.3 Recovery validation
------------------------
- File integrity  
- ProgramRegistry integrity  
- Log consistency  

------------------------------------------------------------
SECTION 12 — SAFE HOTFIX & REDEPLOY
------------------------------------------------------------

12.1 Hotfix rules
-----------------
Hotfix must:
- Not change data layout  
- Not change ProgramRegistry  
- Not change CALL/ENTRY signatures  

12.2 Hotfix deployment
----------------------
1. Build hotfix  
2. Run unit tests  
3. Run snapshot tests  
4. Deploy to staging  
5. Deploy to production  

------------------------------------------------------------
Summary
------------------------------------------------------------
The CobolSharp Operational Runbook:
- Defines incident response, debugging, and recovery procedures
- Provides deterministic, safe, reproducible operational workflows
- Covers FileManager, JSON/XML, SORT, REPORT, AOT, WASM, and multi‑tenant incidents
- Ensures stable, predictable production operations for CobolSharp workloads
