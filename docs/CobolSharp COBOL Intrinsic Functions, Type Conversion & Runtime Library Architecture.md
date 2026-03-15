CobolSharp COBOL Intrinsic Functions, Type Conversion & Runtime Library Architecture (CIL‑Only)
===============================================================================================

Purpose
-------
Define the authoritative architecture for:
- COBOL intrinsic functions (ISO/IEC 1989:2023)
- Numeric, string, date/time, statistical, and system functions
- Type conversion rules (DISPLAY, NATIONAL, COMP, COMP‑3, COMP‑5)
- Function argument evaluation
- Deterministic rounding and scaling
- Runtime library implementation
- CIL‑friendly lowering
- AOT/WASM‑safe function execution

This document governs how CobolSharp implements intrinsic functions and type conversions on .NET.

------------------------------------------------------------
SECTION 1 — INTRINSIC FUNCTION OVERVIEW
------------------------------------------------------------

CobolSharp implements:
- All ISO/IEC 1989:2023 intrinsic functions
- Deterministic argument evaluation
- Strict type checking
- Decimal‑based numeric semantics
- UTF‑16 string semantics for NATIONAL
- ASCII semantics for DISPLAY

Intrinsic functions are executed by:
ExecutionContext.FunctionLibrary

------------------------------------------------------------
SECTION 2 — FUNCTION CATEGORIES
------------------------------------------------------------

2.1 Numeric functions
---------------------
ABS, SQRT, EXP, LOG, LOG10, SIN, COS, TAN, ASIN, ACOS, ATAN  
INTEGER, INTEGER‑PART, FRACTION‑PART  
RANDOM, REM, MOD  
MEAN, MEDIAN, VARIANCE, STANDARD‑DEVIATION

2.2 String functions
--------------------
LENGTH, TRIM, REVERSE, CONCATENATE  
UPPER‑CASE, LOWER‑CASE  
SUBSTITUTE, SUBSTITUTE‑CASE  
ORD, ORD‑MAX, ORD‑MIN  
CHAR, NATIONAL‑OF, DISPLAY‑OF

2.3 Date/time functions
-----------------------
CURRENT‑DATE  
DATE‑OF‑INTEGER  
INTEGER‑OF‑DATE  
DAY‑OF‑INTEGER  
INTEGER‑OF‑DAY

2.4 Boolean/logical functions
-----------------------------
BOOLEAN‑OF‑INTEGER  
INTEGER‑OF‑BOOLEAN

2.5 Statistical functions
-------------------------
MEAN, MEDIAN, MODE, RANGE  
VARIANCE, STANDARD‑DEVIATION

2.6 System/environment functions
--------------------------------
COMMAND‑LINE, ARGUMENT‑VALUE, ARGUMENT‑NUMBER  
LOCALE, LOCALE‑DATE, LOCALE‑TIME

------------------------------------------------------------
SECTION 3 — FUNCTION ARGUMENT EVALUATION
------------------------------------------------------------

3.1 Left‑to‑right evaluation
----------------------------
Arguments evaluated:
- Left to right
- Before function call
- With full Decimal precision

3.2 BY VALUE vs BY REFERENCE
----------------------------
Intrinsic functions always receive:
- BY VALUE arguments
- No aliasing of caller storage

3.3 Optional arguments
----------------------
Some functions allow:
- Omitted arguments
- Default values

------------------------------------------------------------
SECTION 4 — TYPE CONVERSION RULES
------------------------------------------------------------

4.1 DISPLAY numeric → Decimal
-----------------------------
- ASCII digits parsed
- Leading/trailing spaces ignored
- Sign allowed

4.2 NATIONAL numeric → Decimal
------------------------------
- UTF‑16 digits parsed
- Same rules as DISPLAY

4.3 COMP/COMP‑5 → Decimal
-------------------------
- Binary integer converted to Decimal

4.4 COMP‑3 → Decimal
--------------------
- Packed decimal unpacked
- Sign nibble validated

4.5 Decimal → DISPLAY
---------------------
- Converted using PIC rules
- Truncation/rounding applied on store

4.6 Decimal → NATIONAL
----------------------
- Converted to UTF‑16 digits

4.7 String conversions
----------------------
DISPLAY → NATIONAL:
- ASCII → UTF‑16

NATIONAL → DISPLAY:
- UTF‑16 → ASCII (error if non‑ASCII)

------------------------------------------------------------
SECTION 5 — NUMERIC FUNCTION SEMANTICS
------------------------------------------------------------

5.1 ABS
-------
ABS(x) → |x|

5.2 SQRT
--------
SQRT(x):
- x < 0 → SIZE ERROR

