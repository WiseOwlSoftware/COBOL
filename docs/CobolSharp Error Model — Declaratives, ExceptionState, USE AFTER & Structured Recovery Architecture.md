CobolSharp Error Model — Declaratives, ExceptionState, USE AFTER & Structured Recovery Architecture (CIL‑Only)
=============================================================================================================

Purpose
-------
Define the authoritative architecture for:
- Declaratives (USE AFTER ERROR/EXCEPTION)
- ExceptionState propagation
- ON EXCEPTION / ON SIZE ERROR / ON OVERFLOW
- File I/O error routing
- JSON/XML error routing
- SORT error routing
- Standard exception handling
- Structured recovery and resumption
- CIL‑friendly lowering
- AOT/WASM‑safe error semantics

This document governs how CobolSharp implements COBOL’s structured error‑handling model.

------------------------------------------------------------
SECTION 1 — ERROR MODEL OVERVIEW
------------------------------------------------------------

CobolSharp error handling consists of:
1. **Statement‑level handlers**  
   - ON EXCEPTION  
   - ON SIZE ERROR  
   - ON OVERFLOW  

2. **Declaratives**  
   - USE AFTER ERROR ON file  
   - USE AFTER EXCEPTION ON json/xml  
   - USE AFTER STANDARD EXCEPTION  

3. **ExceptionState**  
   - Centralized error record  
   - Populated by subsystems  

4. **Structured recovery**  
   - Declarative runs  
   - Control returns to statement after failing statement  
   - PERFORM stack preserved  

5. **No unwinding unless fatal**  
   - Declaratives do not unwind CALL stack  
   - Only STOP RUN terminates execution  

------------------------------------------------------------
SECTION 2 — EXCEPTIONSTATE ARCHITECTURE
------------------------------------------------------------

2.1 Fields
----------
ExceptionState contains:
- Category  
  (SIZE ERROR, OVERFLOW, I/O ERROR, JSON ERROR, XML ERROR, SORT ERROR, .NET EXCEPTION)  
- Message  
- Source subsystem  
- File name (I/O)  
- Key value (indexed)  
- JSON/XML token  
- Stack trace (optional)  
- Severity  

2.2 Lifecycle
-------------
ExceptionState is:
- Populated on error  
- Passed to ON EXCEPTION or declarative  
- Cleared after handler completes  

2.3 Severity levels
-------------------
- Recoverable  
- Non‑recoverable  
- Fatal  

Only fatal errors terminate execution.

------------------------------------------------------------
SECTION 3 — STATEMENT‑LEVEL HANDLERS
------------------------------------------------------------

3.1 ON EXCEPTION
----------------
Applies to:
- CALL
- INVOKE
- JSON/XML GENERATE
- SORT
- STRING/UNSTRING
- File I/O

Behavior:
- If error occurs → execute ON EXCEPTION block  
- Skip NOT ON EXCEPTION block  

3.2 ON SIZE ERROR
-----------------
Applies to:
- Arithmetic  
- MOVE to numeric  
- COMP/COMP‑3/COMP‑5 overflow  

Behavior:
- Target not modified  
- ON SIZE ERROR block executed  

3.3 ON OVERFLOW
---------------
Applies to:
- MOVE  
- STRING  
- UNSTRING  

Behavior:
- Target not modified  
- ON OVERFLOW block executed  

------------------------------------------------------------
SECTION 4 — DECLARATIVES ARCHITECTURE
------------------------------------------------------------

4.1 Declarative structure
-------------------------
DECLARATIVES.
    USE AFTER ERROR ON fileName.
        Procedure‑1.
    USE AFTER EXCEPTION ON JSON.
        Procedure‑2.
END DECLARATIVES.

4.2 Declarative types
---------------------
- USE AFTER ERROR ON file  
- USE AFTER EXCEPTION ON JSON  
- USE AFTER EXCEPTION ON XML  
- USE AFTER STANDARD EXCEPTION  

4.3 Triggering rules
--------------------
Declarative triggered when:
- Statement‑level handler absent  
- Or statement‑level handler does not handle category  

4.4 Execution model
-------------------
- Declarative runs like a PERFORM  
- After completion → return to statement after failing statement  
- PERFORM stack preserved  
- CALL stack preserved  

4.5 Declarative priority
------------------------
1. File‑specific declarative  
2. JSON/XML declarative  
3. Standard exception declarative  

------------------------------------------------------------
SECTION 5 — FILE I/O ERROR ROUTING
------------------------------------------------------------

