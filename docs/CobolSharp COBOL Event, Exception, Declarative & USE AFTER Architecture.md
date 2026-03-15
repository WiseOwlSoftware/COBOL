CobolSharp COBOL Event, Exception, Declarative & USE AFTER Architecture (CIL‑Only)
=================================================================================

Purpose
-------
Define the authoritative architecture for:
- Declarative sections
- USE AFTER ERROR / USE AFTER EXCEPTION / USE AFTER STANDARD EXCEPTION
- File I/O exception routing
- JSON/XML exception routing
- SIZE ERROR and ON EXCEPTION integration
- Declarative scoping rules
- Declarative re‑entry and re‑execution rules
- ExceptionState lifecycle
- CIL‑friendly lowering
- AOT/WASM‑safe exception handling

This document governs how CobolSharp implements COBOL’s declarative and exception‑handling model on .NET.

------------------------------------------------------------
SECTION 1 — DECLARATIVE OVERVIEW
------------------------------------------------------------

Declaratives are special PROCEDURE DIVISION sections that:
- Are invoked automatically when certain events occur
- Run outside normal control flow
- Resume execution after the failing statement (unless GO TO used)
- Have access to full program state

CobolSharp supports:
- USE AFTER ERROR ON file‑name
- USE AFTER EXCEPTION ON file‑name
- USE AFTER STANDARD EXCEPTION
- USE AFTER EXCEPTION ON JSON/XML PARSE/GENERATE
- USE AFTER EXCEPTION ON REPORT WRITER

------------------------------------------------------------
SECTION 2 — DECLARATIVE STRUCTURE
------------------------------------------------------------

2.1 Syntax
----------
DECLARATIVES.
    Section‑Name SECTION.
        USE AFTER ERROR ON file‑name.
        ...
END DECLARATIVES.

2.2 Declarative sections are:
- Parsed before main PROCEDURE DIVISION
- Registered in ExecutionContext
- Not executed unless triggered

2.3 Declarative types
---------------------
CobolSharp supports:

1. File error declaratives  
2. File exception declaratives  
3. Standard exception declaratives  
4. JSON/XML exception declaratives  
5. Report Writer exception declaratives  

------------------------------------------------------------
SECTION 3 — EXCEPTIONSTATE MODEL
------------------------------------------------------------

ExecutionContext contains:
ExceptionState:
- Category
- File name (if applicable)
- Error code
- Message
- Raw .NET exception (optional)
- JSON/XML error metadata
- Report Writer error metadata

ExceptionState is:
- Set by runtime engines
- Cleared after declarative completes
- Cleared after ON EXCEPTION block completes

------------------------------------------------------------
SECTION 4 — FILE ERROR & FILE EXCEPTION DECLARATIVES
------------------------------------------------------------

4.1 USE AFTER ERROR ON file‑name
--------------------------------
Triggered by:
- OPEN error
- READ error
- WRITE error
- REWRITE error
- DELETE error
- START error

4.2 USE AFTER EXCEPTION ON file‑name
------------------------------------
Triggered by:
- INVALID KEY
- AT END
- File status exceptions

4.3 Routing rules
-----------------
If a file operation fails:
1. Check ON EXCEPTION / INVALID KEY / AT END in statement  
2. If none, check file‑specific declarative  
3. If none, check USE AFTER STANDARD EXCEPTION  
4. If none, propagate runtime exception  

4.4 Declarative execution
-------------------------
Declarative runs:
- With full access to program state
- With ExceptionState populated
- With ability to GO TO anywhere

------------------------------------------------------------
SECTION 5 — STANDARD EXCEPTION DECLARATIVES
------------------------------------------------------------

5.1 USE AFTER STANDARD EXCEPTION
--------------------------------
Triggered by:
- SIZE ERROR not handled by ON SIZE ERROR
- Numeric overflow
- Divide by zero
- Invalid packed decimal
- Invalid UTF‑8/UTF‑16
- JSON/XML exceptions (if no specific declarative)
- Report Writer exceptions

