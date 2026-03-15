CobolSharp Formal Specification — Grammar, Type System, Operational Semantics & Memory Model (ISO‑Aligned, CIL‑Only)
===================================================================================================================

Purpose
-------
Define the authoritative, formal specification for CobolSharp:
- Lexical grammar
- Syntactic grammar (LL(k), ISO‑aligned)
- Type system (PIC categories, numeric hierarchy, group semantics)
- Operational semantics (statement‑level, paragraph‑level, program‑level)
- Memory model (StorageBlocks, REDEFINES, OCCURS, ODO)
- Control‑flow semantics (IF, EVALUATE, PERFORM, GO TO)
- File I/O semantics (sequential, indexed, relative)
- Exception semantics (ON EXCEPTION, declaratives)
- Intrinsic semantics (FUNCTION xxx)
- Determinism and reproducibility rules
- CIL‑level execution semantics

This document defines CobolSharp as a mathematically precise language.

------------------------------------------------------------
SECTION 1 — LEXICAL SPECIFICATION
------------------------------------------------------------

1.1 Character set
-----------------
CobolSharp source uses:
- UTF‑8 or UTF‑16 input
- Normalized to UTF‑16 internally

1.2 Tokens
----------
Tokens include:
- IDENTIFIER  
- NUMERIC‑LITERAL  
- ALPHANUMERIC‑LITERAL  
- NATIONAL‑LITERAL  
- PICTURE‑STRING  
- LEVEL‑NUMBER  
- KEYWORD  
- SYMBOL  

1.3 Identifier rules
--------------------
IDENTIFIER ::= LETTER (LETTER | DIGIT | HYPHEN)*  
Case‑insensitive.

1.4 Numeric literal rules
-------------------------
NUMERIC‑LITERAL ::= DIGIT+ [ "." DIGIT+ ] [ "E" SIGN? DIGIT+ ]

1.5 Picture string rules
------------------------
PIC ::= "PIC" WS+ PICTURE‑STRING  
PICTURE‑STRING ::= (X | 9 | S | V | P | A | N | G | Z | B | 0 | / | , | . | "(" DIGIT+ ")" )+

------------------------------------------------------------
SECTION 2 — SYNTACTIC GRAMMAR (LL(k))
------------------------------------------------------------

2.1 Program structure
---------------------
PROGRAM ::= IDENTIFICATION‑DIVISION  
            ENVIRONMENT‑DIVISION?  
            DATA‑DIVISION?  
            PROCEDURE‑DIVISION  

2.2 Data division grammar
-------------------------
DATA‑DIVISION ::= (WORKING‑STORAGE‑SECTION | LOCAL‑STORAGE‑SECTION | LINKAGE‑SECTION | FILE‑SECTION)*

DATA‑ITEM ::= LEVEL‑NUMBER IDENTIFIER PICTURE‑CLAUSE? USAGE‑CLAUSE?  
              REDEFINES‑CLAUSE? OCCURS‑CLAUSE? VALUE‑CLAUSE?  
              (DATA‑ITEM)*

2.3 Procedure division grammar
------------------------------
PROCEDURE‑DIVISION ::= (USING‑CLAUSE)? (RETURNING‑CLAUSE)?  
                       DECLARATIVES?  
                       (PARAGRAPH | SECTION)*

PARAGRAPH ::= IDENTIFIER "." STATEMENT*

SECTION ::= IDENTIFIER "SECTION." PARAGRAPH*

2.4 Statement grammar (subset)
------------------------------
STATEMENT ::=  
      MOVE‑STMT  
    | ADD‑STMT  
    | SUBTRACT‑STMT  
    | MULTIPLY‑STMT  
    | DIVIDE‑STMT  
    | STRING‑STMT  
    | UNSTRING‑STMT  
    | INSPECT‑STMT  
    | IF‑STMT  
    | EVALUATE‑STMT  
    | PERFORM‑STMT  
    | CALL‑STMT  
    | INVOKE‑STMT  
    | READ‑STMT  
    | WRITE‑STMT  
    | REWRITE‑STMT  
    | DELETE‑STMT  
    | START‑STMT  
    | OPEN‑STMT  
    | CLOSE‑STMT  
    | GOBACK‑STMT  
    | EXIT‑STMT  

------------------------------------------------------------
SECTION 3 — TYPE SYSTEM
------------------------------------------------------------

3.1 Categories
--------------
CobolSharp defines:
- Alphanumeric (DISPLAY)
- National (UTF‑16)
- Numeric DISPLAY
- Numeric PACKED (COMP‑3)
- Numeric BINARY (COMP‑5)
- Boolean
- Group (structural)

3.2 Numeric hierarchy
---------------------
Decimal is the universal numeric type:
DISPLAY → Decimal  
COMP‑3 → Decimal  
COMP‑5 → Integer → Decimal  

3.3 Group types
---------------
Group type = ordered tuple of children.

3.4 REDEFINES
-------------
REDEFINES introduces:
- Union type  
- Overlay semantics  

3.5 OCCURS
----------
OCCURS introduces:
- Array type  
- Fixed or ODO‑bounded length  

------------------------------------------------------------
SECTION 4 — MEMORY MODEL (FORMAL)
------------------------------------------------------------

4.1 StorageBlock
----------------
A StorageBlock is a tuple:
SB = (buffer: byte[], fields: FieldMetadata[])

