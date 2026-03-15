CobolSharp COBOL Compiler Directive & Conditional Compilation Architecture (CIL‑Only)
====================================================================================

Purpose
-------
Define the authoritative architecture for:
- Compiler directives (>>IF/>>ELSE/>>END-IF)
- >>DEFINE / >>UNDEFINE symbol management
- >>EVALUATE and expression evaluation
- >>SET and directive variables
- >>SOURCE, >>PAGE, >>DISPLAY
- Conditional compilation
- Directive scoping rules
- Interaction with COPY/REPLACE
- Preprocessor integration
- Debugger and diagnostic mapping

This document governs how CobolSharp processes compiler directives before parsing.

------------------------------------------------------------
SECTION 1 — DIRECTIVE ENGINE OVERVIEW
------------------------------------------------------------

CobolSharp implements a **full ISO/IEC 1989:2023-compliant directive engine**.

Directives are processed:
1. After COPY expansion (unless BEFORE specified)
2. Before REPLACE AFTER
3. Before tokenization
4. Before parsing

The directive engine produces:
- Filtered source text
- A directive symbol table
- A mapping table for diagnostics and debugging

------------------------------------------------------------
SECTION 2 — DIRECTIVE SYNTAX
------------------------------------------------------------

Directives begin with:
>> (two greater-than signs)

Examples:
>>IF DEBUG  
>>ELSE  
>>END-IF  
>>DEFINE DEBUG  
>>UNDEFINE DEBUG  
>>SET VAR = 42  
>>DISPLAY "Compiling..."  

Directives may appear:
- In column 1 (free-form)
- Anywhere in fixed-form (column 8+)

------------------------------------------------------------
SECTION 3 — SYMBOL MANAGEMENT
------------------------------------------------------------

3.1 >>DEFINE
------------
>>DEFINE NAME  
>>DEFINE NAME value  

Defines a symbol:
- NAME = true (if no value)
- NAME = value (string or numeric)

3.2 >>UNDEFINE
--------------
Removes symbol from directive table.

3.3 Symbol types
----------------
Symbols may be:
- Boolean
- Numeric
- String

3.4 Symbol lifetime
-------------------
Symbols persist:
- Until undefined
- Or end of compilation unit

------------------------------------------------------------
SECTION 4 — CONDITIONAL COMPILATION
------------------------------------------------------------

4.1 >>IF
--------
>>IF condition  
    text  
>>ELSE  
    text  
>>END-IF

4.2 Condition types
-------------------
Conditions may include:
- Symbol existence: IF DEBUG
- Boolean logic: IF DEBUG AND LOGGING
- Numeric comparisons: IF LEVEL > 2
- String comparisons: IF MODE = "TEST"

4.3 Operators
-------------
AND  
OR  
NOT  
=  
<>  
>  
<  
>=  
<=  

4.4 Evaluation rules
--------------------
- Undefined symbol → false
- String comparisons are case-sensitive
- Numeric comparisons require numeric values
- Boolean operators short-circuit

4.5 Nesting
-----------
Unlimited nesting depth.

4.6 Skipped regions
-------------------
Skipped text:
- Removed before tokenization
- Not visible to parser
- Not included in debugging sequence points

------------------------------------------------------------
SECTION 5 — >>EVALUATE DIRECTIVE
------------------------------------------------------------

5.1 Purpose
-----------
Evaluates an expression and assigns result to a symbol.

Syntax:
>>EVALUATE NAME = expression

Example:
>>EVALUATE LEVEL = 1 + 2 * 3

5.2 Supported expressions
-------------------------
- Numeric literals
- Symbol references
- Arithmetic operators
- Parentheses

5.3 Result type
---------------
Always numeric.

------------------------------------------------------------
SECTION 6 — >>SET DIRECTIVE
------------------------------------------------------------

6.1 Purpose
-----------
Assigns a value to a symbol.

Syntax:
>>SET NAME = value

