CobolSharp COBOL Report Writer, Page/Line Control & Control‑Break Architecture (CIL‑Only)
=========================================================================================

Purpose
-------
Define the authoritative architecture for:
- REPORT SECTION
- RD (Report Description)
- Page/line control
- CONTROL FOOTING / CONTROL HEADING
- DETAIL, SUMMARY, PAGE HEADING, PAGE FOOTING
- LINE / COLUMN positioning
- SOURCE / VALUE clauses
- SUM, COUNT, AVERAGE, MIN, MAX
- Control‑break hierarchy
- Report groups and generation order
- Integration with ExecutionContext.ReportEngine
- AOT/WASM‑safe report generation
- CIL‑friendly lowering

This document governs how CobolSharp implements COBOL’s Report Writer facility on .NET.

------------------------------------------------------------
SECTION 1 — REPORTENGINE OVERVIEW
------------------------------------------------------------

ExecutionContext.ReportEngine provides:
- Page buffer management
- Line buffer management
- Control‑break detection
- Accumulator management
- Report group rendering
- Column positioning
- ExceptionState population

Report generation is:
- Deterministic
- Pure managed
- AOT/WASM‑safe

------------------------------------------------------------
SECTION 2 — REPORT SECTION STRUCTURE
------------------------------------------------------------

2.1 REPORT SECTION
------------------
Contains:
- RD reportName
- Report groups:
  - PAGE HEADING
  - PAGE FOOTING
  - CONTROL HEADING
  - CONTROL FOOTING
  - DETAIL
  - SUMMARY

2.2 RD (Report Description)
---------------------------
Defines:
- PAGE LIMIT
- HEADING
- FOOTING
- CONTROL fields
- LINE numbering
- COLUMN positioning

2.3 Report groups
-----------------
Each group contains:
- LINE n
- COLUMN m
- VALUE or SOURCE
- SUM/COUNT/etc.

------------------------------------------------------------
SECTION 3 — PAGE & LINE CONTROL
------------------------------------------------------------

3.1 Page buffer
---------------
Each page is:
- A list of lines
- Each line is a UTF‑16 buffer
- Fixed width (default 132 columns)

3.2 Line numbering
------------------
LINE n:
- Absolute line number on page
- If n < current line → new page

3.3 Automatic line advancing
----------------------------
After each DETAIL:
- Advance to next line
- If beyond PAGE LIMIT → new page

3.4 PAGE HEADING
----------------
Printed:
- At top of each page
- Before first DETAIL
- After each page break

3.5 PAGE FOOTING
----------------
Printed:
- At bottom of each page
- Before page break

------------------------------------------------------------
SECTION 4 — CONTROL‑BREAK ARCHITECTURE
------------------------------------------------------------

4.1 CONTROL fields
------------------
RD defines:
CONTROL IS field1 field2 field3.

Hierarchy:
- field1 = highest level
- field3 = lowest level

4.2 Control‑break detection
---------------------------
On each DETAIL:
- Compare current control fields to previous
- Highest‑level change triggers:
  - CONTROL FOOTING for all lower levels
  - CONTROL HEADING for all levels

4.3 CONTROL HEADING
-------------------
Printed:
- After control‑break
- Before DETAIL

4.4 CONTROL FOOTING
-------------------
Printed:
- Before control‑break
- After last DETAIL of group

------------------------------------------------------------
SECTION 5 — REPORT GROUP TYPES
------------------------------------------------------------

5.1 DETAIL
----------
Printed for each input record.

5.2 CONTROL HEADING
-------------------
Printed when control field changes.

5.3 CONTROL FOOTING
-------------------
Printed after finishing a control group.

5.4 PAGE HEADING
----------------
Printed at top of each page.

5.5 PAGE FOOTING
----------------
Printed at bottom of each page.

5.6 SUMMARY
-----------
Printed after all DETAILs.

------------------------------------------------------------
SECTION 6 — FIELD GENERATION RULES
------------------------------------------------------------

6.1 VALUE clause
----------------
VALUE "literal":
- Literal copied to line buffer

