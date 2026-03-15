CobolSharp COBOL FUNCTION, Intrinsics & Runtime Library Architecture (CIL‑Only)
==============================================================================

Purpose
-------
Define the authoritative architecture for:
- COBOL intrinsic FUNCTION resolution
- Numeric, string, date/time, and statistical intrinsics
- Implementation strategy for each intrinsic category
- Deterministic, locale‑independent behavior
- CIL‑friendly lowering
- Integration with ExecutionContext and runtime engines
- AOT/WASM‑safe intrinsic implementations

This document governs how CobolSharp implements the full COBOL intrinsic function library.

------------------------------------------------------------
SECTION 1 — INTRINSIC FUNCTION MODEL OVERVIEW
------------------------------------------------------------

CobolSharp implements:
- All ISO/IEC 1989:2023 intrinsic functions
- Deterministic, side‑effect‑free behavior
- Locale‑independent semantics
- Unicode‑safe string functions
- Decimal‑safe numeric functions
- AOT/WASM‑compatible implementations

Intrinsic functions are:
- Resolved at semantic analysis time
- Lowered to calls into CobolSharp.Runtime.Functions
- Constant‑folded when all arguments are literals

------------------------------------------------------------
SECTION 2 — FUNCTION RESOLUTION
------------------------------------------------------------

2.1 Resolution rules
--------------------
FUNCTION name(arg1, arg2, ...)

Resolution steps:
1. Normalize name (case‑insensitive)
2. Look up in intrinsic function table
3. Validate argument count
4. Validate argument types
5. Determine return type
6. Annotate AST node with function metadata

2.2 Overload resolution
-----------------------
Some functions have multiple signatures:
- LENGTH
- ORD / CHAR
- NUMVAL / NUMVAL‑C

CobolSharp selects the most specific match.

2.3 Constant folding
--------------------
If all arguments are literals:
- Evaluate at compile time
- Replace with literal result
- Detect overflow or invalid arguments

------------------------------------------------------------
SECTION 3 — NUMERIC FUNCTIONS
------------------------------------------------------------

3.1 Arithmetic functions
------------------------
- ABS
- SQRT
- EXP
- LOG
- LOG10
- REM
- MOD

Implemented using:
- Decimal arithmetic (NumericEngine)
- High‑precision fallback for EXP/LOG

3.2 Statistical functions
-------------------------
- MEAN
- MEDIAN
- VARIANCE
- STANDARD‑DEVIATION

Implemented using:
- Decimal accumulation
- Stable algorithms (Kahan summation)

3.3 Random number functions
---------------------------
- RANDOM
- RANDOM‑SEED

Implemented using:
- Deterministic PRNG per ExecutionContext
- Seeded via RANDOM‑SEED

3.4 Numeric conversion functions
--------------------------------
- NUMVAL
- NUMVAL‑C
- INTEGER
- INTEGER‑OF‑DATE
- INTEGER‑OF‑DAY

NUMVAL/NUMVAL‑C:
- Strict parsing
- Decimal output
- ON EXCEPTION for invalid input

------------------------------------------------------------
SECTION 4 — STRING FUNCTIONS
------------------------------------------------------------

4.1 Basic string functions
--------------------------
- LENGTH
- REVERSE
- TRIM
- LOWER‑CASE
- UPPER‑CASE
- SUBSTITUTE
- SUBSTITUTE‑CASE

Implemented using:
- Unicode‑safe operations
- Span<char> for performance

4.2 Character code functions
----------------------------
- ORD
- ORD‑MAX
- ORD‑MIN
- CHAR

ORD:
- Returns Unicode code point

CHAR:
- Converts code point → UTF‑16

4.3 Pattern functions
---------------------
- SEARCH
- SEARCH‑ALL

SEARCH:
- First occurrence

SEARCH‑ALL:
- All occurrences (returns table)

------------------------------------------------------------
SECTION 5 — DATE/TIME FUNCTIONS
------------------------------------------------------------