5.3 LOG / LOG10
---------------
LOG(x):
- x <= 0 → SIZE ERROR

5.4 RANDOM
----------
RANDOM:
- Uses deterministic PRNG seeded per ExecutionContext

5.5 INTEGER / FRACTION‑PART
---------------------------
INTEGER(x) → truncate  
FRACTION‑PART(x) → x - INTEGER(x)

5.6 REM / MOD
-------------
REM = remainder with sign of dividend  
MOD = remainder with sign of divisor

------------------------------------------------------------
SECTION 6 — STRING FUNCTION SEMANTICS
------------------------------------------------------------

6.1 LENGTH
----------
LENGTH(string):
- DISPLAY → byte count
- NATIONAL → character count

6.2 TRIM
--------
TRIM(string):
- Removes trailing spaces

6.3 REVERSE
-----------
Reverses characters, not bytes.

6.4 SUBSTITUTE
--------------
SUBSTITUTE(a, b, c):
- Replace b with c in a
- Case‑sensitive

6.5 ORD / CHAR
--------------
ORD(character):
- Returns code point

CHAR(integer):
- Returns character for code point

------------------------------------------------------------
SECTION 7 — DATE/TIME FUNCTION SEMANTICS
------------------------------------------------------------

7.1 CURRENT‑DATE
----------------
Returns:
- YYYYMMDDHHMMSShhmm
- Local time zone

7.2 INTEGER‑OF‑DATE
-------------------
Converts YYYYMMDD → integer days since epoch.

7.3 DATE‑OF‑INTEGER
-------------------
Inverse of INTEGER‑OF‑DATE.

7.4 DAY‑OF‑INTEGER / INTEGER‑OF‑DAY
-----------------------------------
Same as above but with day‑of‑year.

------------------------------------------------------------
SECTION 8 — STATISTICAL FUNCTION SEMANTICS
------------------------------------------------------------

8.1 MEAN
--------
Arithmetic mean.

8.2 MEDIAN
----------
Middle value after sorting.

8.3 VARIANCE / STANDARD‑DEVIATION
---------------------------------
Population variance.

8.4 MODE
--------
Most frequent value.

------------------------------------------------------------
SECTION 9 — FUNCTIONLIBRARY IMPLEMENTATION
------------------------------------------------------------

9.1 Numeric operations
----------------------
Use:
- System.Decimal
- System.Math for transcendental functions
- Checked arithmetic for overflow

9.2 String operations
---------------------
Use:
- UTF‑16 spans
- Zero‑allocation slicing
- AOT‑safe operations

9.3 Date/time operations
------------------------
Use:
- System.DateTime
- System.TimeSpan

9.4 Statistical operations
--------------------------
Use:
- Decimal accumulators
- Stable sorting

------------------------------------------------------------
SECTION 10 — CIL LOWERING RULES
------------------------------------------------------------

10.1 Function call lowering
---------------------------
COMPUTE x = FUNCTION ABS(y)

Lowered to:
- Evaluate y
- Call FunctionLibrary.Abs
- Store result

10.2 Argument lowering
----------------------
Arguments lowered to:
- Decimal
- string
- bool
- int

10.3 Temporary locals
---------------------
Compiler allocates:
- Locals for each argument
- Local for return value

10.4 Error handling
-------------------
SIZE ERROR:
- ExceptionState populated
- ON SIZE ERROR block executed

------------------------------------------------------------
SECTION 11 — DEBUGGER INTEGRATION
------------------------------------------------------------

Debugger shows:
- Function name
- Argument values
- Return value
- Type conversions
- ExceptionState (if any)

------------------------------------------------------------
SECTION 12 — EDGE‑CASE BEHAVIOR
------------------------------------------------------------

12.1 SQRT of negative
---------------------
SIZE ERROR.

12.2 LOG of zero or negative
----------------------------
SIZE ERROR.

12.3 RANDOM reproducibility
---------------------------
Seeded per ExecutionContext.

12.4 NATIONAL with surrogate pairs
----------------------------------
Counted as one character.

12.5 CHAR with invalid code point
---------------------------------
SIZE ERROR.

12.6 SUBSTITUTE with overlapping patterns
-----------------------------------------
Left‑to‑right, non‑recursive.

------------------------------------------------------------
Summary
------------------------------------------------------------
The CobolSharp Intrinsic Function Architecture:
- Implements full COBOL intrinsic function semantics
- Provides deterministic type conversion and numeric precision
- Integrates tightly with ExecutionContext.FunctionLibrary
- Generates clean, verifiable, debugger‑friendly CIL
- Ensures correctness across CoreCLR, AOT, and WASM