6.2 SOURCE clause
-----------------
SOURCE fieldName:
- Field value converted to DISPLAY
- Inserted at COLUMN position

6.3 COLUMN positioning
----------------------
COLUMN n:
- 1‑based index
- Overwrites existing characters

6.4 Multiple fields on same line
--------------------------------
Rendered in order of appearance.

------------------------------------------------------------
SECTION 7 — ACCUMULATORS
------------------------------------------------------------

7.1 SUM
-------
SUM fieldName:
- Adds field value to accumulator
- Reset at control‑break

7.2 COUNT
---------
COUNT fieldName:
- Increments counter
- Reset at control‑break

7.3 AVERAGE
-----------
AVERAGE fieldName:
- SUM / COUNT

7.4 MIN / MAX
-------------
Tracks:
- Minimum or maximum value
- Reset at control‑break

7.5 Accumulator lifetime
------------------------
- DETAIL level: reset each DETAIL
- CONTROL level: reset at control‑break
- SUMMARY level: reset at end of report

------------------------------------------------------------
SECTION 8 — REPORT GENERATION ORDER
------------------------------------------------------------

8.1 For each input record
-------------------------
1. Detect control‑break  
2. Print CONTROL FOOTING (if break)  
3. Print CONTROL HEADING (if break)  
4. Print DETAIL  
5. Update accumulators  

8.2 At end of input
-------------------
- Print CONTROL FOOTING for all levels
- Print SUMMARY
- Print PAGE FOOTING

------------------------------------------------------------
SECTION 9 — CIL LOWERING RULES
------------------------------------------------------------

9.1 Report group lowering
-------------------------
Compiler generates:
- A method per report group
- Code to render each LINE/COLUMN

9.2 DETAIL lowering
-------------------
Generated IL:
- Load fields
- Convert to DISPLAY
- Write into line buffer

9.3 CONTROL‑BREAK lowering
--------------------------
Compiler generates:
- Compare control fields
- Branch to break handlers

9.4 Accumulator lowering
------------------------
Compiler generates:
- Decimal locals for SUM/AVG
- Integer locals for COUNT
- Comparison logic for MIN/MAX

9.5 Page/line lowering
----------------------
Compiler generates:
- Check line number
- Trigger page break if needed

------------------------------------------------------------
SECTION 10 — DEBUGGER INTEGRATION
------------------------------------------------------------

Debugger shows:
- Current page buffer
- Current line
- Control fields (current/previous)
- Accumulator values
- Report group being rendered
- ExceptionState

------------------------------------------------------------
SECTION 11 — AOT/WASM‑SAFE REPORT GENERATION
------------------------------------------------------------

11.1 No dynamic codegen
-----------------------
All report logic static.

11.2 No unsafe code
-------------------
No pointers or stackalloc.

11.3 Deterministic output
-------------------------
Same results across platforms.

11.4 WASM output
----------------
ReportEngine writes:
- UTF‑16 strings
- Exportable via host environment

------------------------------------------------------------
SECTION 12 — EDGE‑CASE BEHAVIOR
------------------------------------------------------------

12.1 DETAIL on first record
---------------------------
Triggers:
- PAGE HEADING
- CONTROL HEADING

12.2 CONTROL field missing
--------------------------
Runtime error.

12.3 PAGE LIMIT too small
-------------------------
Minimum enforced = 5 lines.

12.4 COLUMN beyond line width
-----------------------------
Truncation.

12.5 VALUE literal too long
---------------------------
Truncation.

12.6 SUMMARY with no DETAIL
---------------------------
Printed with zero accumulators.

------------------------------------------------------------
Summary
------------------------------------------------------------
The CobolSharp Report Writer Architecture:
- Implements full COBOL REPORT SECTION semantics
- Supports page/line control, control‑breaks, accumulators, and group rendering
- Provides deterministic, structured report generation
- Integrates tightly with ExecutionContext.ReportEngine
- Generates clean, verifiable, debugger‑friendly CIL
- Ensures correctness across CoreCLR, AOT, and WASM
