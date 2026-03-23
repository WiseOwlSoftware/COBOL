# Section 2C: Feature Coverage — File I/O, CALL, Diagnostics

Audit date: 2026-03-22

## 1. File I/O

| Feature | Status | Where (file:class) | Quality | Notes |
|---|---|---|---|---|
| SELECT/ASSIGN | Implemented | `SemanticBuilder.cs:VisitFileControlClauseGroup`, `ProgramSymbol.cs:FileSymbol` | Spec-true | Parses literal and identifier ASSIGN targets; stores on FileSymbol |
| ORGANIZATION (SEQ/REL/IDX) | Implemented | `SemanticBuilder.cs:VisitFileControlClauseGroup`, grammar `organizationType` | Spec-true | SEQUENTIAL, RELATIVE, INDEXED, LINE SEQUENTIAL all handled |
| ACCESS MODE (SEQ/RND/DYN) | Implemented | `SemanticBuilder.cs:VisitFileControlClauseGroup` | Spec-true | Stored on `FileSymbol.AccessMode`; validated in BoundTreeValidator |
| RECORD KEY | Implemented | `SemanticBuilder.cs`, `Binder.cs` (key offset/length resolution) | Spec-true | Primary key parsed and resolved; offset computed for IndexedFileHandler |
| ALTERNATE KEY | Implemented | Grammar:`alternateKeyClause`; `SemanticBuilder.cs`; `ProgramSymbol.cs:AlternateKeyInfo`; `IndexedFileHandler` secondary indices; `FileRuntime.RegisterAlternateKey` | Spec-true | Full pipeline: parsed (ALTERNATE RECORD KEY IS ... WITH DUPLICATES), stored in FileSymbol, registered at runtime, IndexedFileHandler maintains secondary SortedDictionary per alternate key with duplicate support |
| FILE STATUS | Implemented | `FileStatusValidator.cs`, `Binder.cs:EmitFileStatus`, `IrInstruction.cs:IrStoreFileStatus`, `CilEmitter.cs:EmitStoreFileStatus` | Spec-true | Full pipeline: validation (CBL3201-3206), IR generation, CIL emission; checks alphanumeric >= 2 |
| OPEN (INPUT/OUTPUT/I-O/EXTEND) | Implemented | `BoundNodes.cs:BoundOpenStatement`, `BoundTreeBuilder.cs:BindOpen`, `BoundTreeValidator.cs:ValidateOpen`, `Binder.cs:LowerOpen` | Spec-true | All four modes; EXTEND validated against non-sequential (CBL0701) |
| CLOSE | Implemented | `BoundNodes.cs:BoundCloseStatement`, `BoundTreeBuilder.cs`, `Binder.cs:LowerClose` | Spec-true | Multi-file CLOSE supported |
| READ (sequential) | Implemented | `BoundNodes.cs:BoundReadStatement`, `Binder.cs:LowerRead`, `IrInstruction.cs:IrReadRecordToStorage` | Spec-true | AT END / NOT AT END; FILE STATUS update; READ NEXT flag |
| READ (random/keyed) | Implemented | `BoundReadStatement.KeyDataName`; IR:`IrReadByKey`; `FileRuntime.ReadByKey`; `CobolFileManager.ReadByKey` | Spec-true | RANDOM/DYNAMIC access without NEXT emits `IrReadByKey`; extracts key bytes and calls `IFileHandler.ReadByKey` |
| READ INTO | Implemented | `BoundReadStatement.Into`, `Binder.cs:LowerRead` | Spec-true | INTO target bound as identifier expression; implicit MOVE emitted after read |
| WRITE (basic) | Implemented | `BoundNodes.cs:BoundWriteStatement`, `Binder.cs:LowerWrite`, `IrInstruction.cs:IrWriteRecordFromStorage` | Spec-true | Full pipeline through CIL emission |
| WRITE FROM | Implemented | `BoundWriteStatement.From`, `BoundTreeValidator.cs:ValidateWrite` | Spec-true | Validated (CBL1801); permissive compatibility per COBOL spec |
| WRITE ADVANCING | Implemented | `BoundWriteStatement.AdvancingLines/IsAfterAdvancing`; IR:`IrWriteAdvancing` (with IsBefore); `FileRuntime.WriteAdvancing` | Spec-true | BEFORE/AFTER ADVANCING with integer lines; PAGE advancing emits form-feed (-1 sentinel) |
| REWRITE (basic) | Implemented | `BoundNodes.cs:BoundRewriteStatement`, `Binder.cs:LowerRewrite`, `IrInstruction.cs:IrRewriteRecordFromStorage` | Spec-true | Organization check (CBL1901) |
| REWRITE FROM | Implemented | `BoundRewriteStatement.From`, `BoundTreeValidator.cs:ValidateRewrite`, `Binder.cs:LowerRewrite` | Spec-true | Validated (CBL1902); FROM source MOVEd to record before rewrite |
| DELETE | Implemented | `BoundNodes.cs:BoundDeleteStatement`, `Binder.cs:LowerDelete`, `IrInstruction.cs:IrDeleteRecord` | Spec-true | INVALID KEY / NOT INVALID KEY paths; organization validated (CBL2001) |
| START (KEY IS) | Implemented | `BoundNodes.cs:BoundStartStatement`, `Binder.cs:LowerStart`, `IrInstruction.cs:IrStartFile` | Spec-true | Key condition extracted from bound tree and mapped to `StartCondition` enum (Equal/Greater/GreaterOrEqual/Less/LessOrEqual); INVALID KEY paths |

