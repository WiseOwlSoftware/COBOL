CobolSharp COBOL Semantic Analyzer & Symbol Table Architecture (CIL‑Only)
========================================================================

Purpose
-------
Define the authoritative architecture for:
- Symbol table construction
- Name resolution
- Scope rules
- Type checking
- Data Division semantic validation
- Procedure Division semantic validation
- REDEFINES, OCCURS, and ODO validation
- Paragraph/section resolution
- CALL/INVOKE signature checking
- Constant folding and early diagnostics
- Integration with AST, preprocessor, and CIL backend

This document governs how CobolSharp performs semantic analysis before code generation.

------------------------------------------------------------
SECTION 1 — SEMANTIC ANALYZER OVERVIEW
------------------------------------------------------------

CobolSharp uses a **multi‑phase semantic analyzer**:

Phase 1: Symbol table construction  
Phase 2: Type resolution  
Phase 3: Data Division validation  
Phase 4: Procedure Division validation  
Phase 5: Control‑flow validation  
Phase 6: Constant folding  
Phase 7: Diagnostic emission  

Goals:
- Deterministic semantics
- Full COBOL‑85 → COBOL‑2023 compliance
- Early detection of errors
- Clean lowering to CIL

------------------------------------------------------------
SECTION 2 — SYMBOL TABLE ARCHITECTURE
------------------------------------------------------------

2.1 Symbol categories
---------------------
CobolSharp tracks:

Data symbols:
- Elementary items
- Group items
- REDEFINES
- OCCURS / ODO
- Condition names (88‑levels)
- File descriptors
- Linkage items

Procedure symbols:
- Sections
- Paragraphs
- ENTRY points

Program symbols:
- PROGRAM‑ID
- Nested programs
- Class/Method IDs (OO)

2.2 Symbol table structure
--------------------------
SymbolTable is hierarchical:

ProgramScope  
→ DataDivisionScope  
→ FileSectionScope  
→ WorkingStorageScope  
→ LocalStorageScope  
→ LinkageScope  
→ ProcedureDivisionScope  
   → SectionScope  
      → ParagraphScope  

2.3 Symbol resolution rules
---------------------------
Resolution order:
1. Local paragraph
2. Section
3. Program
4. Containing program (nested)
5. COPY‑introduced symbols (after expansion)

------------------------------------------------------------
SECTION 3 — DATA DIVISION SEMANTICS
------------------------------------------------------------

3.1 PIC validation
------------------
Semantic analyzer validates:
- PIC syntax
- Digit counts
- Scale (V)
- Sign rules
- COMP/COMP‑3/COMP‑5 compatibility

3.2 REDEFINES validation
------------------------
Rules:
- REDEFINES must reference a sibling item
- No circular REDEFINES
- Size mismatches allowed but flagged as warnings
- REDEFINES cannot appear inside OCCURS unless allowed by dialect

3.3 OCCURS validation
---------------------
Rules:
- OCCURS must have numeric bounds
- OCCURS DEPENDING ON must reference numeric item
- DEPENDING ON item must be in same or containing group
- Max size computed at compile time

3.4 Condition names (88‑levels)
-------------------------------
Rules:
- Must reference parent item
- VALUE clauses must match parent type
- VALUE THRU ranges validated

3.5 File Section validation
---------------------------
Rules:
- FD must have record description
- RECORD CONTAINS validated
- BLOCK CONTAINS validated
- ORGANIZATION validated

------------------------------------------------------------
SECTION 4 — PROCEDURE DIVISION SEMANTICS
------------------------------------------------------------

4.1 Paragraph/section resolution
--------------------------------
Semantic analyzer:
- Ensures unique paragraph names
- Ensures unique section names
- Resolves PERFORM targets
- Resolves GO TO targets
- Validates PERFORM THRU ranges

4.2 Statement validation
------------------------
Validates:
- MOVE type compatibility
- Arithmetic operand types
- STRING/UNSTRING target types
- INSPECT operand types
- CALL USING parameter count
- INVOKE method signatures
- JSON/XML target types