5.2 Behavior
------------
Declarative runs:
- After failing statement
- Before next statement
- With ExceptionState populated

------------------------------------------------------------
SECTION 6 — JSON/XML DECLARATIVES
------------------------------------------------------------

6.1 USE AFTER EXCEPTION ON JSON
-------------------------------
Triggered by:
- JSON PARSE errors
- JSON GENERATE errors

6.2 USE AFTER EXCEPTION ON XML
------------------------------
Triggered by:
- XML PARSE errors
- XML GENERATE errors

6.3 Routing priority
--------------------
1. ON EXCEPTION in statement  
2. JSON/XML declarative  
3. USE AFTER STANDARD EXCEPTION  

------------------------------------------------------------
SECTION 7 — REPORT WRITER DECLARATIVES
------------------------------------------------------------

7.1 USE AFTER EXCEPTION ON REPORT
---------------------------------
Triggered by:
- Page overflow errors
- Invalid LINE/COLUMN
- Invalid control break state
- Output file errors

7.2 Routing priority
--------------------
Same as JSON/XML.

------------------------------------------------------------
SECTION 8 — DECLARATIVE EXECUTION MODEL
------------------------------------------------------------

8.1 Entry
---------
When triggered:
- Save current execution point
- Jump to declarative section entry
- Execute declarative
- Return to statement after failing statement (unless GO TO used)

8.2 Re‑entry rules
------------------
Declaratives are:
- Re‑entrant
- May be triggered multiple times
- May be nested (rare but allowed)

8.3 GO TO inside declarative
----------------------------
Allowed:
- Transfers control permanently
- Skips return to failing statement

8.4 PERFORM inside declarative
------------------------------
Allowed:
- Uses separate PERFORM stack frame

------------------------------------------------------------
SECTION 9 — CIL LOWERING RULES
------------------------------------------------------------

9.1 Exception detection
-----------------------
Generated IL wraps:
- File operations
- Numeric operations
- JSON/XML operations
- Report Writer operations

In:
try/catch blocks

9.2 Declarative dispatch
------------------------
On exception:
- Populate ExceptionState
- Call DeclarativeDispatcher.Dispatch(ctx)
- Dispatcher selects appropriate declarative
- Branch to declarative entry

9.3 Return from declarative
---------------------------
If no GO TO:
- Branch to continuation label after failing statement

9.4 Clearing ExceptionState
---------------------------
After declarative:
ctx.ExceptionState.Clear()

------------------------------------------------------------
SECTION 10 — DEBUGGER INTEGRATION
------------------------------------------------------------

Debugger shows:
- Declarative entry
- ExceptionState contents
- File status
- JSON/XML error details
- Report Writer error details
- Return to failing statement
- GO TO transitions

------------------------------------------------------------
SECTION 11 — EDGE‑CASE BEHAVIOR
------------------------------------------------------------

11.1 Declarative inside declarative
-----------------------------------
Allowed; nested exceptions handled.

11.2 Declarative triggered during cleanup
-----------------------------------------
Ignored.

11.3 Declarative with EXIT PROGRAM
----------------------------------
Terminates program normally.

11.4 Declarative with STOP RUN
------------------------------
Terminates entire runtime.

11.5 Declarative with PERFORM VARYING
-------------------------------------
Allowed; PERFORM stack isolated.

11.6 Declarative triggered by declarative
-----------------------------------------
Allowed; recursion depth limited by runtime.

------------------------------------------------------------
Summary
------------------------------------------------------------
The CobolSharp Declarative & Exception Architecture:
- Implements full COBOL USE AFTER ERROR/EXCEPTION semantics
- Provides deterministic exception routing across file, JSON/XML, numeric, and report operations
- Uses ExceptionState for structured error metadata
- Integrates tightly with ExecutionContext and runtime engines
- Generates clean, verifiable, debugger‑friendly CIL
- Ensures correctness across CoreCLR, AOT, and WASM
