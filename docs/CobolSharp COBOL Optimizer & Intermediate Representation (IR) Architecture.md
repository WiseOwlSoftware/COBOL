CobolSharp COBOL Optimizer & Intermediate Representation (IR) Architecture (CIL‑Only)
====================================================================================

Purpose
-------
Define the authoritative architecture for:
- CobolSharp’s Intermediate Representation (IR)
- Control‑flow graph (CFG) construction
- Data‑flow analysis
- Constant folding & propagation
- Dead‑code elimination
- Loop normalization
- PERFORM lowering optimizations
- Expression simplification
- Branch optimization
- CIL‑friendly lowering
- Debugger‑safe transformations

This document governs how CobolSharp optimizes COBOL programs before CIL generation.

------------------------------------------------------------
SECTION 1 — OPTIMIZER OVERVIEW
------------------------------------------------------------

CobolSharp uses a **multi‑stage optimizer** designed for:
- Deterministic transformations
- Debugger‑safe behavior
- CIL‑friendly output
- Preservation of COBOL semantics

Optimization stages:
1. IR construction  
2. CFG construction  
3. Data‑flow analysis  
4. Constant folding  
5. Dead‑code elimination  
6. Loop normalization  
7. Branch optimization  
8. Expression simplification  
9. CIL‑specific lowering  

------------------------------------------------------------
SECTION 2 — INTERMEDIATE REPRESENTATION (IR)
------------------------------------------------------------

2.1 IR goals
------------
CobolSharp IR must:
- Represent COBOL semantics precisely
- Support structured control flow
- Support PERFORM, GO TO, and declaratives
- Support numeric/string/JSON/XML operations
- Be easily lowered to CIL

2.2 IR structure
----------------
IR is composed of:
- IRProgram
- IRSection
- IRParagraph
- IRBasicBlock
- IRInstruction

2.3 IRInstruction categories
----------------------------
- Move
- Arithmetic
- Compare
- Branch
- Call/Invoke
- PerformEnter / PerformExit
- LoopBegin / LoopEnd
- JsonParse / JsonGenerate
- XmlParse / XmlGenerate
- FileIO operations
- Runtime service calls

2.4 SSA‑like properties (optional)
----------------------------------
CobolSharp does **not** use full SSA, but:
- Temporary values are immutable
- Data items remain mutable (COBOL semantics)
- Expression trees may be SSA‑like internally

------------------------------------------------------------
SECTION 3 — CONTROL‑FLOW GRAPH (CFG)
------------------------------------------------------------

3.1 CFG construction
--------------------
Each paragraph becomes:
- A node in the CFG
- With edges for:
  - Fall‑through
  - PERFORM calls
  - GO TO targets
  - IF/EVALUATE branches

3.2 Structured regions
----------------------
CobolSharp identifies:
- Loops
- Conditionals
- PERFORM ranges
- Declarative handlers

3.3 CFG invariants
------------------
- No unreachable nodes (after DCE)
- No irreducible loops
- No critical edges (split if needed)

------------------------------------------------------------
SECTION 4 — DATA‑FLOW ANALYSIS
------------------------------------------------------------

4.1 Analyses performed
----------------------
- Live variable analysis
- Reaching definitions
- Constant propagation
- Copy propagation
- Dead store elimination
- Nullability analysis (OO only)
- PERFORM stack correctness

4.2 COBOL‑specific constraints
-----------------------------
- Data items may alias via REDEFINES
- OCCURS DEPENDING ON affects bounds
- File buffers treated as opaque
- Packed decimal operations treated as side‑effecting

------------------------------------------------------------
SECTION 5 — CONSTANT FOLDING & PROPAGATION
------------------------------------------------------------

5.1 Foldable operations
-----------------------
- Numeric literals
- Arithmetic on literals
- Boolean expressions
- LENGTH OF literal
- FUNCTION calls with literal arguments (safe subset)

5.2 Propagation rules
---------------------
- Propagate constants through expressions
- Propagate through MOVE
- Do not propagate through REDEFINES
- Do not propagate through OCCURS

5.3 Overflow detection
----------------------
Constant folding must:
- Detect overflow
- Trigger SIZE ERROR if applicable
- Emit diagnostic if compile‑time overflow

------------------------------------------------------------
SECTION 6 — DEAD‑CODE ELIMINATION (DCE)
------------------------------------------------------------

