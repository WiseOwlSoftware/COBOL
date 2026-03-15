CobolSharp COBOL National Character, Unicode & Locale‑Independent Text Architecture (CIL‑Only)
=============================================================================================

Purpose
-------
Define the authoritative architecture for:
- NATIONAL data items (PIC N)
- Unicode storage and encoding
- Locale‑independent text semantics
- National literal handling (N"…")
- National MOVE, STRING, UNSTRING, INSPECT
- Mixed national/alphanumeric groups
- Collation and comparison rules
- JSON/XML integration with Unicode
- CIL‑friendly lowering
- AOT/WASM‑safe text operations

This document governs how CobolSharp implements COBOL’s national character set and Unicode model.

------------------------------------------------------------
SECTION 1 — NATIONAL CHARACTER MODEL OVERVIEW
------------------------------------------------------------

CobolSharp implements:
- Full ISO/IEC 1989:2023 NATIONAL semantics
- Unicode‑based PIC N storage
- UTF‑16 encoding (2 bytes per character)
- Locale‑independent comparisons
- Deterministic collation
- Unicode‑safe STRING/UNSTRING/INSPECT
- Mixed national/alphanumeric conversions

NATIONAL items:
- Are always UTF‑16
- Are always fixed‑length
- Are padded with U+0020 (space)
- Never use null terminators

------------------------------------------------------------
SECTION 2 — NATIONAL STORAGE LAYOUT
------------------------------------------------------------

2.1 PIC N
---------
PIC N(n):
- Allocates n * 2 bytes
- Each character is UTF‑16 code unit
- No surrogate pair splitting allowed

2.2 NATIONAL GROUP items
------------------------
Group items containing PIC N:
- Are treated as UTF‑16 sequences
- Children must align on 2‑byte boundaries

2.3 Mixed groups
----------------
Group containing PIC X and PIC N:
- Allowed
- Offsets computed sequentially
- No implicit alignment

2.4 NATIONAL literals
---------------------
N"ABC" stored as:
- 41 00 42 00 43 00 (UTF‑16 LE)

------------------------------------------------------------
SECTION 3 — NATIONAL MOVE SEMANTICS
------------------------------------------------------------

3.1 MOVE national → national
----------------------------
- Direct UTF‑16 copy
- Pad with U+0020 if shorter
- Truncate if longer

3.2 MOVE alphanumeric → national
--------------------------------
- Convert ASCII → UTF‑16
- Pad with U+0020

3.3 MOVE national → alphanumeric
--------------------------------
- Convert UTF‑16 → ASCII
- Non‑ASCII characters:
  - ON EXCEPTION if not representable

3.4 MOVE numeric → national
---------------------------
- Convert numeric → string → UTF‑16

------------------------------------------------------------
SECTION 4 — NATIONAL STRING/UNSTRING
------------------------------------------------------------

4.1 STRING
----------
STRING national‑1 national‑2 …
- Concatenates UTF‑16 sequences
- No surrogate splitting
- Pads target with U+0020

4.2 UNSTRING
------------
UNSTRING national‑item
- Delimiters are UTF‑16 sequences
- COUNT and TALLYING count characters, not bytes

4.3 INSPECT
-----------
INSPECT national‑item:
- TALLYING counts Unicode characters
- REPLACING replaces UTF‑16 sequences

------------------------------------------------------------
SECTION 5 — NATIONAL COMPARISON RULES
------------------------------------------------------------

5.1 Locale‑independent
----------------------
CobolSharp uses:
- Binary UTF‑16 comparison
- No locale collation
- No case folding unless explicitly requested

5.2 Comparison types
--------------------
=, <, >, <=, >=:
- Compare UTF‑16 code units lexicographically

5.3 Case conversion
-------------------
LOWER‑CASE / UPPER‑CASE:
- Unicode‑aware
- Uses invariant culture rules

------------------------------------------------------------
SECTION 6 — NATIONAL & JSON/XML INTEGRATION
------------------------------------------------------------

6.1 JSON
--------
JSON PARSE:
- Converts JSON strings → UTF‑16 NATIONAL items

JSON GENERATE:
- Converts NATIONAL items → JSON UTF‑8 strings

6.2 XML
-------
XML PARSE:
- Converts XML text → UTF‑16 NATIONAL items

XML GENERATE:
- Converts NATIONAL items → UTF‑8 XML output

6.3 Encoding rules
------------------
- Internal representation: UTF‑16
- External representation: UTF‑8
- No BOM emitted unless configured

------------------------------------------------------------
SECTION 7 — NATIONAL & FILE I/O
------------------------------------------------------------

7.1 Sequential files
--------------------
- NATIONAL items written as UTF‑16 LE
- Optional transcoding to UTF‑8 (configurable)

7.2 Indexed/relative files
--------------------------
- NATIONAL keys stored as UTF‑16
- Comparisons use binary UTF‑16 ordering

7.3 REPORT WRITER
-----------------
- NATIONAL fields rendered as UTF‑16
- Output transcoded to UTF‑8 or ASCII depending on target

------------------------------------------------------------
SECTION 8 — CIL LOWERING RULES
------------------------------------------------------------

8.1 Storage access
------------------
Generated IL uses:
- ctx.Storage.GetStringUTF16
- ctx.Storage.SetStringUTF16

8.2 STRING/UNSTRING lowering
----------------------------
STRING → ctx.StringEngine.ConcatUTF16  
UNSTRING → ctx.StringEngine.SplitUTF16  

8.3 Comparison lowering
-----------------------
Compare national → call String.CompareOrdinal

8.4 Case conversion lowering
----------------------------
LOWER‑CASE → string.ToLowerInvariant  
UPPER‑CASE → string.ToUpperInvariant  

------------------------------------------------------------
SECTION 9 — DEBUGGER INTEGRATION
------------------------------------------------------------

Debugger shows:
- NATIONAL field value (UTF‑16)
- Raw bytes
- Surrogate pairs (if present)
- Mixed group layout
- Character count vs byte count

------------------------------------------------------------
SECTION 10 — EDGE‑CASE BEHAVIOR
------------------------------------------------------------

10.1 Surrogate pairs
--------------------
- Allowed
- Count as 2 bytes, 1 character

10.2 Invalid UTF‑16 sequences
-----------------------------
- ON EXCEPTION during MOVE or STRING

10.3 MOVE national → alphanumeric with non‑ASCII
------------------------------------------------
- ON EXCEPTION

10.4 NATIONAL inside REDEFINES
------------------------------
Allowed; debugger shows raw bytes.

10.5 NATIONAL in OCCURS DEPENDING ON
------------------------------------
Logical length measured in characters.

------------------------------------------------------------
Summary
------------------------------------------------------------
The CobolSharp National Character & Unicode Architecture:
- Implements full COBOL NATIONAL semantics using UTF‑16
- Provides deterministic, locale‑independent text behavior
- Supports STRING/UNSTRING/INSPECT for Unicode
- Integrates with JSON/XML, file I/O, and Report Writer
- Generates clean, verifiable, debugger‑friendly CIL
- Ensures correctness across CoreCLR, AOT, and WASM
