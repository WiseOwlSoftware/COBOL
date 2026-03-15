CobolSharp Determinism Model — Cross‑Platform Guarantees, Encoding Rules & Reproducibility Architecture (CIL‑Only)
=================================================================================================================

Purpose
-------
Define the authoritative architecture for:
- Deterministic execution across CoreCLR, AOT, and WASM
- Encoding rules (DISPLAY, NATIONAL, COMP, COMP‑3, COMP‑5)
- Numeric determinism (Decimal‑only arithmetic)
- Control‑flow determinism
- File I/O determinism
- JSON/XML/SORT/REPORT determinism
- Randomness determinism
- Reproducible builds and reproducible execution
- Forbidden nondeterministic behaviors
- CIL‑friendly lowering that preserves determinism

This document governs how CobolSharp ensures identical results across all supported platforms.

------------------------------------------------------------
SECTION 1 — DETERMINISM OVERVIEW
------------------------------------------------------------

CobolSharp guarantees:
- Same input → same output
- Same program → same behavior
- Same data → same results
- Across:
  - Windows
  - Linux
  - macOS
  - AOT
  - WASM
  - Debug/Release builds

Determinism applies to:
- Numeric operations
- String operations
- File I/O
- JSON/XML parsing
- SORT/MERGE
- REPORT WRITER
- OO dispatch
- Intrinsic functions
- Random number generation

------------------------------------------------------------
SECTION 2 — ENCODING DETERMINISM
------------------------------------------------------------

2.1 DISPLAY
-----------
- ASCII only
- 1 byte per character
- Non‑ASCII → runtime error
- Padding = space (0x20)
- Truncation = rightmost bytes removed

2.2 NATIONAL
------------
- UTF‑16
- Surrogate‑pair safe
- Padding = U+0020
- Truncation never splits surrogate pair

2.3 COMP/COMP‑5
---------------
- Two’s complement binary
- Endianness normalized to little‑endian
- Overflow detection deterministic

2.4 COMP‑3
----------
- Packed decimal
- Sign nibble = C/D/F
- Invalid nibble → runtime error
- Encoding/decoding deterministic

------------------------------------------------------------
SECTION 3 — NUMERIC DETERMINISM
------------------------------------------------------------

3.1 Decimal‑only arithmetic
---------------------------
All arithmetic uses:
- System.Decimal
- No floating‑point
- No platform‑dependent rounding

3.2 Rounding
------------
COBOL rounding:
- Discarded digit ≥ 5 → increment
- Else → truncate

3.3 Scaling
-----------
Target PIC determines:
- Decimal places
- Scaling factor

3.4 Overflow
------------
Overflow always:
- Leaves target unchanged
- Sets ExceptionState
- Triggers ON SIZE ERROR

------------------------------------------------------------
SECTION 4 — CONTROL‑FLOW DETERMINISM
------------------------------------------------------------

4.1 IF/EVALUATE
---------------
- Short‑circuit evaluation
- Left‑to‑right
- No platform‑dependent branching

4.2 PERFORM
-----------
- Loop bounds evaluated once
- TEST BEFORE/AFTER deterministic
- VARYING increments deterministic

4.3 GO TO
---------
- Branches resolved at compile time
- CFG validated

------------------------------------------------------------
SECTION 5 — FILE I/O DETERMINISM
------------------------------------------------------------

5.1 Sequential files
--------------------
- READ NEXT always returns next record
- No buffering differences across platforms

5.2 Indexed files
-----------------
- B‑tree implementation deterministic
- Key collation deterministic
- Duplicate key handling deterministic

5.3 Relative files
------------------
- RRN mapping deterministic
- Deleted record behavior deterministic

5.4 File status codes
---------------------
Always identical across platforms.

------------------------------------------------------------
SECTION 6 — JSON/XML DETERMINISM
------------------------------------------------------------

6.1 JSON
--------
- SAX‑style parser
- No floating‑point
- No locale‑dependent parsing
- Key ordering preserved

6.2 XML
-------
- SAX‑style parser
- Namespace resolution deterministic
- Attribute ordering preserved

------------------------------------------------------------
SECTION 7 — SORT/MERGE DETERMINISM
------------------------------------------------------------

7.1 Sorting
-----------
- Stable merge sort
- Deterministic collation
- Deterministic key extraction
- Deterministic tie‑breaking

7.2 Merging
-----------
- Deterministic multi‑way merge
- Deterministic record ordering

------------------------------------------------------------
SECTION 8 — REPORT WRITER DETERMINISM
------------------------------------------------------------

8.1 Page/line control
---------------------
- Page breaks deterministic
- Line numbering deterministic

8.2 Control‑breaks
------------------
- Field comparison deterministic
- Accumulator reset deterministic

8.3 Rendering
-------------
- UTF‑16 output deterministic
- Column positioning deterministic

------------------------------------------------------------
SECTION 9 — RANDOMNESS DETERMINISM
------------------------------------------------------------

9.1 PRNG
--------
CobolSharp uses:
- Xoshiro256** deterministic PRNG
- Seeded via RANDOM‑SEED

9.2 RANDOM
----------
RANDOM() returns:
- Same sequence for same seed
- Identical across platforms

------------------------------------------------------------
SECTION 10 — OO DETERMINISM
------------------------------------------------------------

10.1 Virtual dispatch
---------------------
- Deterministic method resolution
- No reflection
- No dynamic invocation

10.2 ObjectTable
----------------
- Deterministic reference indices
- Deterministic lifetime rules

------------------------------------------------------------
SECTION 11 — COMPILER DETERMINISM
------------------------------------------------------------

11.1 Reproducible builds
------------------------
Compiler ensures:
- Same source → same IL
- Same metadata ordering
- Same program registry ordering

11.2 No nondeterministic passes
-------------------------------
- No hash‑based ordering
- No parallel compilation
- No random temp names

------------------------------------------------------------
SECTION 12 — FORBIDDEN NONDETERMINISM
------------------------------------------------------------

CobolSharp forbids:
- Floating‑point arithmetic
- Locale‑dependent behavior
- Reflection
- Dynamic codegen
- Threading
- Parallelism
- Unordered collections
- Hash‑based ordering
- System clock access except via CURRENT‑DATE
- Non‑deterministic file APIs
- Platform‑dependent encoding

------------------------------------------------------------
SECTION 13 — AOT/WASM DETERMINISM
------------------------------------------------------------

13.1 No JIT differences
-----------------------
All IL compiled ahead‑of‑time.

13.2 No platform‑dependent intrinsics
-------------------------------------
No SIMD, no hardware FP.

13.3 WASM restrictions
----------------------
- No threads
- No dynamic linking
- No reflection

------------------------------------------------------------
SECTION 14 — EDGE‑CASE BEHAVIOR
------------------------------------------------------------

14.1 NATIONAL truncation
------------------------
Never splits surrogate pair.

14.2 COMP‑3 odd digits
----------------------
Leading zero inserted deterministically.

14.3 SORT with equal keys
-------------------------
Stable ordering preserved.

14.4 RANDOM without seed
------------------------
Seed = 1.

14.5 CURRENT‑DATE timezone
--------------------------
UTC offset computed deterministically.

------------------------------------------------------------
Summary
------------------------------------------------------------
The CobolSharp Determinism Model:
- Guarantees identical behavior across CoreCLR, AOT, and WASM
- Eliminates all sources of nondeterminism
- Uses Decimal‑only arithmetic, deterministic encodings, deterministic I/O
- Ensures reproducible builds and reproducible execution
- Provides a fully deterministic COBOL runtime and compiler
