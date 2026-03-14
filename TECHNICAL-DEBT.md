# Technical Debt: Code Generation Gaps

Every entry here is a statement type that parses correctly but does NOT generate
correct CIL code per the ISO/IEC 1989:2023 spec. These MUST be fixed before the
compiler can be considered functional.

## Status Key
- **IMPLEMENTED** — Real code generation, output-verified by integration test
- **STUB** — Emits stderr warning at runtime, does NOT execute the statement
- **PARTIAL** — Partial implementation, noted limitations

## Statement Code Generation Audit

### Fully Implemented (17 statements)
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
| CALL | EmitCallStatement (stub) | CallStatement_EmitsDiagnostic |

### Stub — Emits Runtime Warning (NOT WORKING)
These statements compile but emit a stderr diagnostic instead of executing.
A COBOL program using any of these will produce WRONG results.

| Statement | What It Should Do | Blocking Issue |
|-----------|-------------------|----------------|
| OPEN | Open file via CobolFileManager | Emitter must wire file handlers in constructor, create record buffers |
| CLOSE | Close file via CobolFileManager | Same as OPEN |
| READ | Read next/keyed record into buffer | Same as OPEN, plus AT END/INVALID KEY branching |
| WRITE | Write record buffer to file | Same as OPEN |
| REWRITE | Replace current record | Same as OPEN |
| DELETE | Delete current record | Same as OPEN |
| START | Position for keyed sequential read | Same as OPEN |
| SORT | Sort a file by keys | Requires temp file management, merge logic |
| SEARCH | Serial/binary table search | Requires table indexing (OCCURS INDEXED BY) |
| CALL | Call external program | Currently a stub that logs to stderr. Real implementation needs .NET assembly loading or compiled subprogram linking |
| CANCEL | Unload called program | No-op in .NET (GC handles this) — acceptable |
| GO TO DEPENDING | Computed GO TO | Needs jump table emission |
| ALTER | Modify GO TO target at runtime | Archaic feature — needs indirect call mechanism |
| INITIATE | Start report processing | Requires Report Writer runtime |
| GENERATE | Generate report line | Requires Report Writer runtime |
| TERMINATE | End report processing | Requires Report Writer runtime |
| INVOKE | Call OO COBOL method | Requires OO COBOL class compilation |
| RAISE | Raise exception | Requires exception handling framework |
| RESUME | Resume after exception | Requires exception handling framework |

### Priority Order for Implementation
1. **File I/O** (OPEN, CLOSE, READ, WRITE) — most COBOL programs need this
2. **CALL** — subprogram linkage is fundamental
3. **SEARCH** — table operations are common
4. **GO TO DEPENDING** — used in dispatch patterns
5. **SORT** — file sorting
6. **Report Writer** — specialized, lower priority
7. **OO COBOL / Exception Handling** — niche features
8. **ALTER** — archaic, rarely used
