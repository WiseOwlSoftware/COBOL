CobolSharp COBOL Declaratives, USE Statements & Exception Routing Architecture (CIL‑Only)
========================================================================================

Purpose
-------
Define the authoritative architecture for:
- DECLARATIVES blocks
- USE BEFORE/AFTER statements
- USE AFTER STANDARD EXCEPTION
- USE AFTER ERROR/EXCEPTION in file I/O
- Exception routing rules
- Declarative activation and scoping
- Integration with FileManager, ExecutionContext, and exception model
- CIL‑friendly lowering
- Debugger visibility and stepping

This document governs how CobolSharp implements COBOL’s declarative exception‑handling subsystem.

------------------------------------------------------------
SECTION 1 — DECLARATIVES OVERVIEW
------------------------------------------------------------

Declaratives provide:
- Structured exception handling for file I/O
- Program‑level exception routing
- Specialized handlers for STANDARD EXCEPTION
- A separate execution region outside normal control flow

Declaratives are defined as:

DECLARATIVES.
    Section‑A SECTION.
        USE AFTER STANDARD EXCEPTION.
        ...
END DECLARATIVES.

Declaratives:
- Do NOT execute during normal program flow
- Are invoked only when triggered by USE rules
- Cannot be PERFORMed
- Cannot be GO TO targets
- Cannot contain ENTRY statements

------------------------------------------------------------
SECTION 2 — DECLARATIVE SECTIONS
------------------------------------------------------------

2.1 Structure
-------------
Each declarative section contains:
- A SECTION header
- A USE statement
- One or more paragraphs

2.2 Lowering
------------
Each declarative section becomes:
- A separate CIL method
- Registered in ExecutionContext.DeclarativeHandlers

2.3 Activation
--------------
Declaratives activate when:
- A matching USE condition occurs
- No local handler (INVALID KEY, AT END, SIZE ERROR, ON EXCEPTION) applies

------------------------------------------------------------
SECTION 3 — USE STATEMENT SEMANTICS
------------------------------------------------------------

CobolSharp supports the following USE forms:

3.1 USE AFTER STANDARD EXCEPTION
--------------------------------
Triggered by:
- Any unhandled runtime exception
- JSON/XML exceptions
- Numeric exceptions not caught by SIZE ERROR
- File I/O exceptions not caught by INVALID KEY or AT END

3.2 USE AFTER ERROR
-------------------
Triggered by:
- File I/O errors (status 30, 34, 90, etc.)
- Permanent or system errors

3.3 USE AFTER EXCEPTION
-----------------------
Triggered by:
- Any file I/O exception
- Includes INVALID KEY and AT END unless locally handled

3.4 USE BEFORE REPORTING (planned)
----------------------------------
Not yet implemented.

------------------------------------------------------------
SECTION 4 — FILE I/O EXCEPTION ROUTING
------------------------------------------------------------

4.1 Local handlers take precedence
----------------------------------
If a statement includes:
- INVALID KEY
- AT END
- SIZE ERROR
- ON EXCEPTION

Then declaratives are NOT invoked.

4.2 Declaratives invoked only if:
---------------------------------
- No local handler exists
- FileManager returns an error code
- Exception is not handled by ON EXCEPTION

4.3 Routing order
-----------------
1. INVALID KEY / AT END (local)
2. SIZE ERROR (local)
3. ON EXCEPTION (local)
4. USE AFTER EXCEPTION (declarative)
5. USE AFTER ERROR (declarative)
6. USE AFTER STANDARD EXCEPTION (declarative)
7. Runtime exception propagation

------------------------------------------------------------
SECTION 5 — STANDARD EXCEPTION ROUTING
------------------------------------------------------------

5.1 Trigger conditions
----------------------
USE AFTER STANDARD EXCEPTION triggers on:
- Null reference
- Out‑of‑range index
- Arithmetic overflow not caught by SIZE ERROR
- JSON/XML parse errors
- Any .NET exception not mapped to a COBOL exception

