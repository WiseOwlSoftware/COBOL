CobolSharp COBOL Numeric Engine & Packed Decimal Architecture (CIL‑Only)
=======================================================================

Purpose
-------
Define the authoritative architecture for:
- COBOL numeric types
- Packed decimal (COMP‑3) encoding/decoding
- Binary numeric formats (COMP, COMP‑5)
- DISPLAY numeric formats
- Decimal alignment
- Arithmetic operations
- ROUNDED and SIZE ERROR semantics
- Overflow detection
- Mixed‑type arithmetic
- CIL‑friendly lowering
- Integration with CobolSharp.Runtime.NumericEngine

This document governs how CobolSharp implements COBOL numeric behavior on .NET.

------------------------------------------------------------
SECTION 1 — NUMERIC TYPE SYSTEM
------------------------------------------------------------

CobolSharp supports the full COBOL numeric type system:

1. DISPLAY numeric  
2. COMP (binary)  
3. COMP‑5 (native binary)  
4. COMP‑3 (packed decimal)  
5. Decimal with implied V  
6. Signed/unsigned variants  
7. Scaled decimals (PIC 9(n)V9(m))  

Each numeric item is represented internally as:
- NumericType (enum)
- TotalDigits
- Scale (digits after decimal)
- Signed flag
- Storage format (DISPLAY/BINARY/PACKED)

------------------------------------------------------------
SECTION 2 — DISPLAY NUMERIC FORMAT
------------------------------------------------------------

DISPLAY numeric:
- Stored as ASCII/Unicode characters
- Right‑justified
- Zero‑padded
- Sign stored per SIGN clause:
  - Leading separate
  - Trailing separate
  - Leading embedded
  - Trailing embedded

CobolSharp normalizes DISPLAY numeric to:
- String representation
- Parsed to decimal for arithmetic
- Re‑encoded after arithmetic

------------------------------------------------------------
SECTION 3 — BINARY NUMERIC FORMATS
------------------------------------------------------------

3.1 COMP
--------
CobolSharp uses fixed mapping:
- 1–4 digits → Int16
- 5–9 digits → Int32
- 10–18 digits → Int64

Stored in little‑endian format.

3.2 COMP‑5
----------
Native binary:
- Same mapping as COMP
- No truncation on assignment
- Overflow only on arithmetic

------------------------------------------------------------
SECTION 4 — PACKED DECIMAL (COMP‑3)
------------------------------------------------------------

4.1 Encoding
------------
Packed decimal stores:
- Two digits per byte
- Last nibble = sign
- Positive: C or F
- Negative: D

Example:
PIC S9(5)V99 COMP‑3  
Total digits = 7  
Bytes = ceil((7 + 1 sign) / 2) = 4 bytes

4.2 Decoding
------------
NumericEngine:
- Reads each nibble
- Validates digits
- Extracts sign
- Produces decimal value

Invalid nibble:
- ON EXCEPTION

4.3 Encoding after arithmetic
-----------------------------
NumericEngine:
- Converts decimal to digit array
- Packs into nibbles
- Writes sign nibble
- Pads leading zero if odd digits

------------------------------------------------------------
SECTION 5 — DECIMAL ALIGNMENT
------------------------------------------------------------

Before arithmetic:
- Align decimal points
- Pad with zeros
- Preserve sign
- Promote to common scale

Example:
A = PIC 9(3)V9(2) → scale 2  
B = PIC 9(2)V9(4) → scale 4  

Promote A to scale 4:
A: xxx.yy00  
B: xx.yyyy  

------------------------------------------------------------
SECTION 6 — ARITHMETIC OPERATIONS
------------------------------------------------------------

NumericEngine implements:
- ADD
- SUBTRACT
- MULTIPLY
- DIVIDE
- COMPUTE
- Unary minus
- ROUNDED
- SIZE ERROR detection

6.1 ADD/SUBTRACT
----------------
Rules:
- Promote operands to common scale
- Perform decimal arithmetic
- Apply rounding if requested
- Check for overflow
- Write result to target

6.2 MULTIPLY
------------
Rules:
- Multiply full‑precision decimals
- Result scale = sum of operand scales
- Apply rounding if needed
- Check for overflow

