CobolSharp COBOL Report Writer Architecture (CIL‑Only)
=====================================================

Purpose
-------
Define the authoritative architecture for:
- REPORT SECTION and RD entries
- Report groups (TYPE PAGE/CONTROL/DETAIL/FOOTING)
- LINE/COLUMN positioning
- SUM/COUNT/AVERAGE/COMPUTE report fields
- CONTROL BREAK logic
- PAGE LIMIT and automatic page eject
- Report buffers and formatting
- Integration with FileManager and ExecutionContext
- CIL‑friendly lowering
- AOT/WASM‑safe report generation

This document governs how CobolSharp implements the COBOL Report Writer facility.

------------------------------------------------------------
SECTION 1 — REPORT WRITER OVERVIEW
------------------------------------------------------------

CobolSharp implements the full COBOL Report Writer model:
- Declarative REPORT SECTION
- RD (Report Description) entries
- Report groups with TYPE clauses
- Automatic control breaks
- Automatic page headings and footings
- Automatic line counting and page eject
- Aggregate fields (SUM, COUNT, AVERAGE)
- SOURCE and VALUE fields
- LINE/COLUMN positioning
- Multiple reports per program

Report Writer output is:
- A logical report buffer
- Emitted to a file or DISPLAY target
- Fully deterministic and formatting‑accurate

------------------------------------------------------------
SECTION 2 — REPORT SECTION STRUCTURE
------------------------------------------------------------

2.1 REPORT SECTION
------------------
REPORT SECTION.
RD ReportName.

2.2 Report groups
-----------------
Each report contains groups of TYPE:
- PAGE HEADING
- PAGE FOOTING
- CONTROL HEADING
- CONTROL FOOTING
- DETAIL
- REPORT HEADING
- REPORT FOOTING

2.3 Group hierarchy
-------------------
Groups may contain:
- Elementary report fields
- Nested groups
- SUM/COUNT/AVERAGE fields
- SOURCE fields
- VALUE fields

------------------------------------------------------------
SECTION 3 — REPORT GROUP TYPES
------------------------------------------------------------

3.1 PAGE HEADING
----------------
Printed at top of each page.

3.2 PAGE FOOTING
----------------
Printed at bottom of each page.

3.3 CONTROL HEADING
-------------------
Printed when control field changes.

3.4 CONTROL FOOTING
-------------------
Printed after last DETAIL for a control group.

3.5 DETAIL
----------
Printed for each input record.

3.6 REPORT HEADING / FOOTING
----------------------------
Printed once at start/end of report.

------------------------------------------------------------
SECTION 4 — CONTROL BREAK LOGIC
------------------------------------------------------------

4.1 Control fields
------------------
CONTROL IS field‑1 field‑2 ...

4.2 Control break detection
---------------------------
On each DETAIL:
- Compare current control fields to previous
- If changed:
  - Emit CONTROL FOOTING for lower levels
  - Emit CONTROL HEADING for changed level
- Then emit DETAIL

4.3 Nested control levels
-------------------------
Multiple control levels supported.

------------------------------------------------------------
SECTION 5 — REPORT FIELDS
------------------------------------------------------------

5.1 SOURCE fields
-----------------
SOURCE data‑item
- Copies value from data item into report

5.2 VALUE fields
----------------
VALUE literal
- Prints literal text

5.3 SUM fields
--------------
SUM data‑item
- Accumulates numeric values
- Reset at control breaks

5.4 COUNT fields
----------------
COUNT data‑item
- Counts DETAIL lines
- Reset at control breaks

5.5 AVERAGE fields
------------------
AVERAGE data‑item
- Maintains sum and count
- Computes average at print time

5.6 COMPUTE fields
------------------
COMPUTE field = expression

------------------------------------------------------------
SECTION 6 — LINE/COLUMN POSITIONING
------------------------------------------------------------

6.1 LINE clause
---------------
LINE n
- Absolute line number on page

LINE +n
- Relative line advance

6.2 COLUMN clause
-----------------
COLUMN n
- Absolute column

COLUMN +n
- Relative column

6.3 Overflow handling
---------------------
If LINE exceeds page size:
- Emit PAGE FOOTING
- Start new page
- Emit PAGE HEADING

------------------------------------------------------------
SECTION 7 — PAGE MANAGEMENT
------------------------------------------------------------

7.1 PAGE LIMIT
--------------
PAGE LIMIT n
- Maximum lines per page

7.2 Automatic page eject
------------------------
Occurs when:
- PAGE LIMIT exceeded
- CONTROL break requires new page (optional)
- REPORT HEADING printed at start

7.3 Page numbering
------------------
CobolSharp maintains:
- Page counter
- Line counter
- Control break state

------------------------------------------------------------
SECTION 8 — REPORT BUFFER ARCHITECTURE
------------------------------------------------------------

8.1 Buffer structure
--------------------
Report buffer is:
- A list of lines
- Each line is a char array
- Fixed width (configurable)

8.2 Writing to buffer
---------------------
Each report field writes:
- At specified LINE/COLUMN
- With padding/truncation rules

8.3 Emission
------------
At page completion:
- Buffer flushed to output file or DISPLAY target

------------------------------------------------------------
SECTION 9 — CIL LOWERING RULES
------------------------------------------------------------

9.1 REPORT SECTION lowering
---------------------------
RD ReportName → new ReportDescriptor object

9.2 Group lowering
------------------
Each group becomes:
- A CIL method that writes its fields
- Called by ReportEngine

9.3 Field lowering
------------------
SOURCE → load data item → write to buffer  
VALUE → write literal  
SUM → accumulator update  
COUNT → increment counter  
AVERAGE → update sum and count  

9.4 Control break lowering
--------------------------
Generated code:
- Compares control fields
- Calls appropriate group methods

9.5 Page management lowering
----------------------------
Generated code:
- Checks line counter
- Emits page headings/footings
- Resets counters

------------------------------------------------------------
SECTION 10 — RUNTIME ENGINE INTEGRATION
------------------------------------------------------------

10.1 ReportEngine
-----------------
Handles:
- Page management
- Control break logic
- Field formatting
- Buffer emission

10.2 ExecutionContext
---------------------
Stores:
- Report descriptors
- Current report state
- Output file handles

10.3 FileManager
----------------
Used for:
- Writing report output
- Opening/closing report files

------------------------------------------------------------
SECTION 11 — DEBUGGER INTEGRATION
------------------------------------------------------------

Debugger shows:
- Current report name
- Current page number
- Current line number
- Control break state
- Field values before formatting
- Final formatted line

Sequence points emitted for:
- Each report group
- Each field
- Page ejects

------------------------------------------------------------
SECTION 12 — EDGE‑CASE BEHAVIOR
------------------------------------------------------------

12.1 DETAIL with no CONTROL fields
----------------------------------
Printed sequentially.

12.2 SUM of non‑numeric field
-----------------------------
Compile‑time error.

12.3 AVERAGE with zero count
----------------------------
Returns zero.

12.4 PAGE HEADING larger than page
----------------------------------
Truncated.

12.5 CONTROL break at end of file
---------------------------------
All CONTROL FOOTING groups emitted.

12.6 Nested reports
-------------------
Allowed; each has independent state.

------------------------------------------------------------
Summary
------------------------------------------------------------
The CobolSharp Report Writer Architecture:
- Implements full COBOL REPORT SECTION semantics
- Supports control breaks, page headings/footings, and aggregate fields
- Provides deterministic formatting and page management
- Integrates tightly with ExecutionContext and FileManager
- Generates clean, verifiable, debugger‑friendly CIL
- Ensures correctness across CoreCLR, AOT, and WASM
