# Technical Debt: Code Generation Gaps

Every entry here is a statement type that parses correctly but does NOT generate
correct CIL code per the ISO/IEC 1989:2023 spec. These MUST be fixed before the
compiler can be considered functional.

## Status Key
- **IMPLEMENTED** — Real code generation, output-verified by integration test
- **STUB** — Emits stderr warning at runtime, does NOT execute the statement

## Statement Code Generation Audit

### Fully Implemented (31 statements)
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
| GO TO DEPENDING | EmitGoToDependingStatement | (jump table via CIL switch) |
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
| REWRITE | EmitRewriteStatement | (emits code, needs indexed file test) |
| DELETE | EmitDeleteStatement | (emits code, needs indexed file test) |
| SEARCH | EmitSearchStatement | (serial search as if-else chain) |
| CANCEL | Nop (correct — .NET GC) | N/A |

### Stub — Emits Runtime Warning (NOT WORKING)
| Statement | What It Should Do | Blocking Issue |
|-----------|-------------------|----------------|
| SORT (USING/GIVING) | Sort file by keys, read from input file, write to output | Requires temp file management and merge sort. INPUT/OUTPUT PROCEDURE variants work. |
| START | Position for keyed sequential read | Needs IFileHandler.Start pass-through |
| ALTER | Modify GO TO target at runtime | Archaic — needs indirect call mechanism |
| INITIATE | Start report processing | Requires Report Writer runtime |
| GENERATE | Generate report line | Requires Report Writer runtime |
| TERMINATE | End report processing | Requires Report Writer runtime |
| INVOKE | Call OO COBOL method | Requires OO COBOL class compilation |
| RAISE | Raise exception | Requires exception handling framework |
| RESUME | Resume after exception | Requires exception handling framework |

### Notes
- **CALL**: Real implementation via `Activator.CreateInstance` — searches loaded assemblies
  for a CobolProgram subclass matching the called name. Works for statically-linked
  subprograms in the same assembly; dynamic loading from separate assemblies not yet wired.
- **SEARCH**: Emits WHEN conditions as if-else chain. Full serial search with index
  incrementing requires OCCURS INDEXED BY runtime support.
- **SORT INPUT/OUTPUT PROCEDURE**: Works — just calls the named paragraphs. Only the
  USING/GIVING file-based variant is unimplemented.