6.1 Removable constructs
------------------------
- Unreachable paragraphs
- Unreachable blocks after GO TO
- Dead temporary values
- Dead branches (IF TRUE / IF FALSE)
- Empty paragraphs (optional)

6.2 Non‑removable constructs
----------------------------
- Declaratives
- Paragraphs referenced by ENTRY
- Paragraphs referenced by debugging metadata

------------------------------------------------------------
SECTION 7 — LOOP NORMALIZATION
------------------------------------------------------------

7.1 PERFORM UNTIL
------------------
Normalized to:
loop:
    if (condition) break
    body
    goto loop

7.2 PERFORM VARYING
-------------------
Normalized to:
init
loop:
    if (condition) break
    body
    increment
    goto loop

7.3 PERFORM TIMES
-----------------
Normalized to:
i = 1
loop:
    if (i > n) break
    body
    i++
    goto loop

7.4 Benefits
------------
- Easier CFG analysis
- Cleaner CIL lowering
- Better branch optimization

------------------------------------------------------------
SECTION 8 — BRANCH OPTIMIZATION
------------------------------------------------------------

8.1 Simplifications
-------------------
- IF TRUE → unconditional branch
- IF FALSE → fall‑through
- Remove redundant comparisons
- Merge consecutive branches
- Convert IF/ELSE to switch when possible

8.2 EVALUATE optimization
-------------------------
If all WHEN values are numeric:
- Lower to switch
- Remove redundant comparisons

8.3 GO TO optimization
----------------------
- Remove GO TO to next paragraph
- Inline trivial GO TO chains

------------------------------------------------------------
SECTION 9 — EXPRESSION SIMPLIFICATION
------------------------------------------------------------

9.1 Arithmetic simplification
-----------------------------
- x + 0 → x  
- x - 0 → x  
- x * 1 → x  
- x * 0 → 0  
- x / 1 → x  

9.2 Boolean simplification
--------------------------
- TRUE AND x → x  
- FALSE AND x → FALSE  
- TRUE OR x → TRUE  
- FALSE OR x → x  

9.3 String simplification
-------------------------
- "" & x → x  
- x & "" → x  

------------------------------------------------------------
SECTION 10 — CIL‑FRIENDLY LOWERING
------------------------------------------------------------

10.1 Structured lowering
------------------------
IR ensures:
- No irreducible loops
- No unstructured branches
- No overlapping exception regions
- No ambiguous PERFORM ranges

10.2 CIL emission
-----------------
IR lowered to:
- CIL opcodes
- Runtime service calls
- Structured try/catch blocks
- Debugger sequence points

10.3 Debugger‑safe transformations
----------------------------------
Optimizer must not:
- Reorder statements across paragraph boundaries
- Remove paragraph labels
- Remove sequence points
- Inline paragraphs (unless explicitly allowed)

------------------------------------------------------------
SECTION 11 — DEBUGGER INTEGRATION
------------------------------------------------------------

Debugger sees:
- Optimized but source‑aligned code
- Preserved paragraph/section boundaries
- Preserved PERFORM structure
- Accurate sequence points
- Accurate variable lifetimes

------------------------------------------------------------
SECTION 12 — EDGE‑CASE BEHAVIOR
------------------------------------------------------------

12.1 REDEFINES aliasing
------------------------
Optimizer must assume:
- Any write may affect aliased fields
- No constant propagation across REDEFINES

12.2 OCCURS DEPENDING ON
------------------------
Bounds must be:
- Treated as dynamic
- Not constant‑folded

12.3 GO TO into middle of paragraph
-----------------------------------
Allowed; optimizer must preserve block boundaries.

12.4 Declaratives
-----------------
Never removed or reordered.

12.5 JSON/XML operations
------------------------
Treated as side‑effecting; cannot be reordered.

------------------------------------------------------------
Summary
------------------------------------------------------------
The CobolSharp Optimizer & IR Architecture:
- Provides a structured, deterministic IR for COBOL
- Performs CFG and data‑flow analysis
- Implements constant folding, DCE, loop normalization, and branch optimization
- Preserves COBOL semantics and debugging fidelity
- Lowers cleanly to verifiable, efficient CIL
- Ensures correctness across CoreCLR, AOT, and WASM
