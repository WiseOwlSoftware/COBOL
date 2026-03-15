CobolSharp COBOL Compiler IL Generation, Verification & Optimization Architecture (CIL‑Only)
===========================================================================================

Purpose
-------
Define the authoritative architecture for:
- IL generation pipeline
- Method and block structure
- Sequence point emission
- Stack discipline and verifiability
- Temporary locals and register allocation
- Exception region generation
- PERFORM stack lowering
- CALL/RETURN lowering
- Expression tree lowering
- Peephole optimizations
- AOT/WASM‑safe IL patterns
- Debugger‑friendly IL structure

This document governs how CobolSharp emits CIL for COBOL programs and ensures correctness, verifiability, and performance.

------------------------------------------------------------
SECTION 1 — IL GENERATION PIPELINE OVERVIEW
------------------------------------------------------------

CobolSharp’s backend consists of:

1. AST → Control‑Flow Graph (CFG)  
2. CFG → IL Block Graph  
3. IL Block Graph → Verified IL Stream  
4. IL Stream → Portable PDB  
5. IL Stream → .NET assembly  

Goals:
- Structured IL (no irreducible flow)
- Verifiable IL (no stack imbalance)
- Debugger‑friendly sequence points
- AOT/WASM‑safe instructions only
- Deterministic output

------------------------------------------------------------
SECTION 2 — METHOD STRUCTURE
------------------------------------------------------------

Each COBOL paragraph/section becomes:
- A CIL basic block with a label
- A sequence point at entry
- A structured block for statements

Each COBOL program becomes:
- A .NET class
- With one main entry method
- With additional ENTRY methods

Method signature:
void MethodName(ExecutionContext ctx)

------------------------------------------------------------
SECTION 3 — TEMPORARY LOCALS & REGISTER ALLOCATION
------------------------------------------------------------

3.1 Temporary locals
--------------------
Used for:
- Arithmetic intermediates
- STRING/UNSTRING buffers
- JSON/XML temporary values
- Loop counters
- Condition evaluation

3.2 Local types
---------------
- Decimal
- int32
- int64
- string
- object reference
- bool

3.3 Allocation strategy
-----------------------
- Linear scan allocator
- Reuse locals when lifetimes do not overlap
- No dynamic locals (AOT‑safe)

------------------------------------------------------------
SECTION 4 — STACK DISCIPLINE & VERIFIABILITY
------------------------------------------------------------

4.1 Rules
---------
CobolSharp ensures:
- Stack height known at compile time
- No unbalanced branches
- No fall‑through into exception blocks
- No unverifiable instructions

4.2 Forbidden IL patterns
-------------------------
- unverifiable tailcalls
- unaligned access
- unverifiable pointer ops
- unverifiable constrained calls
- unverifiable exception filters

4.3 Allowed IL subset
---------------------
- ldc, ldloc, stloc
- call, callvirt
- br, brtrue, brfalse
- newobj
- leave, endfinally
- try/catch/finally

------------------------------------------------------------
SECTION 5 — CONTROL‑FLOW LOWERING
------------------------------------------------------------

5.1 IF/ELSE
-----------
Lowered to:
brfalse labelElse  
...  
br labelEnd  
labelElse:  
...  
labelEnd:

5.2 EVALUATE
------------
Lowered to:
- if‑chain
- or switch table (if numeric and dense)

5.3 PERFORM UNTIL
-----------------
Lowered to:
loopStart:
    if (condition) br loopEnd
    call paragraph
    br loopStart
loopEnd:

5.4 PERFORM VARYING
-------------------
Lowered to nested loops with explicit increment.

5.5 GO TO
---------
Lowered to:
- br to paragraph label
- PERFORM stack unwinding if needed

------------------------------------------------------------
SECTION 6 — EXCEPTION REGION GENERATION
------------------------------------------------------------

6.1 try/catch blocks
--------------------
Used for:
- File operations
- Numeric operations
- JSON/XML operations
- Report Writer operations

6.2 ExceptionState population
-----------------------------
catch (Exception ex):
    ctx.ExceptionState = ...
    br declarativeEntry

6.3 Declarative return
----------------------
Declarative ends with:
leave continuationLabel

------------------------------------------------------------
SECTION 7 — EXPRESSION TREE LOWERING
------------------------------------------------------------

7.1 Arithmetic expressions
--------------------------
COMPUTE x = a + b * c

Lowered to:
ld a  
ld b  
ld c  
mul  
add  
store x

7.2 Boolean expressions
-----------------------
Short‑circuit lowering:
A AND B:
    ld A
    brfalse end
    ld B
end:

A OR B:
    ld A
    brtrue end
    ld B
end:

7.3 String expressions
----------------------
STRING lowered to:
call ctx.StringEngine.Concat

UNSTRING lowered to:
call ctx.StringEngine.Split

------------------------------------------------------------
SECTION 8 — CALL/RETURN LOWERING
------------------------------------------------------------

8.1 CALL literal
----------------
call Program.Main

8.2 CALL identifier
-------------------
call ProgramRegistry.Lookup  
callvirt Program.Main

8.3 RETURNING
-------------
store return value in local  
ret

8.4 GOBACK
----------
ret

------------------------------------------------------------
SECTION 9 — OPTIMIZATION PASSES
------------------------------------------------------------

9.1 Peephole optimizations
--------------------------
- Remove redundant ldloc/stloc pairs
- Remove dead branches
- Collapse br to br
- Inline trivial temporaries
- Remove unreachable blocks

9.2 Constant folding
--------------------
- Numeric literals
- Boolean literals
- String literals (ASCII only)

9.3 Loop optimizations
----------------------
- Hoist invariant expressions
- Remove dead increments

9.4 COPY/REPLACE optimizations
------------------------------
- Remove dead code introduced by REPLACE
- Collapse empty paragraphs

------------------------------------------------------------
SECTION 10 — DEBUGGER‑FRIENDLY IL
------------------------------------------------------------

10.1 Sequence points
--------------------
Emitted for:
- Every COBOL statement
- Paragraph/section entry
- PERFORM entry/exit
- CALL/RETURN
- Exception handler entry

10.2 Local variable scopes
--------------------------
- Each COBOL variable mapped to debugger symbol
- Temporary locals hidden unless needed

10.3 COPY/REPLACE mapping
-------------------------
Sequence points map to:
- Original source file
- Original line/column

------------------------------------------------------------
SECTION 11 — AOT/WASM‑SAFE IL PATTERNS
------------------------------------------------------------

11.1 No reflection
------------------
No:
- DynamicMethod
- Type.GetType
- Activator.CreateInstance

11.2 No unsafe code
-------------------
No:
- pointers
- stackalloc
- unmanaged memory

11.3 No platform‑specific instructions
--------------------------------------
No:
- cpblk
- initblk
- unverifiable opcodes

11.4 Deterministic IL
---------------------
- No runtime codegen
- No JIT‑dependent behavior

------------------------------------------------------------
SECTION 12 — EDGE‑CASE BEHAVIOR
------------------------------------------------------------

12.1 Paragraph with no statements
---------------------------------
Emit:
nop  
ret (if standalone)

12.2 PERFORM THRU