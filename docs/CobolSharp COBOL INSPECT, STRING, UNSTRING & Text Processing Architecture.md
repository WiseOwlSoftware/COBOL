CobolSharp COBOL INSPECT, STRING, UNSTRING & Text Processing Architecture (CIL‑Only)
===================================================================================

Purpose
-------
Define the authoritative architecture for:
- STRING statement
- UNSTRING statement
- INSPECT (TALLYING, REPLACING, CONVERTING)
- NATIONAL vs DISPLAY text processing
- Unicode‑safe operations
- Overflow and truncation rules
- TALLYING and COUNT semantics
- DELIMITED BY rules
- Integration with ExecutionContext.StringEngine
- CIL‑friendly lowering
- AOT/WASM‑safe text operations

This document governs how CobolSharp implements COBOL’s text‑processing statements on .NET.

------------------------------------------------------------
SECTION 1 — STRING STATEMENT ARCHITECTURE
------------------------------------------------------------

1.1 Basic form
--------------
STRING src‑1 src‑2 ...
    DELIMITED BY delimiter
    INTO target
    WITH POINTER ptr
    ON OVERFLOW ...
    NOT ON OVERFLOW ...

1.2 Semantics
-------------
STRING:
- Concatenates source operands
- Applies DELIMITED BY rules
- Writes into target starting at POINTER
- Updates POINTER to next free position
- Pads unused target space with spaces

1.3 DELIMITED BY rules
----------------------
DELIMITED BY:
- SIZE → use full source length
- literal → stop at literal
- identifier → stop at value of identifier

1.4 Overflow rules
------------------
Overflow occurs if:
- Result exceeds target length
- POINTER moves beyond target

ON OVERFLOW:
- Executes overflow block
- Target may be partially written

NOT ON OVERFLOW:
- Executes only if no overflow

1.5 NATIONAL support
--------------------
STRING supports:
- PIC X (ASCII)
- PIC N (UTF‑16)
- Mixed STRING allowed; conversions applied

------------------------------------------------------------
SECTION 2 — UNSTRING STATEMENT ARCHITECTURE
------------------------------------------------------------

2.1 Basic form
--------------
UNSTRING src
    DELIMITED BY delimiter
    INTO tgt‑1 tgt‑2 ...
    WITH POINTER ptr
    TALLYING in‑count
    ON OVERFLOW ...
    NOT ON OVERFLOW ...

2.2 Semantics
-------------
UNSTRING:
- Splits source into fields
- Writes each field into corresponding target
- Updates POINTER to next position
- Updates TALLYING count
- Pads unused target space with spaces

2.3 DELIMITED BY rules
----------------------
DELIMITED BY:
- literal
- identifier
- ALL literal
- SIZE

ALL literal:
- Treats consecutive delimiters as one

2.4 TALLYING rules
------------------
TALLYING:
- Counts number of characters moved
- Or number of fields extracted (depending on clause)

2.5 Overflow rules
------------------
Overflow occurs if:
- More fields than targets
- Target too small
- POINTER exceeds source length

------------------------------------------------------------
SECTION 3 — INSPECT STATEMENT ARCHITECTURE
------------------------------------------------------------

INSPECT supports:
- TALLYING
- REPLACING
- CONVERTING

3.1 INSPECT TALLYING
---------------------
INSPECT item
    TALLYING counter
    FOR ALL literal
    FOR CHARACTERS
    FOR LEADING literal

Rules:
- ALL literal → count all occurrences
- CHARACTERS → count all characters
- LEADING literal → count leading occurrences only

3.2 INSPECT REPLACING
----------------------
INSPECT item
    REPLACING ALL literal‑1 BY literal‑2
    REPLACING LEADING literal‑1 BY literal‑2

Rules:
- ALL → replace all occurrences
- LEADING → replace only leading occurrences

3.3 INSPECT CONVERTING
-----------------------
INSPECT item
    CONVERTING from‑chars TO to‑chars

Rules:
- Character‑by‑character mapping
- Length of from‑chars must equal to‑chars

------------------------------------------------------------
SECTION 4 — NATIONAL & DISPLAY TEXT PROCESSING
------------------------------------------------------------

4.1 PIC X (DISPLAY)
-------------------
- ASCII bytes
- 1 byte per character

4.2 PIC N (NATIONAL)
--------------------
- UTF‑16
- 2 bytes per character
- No surrogate splitting

4.3 Mixed operations
--------------------
STRING/UNSTRING/INSPECT:
- Convert PIC X → PIC N when needed
- Convert PIC N → PIC X only if ASCII‑safe
- ON EXCEPTION if non‑ASCII cannot be represented

------------------------------------------------------------
SECTION 5 — STRINGENGINE IMPLEMENTATION
------------------------------------------------------------

ExecutionContext.StringEngine provides:

5.1 Concat
----------
ConcatASCII(srcs, delimiter, target, pointer)
ConcatUTF16(srcs, delimiter, target, pointer)

5.2 Split
---------
SplitASCII(src, delimiter, targets, pointer, tally)
SplitUTF16(src, delimiter, targets, pointer, tally)

5.3 Inspect operations
----------------------
InspectTallyASCII  
InspectTallyUTF16  
InspectReplaceASCII  
InspectReplaceUTF16  
InspectConvertASCII  
InspectConvertUTF16  

5.4 Performance
---------------
- Span<char> and Span<byte> used internally
- Zero‑allocation for common cases
- AOT/WASM‑safe

------------------------------------------------------------
SECTION 6 — CIL LOWERING RULES
------------------------------------------------------------

6.1 STRING lowering
-------------------
Generated IL:
- Load src operands
- Load target pointer
- Call StringEngine.Concat
- Update pointer
- Branch to ON OVERFLOW or NOT ON OVERFLOW

6.2 UNSTRING lowering
---------------------
Generated IL:
- Load src
- Load targets
- Call StringEngine.Split
- Update pointer and tally
- Branch to ON OVERFLOW or NOT ON OVERFLOW

6.3 INSPECT lowering
--------------------
Generated IL:
- Load item
- Call appropriate Inspect method
- Update counters

------------------------------------------------------------
SECTION 7 — DEBUGGER INTEGRATION
------------------------------------------------------------

Debugger shows:
- STRING sources and target
- UNSTRING fields and delimiters
- INSPECT patterns and replacements
- POINTER and TALLYING values
- NATIONAL vs DISPLAY interpretation
- Raw bytes and decoded text

------------------------------------------------------------
SECTION 8 — EDGE‑CASE BEHAVIOR
------------------------------------------------------------

8.1 STRING with POINTER = 0
---------------------------
Pointer treated as 1.

8.2 UNSTRING with no delimiters
-------------------------------
Entire string goes to first target.

8.3 INSPECT REPLACING with overlapping patterns
------------------------------------------------
Left‑to‑right, non‑recursive replacement.

8.4 NATIONAL with surrogate pairs
---------------------------------
Counted as 1 character, 2 bytes.

8.5 STRING overflow with partial write
--------------------------------------
Target contains partial concatenation.

8.6 UNSTRING ALL literal with empty fields
------------------------------------------
Consecutive delimiters treated as one.

------------------------------------------------------------
Summary
------------------------------------------------------------
The CobolSharp Text Processing Architecture:
- Implements full COBOL STRING, UNSTRING, and INSPECT semantics
- Supports NATIONAL and DISPLAY text with Unicode safety
- Provides deterministic DELIMITED BY, POINTER, and TALLYING behavior
- Integrates tightly with ExecutionContext.StringEngine
- Generates clean, verifiable, debugger‑friendly CIL
- Ensures correctness across CoreCLR, AOT, and WASM
