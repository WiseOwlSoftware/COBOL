CobolSharp COBOL Expression Evaluation & Type System Architecture (CIL‑Only)
===========================================================================

Purpose
-------
Define the authoritative rules for:
- Expression parsing
- Expression evaluation
- Type inference
- Type checking
- Numeric promotion
- Decimal alignment
- Boolean evaluation
- Comparison semantics
- Function invocation
- CIL‑friendly lowering

This document governs how CobolSharp interprets and compiles expressions across COBOL‑85 → COBOL‑2023.

------------------------------------------------------------
SECTION 1 — TYPE SYSTEM OVERVIEW
------------------------------------------------------------

CobolSharp uses a unified type system that maps COBOL types to internal semantic types and ultimately to .NET types.

1.1 Primitive types
-------------------
Alphanumeric:
- PIC X(n)
- PIC A(n)
- PIC G(n)
→ Internal type: Alphanumeric(n)
→ .NET type: string

National:
- PIC N(n)
→ Internal type: National(n)
→ .NET type: string (UTF‑16)

Numeric:
- PIC 9(n)
- PIC S9(n)
→ Internal type: Numeric(scale=0, signed?)
→ .NET type: int/long/decimal depending on size

Decimal:
- PIC 9(n)V9(m)
→ Internal type: Decimal(scale=m)
→ .NET type: decimal

Packed decimal:
- COMP‑3
→ Internal type: PackedDecimal(scale)
→ .NET type: decimal

Binary:
- COMP
- COMP‑5
→ Internal type: Binary(width)
→ .NET type: int/long

Boolean:
- Condition names (88‑levels)
→ Internal type: Boolean
→ .NET type: bool

1.2 Composite types
-------------------
Group items:
→ Internal type: Record(fields)
→ .NET type: explicit‑layout class

Arrays:
- OCCURS
→ Internal type: Array(elementType, bounds)
→ .NET type: T[] or List<T>

1.3 Type categories
-------------------
CobolSharp classifies types into:
- Alphanumeric
- National
- Numeric (binary)
- Decimal (packed or display)
- Boolean
- Record
- Array
- Object (OO)
- Generic type parameters

------------------------------------------------------------
SECTION 2 — EXPRESSION CATEGORIES
------------------------------------------------------------

CobolSharp supports the following expression categories:

2.1 Primary expressions
-----------------------
- Literals (numeric, alphanumeric, national)
- Identifiers
- Subscripts (A(I))
- Slices (A(I:J))
- Function calls
- Condition names
- OO expressions (object::method)

2.2 Unary expressions
---------------------
- NOT
- Unary minus
- Unary plus

2.3 Binary expressions
----------------------
Arithmetic:
- +, -, *, /, **

Boolean:
- AND, OR

Relational:
- =, NOT =, <, >, <=, >=
- EQUALS, GREATER THAN, LESS THAN

String:
- Concatenation via "&"

2.4 Parenthesized expressions
-----------------------------
- (expression)

------------------------------------------------------------
SECTION 3 — TYPE INFERENCE RULES
------------------------------------------------------------

3.1 Numeric promotion
---------------------
When combining numeric types:
- Binary + Binary → Binary (wider)
- Decimal + Decimal → Decimal (max scale)
- Binary + Decimal → Decimal
- Packed + Decimal → Decimal
- Packed + Packed → Decimal

3.2 Alphanumeric rules
----------------------
- Alphanumeric + Alphanumeric → Alphanumeric
- Alphanumeric + Numeric → Alphanumeric (numeric converted to DISPLAY)
- Alphanumeric + Boolean → Alphanumeric (“TRUE”/“FALSE”)

3.3 Boolean rules
-----------------
Boolean operators require Boolean operands:
- NOT Boolean → Boolean
- Boolean AND Boolean → Boolean
- Boolean OR Boolean → Boolean

3.4 Comparison rules
--------------------
Numeric comparisons:
- Promote both sides to common numeric type

Alphanumeric comparisons:
- Compare lexicographically
- Space‑pad shorter operand

Mixed numeric/alphanumeric:
- Convert alphanumeric to numeric if valid
- Otherwise ON EXCEPTION

------------------------------------------------------------
SECTION 4 — DECIMAL ALIGNMENT RULES
------------------------------------------------------------

