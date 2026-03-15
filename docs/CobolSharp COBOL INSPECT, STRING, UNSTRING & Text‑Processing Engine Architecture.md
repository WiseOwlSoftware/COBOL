CobolSharp COBOL INSPECT, STRING, UNSTRING & Text‑Processing Engine Architecture (CIL‑Only)
===========================================================================================

Purpose
-------
Define the authoritative architecture for:
- INSPECT (TALLYING, REPLACING, CONVERTING)
- STRING statement
- UNSTRING statement
- DELIMITED BY rules
- POINTER and TALLYING semantics
- NATIONAL vs DISPLAY text processing
- Overlapping source/target behavior
- Exception routing (ON OVERFLOW, ON EXCEPTION)
- Integration with ExecutionContext.StringEngine
- AOT/WASM‑safe text operations
- CIL‑friendly lowering

This document governs how CobolSharp implements COBOL’s text‑processing facilities on .NET.

------------------------------------------------------------
SECTION 1 — STRINGENGINE OVERVIEW
------------------------------------------------------------

ExecutionContext.StringEngine provides:
- Concatenation
- Splitting
- Searching
- Replacement
- Case conversion
- UTF‑8/UTF‑16 bridging
- Bounds checking
- ExceptionState population

All operations are:
- Pure managed
- Zero‑allocation where possible
- Deterministic
- AOT/WASM‑safe

------------------------------------------------------------
SECTION 2 — STRING STATEMENT
------------------------------------------------------------

2.1 Basic form
--------------
STRING src1 src2 src3
    DELIMITED BY sizeOrLiteral
    INTO target
    WITH POINTER p
    ON OVERFLOW
        ...
    NOT ON OVERFLOW
        ...

2.2 Semantics
-------------
- Concatenate sources into target
- DELIMITED BY determines substring length
- POINTER indicates starting position in target
- ON OVERFLOW triggered if target too small

2.3 DELIMITED BY rules
-----------------------
DELIMITED BY:
- SIZE → entire source
- literal → up to literal
- identifier → up to value of identifier

2.4 POINTER
-----------
- 1‑based index
- Updated after each append
- If omitted → starts at 1

2.5 Overlapping behavior
------------------------
STRING allows:
- Source and target to overlap
- Engine uses temporary buffer to avoid corruption

------------------------------------------------------------
SECTION 3 — UNSTRING STATEMENT
------------------------------------------------------------

3.1 Basic form
--------------
UNSTRING src
    DELIMITED BY delimiter
    INTO tgt1 tgt2 tgt3
    WITH POINTER p
    TALLYING t
    ON OVERFLOW
        ...
    NOT ON OVERFLOW
        ...

3.2 Semantics
-------------
- Split src into fields
- DELIMITED BY determines split points
- POINTER indicates starting position in src
- TALLYING counts characters moved
- ON OVERFLOW triggered if target too small

3.3 DELIMITED BY rules
-----------------------
DELIMITED BY:
- literal
- identifier
- ALL literal (multi‑character delimiter)
- SIZE (entire string)

3.4 INTO fields
---------------
Each field receives:
- Substring up to delimiter
- Trimmed or padded per PIC rules

3.5 POINTER
-----------
- Updated to position after delimiter
- Allows iterative UNSTRING

3.6 TALLYING
------------
- Counts characters moved into targets
- Does not count delimiters

------------------------------------------------------------
SECTION 4 — INSPECT STATEMENT
------------------------------------------------------------

4.1 Forms
---------
INSPECT identifier
    TALLYING count FOR ALL literal

INSPECT identifier
    REPLACING ALL literal BY literal2

INSPECT identifier
    CONVERTING fromSet TO toSet

4.2 TALLYING
------------
Counts:
- Occurrences of literal
- Occurrences of characters in set
- ALL or LEADING or TRAILING

4.3 REPLACING
-------------
Replaces:
- ALL literal
- FIRST literal
- LEADING literal
- TRAILING literal

4.4 CONVERTING
--------------
Character‑by‑character mapping:
- fromSet[i] → toSet[i]
- Sets must be same length

------------------------------------------------------------
SECTION 5 — NATIONAL VS DISPLAY PROCESSING
------------------------------------------------------------

5.1 DISPLAY
-----------
- ASCII bytes
- 1 byte per character
- Case conversion uses ASCII rules