6.3 DIVIDE
----------
Rules:
- Division by zero → SIZE ERROR
- Result scale = max(target scale, operand scales)
- Apply rounding if needed
- Check for overflow

6.4 COMPUTE
-----------
Rules:
- Evaluate expression left‑to‑right
- Use decimal arithmetic for mixed types
- Use binary arithmetic only if all operands are binary

------------------------------------------------------------
SECTION 7 — ROUNDED SEMANTICS
------------------------------------------------------------

ROUNDED applies only to the target field.

Rules:
- Round half‑up
- Apply rounding after arithmetic
- If target scale < result scale → round
- If target scale >= result scale → no rounding

Example:
COMPUTE A ROUNDED = 1.2345  
A = PIC 9V99 → scale 2  
Result = 1.23 (no rounding)  
If scale 2 → 1.23  
If scale 1 → 1.2 (rounded)

------------------------------------------------------------
SECTION 8 — SIZE ERROR SEMANTICS
------------------------------------------------------------

SIZE ERROR occurs when:
- Result cannot fit in target field
- Packed decimal overflow
- Binary overflow
- Division by zero
- Invalid numeric conversion

On SIZE ERROR:
- No assignment performed
- ON SIZE ERROR handler executes
- NOT ON SIZE ERROR executes only if no error

------------------------------------------------------------
SECTION 9 — MIXED‑TYPE ARITHMETIC
------------------------------------------------------------

CobolSharp uses the following promotion rules:

DISPLAY + DISPLAY → Decimal  
DISPLAY + COMP → Decimal  
DISPLAY + COMP‑3 → Decimal  
COMP + COMP → Binary  
COMP + COMP‑5 → Binary  
COMP‑3 + COMP‑3 → Decimal  
COMP‑3 + Decimal → Decimal  
Decimal + Decimal → Decimal  

Boolean in numeric context:
- TRUE → 1
- FALSE → 0

------------------------------------------------------------
SECTION 10 — CIL LOWERING RULES
------------------------------------------------------------

10.1 Arithmetic lowering
------------------------
All arithmetic lowered to:
NumericEngine.Add  
NumericEngine.Subtract  
NumericEngine.Multiply  
NumericEngine.Divide  
NumericEngine.Compute  

10.2 Binary arithmetic
----------------------
If both operands are COMP/COMP‑5:
- Lower to CIL opcodes (add, sub, mul, div)

10.3 Packed decimal lowering
----------------------------
Always lowered to NumericEngine.

10.4 Overflow detection
-----------------------
NumericEngine returns:
- Success flag
- Result value

CIL checks flag:
- If false → branch to SIZE ERROR handler

------------------------------------------------------------
SECTION 11 — DEBUGGER INTEGRATION
------------------------------------------------------------

Debugger shows:
- Raw bytes of numeric fields
- Decoded packed decimal values
- Binary values
- DISPLAY numeric strings
- Scale and sign
- Overflow state
- ROUNDED behavior
- Decimal alignment visualization

Sequence points emitted for:
- Each arithmetic operation
- Each rounding operation
- Each SIZE ERROR check

------------------------------------------------------------
SECTION 12 — EDGE‑CASE BEHAVIOR
------------------------------------------------------------

12.1 Packed decimal with invalid sign nibble
--------------------------------------------
- ON EXCEPTION

12.2 DISPLAY numeric with spaces
--------------------------------
- Treated as zero

12.3 Mixed alphanumeric/numeric arithmetic
------------------------------------------
- Convert alphanumeric to numeric
- ON EXCEPTION if invalid

12.4 Negative zero
------------------
- Preserved for packed decimal
- Normalized for DISPLAY

12.5 Overflow in intermediate expression
----------------------------------------
- SIZE ERROR even if target could hold result

12.6 Division producing infinite repeating decimal
--------------------------------------------------
- Rounded to target scale

------------------------------------------------------------
Summary
------------------------------------------------------------
The CobolSharp Numeric Engine & Packed Decimal Architecture:
- Implements full COBOL numeric semantics
- Provides packed decimal encoding/decoding
- Handles decimal alignment, rounding, and overflow
- Supports mixed‑type arithmetic with deterministic rules
- Uses NumericEngine for correctness and performance
- Generates clean, verifiable CIL
- Integrates deeply with debugging and runtime services
- Ensures correctness across CoreCLR, AOT, and WASM
