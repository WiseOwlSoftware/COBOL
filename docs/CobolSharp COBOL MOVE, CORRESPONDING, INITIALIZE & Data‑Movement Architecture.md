CobolSharp COBOL MOVE, CORRESPONDING, INITIALIZE & Data‑Movement Architecture (CIL‑Only)
========================================================================================

Purpose
-------
Define the authoritative architecture for:
- MOVE (simple, group, alphanumeric, numeric)
- MOVE CORRESPONDING / MOVE CORR
- INITIALIZE
- Padding, truncation, scaling
- Category conversion (DISPLAY, NATIONAL, COMP, COMP‑3, COMP‑5)
- Group‑to‑group moves
- Overlapping moves
- Exception routing (ON OVERFLOW, ON EXCEPTION)
- Deterministic, AOT/WASM‑safe data movement
- CIL‑friendly lowering

This document governs how CobolSharp implements COBOL’s data‑movement semantics on .NET.

------------------------------------------------------------
SECTION 1 — MOVE OVERVIEW
------------------------------------------------------------

CobolSharp implements:
- MOVE literal TO identifier
- MOVE identifier TO identifier
- MOVE group TO group
- MOVE CORRESPONDING group1 TO group2
- INITIALIZE identifier

MOVE is the most fundamental COBOL operation:
- Converts source to target category
- Applies PIC rules
- Applies padding/truncation
- Applies scaling for numeric targets
- Detects overflow
- Updates ExceptionState

------------------------------------------------------------
SECTION 2 — MOVE CATEGORIES
------------------------------------------------------------

2.1 Alphanumeric MOVE
---------------------
Source categories:
- DISPLAY
- NATIONAL
- Group items

Target categories:
- DISPLAY
- NATIONAL
- Group items

Rules:
- DISPLAY → DISPLAY: byte copy with padding/truncation
- NATIONAL → NATIONAL: UTF‑16 copy with padding/truncation
- DISPLAY → NATIONAL: ASCII → UTF‑16
- NATIONAL → DISPLAY: UTF‑16 → ASCII (error if non‑ASCII)

2.2 Numeric MOVE
----------------
Source categories:
- DISPLAY numeric
- COMP/COMP‑5
- COMP‑3
- Decimal (intermediate)
- Literals

Target categories:
- DISPLAY numeric
- COMP/COMP‑5
- COMP‑3

Rules:
- Convert source to Decimal
- Apply scaling from target PIC
- Apply rounding if ROUNDED
- Detect overflow
- Encode into target format

2.3 Group MOVE
--------------
MOVE group1 TO group2:
- Byte‑for‑byte copy
- Length = group2 length
- Truncation/padding applied

------------------------------------------------------------
SECTION 3 — MOVE CORRESPONDING / MOVE CORR
------------------------------------------------------------

3.1 Matching rules
------------------
Fields match if:
- Same name (case‑insensitive)
- Both elementary items
- Not suppressed by REDEFINES

3.2 Behavior
------------
For each matching field:
- MOVE field1 TO field2
- Category conversion applied
- Overflow rules applied

3.3 Group structure ignored
---------------------------
Only field names matter:
- Levels irrelevant
- Order irrelevant

3.4 Exclusions
--------------
Excluded:
- FILLER
- OCCURS tables (unless element names match)
- Group items

------------------------------------------------------------
SECTION 4 — INITIALIZE
------------------------------------------------------------

4.1 Basic form
--------------
INITIALIZE identifier.

4.2 Rules
---------
Alphanumeric:
- Set to spaces

NATIONAL:
- Set to UTF‑16 spaces

Numeric:
- Set to zero

Group:
- Apply rules recursively

4.3 INITIALIZE with REPLACING
-----------------------------
INITIALIZE group
    REPLACING ALPHANUMERIC BY "X"
              NUMERIC BY 9.

4.4 OCCURS DEPENDING ON
-----------------------
Does not modify DEPENDING ON variable.

------------------------------------------------------------
SECTION 5 — PADDING, TRUNCATION & SCALING
------------------------------------------------------------

5.1 Padding
-----------
DISPLAY:
- Pad right with spaces

NATIONAL:
- Pad right with UTF‑16 spaces