4.3 Control‑flow validation
---------------------------
Detects:
- Unreachable paragraphs
- Infinite loops (optional)
- Illegal GO TO into DECLARATIVES
- Illegal GO TO into middle of IF/EVALUATE (structured lowering)

------------------------------------------------------------
SECTION 5 — TYPE CHECKING
------------------------------------------------------------

5.1 Numeric type checking
-------------------------
Rules:
- DISPLAY → numeric conversion allowed only if valid
- COMP/COMP‑5 → binary arithmetic
- COMP‑3 → decimal arithmetic
- Mixed types → promote to decimal

5.2 Alphanumeric type checking
------------------------------
Rules:
- MOVE alphanumeric → numeric requires validation
- MOVE numeric → alphanumeric always allowed
- STRING/UNSTRING require PIC X or PIC N

5.3 Boolean type checking
-------------------------
Rules:
- Condition names → Boolean
- Boolean in numeric context → 1/0
- Boolean in alphanumeric context → “TRUE”/“FALSE”

5.4 OO type checking
--------------------
Rules:
- INVOKE object::Method must match signature
- RETURNING type validated
- GENERIC type parameters validated

------------------------------------------------------------
SECTION 6 — CONSTANT FOLDING
------------------------------------------------------------

6.1 Foldable expressions
------------------------
- Numeric literals
- Arithmetic on literals
- Boolean expressions
- LENGTH OF literal
- FUNCTION calls with literal arguments (safe subset)

6.2 Non‑foldable expressions
----------------------------
- Expressions involving data items
- Expressions involving OCCURS
- Expressions involving JSON/XML

6.3 Benefits
------------
- Faster execution
- Cleaner CIL
- Early detection of overflow

------------------------------------------------------------
SECTION 7 — DIAGNOSTIC ENGINE
------------------------------------------------------------

7.1 Diagnostic categories
-------------------------
- Error
- Warning
- Info

7.2 Common diagnostics
----------------------
- Undefined paragraph/section
- Undefined data item
- Type mismatch
- Invalid REDEFINES
- Invalid OCCURS DEPENDING ON
- Invalid CALL USING parameters
- Invalid JSON/XML mapping
- Numeric overflow (constant folding)
- Unreachable code

7.3 Diagnostic mapping
----------------------
Diagnostics map to:
- Original source span
- Expanded source span
- COPYbook file path

------------------------------------------------------------
SECTION 8 — INTEGRATION WITH AST & BACKEND
------------------------------------------------------------

8.1 AST annotations
-------------------
Semantic analyzer annotates AST nodes with:
- Resolved symbol
- Type information
- Constant value (if folded)
- Control‑flow metadata
- REDEFINES/OCCURS metadata

8.2 Backend integration
-----------------------
CIL backend uses:
- Resolved types
- Resolved paragraph/section targets
- Resolved CALL/INVOKE signatures
- Constant‑folded values
- Storage layout metadata

------------------------------------------------------------
SECTION 9 — EDGE‑CASE BEHAVIOR
------------------------------------------------------------

9.1 Duplicate paragraph names
-----------------------------
Error unless in different sections.

9.2 Paragraph name same as section name
---------------------------------------
Illegal.

9.3 REDEFINES of OCCURS DEPENDING ON
------------------------------------
Allowed but logical length must be validated.

9.4 GO TO into middle of EVALUATE
---------------------------------
Illegal; structured lowering requires block boundaries.

9.5 CALL with too many USING parameters
---------------------------------------
Error.

9.6 CALL with too few USING parameters
--------------------------------------
Warning (COBOL allows missing parameters).

9.7 Mixed national/alphanumeric groups
--------------------------------------
Illegal unless explicitly converted.

------------------------------------------------------------
Summary
------------------------------------------------------------
The CobolSharp Semantic Analyzer & Symbol Table Architecture:
- Implements full COBOL semantic rules across Data and Procedure Divisions
- Provides deterministic symbol resolution and type checking
- Validates REDEFINES, OCCURS, ODO, CALL, INVOKE, JSON/XML, and control flow
- Annotates AST for clean CIL lowering
- Emits precise diagnostics with full source mapping
- Ensures correctness across CoreCLR, AOT, and WASM
