CobolSharp COBOL Arithmetic, COMPUTE, ROUNDED & SIZE ERROR Architecture (CIL‑Only)
==================================================================================

Purpose
-------
Define the authoritative architecture for:
- Arithmetic statements (ADD, SUBTRACT, MULTIPLY, DIVIDE)
- COMPUTE expression evaluation
- ROUNDED semantics
- SIZE ERROR detection
- Decimal precision and scaling
- Temporary locals and evaluation order
- Mixed‑type arithmetic (DISPLAY, COMP, COMP‑3, COMP‑5)
- CIL‑friendly lowering
- AOT/WASM‑safe numeric operations

This document governs how CobolSharp implements COBOL arithmetic semantics on .NET.

------------------------------------------------------------
SECTION 1 — NUMERIC ENGINE OVERVIEW
------------------------------------------------------------

ExecutionContext.NumericEngine provides:
- Decimal arithmetic
- COMP/COMP‑3/COMP‑5 conversion
- ROUNDED logic
- SIZE ERROR detection
- Overflow/underflow handling
- Division‑by‑zero detection

All arithmetic uses:
- System.Decimal
- Checked operations
- Deterministic rounding
- Deterministic scaling

------------------------------------------------------------
SECTION 2 — COMPUTE STATEMENT
------------------------------------------------------------

2.1 Basic form
--------------
COMPUTE x = expression.

2.2 Expression features
-----------------------
Supports:
- Parentheses
- Unary +/-
- +, -, *, /
- Exponentiation (not ISO; not supported)
- Nested expressions
- Function calls (FUNCTION ABS, etc.)

2.3 Evaluation order
--------------------
- Left‑to‑right within precedence
- *, / before +, -
- Parentheses override precedence

2.4 Temporary locals
--------------------
Compiler generates:
- One local per intermediate value
- Locals typed as Decimal

------------------------------------------------------------
SECTION 3 — ADD / SUBTRACT / MULTIPLY / DIVIDE
------------------------------------------------------------

3.1 ADD
-------
ADD a TO b.
ADD a b c TO d.

Lowering:
- Load b
- Add a
- Store b

3.2 SUBTRACT
------------
SUBTRACT a FROM b.
SUBTRACT a b FROM c.

Lowering:
- Load b
- Subtract a
- Store b

3.3 MULTIPLY
------------
MULTIPLY a BY b.

Lowering:
- Load b
- Multiply by a
- Store b

3.4 DIVIDE
----------
DIVIDE a INTO b.
DIVIDE a BY b GIVING c.

Lowering:
- Division with checked overflow
- Division‑by‑zero → SIZE ERROR

------------------------------------------------------------
SECTION 4 — ROUNDED SEMANTICS
------------------------------------------------------------

4.1 ROUNDED applies to target
-----------------------------
COMPUTE x ROUNDED = expression.

4.2 Rounding rule
-----------------
COBOL rounding:
- If discarded digit ≥ 5 → increment last retained digit
- Else → truncate

4.3 Decimal scaling
-------------------
Target PIC determines:
- Number of decimal places
- Scaling factor

4.4 ROUNDED with multiple targets
---------------------------------
ADD a TO b ROUNDED c ROUNDED.

Each target rounded independently.

------------------------------------------------------------
SECTION 5 — SIZE ERROR SEMANTICS
------------------------------------------------------------

5.1 When SIZE ERROR occurs
--------------------------
Triggered by:
- Overflow (value too large for target PIC)
- Underflow (value too small for target PIC)
- Division by zero
- Invalid COMP‑3 sign nibble
- Invalid numeric conversion

5.2 ON SIZE ERROR
-----------------
ADD a TO b
    ON SIZE ERROR
        ...
    NOT ON SIZE ERROR
        ...

5.3 Behavior on SIZE ERROR
--------------------------
- Target NOT modified
- ExceptionState populated
- ON SIZE ERROR executed
- NOT ON SIZE ERROR skipped