5.2 Lowering
------------
Backend emits:

try {
    operation
} catch (Exception ex) {
    ctx.ExceptionState = ...
    call DeclarativeHandler
}

5.3 Declarative handler behavior
--------------------------------
Handler:
- Executes declarative section
- Returns control to next statement after failing operation
- Does NOT re‑execute the failing statement

------------------------------------------------------------
SECTION 6 — DECLARATIVE ACTIVATION RULES
------------------------------------------------------------

6.1 Declaratives are global
---------------------------
Declaratives apply to:
- Entire program
- All nested paragraphs/sections
- All file operations
- All JSON/XML operations

6.2 Declaratives cannot be nested
---------------------------------
Only one DECLARATIVES block allowed per program.

6.3 Declaratives cannot call each other
---------------------------------------
If multiple USE statements match:
- Most specific handler executes
- Others ignored

6.4 Declaratives cannot PERFORM normal paragraphs
-------------------------------------------------
They may PERFORM paragraphs inside the declarative section only.

------------------------------------------------------------
SECTION 7 — EXECUTION MODEL
------------------------------------------------------------

7.1 When a declarative is triggered
-----------------------------------
ExecutionContext:
- Saves current call stack
- Saves current PERFORM stack
- Switches to declarative handler
- Executes declarative section
- Restores call stack and PERFORM stack
- Continues after failing statement

7.2 Declaratives do NOT unwind PERFORM stack
--------------------------------------------
Unlike EXIT PROGRAM or STOP RUN.

7.3 Declaratives do NOT change paragraph flow
---------------------------------------------
Execution resumes at the next statement.

------------------------------------------------------------
SECTION 8 — CIL LOWERING RULES
------------------------------------------------------------

8.1 Declarative registration
----------------------------
At program startup:
ctx.RegisterDeclarativeHandler(type, method)

8.2 Exception routing
---------------------
Backend emits:
- try/catch around file operations
- try/catch around JSON/XML operations
- try/catch around arithmetic operations (if needed)

8.3 Handler invocation
----------------------
call DeclarativeHandler(ctx)

8.4 No re‑execution of failing statement
----------------------------------------
Backend ensures:
- Handler returns to next instruction
- Not to failing instruction

------------------------------------------------------------
SECTION 9 — DEBUGGER INTEGRATION
------------------------------------------------------------

Debugger shows:
- Declarative section name
- USE condition that triggered handler
- ExceptionState contents
- File status (if applicable)
- Call stack before/after handler
- PERFORM stack before/after handler

Sequence points emitted for:
- Declarative entry
- Declarative exit
- USE statement

------------------------------------------------------------
SECTION 10 — EDGE‑CASE BEHAVIOR
------------------------------------------------------------

10.1 Multiple USE statements match
----------------------------------
Priority:
1. USE AFTER EXCEPTION
2. USE AFTER ERROR
3. USE AFTER STANDARD EXCEPTION

10.2 Declarative handler raises exception
-----------------------------------------
- Triggers STANDARD EXCEPTION declarative (if exists)
- Otherwise propagates to runtime

10.3 Declaratives inside nested programs
----------------------------------------
- Apply only to that program
- Do not affect outer program

10.4 File I/O inside declaratives
---------------------------------
- May trigger declaratives recursively
- CobolSharp prevents infinite recursion

10.5 STOP RUN inside declaratives
---------------------------------
- Terminates entire program

------------------------------------------------------------
Summary
------------------------------------------------------------
The CobolSharp Declaratives, USE Statements & Exception Routing Architecture:
- Implements full COBOL declarative semantics
- Provides structured exception routing for file I/O and runtime errors
- Integrates cleanly with ExecutionContext and FileManager
- Generates clean, verifiable, debugger‑friendly CIL
- Ensures deterministic behavior across CoreCLR, AOT, and WASM
