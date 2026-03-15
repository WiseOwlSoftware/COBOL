CobolSharp Optimization Pipeline Architecture (CIL‑Only)
========================================================

High‑level goals
----------------
- Transform CobolSharp IL into a cleaner, faster, more compact intermediate form before CIL emission.
- Ensure optimizations are:
  - Deterministic
  - Safe under COBOL semantics
  - Fully compatible with .NET IL generation
  - Aware of COBOL‑specific constructs (PERFORM, REDEFINES, OCCURS, packed decimal)
- Provide a modular pipeline where each optimization pass is independently testable and replaceable.
- Guarantee that optimizations never change observable COBOL behavior.

Pipeline overview
-----------------
The optimization pipeline operates on the ILModule produced by the IL generator:

SemanticModel
    ↓
ILGenerator
    ↓
ILModule (unoptimized)
    ↓
Optimization Pipeline
    ↓
ILModule (optimized)
    ↓
CIL Backend
    ↓
.NET Assembly + PDB

Optimization passes
-------------------
The pipeline consists of the following passes, executed in a well‑defined order:

1. Control‑flow simplification  
2. Constant folding  
3. Constant propagation  
4. Copy propagation  
5. Dead code elimination  
6. Redundant move elimination  
7. Strength reduction  
8. Loop optimization  
9. Branch optimization  
10. Peephole optimization  
11. Generic specialization (optional)  
12. Data layout optimization (metadata‑only)  

Each pass operates on ILBasicBlocks and ILInstructions.

1. Control‑flow simplification
------------------------------
Simplifies the control‑flow graph (CFG):

- Remove unreachable blocks
- Merge trivial blocks
- Remove redundant jumps
- Normalize PERFORM lowering
- Convert linear PERFORM chains into structured loops where safe

Ensures:
- Cleaner IL
- Better downstream optimization
- More efficient CIL emission

2. Constant folding
-------------------
Evaluates constant expressions at compile time:

Examples:
- 3 + 4 → 7
- LENGTH OF literal → constant
- Numeric literal conversions
- Boolean comparisons with constants

Special COBOL handling:
- Packed decimal literals folded using NumericEngine
- STRING/UNSTRING literal operations folded when safe

3. Constant propagation
-----------------------
Propagates known constant values through IL:

Before:
  x = 5
  y = x + 3

After:
  y = 8

Supports:
- Local variables
- Temporary values
- Simple field loads (if static and literal)

4. Copy propagation
-------------------
Eliminates unnecessary copies:

Before:
  t1 = x
  y = t1

After:
  y = x

Improves:
- Register allocation (conceptually)
- CIL emission quality

5. Dead code elimination
------------------------
Removes:
- Unused local variables
- Unreachable code
- Redundant assignments
- No‑op instructions

COBOL‑aware rules:
- Cannot remove statements with side effects (e.g., file I/O, JSON/XML, STRING/UNSTRING)
- Cannot remove moves that affect REDEFINES overlays
- Cannot remove moves that affect condition names (88‑levels)

6. Redundant move elimination
-----------------------------
COBOL generates many MOVEs that are semantically redundant.

Examples:
- MOVE A TO A
- MOVE literal TO field where literal equals default value
- MOVE field TO field where both refer to same REDEFINES region

This pass removes them safely.

7. Strength reduction
---------------------
Replaces expensive operations with cheaper equivalents.

Examples:
- Multiplication by 2 → shift left (if binary)
- Division by 2 → shift right (if binary)
- Repeated ADD → ADD with constant

Packed decimal operations are not strength‑reduced unless provably safe.

8. Loop optimization
--------------------
Optimizes loops generated from PERFORM UNTIL / PERFORM VARYING.

Transformations:
- Loop invariant code motion
- Induction variable simplification
- Bounds check hoisting (when safe)
- Early exit detection

COBOL‑aware:
- Must preserve PERFORM semantics exactly
- Must not reorder file I/O or runtime calls

9. Branch optimization
----------------------
Simplifies conditional branches:

- Remove redundant comparisons
- Collapse nested branches
- Convert branch chains into switch tables (when safe)
- Remove branches to next instruction

10. Peephole optimization
-------------------------
Local pattern‑based optimizations on IL instruction sequences.

Examples:
- LOAD x; STORE x → NOP
- LOAD literal; ADD 0 → LOAD literal
- LOAD x; LOAD x; COMPARE → DUP; COMPARE

COBOL‑aware peepholes:
- Packed decimal operations
- String slicing
- Condition name evaluation

11. Generic specialization (optional)
-------------------------------------
If COBOL generics are used, the optimizer can specialize generic methods/types when:

- Type arguments are known
- Specialization reduces runtime overhead
- Specialization does not explode code size

This is similar to .NET JIT generic specialization but done ahead‑of‑time.

12. Data layout optimization (metadata‑only)
--------------------------------------------
Optimizes metadata describing COBOL data structures:

- Collapse contiguous fields
- Remove unused metadata entries
- Normalize REDEFINES groups
- Precompute OCCURS bounds

This does not change runtime layout; it improves debugger and tooling performance.

CIL‑aware constraints
---------------------
All optimizations must preserve:

- Verifiable IL
- Stack balance
- Type correctness
- Exception region boundaries
- PDB sequence point integrity

COBOL‑aware constraints
-----------------------
Optimizations must not change:

- File I/O ordering
- Numeric precision/rounding
- Packed decimal semantics
- REDEFINES aliasing behavior
- OCCURS DEPENDING ON bounds
- Condition name evaluation
- PERFORM/GO TO control flow

Testing strategy
----------------
The optimization pipeline is validated with:

- Unit tests for each pass
- Golden IL tests (before/after)
- CIL verification tests
- Semantic equivalence tests
- Cross‑compiler behavior tests
- Regression suite

Performance strategy
--------------------
- Passes operate on SSA‑like IL where possible
- CFG cached between passes
- Peephole patterns compiled into a fast matcher
- Parallelizable across methods

Summary
-------
The CobolSharp Optimization Pipeline:
- Cleans and improves IL before CIL emission
- Ensures correctness under strict COBOL semantics
- Produces faster, smaller, more efficient .NET assemblies
- Is fully aligned with the CIL‑only architecture
- Provides a modular, testable, deterministic optimization framework
