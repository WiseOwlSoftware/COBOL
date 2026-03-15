CobolSharp Standard Library — Intrinsics, FUNCTION Resolution, Runtime Helpers & Deterministic Semantics (CIL‑Only)
===================================================================================================================

Purpose
-------
Define the authoritative architecture for:
- COBOL intrinsic functions (FUNCTION xxx)
- Deterministic numeric, string, date/time, and statistical intrinsics
- Intrinsic resolution and overload selection
- Runtime helper library (CobolSharp.Runtime)
- Deterministic semantics across CoreCLR, AOT, and WASM
- CIL‑friendly lowering
- Error handling and ExceptionState integration

This document governs how CobolSharp implements the COBOL standard library and intrinsic functions.

------------------------------------------------------------
SECTION 1 — INTRINSIC FUNCTION OVERVIEW
------------------------------------------------------------

CobolSharp implements all ISO/IEC 1989:2023 intrinsic functions:
- Numeric intrinsics (ABS, SQRT, INTEGER, FRACTION, REM, MOD, EXP, LOG)
- String intrinsics (LENGTH, TRIM, REVERSE, UPPER-CASE, LOWER-CASE)
- Date/time intrinsics (CURRENT-DATE, WHEN-COMPILED)
- Statistical intrinsics (MEAN, MEDIAN, RANGE)
- Conversion intrinsics (NUMVAL, NUMVAL-C, ORD, CHAR)
- Boolean intrinsics (BOOLEAN-OF-INTEGER, INTEGER-OF-BOOLEAN)
- Random intrinsics (RANDOM, RANDOM-SEED)
- National‑character intrinsics (NATIONAL-OF, DISPLAY-OF)
- Bitwise intrinsics (BIT-AND, BIT-OR, BIT-XOR, BIT-NOT)

All intrinsics are:
- Pure functions
- Deterministic
- Side‑effect free
- AOT/WASM‑safe

------------------------------------------------------------
SECTION 2 — FUNCTION RESOLUTION
------------------------------------------------------------

2.1 Compile‑time resolution
---------------------------
Compiler resolves:
- Function name
- Parameter count
- Parameter types
- Return type
- Overload selection

2.2 No runtime lookup
---------------------
All intrinsics are bound statically.

2.3 Overload rules
------------------
Overloads selected based on:
- Numeric category (integer, decimal, floating)
- String category (DISPLAY, NATIONAL)
- Parameter count
- Optional parameters

2.4 Error cases
---------------
- Unknown intrinsic → compile‑time error
- Wrong number of arguments → compile‑time error
- Wrong type → compile‑time error

------------------------------------------------------------
SECTION 3 — NUMERIC INTRINSICS
------------------------------------------------------------

3.1 ABS
-------
ABS(x):
- Returns absolute value
- Decimal‑based

3.2 SQRT
--------
SQRT(x):
- Decimal square root
- Error if x < 0

3.3 INTEGER / FRACTION
----------------------
INTEGER(x):
- Truncates toward zero

FRACTION(x):
- x - INTEGER(x)

3.4 REM / MOD
-------------
REM(a, b):
- Remainder with sign of a

MOD(a, b):
- Remainder with sign of b

3.5 EXP / LOG
-------------
EXP(x):
- e^x using Decimal approximation

LOG(x):
- Natural log
- Error if x ≤ 0

------------------------------------------------------------
SECTION 4 — STRING INTRINSICS
------------------------------------------------------------

4.1 LENGTH
----------
LENGTH(x):
- DISPLAY: byte length
- NATIONAL: UTF‑16 code unit length

4.2 TRIM
--------
TRIM(x):
- Removes trailing spaces

TRIM(x, LEADING):
- Removes leading spaces

TRIM(x, BOTH):
- Removes both

4.3 REVERSE
-----------
REVERSE(x):
- Reverses characters
- NATIONAL: surrogate‑pair safe

4.4 UPPER-CASE / LOWER-CASE
---------------------------
DISPLAY:
- ASCII only

NATIONAL:
- Unicode case mapping

------------------------------------------------------------
SECTION 5 — DATE/TIME INTRINSICS
------------------------------------------------------------

5.1 CURRENT-DATE
----------------
Returns:
- YYYYMMDDHHMMSShh+/-ZZZZ

5.2 WHEN-COMPILED
-----------------
Returns:
- Timestamp of compilation

------------------------------------------------------------
SECTION 6 — STATISTICAL INTRINSICS
------------------------------------------------------------

6.1 MEAN
--------
MEAN(a, b, c):
- Decimal average

