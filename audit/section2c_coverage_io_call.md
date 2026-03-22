# Section 2C: Feature Coverage — File I/O, CALL, Diagnostics

Audit date: 2026-03-22

## 1. File I/O

| Feature | Status | Where (file:class) | Quality | Notes |
|---|---|---|---|---|
| SELECT/ASSIGN | Implemented | `SemanticBuilder.cs:VisitFileControlClauseGroup`, `ProgramSymbol.cs:FileSymbol` | Spec-true | Parses literal and identifier ASSIGN targets; stores on FileSymbol |
| ORGANIZATION (SEQ/REL/IDX) | Implemented | `SemanticBuilder.cs:VisitFileControlClauseGroup`, grammar `organizationType` | Spec-true | SEQUENTIAL, RELATIVE, INDEXED, LINE SEQUENTIAL all handled |
| ACCESS MODE (SEQ/RND/DYN) | Implemented | `SemanticBuilder.cs:VisitFileControlClauseGroup` | Spec-true | Stored on `FileSymbol.AccessMode`; validated in BoundTreeValidator |
| RECORD KEY | Partially | `SemanticBuilder.cs`, `Binder.cs` (key offset/length resolution) | Spec-true | Primary key parsed and resolved; offset computed for IndexedFileHandler |
| ALTERNATE KEY | Not implemented | Grammar rule `AlternateKeyClause` exists in parser | Unknown | Parsed by ANTLR but **not visited** in SemanticBuilder; no FileSymbol property; no runtime support |
| FILE STATUS | Implemented | `FileStatusValidator.cs`, `Binder.cs:EmitFileStatus`, `IrInstruction.cs:IrStoreFileStatus`, `CilEmitter.cs:EmitStoreFileStatus` | Spec-true | Full pipeline: validation (CBL3201-3206), IR generation, CIL emission; checks alphanumeric >= 2 |
| OPEN (INPUT/OUTPUT/I-O/EXTEND) | Implemented | `BoundNodes.cs:BoundOpenStatement`, `BoundTreeBuilder.cs:BindOpen`, `BoundTreeValidator.cs:ValidateOpen`, `Binder.cs:LowerOpen` | Spec-true | All four modes; EXTEND validated against non-sequential (CBL0701) |
| CLOSE | Implemented | `BoundNodes.cs:BoundCloseStatement`, `BoundTreeBuilder.cs`, `Binder.cs:LowerClose` | Spec-true | Multi-file CLOSE supported |
| READ (sequential) | Implemented | `BoundNodes.cs:BoundReadStatement`, `Binder.cs:LowerRead`, `IrInstruction.cs:IrReadRecordToStorage` | Spec-true | AT END / NOT AT END; FILE STATUS update; READ NEXT flag |
| READ (random/keyed) | Partially | `BoundReadStatement.KeyDataName`, `IrReadRecordToStorage` | Likely incorrect | KEY IS parsed and stored, but IR only emits `IrReadRecordToStorage` without key dispatch; runtime `ReadByKey` exists but binder may not wire it for random access |
| READ INTO | Implemented | `BoundReadStatement.Into`, `Binder.cs:LowerRead` | Spec-true | INTO target bound as identifier expression; implicit MOVE emitted after read |
| WRITE (basic) | Implemented | `BoundNodes.cs:BoundWriteStatement`, `Binder.cs:LowerWrite`, `IrInstruction.cs:IrWriteRecordFromStorage` | Spec-true | Full pipeline through CIL emission |
| WRITE FROM | Implemented | `BoundWriteStatement.From`, `BoundTreeValidator.cs:ValidateWrite` | Spec-true | Validated (CBL1801); permissive compatibility per COBOL spec |
| WRITE ADVANCING | Implemented | `BoundWriteStatement.AdvancingLines`, `IrInstruction.cs:IrWriteAfterAdvancing` | Partially | AFTER ADVANCING with integer lines; BEFORE ADVANCING flag exists but BEFORE not fully wired in IR; PAGE advancing not supported |
| REWRITE (basic) | Implemented | `BoundNodes.cs:BoundRewriteStatement`, `Binder.cs:LowerRewrite`, `IrInstruction.cs:IrRewriteRecordFromStorage` | Spec-true | Organization check (CBL1901) |
| REWRITE FROM | Implemented | `BoundRewriteStatement.From`, `BoundTreeValidator.cs:ValidateRewrite` | Spec-true | Validated (CBL1902); permissive compatibility |
| DELETE | Implemented | `BoundNodes.cs:BoundDeleteStatement`, `Binder.cs:LowerDelete`, `IrInstruction.cs:IrDeleteRecord` | Spec-true | INVALID KEY / NOT INVALID KEY paths; organization validated (CBL2001) |
| START (KEY IS) | Implemented | `BoundNodes.cs:BoundStartStatement`, `Binder.cs:LowerStart`, `IrInstruction.cs:IrStartFile` | Spec-true | Key condition validated (CBL1601-1605); maps to `StartCondition` enum; INVALID KEY paths |

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
| CALL statement | Partially | `BoundNodes.cs:BoundCallStatement`, `BoundTreeBuilder.cs:BindCall`, `Binder.cs:LowerCall` | N/A — stub | Parsed + bound; IR emits **DISPLAY stub** ("CALL not implemented"); no actual inter-program call |
| BY REFERENCE | Partially | `BoundCallArgument(ParameterMode.ByReference, ...)`, `BoundTreeBuilder.cs` | N/A — stub | Parsed and stored in bound tree; not lowered to real IR |
| BY CONTENT | Partially | `BoundCallArgument(ParameterMode.ByContent, ...)` | N/A — stub | Parsed and stored; not lowered |
| BY VALUE | Partially | `BoundCallArgument(ParameterMode.ByValue, ...)` | N/A — stub | Parsed and stored; not lowered |
| RETURNING | Partially | `BoundCallStatement.ReturningTarget` | N/A — stub | Parsed as identifier expression; not wired |
| ON EXCEPTION / NOT ON EXCEPTION | Partially | `BoundCallStatement.OnException`, `Binder.cs:LowerCall` | Spec-true (structural) | Both paths bound; stub always takes ON EXCEPTION path |
| Linkage Section parsing | Implemented | `SemanticBuilder.cs:VisitLinkageSection`, `StorageAreaKind.LinkageSection` | Spec-true | Items tagged with `StorageAreaKind.LinkageSection` |
| Linkage Section validation | Implemented | `SymbolValidator.cs:ValidateLinkageSection` | Spec-true | VALUE not allowed (CBL3110), REDEFINES not allowed (CBL3111) |
| PROCEDURE DIVISION USING/RETURNING | Not implemented | Grammar has `linkageProcedureParameter` rule | Unknown | Parser accepts it but `SemanticBuilder` does not visit; no `ProcedureParameter` list populated |
| Inter-program communication | Not implemented | — | — | No program linking, no shared memory, no external data |
| Dynamic CALL | Partially | `BoundCallStatement.IsDynamic` | N/A — stub | Flag parsed; diagnostic CBL3310 warns param list cannot be validated |

### CALL Diagnostics Defined

CBL3301 (arg count mismatch), CBL3302 (invalid arg for mode), CBL3303 (type incompatible), CBL3304 (RETURNING not linkage), CBL3305 (RETURNING type mismatch), CBL3310 (dynamic CALL warning). These descriptors exist but are **not wired** since CALL is a stub.

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

1. **ALTERNATE KEY**: Grammar exists, not visited in semantic builder, no runtime support.
2. **READ random/keyed**: KEY IS parsed but binder does not dispatch to `ReadByKey`; likely broken for indexed random access.
3. **WRITE BEFORE ADVANCING / PAGE**: Flag stored but IR only has `IrWriteAfterAdvancing`; BEFORE and PAGE not wired.
4. **CALL inter-program linkage**: Full parse + bind, but IR is a display stub. No actual call mechanism.
5. **PROCEDURE DIVISION USING/RETURNING**: Grammar rule exists, not visited in SemanticBuilder; `ProcedureParameter` list never populated.
6. **Inter-program communication**: No shared storage, external data, or program loading.