5.2 NATIONAL
------------
- UTF‑16
- 2 bytes per character
- Case conversion uses Unicode rules

5.3 Mixed operations
--------------------
DISPLAY → NATIONAL:
- ASCII → UTF‑16

NATIONAL → DISPLAY:
- UTF‑16 → ASCII (error if non‑ASCII)

------------------------------------------------------------
SECTION 6 — ERROR HANDLING & EXCEPTIONSTATE
------------------------------------------------------------

6.1 STRING overflow
-------------------
Triggered when:
- Target too small
- POINTER out of range

6.2 UNSTRING overflow
---------------------
Triggered when:
- Target field too small
- POINTER out of range

6.3 INSPECT errors
------------------
- Invalid CONVERTING set lengths
- Invalid NATIONAL → DISPLAY conversion

6.4 ExceptionState
------------------
Populated with:
- Operation type
- Source/target names
- Pointer position
- Error message

6.5 Routing
-----------
1. ON OVERFLOW  
2. ON EXCEPTION  
3. USE AFTER EXCEPTION ON STANDARD  

------------------------------------------------------------
SECTION 7 — CIL LOWERING RULES
------------------------------------------------------------

7.1 STRING lowering
-------------------
Generated IL:
- Evaluate each source
- Call StringEngine.Append
- Update POINTER
- Check overflow

7.2 UNSTRING lowering
---------------------
Generated IL:
- Call StringEngine.Split
- Assign to target fields
- Update POINTER
- Update TALLYING

7.3 INSPECT lowering
--------------------
Generated IL:
- Call StringEngine.Tally / Replace / Convert

7.4 Temporary buffers
---------------------
Compiler allocates:
- Temporary locals for substring extraction
- Temporary buffer for overlapping STRING

------------------------------------------------------------
SECTION 8 — STRINGENGINE IMPLEMENTATION
------------------------------------------------------------

8.1 Append
----------
Append(src, target, pointer):
- Bounds check
- Copy bytes/chars
- Update pointer

8.2 Split
---------
Split(src, delimiter, pointer):
- Find delimiter
- Extract substring
- Update pointer

8.3 Replace
-----------
ReplaceAll(src, literal, replacement):
- Streaming search/replace
- Zero‑allocation where possible

8.4 Convert
-----------
Convert(src, fromSet, toSet):
- Character‑by‑character mapping

8.5 Tally
---------
Count occurrences of:
- Literal
- Character set
- LEADING/TRAILING rules

------------------------------------------------------------
SECTION 9 — DEBUGGER INTEGRATION
------------------------------------------------------------

Debugger shows:
- Source and target fields
- POINTER value
- TALLYING value
- Delimiter
- Intermediate substrings
- ExceptionState

------------------------------------------------------------
SECTION 10 — AOT/WASM‑SAFE TEXT PROCESSING
------------------------------------------------------------

10.1 No reflection
------------------
All operations static.

10.2 No dynamic codegen
-----------------------
No runtime IL.

10.3 No unsafe code
-------------------
No pointers or stackalloc.

10.4 Deterministic behavior
---------------------------
Same results across platforms.

------------------------------------------------------------
SECTION 11 — EDGE‑CASE BEHAVIOR
------------------------------------------------------------

11.1 STRING with zero‑length source
-----------------------------------
No effect.

11.2 UNSTRING with missing delimiter
------------------------------------
Remainder goes to last field.

11.3 INSPECT REPLACING overlapping patterns
--------------------------------------------
Left‑to‑right, non‑recursive.

11.4 CONVERTING with duplicate characters
-----------------------------------------
First match wins.

11.5 NATIONAL surrogate pairs
-----------------------------
Counted as one character.

11.6 POINTER < 1 or > target length
-----------------------------------
Overflow.

------------------------------------------------------------
Summary
------------------------------------------------------------
The CobolSharp Text‑Processing Architecture:
- Implements full COBOL STRING, UNSTRING, and INSPECT semantics
- Provides deterministic, safe, high‑performance text operations
- Supports DISPLAY and NATIONAL with correct encoding rules
- Integrates tightly with ExecutionContext.StringEngine
- Generates clean, verifiable, debugger‑friendly CIL
- Ensures correctness across CoreCLR, AOT, and WASM
