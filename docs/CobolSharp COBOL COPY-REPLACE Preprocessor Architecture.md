CobolSharp COBOL COPY/REPLACE Preprocessor Architecture (CIL‑Only)
=================================================================

Purpose
-------
Define the authoritative architecture for:
- COPY expansion
- COPY REPLACING
- Nested COPY books
- Pseudo‑text delimiters
- REPLACE BEFORE/AFTER
- Compiler‑directive evaluation (>>IF/>>ELSE/>>END‑IF)
- Source mapping (original → expanded)
- Preprocessor error handling
- Integration with the parser, AST builder, and debugger

This document governs how CobolSharp preprocesses COBOL source before parsing.

------------------------------------------------------------
SECTION 1 — PREPROCESSOR OVERVIEW
------------------------------------------------------------

CobolSharp uses a **two‑phase preprocessor**:

Phase 1: COPY expansion  
Phase 2: REPLACE and compiler‑directive evaluation  

The preprocessor produces:
- Expanded source text
- A mapping table linking expanded spans to original files/lines
- A token stream for the parser

Goals:
- Deterministic expansion
- Full ISO/IEC 1989:2023 compliance
- Nested COPY support
- Precise source mapping for debugging and diagnostics

------------------------------------------------------------
SECTION 2 — COPY STATEMENT SEMANTICS
------------------------------------------------------------

2.1 Basic form
--------------
COPY copybook.

2.2 COPY with REPLACING
-----------------------
COPY copybook  
    REPLACING  
        old‑text BY new‑text  
        old‑text BY new‑text  
    .

2.3 COPY with pseudo‑text
-------------------------
COPY copybook  
    REPLACING  
        ==old== BY ==new==  
    .

Pseudo‑text:
- Delimited by ==  
- May contain spaces, punctuation, keywords  
- Must match exactly

2.4 COPY search path
--------------------
CobolSharp searches:
- Current directory
- COPY library paths (configurable)
- Embedded resources (optional)

2.5 COPY recursion
------------------
COPY inside COPY is fully supported.

Maximum depth:
- 256 nested COPYs (configurable)
- Prevents infinite recursion

------------------------------------------------------------
SECTION 3 — COPY EXPANSION ALGORITHM
------------------------------------------------------------

3.1 Steps
---------
1. Read source line  
2. Detect COPY statement  
3. Load copybook text  
4. Apply REPLACING (if present)  
5. Recursively expand nested COPYs  
6. Insert expanded text into output  
7. Update source mapping table  

3.2 COPY boundaries
-------------------
COPY ends at:
- Period (.)  
- End of line (if period omitted)  

3.3 COPY REPLACING application
------------------------------
REPLACING applies:
- Only to the copybook being expanded  
- Before nested COPY expansion  
- Left‑to‑right  
- Non‑overlapping  

3.4 COPY with pseudo‑text matching
----------------------------------
Pseudo‑text must match:
- Exact sequence of characters  
- Case‑insensitive for keywords  
- Case‑sensitive for literals  

------------------------------------------------------------
SECTION 4 — REPLACE STATEMENT SEMANTICS
------------------------------------------------------------

4.1 Basic form
--------------
REPLACE old‑text BY new‑text.

4.2 REPLACE OFF
---------------
REPLACE OFF.  
- Disables REPLACE until next REPLACE statement

4.3 REPLACE BEFORE/AFTER
------------------------
REPLACE BEFORE:  
- Applies before COPY expansion  

REPLACE AFTER:  
- Applies after COPY expansion  

CobolSharp supports both.

4.4 REPLACE scope
-----------------
REPLACE applies:
- From point of declaration  
- Until REPLACE OFF  
- Or end of compilation unit  

4.5 REPLACE matching rules
--------------------------
Matches:
- Tokens, not raw characters  
- Keyword matching is case‑insensitive  
- Literal matching is case‑sensitive  
- Pseudo‑text matching allowed  

------------------------------------------------------------
SECTION 5 — COMPILER DIRECTIVES
------------------------------------------------------------

CobolSharp supports full directive set:

