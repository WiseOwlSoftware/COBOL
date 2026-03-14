# Technical Debt: Code Generation Gaps

Every entry here is a statement type that parses correctly but does NOT generate
correct CIL code per the ISO/IEC 1989:2023 spec. These MUST be fixed before the
compiler can be considered functional.

## Status Key
- **IMPLEMENTED** — Real code generation, output-verified by integration test
- **STUB** — Emits stderr warning at runtime, does NOT execute the statement
- **PARTIAL** — Partial implementation, noted limitations

## Statement Code Generation Audit

### Fully Implemented (28 statements)
| Statement | Emitter Method | Integration Test |
|-----------|---------------|-----------------|
| DISPLAY | EmitDisplayStatement | HelloWorld_PrintsCorrectOutput |
| STOP RUN | EmitStopRunStatement | (multiple) |
| MOVE | EmitMoveStatement | MoveStringToField, MoveAndDisplay_NumericField |
| ADD | EmitAddStatement | AddToField_UpdatesValue |
| SUBTRACT | EmitSubtractStatement | SubtractFromField |
| COMPUTE | EmitComputeStatement | ComputeExpression |
| IF | EmitIfStatement | IfElse_ThenBranch |
| PERFORM | EmitPerformStatement | PerformParagraph, PerformThru |
| GO TO | inline | (via PerformThru) |
| GO TO DEPENDING | EmitGoToDependingStatement | (jump table) |
| CONTINUE | Nop (correct) | N/A |
| EXIT | Ret | N/A |
| MULTIPLY | EmitMultiplyStatement | MultiplyStatement_CorrectResult |
| DIVIDE | EmitDivideStatement | DivideStatement_CorrectResult |
| EVALUATE | EmitEvaluateStatement | EvaluateStatement_SelectsCorrectBranch |
| SET | EmitSetStatement | SetStatement_SetsValue |
| GOBACK | EmitStopRunStatement | GobackStatement_ExitsProgram |
| ACCEPT | EmitAcceptStatement | AcceptFromDate_GetsCurrentDate |
| INITIALIZE | EmitInitializeStatement | InitializeStatement_ResetsFields |
| INSPECT | EmitInspectStatement | InspectReplacing_ReplacesCharacters |
| CALL | EmitCallStatement | CallStatement_EmitsDiagnostic |
| STRING | EmitStringStatement | (via runtime StringConcat) |
| UNSTRING | EmitUnstringStatement | (via runtime UnstringField) |
| OPEN | EmitOpenStatement | FileIO_WriteAndReadBack |
| CLOSE | EmitCloseStatement | FileIO_WriteAndReadBack |
| READ | EmitReadStatement | FileIO_WriteAndReadBack |
| WRITE | EmitWriteStatement | FileIO_WriteAndReadBack |
| CANCEL | Nop (correct — .NET GC) | N/A |

### Partial Implementation (3 statements)
| Statement | Status | What's Missing |
|-----------|--------|----------------|
| REWRITE | EmitRewriteStatement | Emits code but not tested with indexed files |
| DELETE | EmitDeleteStatement | Emits code but not tested with indexed files |
| CALL | EmitCallStatement | Stub — logs to stderr. Needs .NET assembly loading |

### Stub — Emits Runtime Warning (NOT WORKING)
| Statement | What It Should Do | Blocking Issue |
|-----------|-------------------|----------------|
| START | Position for keyed sequential read | Needs IFileHandler.Start pass-through in manager |
| SORT | Sort a file by keys | Requires temp file management, merge logic |
| SEARCH | Serial/binary table search | Requires table indexing (OCCURS INDEXED BY) |
| ALTER | Modify GO TO target at runtime | Archaic — needs indirect call mechanism |
| INITIATE | Start report processing | Requires Report Writer runtime |
| GENERATE | Generate report line | Requires Report Writer runtime |
| TERMINATE | End report processing | Requires Report Writer runtime |
| INVOKE | Call OO COBOL method | Requires OO COBOL class compilation |
| RAISE | Raise exception | Requires exception handling framework |
| RESUME | Resume after exception | Requires exception handling framework |

### Priority Order for Remaining Implementation
1. **CALL** — real subprogram linkage via .NET assembly loading
2. **SEARCH** — table operations (common in business COBOL)
3. **SORT** — file sorting
4. **START** — keyed file positioning
5. **Report Writer** — specialized
6. **OO COBOL / Exceptions** — niche
7. **ALTER** — archaic
