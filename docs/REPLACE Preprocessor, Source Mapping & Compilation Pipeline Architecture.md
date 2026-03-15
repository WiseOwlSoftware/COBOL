CobolSharp COBOL COPY/REPLACE Preprocessor, Source Mapping & Compilation Pipeline Architecture (CIL‑Only)
=======================================================================================================

Purpose
-------
Define the authoritative architecture for:
- COPY statement resolution
- REPLACE BEFORE/AFTER semantics
- Nested COPY books
- COPY … REPLACING
- Source expansion model
- Original → expanded source mapping
- Debugger sequence‑point mapping
- Preprocessor pipeline
- Compiler pipeline (lexing → parsing → semantic analysis → IL generation)
- AOT/WASM‑safe preprocessing

This document governs how CobolSharp preprocesses, expands, maps, and compiles COBOL source code.

------------------------------------------------------------
SECTION 1 — PREPROCESSOR OVERVIEW
------------------------------------------------------------

CobolSharp implements a **two‑phase preprocessor**:

1. COPY/REPLACE expansion  
2. Source mapping generation  

The preprocessor:
- Expands COPY books recursively
- Applies REPLACE BEFORE/AFTER rules
- Applies COPY … REPLACING rules
- Produces a fully expanded source buffer
- Produces a mapping table linking expanded text → original files/lines

The compiler consumes:
- Expanded source
- Mapping table

------------------------------------------------------------
SECTION 2 — COPY STATEMENT SEMANTICS
------------------------------------------------------------

2.1 Basic form
--------------
COPY copybook.

2.2 COPY library resolution
---------------------------
Search order:
1. Local directory
2. COPY library paths
3. Embedded resources (optional)

2.3 COPY … REPLACING
---------------------
COPY copybook
    REPLACING ==old== BY ==new==.

Rules:
- Replacement applied only to text of copybook
- Does not affect surrounding source
- Multiple replacements allowed
- Replacement is lexical, not semantic

2.4 Nested COPY books
---------------------
COPY inside a copybook:
- Allowed
- Recursively expanded
- Mapping table tracks nested origins

2.5 COPY circularity
--------------------
Circular COPY references:
- Detected
- Compile‑time error

------------------------------------------------------------
SECTION 3 — REPLACE STATEMENT SEMANTICS
------------------------------------------------------------

3.1 REPLACE BEFORE
------------------
REPLACE BEFORE
    ==old== BY ==new==.

Applies to:
- All subsequent source
- Including COPY books
- Until REPLACE OFF

3.2 REPLACE AFTER
-----------------
REPLACE AFTER
    ==old== BY ==new==.

Applies to:
- Source after COPY expansion
- Does NOT apply inside COPY books

3.3 REPLACE OFF
---------------
Ends active REPLACE BEFORE region.

3.4 Replacement rules
---------------------
- Lexical replacement
- Longest match wins
- Case‑sensitive
- Does not cross token boundaries unless delimited

------------------------------------------------------------
SECTION 4 — SOURCE EXPANSION MODEL
------------------------------------------------------------

4.1 Expansion buffer
--------------------
Preprocessor builds:
- A single contiguous expanded source buffer
- With all COPY/REPLACE applied

4.2 Mapping table
-----------------
For each span in expanded source:
- Original file path
- Original line/column
- COPY nesting depth
- REPLACE transformations applied

4.3 Mapping granularity
-----------------------
Mapping is tracked at:
- Token level
- Line level
- Character span level (for debugging)

4.4 COPY boundaries
-------------------
Debugger can:
- Step into COPY book
- Show original COPY file
- Set breakpoints inside COPY

------------------------------------------------------------
SECTION 5 — COMPILATION PIPELINE
------------------------------------------------------------

5.1 Phase 1 — Preprocessing
---------------------------
Input:
- Original source files

Output:
- Expanded source buffer
- Mapping table

5.2 Phase 2 — Lexing
---------------------
Lexer consumes expanded source:
- Produces token stream
- Tokens carry mapping metadata