>>IF  
>>ELSE  
>>END‑IF  
>>DEFINE  
>>UNDEFINE  
>>EVALUATE  
>>SET  
>>DISPLAY  
>>SOURCE  
>>PAGE  

5.1 Directive evaluation
------------------------
Directives are evaluated:
- During preprocessing  
- Before parsing  
- After COPY expansion  
- Before REPLACE AFTER  

5.2 Conditional compilation
---------------------------
>>IF condition  
    text  
>>ELSE  
    text  
>>END‑IF

Conditions may reference:
- >>DEFINE symbols  
- Numeric comparisons  
- Boolean operators  

5.3 Macro‑like behavior
-----------------------
>>DEFINE NAME value  
- Replaces NAME with value in subsequent directives  
- Does not affect COBOL tokens  

------------------------------------------------------------
SECTION 6 — SOURCE MAPPING ARCHITECTURE
------------------------------------------------------------

6.1 Mapping table
-----------------
For each expanded span, CobolSharp records:
- Expanded start offset  
- Expanded end offset  
- Original file path  
- Original line/column  

6.2 Mapping uses
----------------
Used for:
- Diagnostics  
- Debugger sequence points  
- Error messages  
- Hover info in LSP  

6.3 COPY mapping
----------------
Each COPY expansion produces:
- A mapping entry for each line  
- Nested COPYs produce nested mappings  

6.4 REPLACE mapping
-------------------
REPLACE does not alter mapping:
- Original source location preserved  
- Replacement text inherits original span  

------------------------------------------------------------
SECTION 7 — ERROR HANDLING
------------------------------------------------------------

7.1 COPY errors
---------------
Missing copybook → fatal error  
Recursive COPY loop → fatal error  
Invalid pseudo‑text → fatal error  

7.2 REPLACE errors
------------------
Invalid pseudo‑text → fatal error  
REPLACE without BY → fatal error  

7.3 Directive errors
--------------------
Undefined symbol in >>IF → false  
Malformed directive → diagnostic + ignore  

7.4 Diagnostics include:
------------------------
- File path  
- Line/column  
- Expanded source context  
- Original source context  

------------------------------------------------------------
SECTION 8 — CIL LOWERING INTEGRATION
------------------------------------------------------------

Preprocessor output feeds:
- Lexer  
- Parser  
- AST builder  
- Semantic analyzer  
- CIL backend  

Preprocessor must guarantee:
- No invalid token sequences  
- No unterminated pseudo‑text  
- No dangling directives  
- No malformed COPY statements  

------------------------------------------------------------
SECTION 9 — DEBUGGER INTEGRATION
------------------------------------------------------------

Debugger shows:
- Expanded source  
- Original source  
- COPYbook file paths  
- REPLACE substitutions  
- Directive‑controlled regions  

Sequence points map to:
- Original source  
- Not expanded source  

------------------------------------------------------------
SECTION 10 — EDGE‑CASE BEHAVIOR
------------------------------------------------------------

10.1 COPY inside REPLACE
------------------------
Allowed; REPLACE applies after COPY unless BEFORE specified.

10.2 REPLACE inside COPY
------------------------
Allowed; applies only to that copybook.

10.3 COPY with no terminating period
------------------------------------
Legal; ends at end of line.

10.4 COPY with nested REPLACING
-------------------------------
Inner REPLACING applies only to inner COPY.

10.5 Pseudo‑text containing keywords
------------------------------------
Matched literally, not as tokens.

10.6 COPY of empty file
-----------------------
Legal; produces no output.

------------------------------------------------------------
Summary
------------------------------------------------------------
The CobolSharp COPY/REPLACE Preprocessor Architecture:
- Implements full COBOL COPY, REPLACING, and directive semantics
- Supports nested COPYs, pseudo‑text, and conditional compilation
- Produces precise source mapping for debugging and diagnostics
- Ensures deterministic, CIL‑friendly preprocessing
- Integrates cleanly with the lexer, parser, and runtime
- Forms the foundation for correct COBOL compilation on .NET