5.1 File status codes
---------------------
Examples:
- "10" end of file  
- "21" key invalid  
- "22" duplicate key  
- "23" key not found  
- "30" permanent error  
- "35" file not found  
- "92" logic error  

5.2 Routing
-----------
If READ/WRITE/REWRITE/DELETE fails:
1. If ON EXCEPTION present → run it  
2. Else if USE AFTER ERROR ON file present → run declarative  
3. Else if USE AFTER STANDARD EXCEPTION present → run declarative  
4. Else → continue with ExceptionState set  

------------------------------------------------------------
SECTION 6 — JSON/XML ERROR ROUTING
------------------------------------------------------------

6.1 JSON errors
---------------
- Invalid token  
- Unexpected type  
- Overflow  
- Missing field  
- Encoding error  

6.2 XML errors
--------------
- Invalid element  
- Namespace mismatch  
- Attribute error  
- Encoding error  

6.3 Routing
-----------
1. ON EXCEPTION  
2. USE AFTER EXCEPTION ON JSON/XML  
3. USE AFTER STANDARD EXCEPTION  

------------------------------------------------------------
SECTION 7 — SORT ERROR ROUTING
------------------------------------------------------------

7.1 SORT errors
---------------
- File I/O error  
- Key extraction error  
- Collation error  
- Merge failure  

7.2 Routing
-----------
1. ON EXCEPTION  
2. USE AFTER STANDARD EXCEPTION  

------------------------------------------------------------
SECTION 8 — STANDARD EXCEPTION HANDLING
------------------------------------------------------------

8.1 Triggered by:
-----------------
- .NET exceptions  
- Runtime errors  
- Invalid PIC  
- Invalid COMP‑3 nibble  
- DISPLAY → NATIONAL non‑ASCII  
- NATIONAL truncation of surrogate pair  
- Object reference null  
- INVOKE failure  

8.2 Routing
-----------
1. ON EXCEPTION  
2. USE AFTER STANDARD EXCEPTION  

------------------------------------------------------------
SECTION 9 — STRUCTURED RECOVERY MODEL
------------------------------------------------------------

9.1 Declarative execution
-------------------------
Declarative runs:
- As a PERFORM  
- With its own PERFORM frame  
- Without unwinding caller  

9.2 Resumption
--------------
After declarative:
- Execution resumes at statement after failing statement  
- ExceptionState cleared  

9.3 No retry semantics
----------------------
Declaratives do not retry failing statement.

9.4 No unwinding
----------------
Declaratives do not unwind:
- CALL stack  
- PERFORM stack  

------------------------------------------------------------
SECTION 10 — CIL LOWERING RULES
------------------------------------------------------------

10.1 Statement‑level handler lowering
-------------------------------------
Compiler generates:
try {
    // operation
} catch {
    ExceptionState = ...
    goto onExceptionBlock
}

10.2 Declarative lowering
-------------------------
Compiler generates:
- Declarative table  
- Dispatch logic  
- PERFORM call to declarative  

10.3 Resumption lowering
------------------------
Compiler inserts:
- Branch to resume label  

------------------------------------------------------------
SECTION 11 — DEBUGGER INTEGRATION
------------------------------------------------------------

Debugger shows:
- ExceptionState  
- Declarative invoked  
- Statement that failed  
- Resume location  
- File status codes  
- JSON/XML token  

------------------------------------------------------------
SECTION 12 — AOT/WASM‑SAFE ERROR MODEL
------------------------------------------------------------

12.1 No reflection
------------------
Exception types mapped statically.

12.2 No dynamic codegen
-----------------------
All handlers compiled statically.

12.3 Deterministic behavior
---------------------------
Error routing identical across platforms.

------------------------------------------------------------
SECTION 13 — EDGE‑CASE BEHAVIOR
------------------------------------------------------------

13.1 Declarative triggers inside declarative
--------------------------------------------
Allowed; nested declaratives run.

13.2 ON EXCEPTION inside declarative
------------------------------------
Allowed.

13.3 Declarative modifies failing record
----------------------------------------
Allowed.

13.4 Declarative triggers STOP RUN
----------------------------------
Terminates program.

13.5 Multiple declaratives match
--------------------------------
Highest‑priority declarative runs.

------------------------------------------------------------
Summary
------------------------------------------------------------
The CobolSharp Error Model:
- Implements full COBOL declaratives and structured recovery
- Provides deterministic ExceptionState routing
- Supports file, JSON, XML, SORT, and standard exceptions
- Ensures safe, resumable error handling without stack unwinding
- Generates clean, verifiable, debugger‑friendly CIL
- Ensures correctness across CoreCLR, AOT, and WASM
