CobolSharp COBOL Condition Names, Boolean Evaluation & Predicate Architecture (CIL‑Only)
=======================================================================================

Purpose
-------
Define the authoritative architecture for:
- Condition names (88‑levels)
- Boolean evaluation rules
- Relational conditions (=, <>, <, >, <=, >=)
- Class tests (NUMERIC, ALPHABETIC, ALPHABETIC‑LOWER, ALPHABETIC‑UPPER)
- Sign tests (POSITIVE, NEGATIVE, ZERO)
- Condition‑name sets
- Combined conditions (AND, OR, NOT)
- Parenthesized expressions
- Short‑circuit evaluation
- Integration with ExecutionContext and runtime engines
- CIL‑friendly lowering
- AOT/WASM‑safe predicate evaluation

This document governs how CobolSharp implements COBOL’s boolean and predicate system on .NET.

------------------------------------------------------------
SECTION 1 — CONDITION NAME (88‑LEVEL) ARCHITECTURE
------------------------------------------------------------

1.1 Definition
--------------
88 condition‑name VALUE literal(s).

Example:
05 STATUS-CODE PIC 9.
   88 SUCCESS VALUE 0.
   88 FAILURE VALUE 1 2 3.

1.2 Semantics
-------------
A condition‑name is TRUE if:
- The parent field’s value matches any VALUE literal
- Otherwise FALSE

1.3 Multiple VALUEs
-------------------
88 ERROR VALUE 4 5 6.
TRUE if parent = 4 OR 5 OR 6.

1.4 VALUE RANGE
---------------
88 OK VALUE 1 THRU 10.
TRUE if parent ∈ [1,10].

1.5 Negation
------------
NOT condition‑name:
- TRUE if condition‑name is FALSE

------------------------------------------------------------
SECTION 2 — BOOLEAN STORAGE MODEL
------------------------------------------------------------

2.1 Condition‑names are not stored
----------------------------------
They are:
- Computed dynamically
- Not represented as memory fields

2.2 Boolean results
-------------------
Boolean expressions evaluate to:
- .NET bool
- Used for branching (IF, PERFORM UNTIL, EVALUATE)

------------------------------------------------------------
SECTION 3 — RELATIONAL CONDITIONS
------------------------------------------------------------

3.1 Supported operators
-----------------------
=  
<>  
<  
>  
<=  
>=  

3.2 Type rules
--------------
DISPLAY numeric → converted to Decimal  
COMP/COMP‑5 → converted to Decimal  
COMP‑3 → unpacked to Decimal  
PIC X → compared lexicographically  
PIC N → compared by UTF‑16 code units  

3.3 Mixed comparisons
---------------------
Numeric vs alphanumeric → illegal  
Alphanumeric vs national → illegal  

3.4 Collation
-------------
PIC X:
- ASCII binary comparison

PIC N:
- UTF‑16 binary comparison

------------------------------------------------------------
SECTION 4 — CLASS TESTS
------------------------------------------------------------

4.1 NUMERIC
-----------
TRUE if:
- All characters are digits
- Optional sign allowed
- Optional decimal point allowed

4.2 ALPHABETIC
--------------
TRUE if:
- All characters are A–Z or a–z
- Spaces allowed

4.3 ALPHABETIC‑LOWER
--------------------
TRUE if:
- All characters are lowercase letters or spaces

4.4 ALPHABETIC‑UPPER
--------------------
TRUE if:
- All characters are uppercase letters or spaces

4.5 NATIONAL variants
---------------------
Same rules applied to UTF‑16 characters.

------------------------------------------------------------
SECTION 5 — SIGN TESTS
------------------------------------------------------------

5.1 POSITIVE
------------
TRUE if:
- Value > 0

5.2 NEGATIVE
------------
TRUE if:
- Value < 0

5.3 ZERO
--------
TRUE if:
- Value = 0

------------------------------------------------------------
SECTION 6 — COMBINED CONDITIONS
------------------------------------------------------------

6.1 AND
-------
A AND B:
- TRUE if both A and B are TRUE

6.2 OR
------
A OR B:
- TRUE if either A or B is TRUE

6.3 NOT
-------
NOT A:
- TRUE if A is FALSE

6.4 Parentheses
---------------
Parentheses override precedence.

6.5 Precedence rules
--------------------
1. NOT  
2. AND  
3. OR  

------------------------------------------------------------
SECTION 7 — SHORT‑CIRCUIT EVALUATION
------------------------------------------------------------

CobolSharp uses **short‑circuit logic**:

A AND B:
- If A is FALSE → B not evaluated

A OR B:
- If A is TRUE → B not evaluated

This matches modern COBOL implementations and improves performance.

------------------------------------------------------------
SECTION 8 — CIL LOWERING RULES
------------------------------------------------------------

8.1 Condition‑name lowering
---------------------------
SUCCESS → call ConditionNameEvaluator(parentValue, literalSet)

8.2 Relational lowering
-----------------------
a = b → Decimal.Compare(a,b) == 0  
a < b → Decimal.Compare(a,b) < 0  
etc.

8.3 Class test lowering
-----------------------
NUMERIC → StringEngine.IsNumeric  
ALPHABETIC → StringEngine.IsAlphabetic  
etc.

8.4 Combined conditions
-----------------------
A AND B → brfalse skip_B; evaluate B  
A OR B → brtrue skip_B; evaluate B  

8.5 Parentheses
---------------
Compiler builds expression tree and emits structured IL.

------------------------------------------------------------
SECTION 9 — EVALUATE STATEMENT INTEGRATION
------------------------------------------------------------

9.1 EVALUATE TRUE
-----------------
EVALUATE TRUE
   WHEN condition‑1
   WHEN condition‑2
   WHEN OTHER

Lowered to:
- Evaluate each condition in order
- Branch to matching WHEN

9.2 EVALUATE expression
-----------------------
If numeric → switch  
If non‑numeric → if‑chain

------------------------------------------------------------
SECTION 10 — DEBUGGER INTEGRATION
------------------------------------------------------------

Debugger shows:
- Condition‑name values (TRUE/FALSE)
- Parent field value
- Relational comparisons
- Class test results
- Combined condition evaluation
- Short‑circuit behavior

------------------------------------------------------------
SECTION 11 — EDGE‑CASE BEHAVIOR
------------------------------------------------------------

11.1 Condition‑name on group item
---------------------------------
Illegal.

11.2 NUMERIC on COMP/COMP‑3
---------------------------
Illegal; NUMERIC applies to DISPLAY/NATIONAL.

11.3 Comparing NATIONAL to DISPLAY
----------------------------------
Illegal.

11.4 Comparing COMP‑3 to COMP‑5
-------------------------------
Allowed; both converted to Decimal.

11.5 NOT applied to combined conditions
---------------------------------------
NOT (A AND B) → legal.

11.6 Empty strings in class tests
---------------------------------
ALPHABETIC → TRUE  
NUMERIC → FALSE  

------------------------------------------------------------
Summary
------------------------------------------------------------
The CobolSharp Condition & Predicate Architecture:
- Implements full COBOL boolean semantics
- Supports condition‑names, relational tests, class tests, and sign tests
- Uses short‑circuit evaluation for AND/OR
- Provides deterministic