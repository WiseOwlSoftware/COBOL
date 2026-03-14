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

## Deferred Semantic Features

These features parse correctly but have incomplete semantic/code-gen implementation.
They MUST be completed for spec compliance.

| Feature | Current State | Spec Requirement |
|---------|--------------|------------------|
| Duplicate data-name resolution | First declaration wins; IN/OF qualification consumed but ignored | §8.5.3.2: unqualified ambiguous name is an error at USE; qualified name resolves to the specific declaration |
| EVALUATE WHEN THRU range | Range end parsed and consumed but not used in comparison | §14.9.13: WHEN value-1 THRU value-2 means value is in range [value-1, value-2] |
| PERFORM VARYING | Varying clause parsed; emitter generates SET+loop+ADD | Full spec has AFTER clause for nested varying, TEST BEFORE/AFTER affects loop entry |
| Abbreviated combined relations | `A > B AND C` expansion implemented | Full spec allows `A > B AND < C` (new operator with carried subject) |
| CALL BY VALUE/BY CONTENT | Parsed but parameters not passed to called program | §14.9.4: BY VALUE passes copy, BY CONTENT passes read-only copy |
| ROUNDED phrase | Consumed and ignored | §14.5.6: ROUNDED truncates to field's decimal places instead of truncating |

### Notes
- **CALL**: Real implementation via `Activator.CreateInstance` — searches loaded assemblies
  for a CobolProgram subclass matching the called name. Works for statically-linked
  subprograms in the same assembly; dynamic loading from separate assemblies not yet wired.
- **SEARCH**: Emits WHEN conditions as if-else chain. Full serial search with index
  incrementing requires OCCURS INDEXED BY runtime support.
- **SORT INPUT/OUTPUT PROCEDURE**: Works — just calls the named paragraphs. Only the
  USING/GIVING file-based variant is unimplemented.
