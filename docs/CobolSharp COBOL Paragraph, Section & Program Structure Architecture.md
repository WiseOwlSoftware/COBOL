CobolSharp COBOL Paragraph, Section & Program Structure Architecture (CIL‑Only)
=============================================================================

Purpose
-------
Define the authoritative architecture for:
- Paragraph and section structure
- Program and nested program structure
- Declaratives
- Fall‑through semantics
- ENTRY points
- CALL and GOBACK behavior
- EXIT PROGRAM / EXIT PARAGRAPH / EXIT SECTION
- Structured lowering to CIL
- Integration with PERFORM, GO TO, and the runtime execution model

This document governs how CobolSharp represents and executes COBOL program structure on .NET.

------------------------------------------------------------
SECTION 1 — PROGRAM STRUCTURE OVERVIEW
------------------------------------------------------------

A COBOL program consists of:
- IDENTIFICATION DIVISION
- ENVIRONMENT DIVISION
- DATA DIVISION
- PROCEDURE DIVISION

CobolSharp maps each program to:
- A generated .NET class
- A static entry method (Main‑like)
- A set of CIL methods or basic blocks representing paragraphs/sections

Programs may contain:
- Nested programs
- Declaratives
- ENTRY points
- Sections and paragraphs

------------------------------------------------------------
SECTION 2 — PARAGRAPHS
------------------------------------------------------------

2.1 Definition
--------------
A paragraph is:
- A named block of statements
- Introduced by a label ending with a period
- May fall through to the next paragraph

Example:
Para-A.
    DISPLAY "A".
Para-B.
    DISPLAY "B".

2.2 Fall‑through semantics
--------------------------
If a paragraph ends without:
- EXIT PARAGRAPH
- GO TO
- PERFORM
- STOP RUN
- GOBACK

Then execution continues into the next paragraph.

CobolSharp preserves this behavior exactly.

2.3 Lowering
------------
Each paragraph becomes:
- A CIL basic block with a unique label
- Optionally a separate CIL method (configurable)

2.4 Paragraph entry points
--------------------------
Used for:
- PERFORM
- GO TO
- Debugger stepping

------------------------------------------------------------
SECTION 3 — SECTIONS
------------------------------------------------------------

3.1 Definition
--------------
A section:
- Contains one or more paragraphs
- Has a name ending with the word SECTION
- Executes paragraphs in order

3.2 Fall‑through
----------------
If a section ends without explicit transfer:
- Execution continues to the next section

3.3 Lowering
------------
Sections become:
- A label for section entry
- Paragraph labels inside
- Fall‑through preserved

3.4 PERFORM section
-------------------
PERFORM Section‑A:
- Executes all paragraphs in Section‑A
- Stops at end of section

------------------------------------------------------------
SECTION 4 — DECLARATIVES
------------------------------------------------------------

4.1 Purpose
-----------
Declaratives handle:
- File I/O exceptions
- USE BEFORE REPORTING (planned)
- USE AFTER STANDARD ERROR
- USE AFTER STANDARD EXCEPTION

4.2 Structure
-------------
DECLARATIVES.
    Section‑A SECTION.
        USE AFTER STANDARD EXCEPTION.
        ...
END DECLARATIVES.

4.3 Execution model
-------------------
Declaratives:
- Execute only when triggered
- Do not participate in normal control flow
- Cannot be PERFORMed
- Cannot be GO TO targets

4.4 Lowering
------------
Declaratives become:
- Separate CIL methods
- Registered as exception handlers in ExecutionContext

------------------------------------------------------------
SECTION 5 — ENTRY POINTS
------------------------------------------------------------

5.1 ENTRY statement
-------------------
ENTRY "name" USING parameters.

Allows:
- Multiple entry points into a program
- Alternate calling conventions

5.2 Lowering
------------
CobolSharp generates:
- A .NET method for each ENTRY
- Marshaling logic for USING parameters
- Shared ExecutionContext

------------------------------------------------------------
SECTION 6 — CALL AND GOBACK SEMANTICS
------------------------------------------------------------

6.1 CALL program
----------------
CALL "PROG" USING args:
- Creates new ExecutionContext
- Allocates LOCAL‑STORAGE
- Shares WORKING‑STORAGE if COMMON
- Maps LINKAGE SECTION to caller

