CobolSharp COBOL STRING, UNSTRING, and INSPECT Engine Architecture (CIL‑Only)
============================================================================

Purpose
-------
Define the authoritative architecture for:
- STRING statement semantics
- UNSTRING statement semantics
- INSPECT (TALLYING, REPLACING, CONVERTING)
- Delimited‑by rules
- Pointer and tallying behavior
- Overflow detection
- Unicode and national character handling
- CIL‑friendly lowering
- Integration with CobolSharp.Runtime.StringEngine

This document governs how CobolSharp implements COBOL’s text‑processing features on .NET.

------------------------------------------------------------
SECTION 1 — STRING ENGINE OVERVIEW
------------------------------------------------------------

CobolSharp implements STRING, UNSTRING, and INSPECT using a dedicated **StringEngine** inside the runtime.

StringEngine responsibilities:
- Concatenation
- Delimited extraction
- Character scanning
- Replacement and conversion
- Padding and truncation
- Overflow detection
- Pointer and tallying updates
- Unicode‑safe operations
- National character support (PIC N)

All operations are:
- Deterministic
- Locale‑independent
- CIL‑friendly
- Compatible with CoreCLR, AOT, and WASM

------------------------------------------------------------
SECTION 2 — STRING STATEMENT SEMANTICS
------------------------------------------------------------

2.1 Basic form
--------------
STRING src1 src2 ...  
  DELIMITED BY delim  
  INTO target  
  WITH POINTER ptr  
  ON OVERFLOW handler  
END‑STRING

2.2 Concatenation rules
-----------------------
For each source operand:
- If DELIMITED BY SIZE → take entire operand
- If DELIMITED BY literal/identifier → stop at delimiter
- If DELIMITED BY ALL literal → treat consecutive delimiters as one

2.3 Pointer rules
-----------------
Pointer is:
- 1‑based
- Updated after each write
- If omitted → defaults to 1

After writing N characters:
ptr = ptr + N

2.4 Overflow rules
------------------
Overflow occurs when:
- Target buffer too small
- Pointer exceeds target length

On overflow:
- No partial write
- ON OVERFLOW handler executes
- NOT ON OVERFLOW executes only if no overflow

2.5 Padding/truncation
----------------------
- No padding for STRING
- Truncation only occurs if DELIMITED BY SIZE and target too small → overflow

2.6 National character handling
-------------------------------
PIC N uses UTF‑16:
- Each character = 2 bytes
- Pointer increments by characters, not bytes

------------------------------------------------------------
SECTION 3 — UNSTRING STATEMENT SEMANTICS
------------------------------------------------------------

3.1 Basic form
--------------
UNSTRING src  
  DELIMITED BY delim  
  INTO tgt1 tgt2 ...  
  WITH POINTER ptr  
  TALLYING IN tally  
  ON OVERFLOW handler  
END‑UNSTRING

3.2 Extraction rules
--------------------
For each target:
- Extract characters until delimiter
- Write into target
- Advance pointer past delimiter
- If DELIMITED BY ALL literal → skip consecutive delimiters

3.3 Pointer rules
-----------------
Pointer:
- 1‑based
- Points into source
- Updated after each extraction
- If omitted → defaults to 1

3.4 TALLYING rules
------------------
TALLYING IN var:
- Incremented by number of characters moved
- Includes characters written to target
- Does not include delimiter

3.5 Overflow rules
------------------
Overflow occurs when:
- Target too small
- Pointer beyond source
- No more delimiters but more targets remain

On overflow:
- Partial writes allowed (COBOL standard)
- ON OVERFLOW handler executes

3.6 National character handling
-------------------------------
Same as STRING:
- UTF‑16
- Character‑based pointer

------------------------------------------------------------
SECTION 4 — INSPECT STATEMENT SEMANTICS
------------------------------------------------------------

INSPECT supports:
- TALLYING
- REPLACING
- CONVERTING

4.1 INSPECT TALLYING
---------------------
INSPECT src  
  TALLYING tally  
    FOR ALL literal  
    FOR LEADING literal  
    FOR TRAILING literal