Numeric DISPLAY:
- Pad left with spaces or zeros depending on PIC

5.2 Truncation
--------------
DISPLAY:
- Rightmost characters truncated

NATIONAL:
- Rightmost UTF‑16 units truncated

Numeric DISPLAY:
- Leftmost digits truncated (overflow)

5.3 Scaling
-----------
Target PIC S9(5)V99:
- Scale Decimal to 2 decimal places
- Apply rounding if ROUNDED
- Detect overflow

------------------------------------------------------------
SECTION 6 — OVERLAPPING MOVES
------------------------------------------------------------

6.1 Allowed
-----------
MOVE allows overlapping source/target:
- Engine uses temporary buffer
- Ensures deterministic behavior

6.2 Example
-----------
MOVE A(1:5) TO A(3:5):
- Extract substring
- Write to target region

------------------------------------------------------------
SECTION 7 — ERROR HANDLING & EXCEPTIONSTATE
------------------------------------------------------------

7.1 Overflow
------------
Occurs when:
- Numeric value too large for target PIC
- COMP/COMP‑5 out of range
- COMP‑3 cannot encode digits

7.2 Invalid conversion
----------------------
Occurs when:
- NATIONAL → DISPLAY contains non‑ASCII
- DISPLAY numeric contains invalid characters

7.3 ON OVERFLOW
---------------
MOVE A TO B
    ON OVERFLOW
        ...
    NOT ON OVERFLOW
        ...

7.4 Behavior on overflow
------------------------
- Target NOT modified
- ExceptionState populated
- ON OVERFLOW executed

------------------------------------------------------------
SECTION 8 — CIL LOWERING RULES
------------------------------------------------------------

8.1 MOVE lowering
-----------------
Compiler generates:
- Evaluate source
- Convert to Decimal/string/UTF‑16
- Apply PIC rules
- Write to target buffer

8.2 MOVE CORRESPONDING lowering
-------------------------------
Compiler generates:
- Field‑matching table at compile time
- Loop over matches
- Emit MOVE for each

8.3 INITIALIZE lowering
-----------------------
Compiler generates:
- Recursive field traversal
- Write default values

8.4 Temporary locals
--------------------
Used for:
- Overlapping moves
- Numeric conversions
- Intermediate strings

------------------------------------------------------------
SECTION 9 — DEBUGGER INTEGRATION
------------------------------------------------------------

Debugger shows:
- Source value (decoded)
- Target value (decoded)
- PIC metadata
- Padding/truncation
- Scaling
- ExceptionState
- CORRESPONDING field matches

------------------------------------------------------------
SECTION 10 — AOT/WASM‑SAFE DATA MOVEMENT
------------------------------------------------------------

10.1 No unsafe code
-------------------
- No pointers
- No stackalloc

10.2 Pure managed operations
----------------------------
- Span<byte>
- Span<char>
- Deterministic encoding

10.3 Deterministic behavior
---------------------------
Same results across platforms.

------------------------------------------------------------
SECTION 11 — EDGE‑CASE BEHAVIOR
------------------------------------------------------------

11.1 MOVE to smaller field
--------------------------
Truncation; overflow for numeric.

11.2 MOVE CORRESPONDING with duplicate names
--------------------------------------------
Leftmost match wins.

11.3 INITIALIZE on REDEFINES
----------------------------
Applies to redefining item only.

11.4 NATIONAL with surrogate pairs
----------------------------------
Truncation must not split pair.

11.5 Numeric DISPLAY with embedded spaces
-----------------------------------------
Allowed; treated as zero unless SIGNED.

11.6 MOVE group to smaller group
--------------------------------
Truncation of trailing bytes.

------------------------------------------------------------
Summary
------------------------------------------------------------
The CobolSharp Data‑Movement Architecture:
- Implements full MOVE, CORRESPONDING, and INITIALIZE semantics
- Provides deterministic padding, truncation, scaling, and conversion
- Supports DISPLAY, NATIONAL, COMP, COMP‑3, COMP‑5 with correct encoding
- Integrates tightly with ExecutionContext.StringEngine and NumericEngine
- Generates clean, verifiable, debugger‑friendly CIL
- Ensures correctness across CoreCLR, AOT, and WASM