6.2 MEDIAN
----------
MEDIAN(a, b, c):
- Middle value

6.3 RANGE
---------
RANGE(a, b, c):
- max - min

------------------------------------------------------------
SECTION 7 — CONVERSION INTRINSICS
------------------------------------------------------------

7.1 NUMVAL / NUMVAL-C
---------------------
NUMVAL(x):
- Converts DISPLAY numeric to Decimal

NUMVAL-C(x):
- Handles currency symbols and commas

7.2 ORD / CHAR
--------------
ORD(x):
- Character → integer code

CHAR(n):
- Integer code → character

7.3 NATIONAL-OF / DISPLAY-OF
----------------------------
NATIONAL-OF(x):
- DISPLAY → NATIONAL

DISPLAY-OF(x):
- NATIONAL → DISPLAY (ASCII only)

------------------------------------------------------------
SECTION 8 — BOOLEAN INTRINSICS
------------------------------------------------------------

8.1 BOOLEAN-OF-INTEGER
----------------------
0 → FALSE  
Non‑zero → TRUE

8.2 INTEGER-OF-BOOLEAN
----------------------
FALSE → 0  
TRUE → 1

------------------------------------------------------------
SECTION 9 — RANDOM INTRINSICS
------------------------------------------------------------

9.1 RANDOM
----------
RANDOM():
- Deterministic PRNG
- Seeded via RANDOM-SEED

9.2 RANDOM-SEED
---------------
RANDOM-SEED(x):
- Sets PRNG seed

------------------------------------------------------------
SECTION 10 — BITWISE INTRINSICS
------------------------------------------------------------

10.1 BIT-AND / BIT-OR / BIT-XOR
-------------------------------
Operate on:
- Integer values
- Bitstrings

10.2 BIT-NOT
------------
Unary bitwise negation.

------------------------------------------------------------
SECTION 11 — RUNTIME HELPERS
------------------------------------------------------------

11.1 Numeric helpers
--------------------
- DecimalMath.Sqrt
- DecimalMath.Exp
- DecimalMath.Log
- DecimalMath.Pow (restricted)

11.2 String helpers
-------------------
- Utf16Reverse
- Utf16Trim
- Utf16CaseMap

11.3 Date/time helpers
----------------------
- DateTimeProvider.Now
- Timezone offset calculation

11.4 Random helpers
-------------------
- Deterministic PRNG (Xoshiro256**)

------------------------------------------------------------
SECTION 12 — CIL LOWERING RULES
------------------------------------------------------------

12.1 Intrinsic lowering
-----------------------
FUNCTION ABS(x) → call Runtime.Abs  
FUNCTION LENGTH(x) → call Runtime.Length

12.2 Parameter lowering
-----------------------
- Evaluate arguments
- Convert to required type
- Pass to helper method

12.3 Return value lowering
--------------------------
- Convert helper return to target PIC

------------------------------------------------------------
SECTION 13 — EXCEPTION HANDLING
------------------------------------------------------------

13.1 Intrinsic errors
---------------------
- Domain errors (SQRT(-1))
- Overflow
- Invalid character
- Non‑ASCII DISPLAY conversion

13.2 Routing
------------
1. ON EXCEPTION  
2. Declarative  
3. ExceptionState  

------------------------------------------------------------
SECTION 14 — AOT/WASM‑SAFE SEMANTICS
------------------------------------------------------------

14.1 No floating‑point nondeterminism
-------------------------------------
All math uses Decimal.

14.2 No locale‑dependent behavior
---------------------------------
Case mapping and numeric parsing deterministic.

14.3 No reflection
------------------
All intrinsics statically bound.

14.4 WASM compatibility
-----------------------
All helpers pure managed.

------------------------------------------------------------
SECTION 15 — EDGE‑CASE BEHAVIOR
------------------------------------------------------------

15.1 LENGTH of empty string
---------------------------
Returns 0.

15.2 TRIM of NATIONAL with surrogate pairs
------------------------------------------
Never splits pair.

15.3 NUMVAL of invalid numeric
------------------------------
Runtime error.

15.4 RANDOM without seed
------------------------
Seed = 1.

15.5 CHAR of out‑of‑range integer
---------------------------------
Runtime error.

------------------------------------------------------------
Summary
------------------------------------------------------------
The CobolSharp Standard Library:
- Implements full COBOL intrinsic semantics
- Provides deterministic numeric, string, date/time, and statistical functions
- Uses pure managed helpers for AOT/WASM safety
- Generates clean, verifiable, debugger‑friendly CIL
- Ensures correctness across CoreCLR, AOT, and WASM