Before arithmetic:
- Align decimal points
- Pad with zeros
- Preserve sign
- Preserve scale

Example:
A = PIC 9(3)V9(2)
B = PIC 9(2)V9(4)

COMPUTE C = A + B

Internal alignment:
A: xxx.yy
B: xx.yyyy
→ Promote to scale 4:
A: xxx.yy00
B: xx.yyyy

------------------------------------------------------------
SECTION 5 — BOOLEAN SEMANTICS
------------------------------------------------------------

5.1 Condition names (88‑levels)
-------------------------------
Condition name is true if:
- Storage matches any VALUE
- Or VALUE THRU range

5.2 Truth tables
----------------
NOT:
- NOT TRUE → FALSE
- NOT FALSE → TRUE

AND:
- TRUE AND TRUE → TRUE
- Otherwise FALSE

OR:
- FALSE OR FALSE → FALSE
- Otherwise TRUE

------------------------------------------------------------
SECTION 6 — COMPARISON SEMANTICS
------------------------------------------------------------

6.1 Numeric comparisons
-----------------------
- Compare after promotion
- Compare signed values
- Zero‑padding allowed
- Leading zeros ignored

6.2 Alphanumeric comparisons
----------------------------
- Space‑pad shorter operand
- Compare lexicographically
- National uses UTF‑16 code points

6.3 Mixed comparisons
---------------------
Rules:
1. Attempt numeric conversion of alphanumeric
2. If invalid → ON EXCEPTION
3. Compare as numeric

6.4 Boolean comparisons
-----------------------
- TRUE > FALSE
- TRUE = TRUE
- FALSE = FALSE

------------------------------------------------------------
SECTION 7 — FUNCTION INVOCATION SEMANTICS
------------------------------------------------------------

7.1 Intrinsic functions
-----------------------
NUMVAL:
- Convert string to numeric
- ON EXCEPTION for invalid characters

LENGTH:
- Return length of alphanumeric/national

UPPER‑CASE / LOWER‑CASE:
- Unicode‑aware

JSON/XML functions:
- Return structured data
- ON EXCEPTION for malformed input

7.2 User‑defined functions (OO)
-------------------------------
INVOKE object::Method USING args

Type rules:
- Parameter types must match or be convertible
- Return type inferred from method signature

------------------------------------------------------------
SECTION 8 — CIL LOWERING RULES
------------------------------------------------------------

8.1 Arithmetic lowering
-----------------------
Decimal arithmetic:
- Lower to calls into NumericEngine

Binary arithmetic:
- Lower to CIL opcodes (add, sub, mul, div)

8.2 Comparison lowering
-----------------------
Numeric:
- Lower to NumericEngine.Compare

Alphanumeric:
- Lower to String.CompareOrdinal

Boolean:
- Lower to CIL brtrue/brfalse

8.3 Function calls
------------------
Intrinsic:
- Lower to runtime helpers

OO:
- Lower to direct .NET method calls

------------------------------------------------------------
SECTION 9 — EDGE‑CASE BEHAVIOR
------------------------------------------------------------

9.1 Division by zero
--------------------
- SIZE ERROR
- ON EXCEPTION block executes
- No assignment performed

9.2 Overflow
------------
- SIZE ERROR
- ON EXCEPTION block executes

9.3 Invalid numeric conversion
------------------------------
- ON EXCEPTION
- No assignment

9.4 Mixed alphanumeric/numeric arithmetic
-----------------------------------------
- Convert alphanumeric to numeric
- ON EXCEPTION if invalid

9.5 Empty strings in numeric context
------------------------------------
- Treated as zero

9.6 Boolean in numeric context
------------------------------
- TRUE → 1
- FALSE → 0

------------------------------------------------------------
Summary
------------------------------------------------------------
The CobolSharp Expression Evaluation & Type System Architecture:
- Defines the full type system used by the compiler
- Specifies numeric promotion, decimal alignment, and comparison rules
- Governs Boolean, arithmetic, string, and mixed‑type expressions
- Ensures deterministic, COBOL‑correct behavior
- Produces CIL‑friendly expression trees
- Integrates cleanly with the optimizer and runtime
- Forms the semantic backbone of the entire compiler pipeline