6.2 GOBACK
----------
GOBACK:
- Returns from program or method
- If in main program → terminates program

Lowering:
- Return from generated .NET method

6.3 EXIT PROGRAM
----------------
EXIT PROGRAM:
- Terminates current program
- Returns control to caller
- Unwinds PERFORM stack

------------------------------------------------------------
SECTION 7 — EXIT PARAGRAPH / EXIT SECTION
------------------------------------------------------------

7.1 EXIT PARAGRAPH
------------------
- Immediately exits current paragraph
- Transfers control to next paragraph or caller

7.2 EXIT SECTION
----------------
- Immediately exits current section
- Transfers control to next section or caller

7.3 Lowering
------------
EXIT PARAGRAPH → branch to paragraph end label  
EXIT SECTION → branch to section end label  

------------------------------------------------------------
SECTION 8 — NESTED PROGRAMS
------------------------------------------------------------

8.1 Structure
-------------
A program may contain nested programs:

PROGRAM-ID. Outer.
    ...
    PROGRAM-ID. Inner.
        ...
    END PROGRAM Inner.
END PROGRAM Outer.

8.2 Visibility rules
--------------------
- Inner programs may access outer WORKING‑STORAGE
- Outer programs cannot access inner data
- CALL "Inner" allowed

8.3 Lowering
------------
Each nested program becomes:
- A nested .NET class
- With its own ExecutionContext

------------------------------------------------------------
SECTION 9 — GO TO SEMANTICS
------------------------------------------------------------

9.1 GO TO paragraph
-------------------
GO TO Para-A:
- Unconditional branch
- May cross paragraph boundaries

9.2 GO TO section
-----------------
GO TO Section-A:
- Branches to first paragraph of section

9.3 GO TO DEPENDING ON
-----------------------
Lowered to:
- switch instruction
- Branch to appropriate label

9.4 PERFORM interactions
------------------------
If GO TO exits a PERFORM range:
- PERFORM stack unwound

------------------------------------------------------------
SECTION 10 — CIL LOWERING RULES
------------------------------------------------------------

10.1 Paragraph lowering
-----------------------
Paragraph → CIL label or method

10.2 Section lowering
---------------------
Section → label + paragraph labels

10.3 Program lowering
---------------------
Program → .NET class with:
- Entry method
- Paragraph/section methods
- Declarative handlers

10.4 Control flow lowering
--------------------------
IF → structured branch  
EVALUATE → switch or if‑chain  
PERFORM → structured loop or block  
GO TO → br instruction  

------------------------------------------------------------
SECTION 11 — DEBUGGER INTEGRATION
------------------------------------------------------------

Debugger shows:
- Current paragraph
- Current section
- Call stack with paragraph/section names
- PERFORM stack
- ENTRY point used
- EXIT PARAGRAPH/SECTION transitions

Sequence points emitted for:
- Paragraph entry
- Section entry
- ENTRY points
- EXIT statements

------------------------------------------------------------
SECTION 12 — EDGE‑CASE BEHAVIOR
------------------------------------------------------------

12.1 Paragraph with no statements
---------------------------------
Legal; acts as a label

12.2 Section with no paragraphs
-------------------------------
Illegal; compile‑time error

12.3 GO TO into middle of PERFORM
---------------------------------
Allowed; PERFORM frame remains active

12.4 GO TO out of nested PERFORMs
---------------------------------
All exited frames popped

12.5 EXIT PROGRAM inside nested PERFORM
---------------------------------------
Unwinds entire PERFORM stack

12.6 ENTRY with no USING
------------------------
Allowed; no parameters

------------------------------------------------------------
Summary
------------------------------------------------------------
The CobolSharp Paragraph, Section & Program Structure Architecture:
- Implements full COBOL program structure semantics
- Preserves fall‑through, ENTRY, EXIT, and nested program behavior
- Generates clean, structured CIL for paragraphs and sections
- Integrates deeply with PERFORM, GO TO, and the runtime
- Provides rich debugging metadata for paragraphs and sections
- Ensures correctness across CoreCLR, AOT, and WASM