Value may be:
- Numeric
- String
- Boolean

6.2 Difference from >>DEFINE
----------------------------
>>DEFINE creates a symbol  
>>SET modifies an existing symbol  

------------------------------------------------------------
SECTION 7 — >>DISPLAY DIRECTIVE
------------------------------------------------------------

7.1 Purpose
-----------
Emits a message during compilation.

Syntax:
>>DISPLAY "message"

Output:
- Sent to compiler console
- Not included in generated code

------------------------------------------------------------
SECTION 8 — >>SOURCE AND >>PAGE
------------------------------------------------------------

8.1 >>SOURCE
------------
Indicates source file name for debugging.

Used when:
- COPY books include embedded source metadata
- External tools generate COBOL

8.2 >>PAGE
----------
Indicates page break in listing output.

CobolSharp:
- Ignores for compilation
- Preserves for listing output (optional)

------------------------------------------------------------
SECTION 9 — DIRECTIVE SCOPING RULES
------------------------------------------------------------

9.1 Global scope
----------------
Symbols defined at top level apply to:
- Entire compilation unit
- All COPY books (unless overridden)

9.2 COPY-local scope
--------------------
COPY REPLACING BEFORE may introduce local symbols.

9.3 Nested scope
----------------
>>IF blocks create nested evaluation contexts.

------------------------------------------------------------
SECTION 10 — INTERACTION WITH COPY/REPLACE
------------------------------------------------------------

10.1 COPY BEFORE/AFTER
----------------------
REPLACE BEFORE:
- Applies before COPY expansion

REPLACE AFTER:
- Applies after COPY expansion

10.2 Directive order
--------------------
Order of operations:
1. COPY expansion  
2. REPLACE BEFORE  
3. Directive evaluation  
4. REPLACE AFTER  
5. Tokenization  

10.3 Directives inside COPY
---------------------------
Allowed:
- Evaluated in caller’s directive context

10.4 COPY inside >>IF false region
----------------------------------
COPY is skipped entirely.

------------------------------------------------------------
SECTION 11 — ERROR HANDLING
------------------------------------------------------------

11.1 Undefined symbol in expression
-----------------------------------
Evaluates to false (boolean) or zero (numeric).

11.2 Malformed directive
------------------------
Produces diagnostic:
- Line number
- Original source
- Expanded source

11.3 Unterminated >>IF
----------------------
Fatal error:
- Reports missing >>END-IF

11.4 Invalid operator
---------------------
Fatal error:
- Reports invalid token

------------------------------------------------------------
SECTION 12 — DEBUGGER INTEGRATION
------------------------------------------------------------

Debugger shows:
- Active directive symbols
- Skipped regions (grayed out)
- Expanded vs original source
- COPYbook file paths
- Directive evaluation results

Sequence points map to:
- Original source
- Not skipped regions

------------------------------------------------------------
SECTION 13 — EDGE-CASE BEHAVIOR
------------------------------------------------------------

13.1 >>IF 0
-----------
Always false.

13.2 >>IF "string"
-------------------
True if non-empty.

13.3 >>IF NOT symbol
---------------------
True if symbol undefined or false.

13.4 >>DEFINE NAME =
--------------------
Defines NAME = empty string.

13.5 >>SET NAME without value
-----------------------------
Illegal.

13.6 >>IF inside COPY
----------------------
Evaluated in caller context.

13.7 COPY inside >>IF false
---------------------------
Skipped entirely.

------------------------------------------------------------
Summary
------------------------------------------------------------
The CobolSharp Compiler Directive & Conditional Compilation Architecture:
- Implements full COBOL directive semantics
- Supports >>IF/>>ELSE/>>END-IF, >>DEFINE, >>SET, >>EVALUATE, >>DISPLAY
- Provides deterministic conditional compilation
- Integrates cleanly with COPY/REPLACE and the parser
- Produces precise source mapping for debugging and diagnostics
- Ensures correctness across CoreCLR, AOT, and WASM
