CobolSharp Compiler Pipeline — Lexing, Parsing, Semantic Binding, IL Generation & AOT/WASM Emission Architecture (CIL‑Only)
=========================================================================================================================

Purpose
-------
Define the authoritative architecture for:
- Lexical analysis (tokenization)
- Parsing (grammar, AST construction)
- Semantic binding (symbol tables, type resolution, PIC analysis)
- Control‑flow graph (CFG) construction
- Data‑flow analysis
- IL generation (CIL)
- AOT/WASM emission
- Error reporting and recovery
- Compiler services (registry, metadata, diagnostics)

This document governs how the CobolSharp compiler transforms COBOL source into verifiable, deterministic .NET assemblies.

------------------------------------------------------------
SECTION 1 — COMPILER PIPELINE OVERVIEW
------------------------------------------------------------

CobolSharp compiler pipeline stages:
1. Lexing  
2. Parsing  
3. AST normalization  
4. Semantic binding  
5. Control‑flow graph generation  
6. Data‑flow analysis  
7. IL generation  
8. AOT/WASM emission  
9. Assembly linking  
10. Metadata generation  
11. Diagnostics and error reporting  

All stages are:
- Deterministic
- Pure managed
- AOT/WASM‑safe
- Free of reflection

------------------------------------------------------------
SECTION 2 — LEXER ARCHITECTURE
------------------------------------------------------------

2.1 Input
---------
- UTF‑8 or UTF‑16 source
- Normalized to UTF‑16

2.2 Token types
---------------
- Keywords
- Identifiers
- Literals (numeric, alphanumeric, national)
- Punctuation
- Operators
- Picture strings (PIC)
- Level numbers
- Compiler directives

2.3 Picture string lexing
-------------------------
PIC X(10)  
PIC S9(5)V99 COMP‑3

Lexer produces:
- PIC token
- Raw picture string
- Attributes (sign, scale, category)

2.4 Error handling
------------------
- Unterminated literal
- Invalid character
- Invalid numeric literal

------------------------------------------------------------
SECTION 3 — PARSER ARCHITECTURE
------------------------------------------------------------

3.1 Grammar
-----------
CobolSharp uses:
- LL(k) grammar
- Predictive parsing
- No backtracking

3.2 AST nodes
-------------
Nodes include:
- Program
- Division
- Section
- Paragraph
- Statement
- Expression
- Condition
- Picture
- File description
- Class definition (OO)
- Method definition

3.3 AST normalization
---------------------
Transforms:
- THRU ranges → explicit paragraph lists
- Abbreviated IF → full IF/ELSE
- Abbreviated PERFORM → full loop structure
- EVALUATE → normalized match tree

------------------------------------------------------------
SECTION 4 — SEMANTIC BINDING
------------------------------------------------------------

4.1 Symbol tables
-----------------
Compiler builds:
- Program symbol table
- Data division symbol table
- File symbol table
- Class/method symbol tables
- Scope stack

4.2 Type resolution
-------------------
Resolves:
- PIC → numeric/alphanumeric/national type
- COMP/COMP‑3/COMP‑5 → binary/packed type
- Group items → struct type
- OO types → .NET types

4.3 StorageBlock layout
-----------------------
Compiler computes:
- Offsets
- Lengths
- Alignment
- REDEFINES overlays
- OCCURS tables
- DEPENDING ON bounds

4.4 Control‑flow binding
------------------------
Resolves:
- Paragraph labels
- PERFORM ranges
- GO TO targets
- Declarative routing

4.5 File binding
----------------
Resolves:
- FD/SD metadata
- Keys
- Record lengths
- Access modes

------------------------------------------------------------
SECTION 5 — CONTROL‑FLOW GRAPH (CFG)
------------------------------------------------------------

5.1 CFG nodes
-------------
- Basic blocks
- Branches
- Loop headers
- Exception edges
- Declarative edges

5.2 CFG construction
--------------------
Compiler builds CFG for:
- PROCEDURE DIVISION
- OO methods
- Declaratives

5.3 CFG validation
------------------
Detects:
- Unreachable code
- Infinite loops (static)
- Illegal GO TO targets
- Illegal EXIT behavior

------------------------------------------------------------
SECTION 6 — DATA‑FLOW ANALYSIS
------------------------------------------------------------