### Runtime I/O Handlers

| Handler | Status | Where | Notes |
|---|---|---|---|
| SequentialFileHandler | Implemented | `Runtime/IO/SequentialFileHandler.cs` | Fixed-length and line-sequential; all OPEN modes |
| IndexedFileHandler | Implemented | `Runtime/IO/IndexedFileHandler.cs` | SortedDictionary in-memory B+ tree; single primary key only |
| RelativeFileHandler | Implemented | `Runtime/IO/RelativeFileHandler.cs` | Fixed-length records; 1-based record number |
| IFileHandler interface | Implemented | `Runtime/IO/IFileHandler.cs` | Open, Close, ReadNext, ReadByKey, Write, Rewrite, Delete, Start |
| CobolFileManager | Implemented | `Runtime/IO/CobolFileManager.cs` | Dispatches to handler by COBOL file name |
| FileStatus codes | Implemented | `Runtime/IO/FileStatus.cs` | Status code constants for return values |

---

## 2. CALL / USING / RETURNING

| Feature | Status | Where | Quality | Notes |
|---|---|---|---|---|
| CALL statement | Implemented | `BoundCallStatement`; `Binder.cs:LowerCall`→`IrCallProgram`; `CilEmitter:EmitCallProgram`; `CobolProgramRegistry` | Spec-true | Static CALL resolves via registry; Entry method invoked; paragraph dispatch in Entry |
| BY REFERENCE | Implemented | `CobolDataPointer.CreateByReference`; `EmitLinkageLocationArgs`; static `_linkage_` fields | Spec-true | Callee's LINKAGE items alias caller's WorkingStorage bytes via CobolDataPointer |
| BY CONTENT | Implemented | `CobolDataPointer.CreateByContent`; copies argument bytes | Spec-true | Callee gets private copy; modifications don't propagate |
| BY VALUE | Implemented | Grammar gated by `{is2002()}?`; CilEmitter treats mode 2 as copy (BY CONTENT semantics) | Dialect-gated | Value encoded in source location; copy semantics prevent callee modification |
| RETURNING | Implemented | `IrCallProgram.ReturningTarget`; extra BY REFERENCE arg in CobolDataPointer[] | Spec-true | RETURNING target passed as additional arg; callee writes via LINKAGE, caller sees result |
| ON EXCEPTION / NOT ON EXCEPTION | Implemented | `IrCheckCallException`; branch on `_lastCallResult` | Spec-true | Unresolvable programs take ON EXCEPTION; successful calls take NOT ON EXCEPTION |
| Linkage Section parsing | Implemented | `SemanticBuilder.cs:VisitLinkageSection` | Spec-true | Items tagged with `StorageAreaKind.LinkageSection` |
| Linkage Section layout | Implemented | `StorageLayoutComputer`; relative offsets per 01-level | Spec-true | Each LINKAGE item layout starts at offset 0 |
| Linkage Section validation | Implemented | `SymbolValidator.cs:ValidateLinkageSection` | Spec-true | VALUE not allowed (CBL3110), REDEFINES not allowed (CBL3111) |
| PROCEDURE DIVISION USING | Implemented | `SemanticBuilder:VisitProcedureDivision`; `SemanticModel.ProcedureUsingParameters` | Spec-true | Parameters resolved to LINKAGE DataSymbols; mapped to Entry args |
| PROCEDURE DIVISION RETURNING | Implemented | `SemanticModel.ProcedureReturningItem` | Spec-true | Resolved to LINKAGE DataSymbol; mapped to args slot in Entry |
| ENTRY statement | Implemented | Grammar:`entryStatement`; `BoundEntryStatement`; `CilEmitter:EmitAlternateEntryMethod` | Spec-true | Alternate entry points; Entry_<name> methods generated |
| EXIT PROGRAM | Implemented | `BoundExitProgramStatement`; `IrExitProgram` | Spec-true | Returns from Entry method (was broken — fell through to no-op) |
| GOBACK | Implemented | `BoundGoBackStatement`; `IrGoBack` | Spec-true | Returns from called program; distinct from STOP RUN |
| Dynamic CALL | Implemented | `BoundCallStatement.IsDynamic`; `IrCallProgram.TargetLocation`; CIL reads name from storage at runtime | Spec-true | Target name decoded via GetDisplayString; registry-based resolution; Assembly.LoadFrom discovery; CBL3310 diagnostic |
| INITIAL program | Implemented | `ProgramSymbol.IsInitial`; `IrModule.IsInitial`; `CilEmitter:ResetState` | Spec-true | IsInitial extracted from PROGRAM-ID; ResetState re-creates ProgramState at each Entry call |
| CANCEL statement | Implemented | Grammar:`cancelStatement`; `BoundCancelStatement`; `IrCancelProgram`; `CobolProgramRegistry.Cancel` | Spec-true | Literal and identifier targets; removes program from registry; next CALL re-discovers |
| Inter-program communication | Implemented | `CobolProgramRegistry`; `CobolDataPointer`; `StopRunException` | Spec-true | Same-process, shared-address-space; BY REFERENCE shares memory |

