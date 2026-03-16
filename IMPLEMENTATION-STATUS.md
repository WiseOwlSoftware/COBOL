# CobolSharp Implementation Status

Verified against source code as of 2026-03-15 (session 10).

## Fully Implemented (Binder → IR → CIL → Runtime)

| Statement | ON SIZE ERROR | ROUNDED | Notes |
|-----------|:---:|:---:|-------|
| DISPLAY | — | — | Multi-operand, fields + literals |
| MOVE | — | Yes | Numeric, alphanumeric, numeric-edited, group dispatch |
| ADD | Yes | Yes | Multi-target, accumulator pattern, ON SIZE ERROR |
| MULTIPLY | Yes | Yes | Multi-target, statement-level ArithmeticStatus |
| IF / ELSE | — | — | Nested, with conditions |
| PERFORM | — | — | Simple, TIMES, THRU (dynamic dispatch) |
| GO TO | — | — | Within paragraph, within PERFORM THRU |
| GO TO DEPENDING | — | — | CIL switch |
| ~~EVALUATE / WHEN~~ | — | — | **MOVED TO NOT IMPLEMENTED** — see below |
| OPEN / CLOSE | — | — | Sequential files |
| READ | — | — | Sequential, AT END |
| WRITE | — | — | AFTER ADVANCING, full record length |
| REWRITE / DELETE | — | — | Emit CIL, need indexed file testing |
| INITIALIZE | — | — | Zeros/spaces by category |
| ACCEPT | — | — | FROM DATE, FROM TIME, FROM CONSOLE |
| INSPECT | — | — | REPLACING ALL/LEADING/TRAILING |
| SET | — | — | Numeric and index targets |
| STRING | — | — | DELIMITED BY |
| UNSTRING | — | — | DELIMITED BY |
| SEARCH | — | — | Serial search (WHEN conditions) |
| STOP RUN / GOBACK | — | — | Program termination |
| EXIT | — | — | No-op per spec |
| CONTINUE | — | — | No-op per spec |
| CANCEL | — | — | No-op (.NET GC handles cleanup) |

## Not Implemented — Silent Skip (P0: These should be ICEs)

These statements are parsed but produce NO code. The binder silently drops them.
**Every one of these should emit a diagnostic or throw an ICE, not silently skip.**

| Statement | Bound Node | Binder Handling | What's Needed |
|-----------|-----------|----------------|---------------|
| **SUBTRACT** | BoundArithmeticStatement | `break` (no-op) | IrPicSubtract + IrPicSubtractLiteral, LowerSubtract with ON SIZE ERROR, emitter |
| **DIVIDE** | BoundArithmeticStatement | `break` (no-op) | IrPicDivide + IrPicDivideLiteral, LowerDivide with ON SIZE ERROR, emitter |
| **COMPUTE** | BoundArithmeticStatement | `break` (no-op) | Expression tree evaluation → arithmetic IR, full expression lowering |
| **CALL** | Returns null | Not bound | BoundCallStatement, parameter passing (BY REF/VAL/CONTENT), dynamic loading |
| **EVALUATE** | Returns null | Not bound | BoundEvaluateStatement, WHEN/WHEN OTHER/WHEN THRU lowering, CIL emission |

## Incomplete — Partially Implemented

| Feature | Current State | What's Missing |
|---------|--------------|----------------|
| ~~ADD ON SIZE ERROR~~ | **DONE** | Accumulator pattern with proper SIZE ERROR handling |
| **EVALUATE** | Not bound or lowered at all | Full implementation needed: WHEN, WHEN OTHER, WHEN THRU |
| **PERFORM VARYING** | Only TIMES and THRU work | VARYING/UNTIL clauses parsed but ignored in BoundTreeBuilder |
| **PERFORM UNTIL** | Not implemented | TEST BEFORE/TEST AFTER semantics |
| **ROUNDED on SUBTRACT/DIVIDE** | N/A (statements not implemented) | Will come with SUBTRACT/DIVIDE implementation |
| **Numeric-to-alphanumeric zero-pad** | Works for MoveNumericToAlphanumeric | Other MOVE paths may not zero-pad |

## Subsystem Status

| Subsystem | Status | Notes |
|-----------|--------|-------|
| Sequential File I/O | **Working** | OPEN, CLOSE, READ, WRITE, AT END, AFTER ADVANCING |
| Indexed File I/O | Skeleton | Interface defined, not wired |
| Relative File I/O | Skeleton | Interface defined, not wired |
| SORT (USING/GIVING) | Stub | INPUT/OUTPUT PROCEDURE works |
| START | Not implemented | Keyed positioning |
| Intrinsic Functions | **100%** | 60+ functions |
| COPY / REPLACE | **100%** | Preprocessor |
| Fixed/Free Form | **100%** | Lexer |
| Numeric Arithmetic | **100%** | Per NC101A conformance |
| REDEFINES | **Working** | Two-pass resolution, group recursion, own PicDescriptor |
| Report Writer | 0% | INITIATE/GENERATE/TERMINATE not lowered |
| OO COBOL | 0% | INVOKE, CLASS not lowered |
| Screen Section | 0% | Not parsed or generated |
| Exception Handling | 0% | RAISE/RESUME not lowered |
| ALTER | 0% | Archaic; needs runtime GO TO indirection |

## Silent Skip Inventory (Should Be ICEs)

The binder's `LowerStatement` has a catch-all at line 170:
```csharp
case BoundArithmeticStatement:
    break;
```
This silently drops SUBTRACT, DIVIDE, COMPUTE. Additionally, `BoundTreeBuilder.BindStatement`
returns `null` for ~20 statement types that are parsed but not bound. These nulls propagate
silently through the compilation with no diagnostic.

**Action:** Replace all silent skips with explicit diagnostics or ICEs.

## TODO/FIXME in Source

1. `RecordLayoutBuilder.cs` — "TODO: extract OCCURS count from data description"
2. `BoundTreeBuilder.cs:194` — "TODO: resolve file from record → FD relationship"

## NIST Conformance

| Test | Status | Notes |
|------|--------|-------|
| NC101A (MULTIPLY) | **PASS — byte-for-byte match** | 94/94 tests, exact output |
| NC171A (DIVIDE F1) | **PASS — 100%** | 109/109 tests |
| NC106A (SUBTRACT F1) | **PASS — 100%** | 127/127 tests |
| NC176A (ADD F1) | **PASS — 100%** | 125/125 tests |
| All others | Not yet attempted | 391 programs extracted, 4 validated |

## Priority Order for Next Implementation

Goal: full COBOL-85 compliance via NIST test suite.

1. **SUBTRACT + DIVIDE** — Core arithmetic; needed by most NIST tests
2. **COMPUTE** — Expression evaluation; very common in COBOL programs
3. **EVALUATE** — Not bound at all; multi-way branching used heavily
4. **ADD ON SIZE ERROR** — Complete the ADD statement
5. **PERFORM VARYING/UNTIL** — Required by most non-trivial programs
6. **CALL with parameters** — Inter-program communication
7. **Silent skip → ICE conversion** — Every unhandled statement should error, not silently skip
8. **Indexed/Relative file I/O** — Needed for file-oriented NIST tests
9. **SORT USING/GIVING** — Common in batch programs