6.1 Analyses performed
----------------------
- Def‑use chains
- Live variable analysis
- Constant propagation
- Dead store elimination
- PIC scaling propagation
- Numeric category propagation

6.2 MOVE/CORRESPONDING analysis
-------------------------------
- Field‑to‑field mapping
- Overlapping detection
- Temporary buffer insertion

6.3 PERFORM analysis
--------------------
- Loop bounds
- VARYING variable lifetimes

------------------------------------------------------------
SECTION 7 — IL GENERATION ARCHITECTURE
------------------------------------------------------------

7.1 IL emitter
--------------
Generates:
- Verifiable CIL
- No unsafe code
- No unverifiable branches

7.2 Method lowering
-------------------
Each paragraph/method lowered to:
- .NET method
- Basic blocks
- Branch instructions

7.3 Data movement lowering
--------------------------
- StorageBlock loads/stores
- Numeric conversions
- Padding/truncation

7.4 Arithmetic lowering
-----------------------
- Decimal operations
- Checked overflow
- ROUNDED logic

7.5 Control‑flow lowering
-------------------------
- IF → brtrue/brfalse
- PERFORM → loops
- EVALUATE → if/else chain
- GO TO → branch

7.6 File I/O lowering
---------------------
- Calls to FileManager
- RecordBuffer loads/stores

7.7 OO lowering
---------------
- newobj
- call/callvirt
- get_Property/set_Property

------------------------------------------------------------
SECTION 8 — AOT/WASM EMISSION
------------------------------------------------------------

8.1 AOT mode
------------
Compiler emits:
- Ready‑to‑run assemblies
- No JIT required
- Pre‑computed metadata

8.2 WASM mode
-------------
Compiler emits:
- IL → WASM via .NET AOT toolchain
- No reflection
- No dynamic codegen
- No unsafe code

8.3 Restrictions
----------------
- No dynamic invocation
- No runtime type discovery
- No dynamic assembly loading

------------------------------------------------------------
SECTION 9 — METADATA GENERATION
------------------------------------------------------------

9.1 Program registry
--------------------
Compiler generates:
- Program name → .NET type map
- ENTRY name → method map

9.2 FD metadata
---------------
Includes:
- Record length
- Key offsets
- Access mode
- Collation

9.3 OO metadata
---------------
Includes:
- Class hierarchy
- Method signatures
- Property signatures

------------------------------------------------------------
SECTION 10 — DIAGNOSTICS & ERROR REPORTING
------------------------------------------------------------

10.1 Error categories
---------------------
- Lexical error
- Syntax error
- Semantic error
- Type mismatch
- PIC mismatch
- Illegal GO TO
- Illegal PERFORM
- File definition error
- OO inheritance error

10.2 Error recovery
-------------------
Parser uses:
- Panic mode
- Synchronization tokens
- Best‑effort continuation

10.3 Warning categories
-----------------------
- Unreachable code
- Unused variable
- Implicit conversion
- Truncation warning

------------------------------------------------------------
SECTION 11 — DEBUGGER INTEGRATION
------------------------------------------------------------

Compiler emits:
- Sequence points
- Local variable names
- StorageBlock field names
- Paragraph/method names
- File/line mapping

Debugger can show:
- COBOL source
- IL
- StorageBlocks
- ObjectTable
- FileManager state

------------------------------------------------------------
SECTION 12 — EDGE‑CASE BEHAVIOR
------------------------------------------------------------

12.1 REDEFINES with OCCURS
--------------------------
Allowed; compiler overlays correctly.

12.2 GO TO into middle of PERFORM
---------------------------------
Compiler restructures blocks.

12.3 EVALUATE with mixed types
------------------------------
Compiler inserts conversions.

12.4 Numeric literal overflow
-----------------------------
Compile‑time error.

12.5 PIC with invalid scale
---------------------------
Compile‑time error.

12.6 OO circular inheritance
----------------------------
Compile‑time error.

------------------------------------------------------------
Summary
------------------------------------------------------------
The CobolSharp Compiler Architecture:
- Implements a full, deterministic COBOL → CIL pipeline
- Provides robust lexing, parsing, semantic binding, CFG/DFG analysis
- Generates verifiable IL suitable for CoreCLR, AOT, and WASM
- Integrates tightly with runtime metadata and ExecutionContext
- Ensures correctness, performance, and cross‑platform determinism