### CALL Diagnostics Defined

CBL3301 (arg count mismatch), CBL3302 (invalid arg for mode), CBL3303 (type incompatible), CBL3304 (RETURNING not linkage), CBL3305 (RETURNING type mismatch), CBL3310 (dynamic CALL warning). CBL3310 is wired (emits for dynamic CALL). Others available for future compile-time validation when caller/callee are compiled together.

---

## 3. Diagnostics Infrastructure

| Feature | Status | Where | Quality | Notes |
|---|---|---|---|---|
| Error reporting mechanism | Implemented | `Diagnostics/DiagnosticBag.cs` | Spec-true | Accumulates `Diagnostic` records; `HasErrors` check; `Report`, `ReportError`, `ReportWarning` methods |
| Diagnostic code catalog | Implemented | `Diagnostics/DiagnosticDescriptors.cs` | Spec-true | 80+ descriptors across CBL0601-CBL3502; each has code, severity, message template |
| Source location tracking | Implemented | `Common/SourceLocation.cs`, `Common/TextSpan.cs` | Spec-true | File path, offset, line, column; `TextSpan` carries start/stop index |
| Error recovery in parser | Implemented | `Parsing/CobolErrorStrategy.cs` | Spec-true | 25+ pattern-matched hints for common COBOL mistakes; context-aware suggestions |
| Error cap / cascade prevention | Implemented | `Parsing/CobolErrorListener.cs` | Spec-true | Max 20 parse errors per file; extracts `[COBOLxxxx]` codes from messages |
| Warning vs Error distinction | Implemented | `Diagnostics/DiagnosticSeverity.cs` | Spec-true | Three levels: `Info`, `Warning`, `Error`; `IsError` property on `Diagnostic` |
| Diagnostic output formatting | Implemented | `Diagnostics/Diagnostic.cs:ToString()` | Spec-true | Format: `location: severity code: message` |

### Diagnostic Code Ranges

| Range | Area | Count |
|---|---|---|
| CBL0601-0602 | SELECT/FD consistency | 1 |
| CBL0701 | OPEN enforcement | 1 |
| CBL0801-0803 | Data item classification | 3 |
| CBL0901-0905 | MOVE enforcement | 5 |
| CBL1001-1004 | VALUE clause | 4 |
| CBL1101-1105 | OCCURS / DEPENDING ON | 5 |
| CBL1201-1205 | SEARCH / SEARCH ALL | 5 |
| CBL1301-1304 | STRING | 4 |
| CBL1401-1406 | UNSTRING | 6 |
| CBL1501-1503 | INSPECT | 3 |
| CBL1601-1605 | START | 5 |
| CBL1701-1704 | READ | 4 |
| CBL1801-1803 | WRITE | 3 |
| CBL1901-1902 | REWRITE | 2 |
| CBL2001 | DELETE | 1 |
| CBL2101-2102 | RETURN (sort/merge) | 2 |
| CBL2201 | RELEASE | 1 |
| CBL2301-2308 | PERFORM | 8 |
| CBL2401-2402 | IF | 2 |
| CBL2501-2503 | EVALUATE | 3 |
| CBL2601-2605 | Arithmetic | 5 |
| CBL3001-3004 | Flow analysis | 4 |
| CBL3101-3114 | Scope & symbols | 14 |
| CBL3201-3206 | File status | 6 |
| CBL3301-3310 | CALL/USING/RETURNING | 6 |
| CBL3401-3406 | Report Writer | 6 |
| CBL3501-3502 | Strict COBOL-85 mode | 2 |

---

## Summary of Gaps

1. **Compile-time linking**: Programs resolve at runtime via registry; no cross-assembly compile-time references (future optimization).
2. **BY VALUE type widening**: BY VALUE uses copy semantics; callee-type-based value widening not implemented.
3. **StopRunException across CALL boundaries**: STOP RUN in callee currently exits callee's dispatch loop but does not propagate to unwind caller. Needs try/catch in Main.