5.1 Current date/time
---------------------
- CURRENT‑DATE

Returns:
- YYYYMMDDHHMMSSCC
- Time zone offset

5.2 Date arithmetic
-------------------
- DATE‑OF‑INTEGER
- DAY‑OF‑INTEGER
- INTEGER‑OF‑DATE
- INTEGER‑OF‑DAY

Implemented using:
- Gregorian calendar rules
- Leap year logic
- No locale dependence

5.3 Duration functions
----------------------
- WHEN‑COMPILED
- TIME‑OF‑DAY (legacy)

------------------------------------------------------------
SECTION 6 — BOOLEAN & COMPARISON FUNCTIONS
------------------------------------------------------------

6.1 Boolean functions
---------------------
- BOOLEAN‑OF‑INTEGER
- INTEGER‑OF‑BOOLEAN

6.2 Comparison functions
------------------------
- MAX
- MIN

Type rules:
- Mixed numeric → promote to decimal
- Mixed string → illegal

------------------------------------------------------------
SECTION 7 — TABLE & COLLECTION FUNCTIONS
------------------------------------------------------------

7.1 Table functions
-------------------
- SUM
- MEAN
- MAX
- MIN

Applied to:
- OCCURS tables
- Literal lists

7.2 Sorting functions (planned)
-------------------------------
- SORT
- SORT‑KEY

------------------------------------------------------------
SECTION 8 — IMPLEMENTATION ARCHITECTURE
------------------------------------------------------------

8.1 Function dispatch
---------------------
Each intrinsic maps to:
- A static method in CobolSharp.Runtime.Functions

Example:
FUNCTION ABS(x) → Functions.Abs(ctx, x)

8.2 NumericEngine integration
-----------------------------
Numeric functions use:
- Decimal arithmetic
- Packed decimal conversion
- Overflow detection

8.3 StringEngine integration
----------------------------
String functions use:
- Unicode‑safe slicing
- Case conversion
- Pattern matching

8.4 DateTimeEngine integration
------------------------------
Date/time functions use:
- UTC‑based calculations
- ISO‑compliant date math

------------------------------------------------------------
SECTION 9 — CIL LOWERING RULES
------------------------------------------------------------

9.1 Function call lowering
--------------------------
FUNCTION name(args)
→ call Functions.Name(ctx, args)

9.2 Constant folding lowering
-----------------------------
If folded:
- Replace with literal
- No runtime call emitted

9.3 Type conversion lowering
----------------------------
Arguments converted to:
- Decimal
- String
- Boolean
- Table reference

------------------------------------------------------------
SECTION 10 — DEBUGGER INTEGRATION
------------------------------------------------------------

Debugger shows:
- Function name
- Arguments
- Return value
- Constant‑folded vs runtime evaluation
- Overflow or ON EXCEPTION state

Sequence points emitted for:
- Function call start
- Function call end

------------------------------------------------------------
SECTION 11 — EDGE‑CASE BEHAVIOR
------------------------------------------------------------

11.1 NUMVAL on invalid string
-----------------------------
ON EXCEPTION

11.2 LENGTH of national string
------------------------------
Returns character count, not bytes.

11.3 CHAR of invalid code point
-------------------------------
ON EXCEPTION

11.4 SQRT of negative number
----------------------------
ON EXCEPTION

11.5 RANDOM without seed
------------------------
Uses ExecutionContext default seed.

11.6 SEARCH‑ALL on empty string
-------------------------------
Returns zero occurrences.

------------------------------------------------------------
Summary
------------------------------------------------------------
The CobolSharp FUNCTION, Intrinsics & Runtime Library Architecture:
- Implements the full COBOL intrinsic function set
- Provides deterministic, locale‑independent behavior
- Uses specialized runtime engines for numeric, string, and date/time operations
- Supports constant folding and compile‑time evaluation
- Generates clean, verifiable, debugger‑friendly CIL
- Ensures correctness across CoreCLR, AOT, and WASM