5.4 Behavior without ON SIZE ERROR
----------------------------------
- Target NOT modified
- Execution continues

------------------------------------------------------------
SECTION 6 — TYPE CONVERSION RULES
------------------------------------------------------------

6.1 DISPLAY numeric → Decimal
-----------------------------
- ASCII digits parsed
- Leading/trailing spaces allowed
- Sign allowed

6.2 COMP/COMP‑5 → Decimal
-------------------------
- Binary integer converted to Decimal

6.3 COMP‑3 → Decimal
--------------------
- Packed decimal unpacked
- Sign nibble validated

6.4 Decimal → DISPLAY
---------------------
- Converted using PIC rules
- Truncation/rounding applied

6.5 Decimal → COMP/COMP‑5
-------------------------
- Checked for overflow
- Truncated to binary width

6.6 Decimal → COMP‑3
---------------------
- Packed decimal encoding
- Sign nibble applied

------------------------------------------------------------
SECTION 7 — DIVISION SEMANTICS
------------------------------------------------------------

7.1 DIVIDE INTO
---------------
DIVIDE a INTO b.
b = b / a.

7.2 DIVIDE BY
-------------
DIVIDE a BY b GIVING c.
c = a / b.

7.3 REMAINDER
-------------
DIVIDE a BY b GIVING c REMAINDER r.
r = a - (b * c).

7.4 Division by zero
--------------------
SIZE ERROR.

------------------------------------------------------------
SECTION 8 — CIL LOWERING RULES
------------------------------------------------------------

8.1 COMPUTE lowering
--------------------
Expression tree lowered to:
- ldloc temp
- add/mul/sub/div
- stloc temp
- store into target

8.2 ADD/SUBTRACT/MULTIPLY lowering
----------------------------------
Direct Decimal operations.

8.3 DIVIDE lowering
-------------------
try {
    Decimal.Divide
} catch (DivideByZeroException) {
    SIZE ERROR
}

8.4 ROUNDED lowering
--------------------
- Compute full precision
- Apply rounding based on target PIC
- Store rounded value

8.5 SIZE ERROR lowering
-----------------------
Compiler generates:
- try/catch around arithmetic
- Branch to ON SIZE ERROR block

------------------------------------------------------------
SECTION 9 — DEBUGGER INTEGRATION
------------------------------------------------------------

Debugger shows:
- Intermediate values
- Temporary locals
- Target PIC scaling
- ROUNDED flag
- SIZE ERROR state
- ExceptionState

------------------------------------------------------------
SECTION 10 — AOT/WASM‑SAFE NUMERIC EXECUTION
------------------------------------------------------------

10.1 No floating‑point
----------------------
All arithmetic uses Decimal.

10.2 No unsafe code
-------------------
No pointers or stackalloc.

10.3 Deterministic rounding
---------------------------
Same results across platforms.

------------------------------------------------------------
SECTION 11 — EDGE‑CASE BEHAVIOR
------------------------------------------------------------

11.1 Overflow without ON SIZE ERROR
-----------------------------------
Target unchanged.

11.2 ROUNDED with insufficient scale
------------------------------------
Rounded to target PIC.

11.3 COMP‑3 with odd digits
---------------------------
Leading zero inserted.

11.4 Negative zero
------------------
Normalized to zero.

11.5 Mixed DISPLAY/NATIONAL numeric
-----------------------------------
NATIONAL converted to DISPLAY if ASCII; else error.

11.6 COMPUTE with no target
---------------------------
Runtime error.

------------------------------------------------------------
Summary
------------------------------------------------------------
The CobolSharp Arithmetic Architecture:
- Implements full COBOL arithmetic semantics
- Provides deterministic Decimal‑based computation
- Supports ROUNDED, SIZE ERROR, and PIC‑based scaling
- Integrates tightly with ExecutionContext.NumericEngine
- Generates clean, verifiable, debugger‑friendly CIL
- Ensures correctness across CoreCLR, AOT, and WASM