Rules:
- ALL → count all occurrences
- LEADING → count from start until mismatch
- TRAILING → count from end until mismatch

4.2 INSPECT REPLACING
----------------------
INSPECT src  
  REPLACING  
    ALL literal BY replacement  
    LEADING literal BY replacement  
    TRAILING literal BY replacement

Rules:
- ALL → replace all occurrences
- LEADING → replace from start until mismatch
- TRAILING → replace from end until mismatch

4.3 INSPECT CONVERTING
-----------------------
INSPECT src  
  CONVERTING from‑chars TO to‑chars

Rules:
- Character‑by‑character mapping
- from‑chars and to‑chars must be same length
- National characters supported

------------------------------------------------------------
SECTION 5 — STRINGENGINE INTERNAL DESIGN
------------------------------------------------------------

StringEngine provides:

5.1 Core operations
-------------------
- ExtractUntilDelimiter()
- AppendWithPointer()
- ReplaceAll()
- ReplaceLeading()
- ReplaceTrailing()
- ConvertCharacters()
- CountOccurrences()
- CountLeading()
- CountTrailing()

5.2 Safety guarantees
---------------------
- No buffer overruns
- No partial writes on STRING overflow
- Partial writes allowed for UNSTRING (per COBOL standard)
- Unicode‑safe slicing
- No culture‑dependent behavior

5.3 Performance characteristics
-------------------------------
- Uses Span<char> internally
- Avoids allocations where possible
- Optimized for large OCCURS tables
- Deterministic execution

------------------------------------------------------------
SECTION 6 — CIL LOWERING RULES
------------------------------------------------------------

6.1 STRING lowering
-------------------
STRING → call:
StringEngine.String(ctx, srcList, target, pointer, overflowHandler)

6.2 UNSTRING lowering
---------------------
UNSTRING → call:
StringEngine.Unstring(ctx, src, targetList, pointer, tally, overflowHandler)

6.3 INSPECT lowering
--------------------
INSPECT → call:
StringEngine.Inspect(ctx, src, mode, args)

6.4 Pointer lowering
--------------------
Pointer variables are:
- Loaded before call
- Updated after call
- Stored back into StorageBlock

6.5 TALLYING lowering
---------------------
Tally variables updated by StringEngine.

------------------------------------------------------------
SECTION 7 — DEBUGGER INTEGRATION
------------------------------------------------------------

Debugger shows:
- Pointer before/after STRING/UNSTRING
- TALLYING values
- Delimiter matches
- Extracted segments
- Replacement operations
- Unicode code points (optional)
- Raw bytes for PIC X and PIC N

Sequence points emitted for:
- Each STRING source
- Each UNSTRING target
- Each INSPECT clause

------------------------------------------------------------
SECTION 8 — EDGE‑CASE BEHAVIOR
------------------------------------------------------------

8.1 Empty delimiter
-------------------
Illegal → compile‑time error

8.2 DELIMITED BY SIZE with zero‑length source
---------------------------------------------
Writes nothing

8.3 UNSTRING with more targets than delimiters
----------------------------------------------
Remaining targets receive spaces

8.4 UNSTRING with no delimiters
-------------------------------
Entire source goes to first target

8.5 INSPECT REPLACING overlapping patterns
------------------------------------------
Left‑to‑right, non‑overlapping

8.6 National vs alphanumeric mixing
-----------------------------------
Illegal unless explicitly converted

8.7 Pointer beyond source
-------------------------
UNSTRING → overflow  
STRING → overflow  

8.8 Replacement with different lengths
--------------------------------------
INSPECT REPLACING requires equal lengths

------------------------------------------------------------
Summary
------------------------------------------------------------
The CobolSharp STRING, UNSTRING, and INSPECT Engine Architecture:
- Implements full COBOL text‑processing semantics
- Provides deterministic, Unicode‑safe behavior
- Handles pointers, tallying, delimiters, and overflow correctly
- Uses a dedicated StringEngine for performance and correctness
- Lowers cleanly to CIL with structured calls
- Integrates deeply with debugging and runtime services
- Ensures compatibility across CoreCLR, AOT, and WASM