5.3 Phase 3 — Parsing
----------------------
Parser builds:
- Abstract Syntax Tree (AST)
- AST nodes carry mapping metadata

5.4 Phase 4 — Semantic Analysis
-------------------------------
Semantic analyzer:
- Resolves identifiers
- Validates PIC clauses
- Validates OCCURS, REDEFINES
- Validates CALL targets
- Builds symbol tables
- Builds data‑division layout

5.5 Phase 5 — IL Generation
---------------------------
IL generator:
- Emits CIL methods
- Emits sequence points using mapping table
- Emits PDB with original file paths

------------------------------------------------------------
SECTION 6 — DEBUGGER SEQUENCE‑POINT MAPPING
------------------------------------------------------------

6.1 Sequence point origin
-------------------------
Each sequence point maps to:
- Original file
- Original line/column
- COPY nesting depth

6.2 COPY stepping
-----------------
Debugger:
- Steps into COPY book
- Shows original COPY file
- Maintains correct line numbers

6.3 REPLACE mapping
-------------------
Debugger shows:
- Original text before replacement
- Expanded text after replacement

6.4 Breakpoint resolution
-------------------------
Breakpoints set in:
- Main source → mapped to expanded code
- COPY books → mapped to expanded code
- REPLACE regions → mapped to original source

------------------------------------------------------------
SECTION 7 — AOT/WASM‑SAFE PREPROCESSING
------------------------------------------------------------

7.1 No dynamic codegen
----------------------
Preprocessor uses:
- Pure managed string operations
- No reflection
- No unsafe code

7.2 Deterministic expansion
---------------------------
Expansion is:
- Pure function of input files
- Fully deterministic
- Stable across platforms

7.3 No file system access in WASM
---------------------------------
COPY books must be:
- Embedded resources
- Or provided via virtual file system

------------------------------------------------------------
SECTION 8 — ERROR HANDLING
------------------------------------------------------------

8.1 COPY not found
------------------
Compile‑time error:
COPYBOOK-NOT-FOUND

8.2 REPLACE malformed
---------------------
Compile‑time error:
INVALID-REPLACE-SYNTAX

8.3 COPY recursion
------------------
Compile‑time error:
COPY-RECURSION-DETECTED

8.4 Mapping inconsistencies
---------------------------
Compile‑time error:
SOURCE-MAPPING-ERROR

------------------------------------------------------------
SECTION 9 — DEBUGGER INTEGRATION
------------------------------------------------------------

Debugger shows:
- Original source file
- COPY book file
- REPLACE transformations
- Expanded source (optional)
- Mapping table entries
- COPY nesting depth

------------------------------------------------------------
SECTION 10 — EDGE‑CASE BEHAVIOR
------------------------------------------------------------

10.1 COPY inside REPLACE BEFORE
-------------------------------
Replacement applies inside COPY.

10.2 COPY inside REPLACE AFTER
------------------------------
Replacement does NOT apply inside COPY.

10.3 COPY … REPLACING inside REPLACE BEFORE
-------------------------------------------
COPY … REPLACING applies first  
REPLACE BEFORE applies second

10.4 COPY with no terminating period
------------------------------------
Illegal.

10.5 REPLACE with overlapping patterns
--------------------------------------
Longest match wins.

10.6 COPY of empty file
-----------------------
Allowed; expands to nothing.

------------------------------------------------------------
Summary
------------------------------------------------------------
The CobolSharp Preprocessor & Compilation Pipeline Architecture:
- Implements full COBOL COPY, REPLACE BEFORE/AFTER, and COPY … REPLACING semantics
- Produces deterministic expanded source and precise mapping tables
- Enables debugger stepping into COPY books with correct line numbers
- Integrates tightly with lexing, parsing, semantic analysis, and IL generation
- Generates clean, verifiable, debugger‑friendly CIL
- Ensures correctness across CoreCLR, AOT, and WASM