4.2 Field interpretation
------------------------
Interpretation function:
Interpret(SB, field, offset, length) → Value

4.3 REDEFINES
-------------
If A REDEFINES B:
offset(A) = offset(B)  
length(A) = length(B)

4.4 OCCURS
----------
If A OCCURS n TIMES:
offset(A[i]) = offset(A) + i * size(A)

4.5 ODO
-------
ActiveCount = clamp( value(ODO‑var), min, max )

------------------------------------------------------------
SECTION 5 — OPERATIONAL SEMANTICS
------------------------------------------------------------

5.1 MOVE
--------
MOVE src TO dst:
- Evaluate src  
- Convert to dst type  
- If overflow → SIZE ERROR  
- Else write bytes  

5.2 Arithmetic
--------------
ADD a TO b:
b := b + a  
Overflow → SIZE ERROR  

5.3 IF
------
IF cond THEN S1 ELSE S2:
Evaluate cond  
If true → S1  
Else → S2  

5.4 EVALUATE
------------
Pattern‑matching semantics:
EVALUATE x  
  WHEN v1 → S1  
  WHEN v2 → S2  
  WHEN OTHER → Sn  

5.5 PERFORM
-----------
PERFORM A THRU B:
Execute paragraphs A..B in order.

PERFORM UNTIL cond:
While not cond → execute body.

5.6 CALL
--------
CALL P USING args:
- Create new ExecutionContext  
- Map LINKAGE  
- Execute ENTRY  

5.7 Declaratives
----------------
If error occurs:
- If ON EXCEPTION → run handler  
- Else if declarative matches → run declarative  
- Resume after failing statement  

------------------------------------------------------------
SECTION 6 — FILE I/O SEMANTICS
------------------------------------------------------------

6.1 Sequential READ
-------------------
READ file:
- If next record exists → load into FD buffer  
- Else → AT END  

6.2 Indexed READ
----------------
READ file KEY key:
- B‑tree lookup  
- If found → load record  
- Else → INVALID KEY  

6.3 WRITE
---------
WRITE record:
- Append or insert depending on mode  

6.4 REWRITE
-----------
REWRITE record:
- Replace current record  

------------------------------------------------------------
SECTION 7 — EXCEPTION SEMANTICS
------------------------------------------------------------

7.1 ExceptionState
------------------
ExceptionState := (category, message, source, metadata)

7.2 Routing
-----------
1. ON EXCEPTION  
2. USE AFTER ERROR/EXCEPTION  
3. USE AFTER STANDARD EXCEPTION  

7.3 Resumption
--------------
After handler:
- Resume at next statement  
- ExceptionState cleared  

------------------------------------------------------------
SECTION 8 — INTRINSIC SEMANTICS
------------------------------------------------------------

8.1 FUNCTION ABS(x)
-------------------
Return |x|.

8.2 FUNCTION LENGTH(x)
----------------------
DISPLAY → byte length  
NATIONAL → UTF‑16 code units  

8.3 FUNCTION NUMVAL(x)
----------------------
Parse DISPLAY numeric → Decimal.

8.4 FUNCTION RANDOM
-------------------
Deterministic PRNG.

------------------------------------------------------------
SECTION 9 — CONTROL‑FLOW GRAPH (FORMAL)
------------------------------------------------------------

CFG = (Nodes, Edges, Entry, Exit)

Nodes:
- Basic blocks  
- Paragraph entry  
- Declarative entry  

Edges:
- Branch edges  
- Exception edges  
- PERFORM edges  

------------------------------------------------------------
SECTION 10 — CIL EXECUTION SEMANTICS
------------------------------------------------------------

10.1 Mapping
------------
Each paragraph → .NET method  
Each statement → IL block  
Each field access → buffer[offset]  

10.2 Determinism
----------------
No reflection  
No dynamic codegen  
No floating‑point  

10.3 ExecutionContext
---------------------
ExecutionContext is implicit parameter to all generated methods.

------------------------------------------------------------
SECTION 11 — FORMAL DETERMINISM RULES
------------------------------------------------------------

11.1 Same input → same output
-----------------------------
∀ programs P, inputs I:  
Eval(P, I) = Eval(P, I)

11.2 No nondeterministic sources
--------------------------------
Forbidden:
- System clock (except CURRENT‑DATE)  
- Random entropy  
- Threads  
- Parallelism  
- Locale‑dependent behavior  

11.3 WASM equivalence
---------------------
EvalCoreCLR(P, I) = EvalWASM(P, I)

------------------------------------------------------------
SECTION 12 — EDGE‑CASE SEMANTICS
------------------------------------------------------------

12.1 NATIONAL truncation
------------------------
Never split surrogate pair.

12.2 COMP‑3 odd digits
----------------------
Leading zero inserted.

12.3 GO TO into PERFORM
------------------------
Compiler restructures CFG.

12.4 ODO < min or > max
-----------------------
Clamp to bounds.

12.5 Recursive COMMON program
-----------------------------
Shared WORKING‑STORAGE.

------------------------------------------------------------
Summary
------------------------------------------------------------
The CobolSharp Formal Specification:
- Defines COBOL as a precise, deterministic language
- Aligns with ISO/IEC 1989:2023 while targeting CIL
- Formalizes grammar, type system, operational semantics, and memory model
- Ensures reproducible behavior across CoreCLR, AOT, and WASM
- Provides a mathematically rigorous foundation for the compiler and runtime
