CobolSharp COBOL Arithmetic, ROUNDED, SIZE ERROR & Numeric Engine Architecture (CIL‑Only)
=========================================================================================

Purpose
-------
Define the authoritative architecture for:
- COBOL arithmetic operations (ADD, SUBTRACT, MULTIPLY, DIVIDE, COMPUTE)
- Decimal precision and scaling rules
- ROUNDED semantics
- ON SIZE ERROR routing
- Overflow detection
- Mixed‑type arithmetic (DISPLAY, COMP, COMP‑3, COMP‑5)
- Temporary arithmetic registers
- Integration with ExecutionContext.NumericEngine
- CIL‑friendly lowering
- AOT/WASM‑safe numeric operations

This document governs how CobolSharp implements COBOL’s arithmetic model on .NET.

------------------------------------------------------------
SECTION 1 — NUMERIC ENGINE OVERVIEW
------------------------------------------------------------

ExecutionContext.NumericEngine provides:
- Decimal arithmetic
- Packed decimal (COMP‑3) encode/decode
- Binary integer (COMP/COMP‑5) encode/decode
- Overflow detection
- ROUNDED behavior
- Scaling and alignment
- Temporary arithmetic registers

CobolSharp uses:
- System.Decimal for all intermediate arithmetic
- Strict COBOL scaling rules
- Deterministic rounding

------------------------------------------------------------
SECTION 2 — NUMERIC TYPES SUPPORTED
------------------------------------------------------------

2.1 DISPLAY numeric (PIC 9)
---------------------------
- Stored as ASCII digits
- Converted to Decimal for arithmetic

2.2 COMP (binary)
-----------------
- 2, 4, or 8 bytes
- Little‑endian integer
- Converted to Decimal for arithmetic

2.3 COMP‑5 (native binary)
--------------------------
- Same as COMP but:
- No truncation rules
- No scaling

2.4 COMP‑3 (packed decimal)
---------------------------
- BCD encoding
- Sign nibble in last byte
- Converted to Decimal for arithmetic

2.5 COMPUTATIONAL‑1 / COMPUTATIONAL‑2
-------------------------------------
Not supported (floating‑point types).

------------------------------------------------------------
SECTION 3 — ARITHMETIC STATEMENTS
------------------------------------------------------------

3.1 ADD
-------
ADD a TO b  
ADD a b c TO d  
ADD a GIVING b

3.2 SUBTRACT
------------
SUBTRACT a FROM b  
SUBTRACT a b FROM c  
SUBTRACT a FROM b GIVING c

3.3 MULTIPLY
------------
MULTIPLY a BY b  
MULTIPLY a BY b GIVING c

3.4 DIVIDE
----------
DIVIDE a INTO b  
DIVIDE a INTO b GIVING c  
DIVIDE a BY b GIVING c REMAINDER r

3.5 COMPUTE
-----------
COMPUTE x = expression

Supports:
- Parentheses
- Unary +/-
- Mixed types
- Intrinsics inside expressions

------------------------------------------------------------
SECTION 4 — SCALING & PRECISION RULES
------------------------------------------------------------

4.1 Decimal alignment
---------------------
Before arithmetic:
- Align decimal points
- Extend fractional digits as needed
- No implicit truncation

4.2 Result precision
--------------------
CobolSharp uses:
- Full Decimal precision (28–29 digits)
- Then applies COBOL truncation rules when storing into target

4.3 Truncation
--------------
When storing into target:
- If target has fewer fractional digits → truncate
- If target has fewer integer digits → SIZE ERROR

------------------------------------------------------------
SECTION 5 — ROUNDED SEMANTICS
------------------------------------------------------------

5.1 ROUNDED clause
------------------
ADD a TO b ROUNDED  
COMPUTE x ROUNDED = expression

ROUNDED applies:
- Only when storing into target
- Not during intermediate arithmetic

5.2 Rounding rule
-----------------
COBOL rounding = round half up:
- If discarded digit ≥ 5 → increment last kept digit
- Else → leave unchanged

5.3 ROUNDED with multiple targets
---------------------------------
Each target is rounded independently.

------------------------------------------------------------
SECTION 6 — SIZE ERROR SEMANTICS
------------------------------------------------------------

6.1 Trigger conditions
----------------------
SIZE ERROR occurs when:
- Integer part too large for target
- Packed decimal overflow
- Binary overflow
- Division by zero
- Invalid sign nibble (COMP‑3)
- COMP/COMP‑5 out of range

6.2 ON SIZE ERROR
-----------------
ON SIZE ERROR:
- Executes handler block
- Target remains unchanged

6.3 NOT ON SIZE ERROR
---------------------
Executed only if no overflow.

6.4 ExceptionState
------------------
ExceptionState.Category = SIZE ERROR  
ExceptionState.Message = details

------------------------------------------------------------
SECTION 7 — TEMPORARY ARITHMETIC REGISTERS
------------------------------------------------------------

7.1 Purpose
-----------
Used for:
- Intermediate results
- Expression evaluation
- GIVING targets
- COMPUTE expressions

7.2 Representation
------------------
Always Decimal.

7.3 Lifetime
------------
- Allocated per statement
- Released after storing into target

------------------------------------------------------------
SECTION 8 — CIL LOWERING RULES
------------------------------------------------------------

8.1 ADD lowering
----------------
ADD a TO b:
- Load a → Decimal
- Load b → Decimal
- Add
- Store into b with scaling/rounding

8.2 SUBTRACT lowering
---------------------
SUBTRACT a FROM b:
- b = b - a

8.3 MULTIPLY lowering
---------------------
MULTIPLY a BY b:
- b = a * b

8.4 DIVIDE lowering
-------------------
DIVIDE a INTO b:
- b = b / a

DIVIDE a BY b GIVING c REMAINDER r:
- c = a / b
- r = a % b

8.5 COMPUTE lowering
--------------------
COMPUTE x = expression:
- Expression compiled to Decimal operations
- Store into x with scaling/rounding

8.6 SIZE ERROR lowering
-----------------------
Generated IL:
try {
    operation
    check overflow
} catch {
    ctx.ExceptionState = SIZE ERROR
    branch to ON SIZE ERROR
}

------------------------------------------------------------
SECTION 9 — DEBUGGER INTEGRATION
------------------------------------------------------------

Debugger shows:
- Intermediate Decimal values
- Target field before/after
- ROUNDED behavior
- SIZE ERROR state
- COMP‑3 decoded values
- Binary COMP/COMP‑5 values

------------------------------------------------------------
SECTION 10 — EDGE‑CASE BEHAVIOR
------------------------------------------------------------

10.1 Division by zero
---------------------
Triggers SIZE ERROR.

10.2 Negative zero
------------------
Normalized to zero.

10.3 COMP‑3 with odd digits
---------------------------
High nibble padded with zero.

10.4 ROUNDED with insufficient precision
----------------------------------------
Rounded result may still overflow → SIZE ERROR.

10.5 COMPUTE with nested expressions
------------------------------------
Evaluated left‑to‑right with Decimal precision.

10.6 Mixed NATIONAL + numeric
-----------------------------
Illegal unless explicitly converted.

------------------------------------------------------------
Summary
------------------------------------------------------------
The CobolSharp Numeric Engine Architecture:
- Implements full COBOL arithmetic semantics
- Supports ROUNDED, SIZE ERROR, and mixed‑type arithmetic
- Uses Decimal for all intermediate operations
- Provides deterministic scaling, truncation, and overflow rules
- Integrates tightly with ExecutionContext.NumericEngine
- Generates clean, verifiable, debugger‑friendly CIL
- Ensures correctness across CoreCLR, AOT, and WASM
