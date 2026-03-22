# Section 5: Known Gaps, Risks & Technical Debt

## 5.1 Known Functional Gaps

The compiler has completed all six planned implementation phases but carries several
functional gaps that affect real-world COBOL program coverage. These are documented in
CLAUDE.md and corroborated by NIST test failures.

### 5.1.1 CALL Statement (IR Stub Only)

The CALL statement is bound and validated (`BoundCallStatement`, `BoundCallArgument`
with `ParameterMode`), and a CBL3310 diagnostic fires for dynamic calls. However, the
IR lowering is a **stub**: it emits a "CALL not implemented" DISPLAY message and
unconditionally takes the ON EXCEPTION path. No inter-program linkage exists.

**Location**: `src/CobolSharp.Compiler/CodeGen/Binder.cs`, lines 1369-1383

**Impact**: Any COBOL program that uses CALL to invoke subprograms will compile but
will not execute the target program. This blocks multi-program systems, which represent
the majority of production COBOL codebases.

### 5.1.2 RETURN Statement (IR Stub Only)

RETURN (used with SORT/MERGE SD files) is bound (`BoundReturnStatement`, CBL2101
diagnostic) but lowered as a stub that emits "RETURN not implemented" and takes the
AT END path unconditionally.

**Location**: `src/CobolSharp.Compiler/CodeGen/Binder.cs`, lines 1350-1364

**Impact**: SORT/MERGE with OUTPUT PROCEDURE cannot retrieve sorted records.

### 5.1.3 SORT/MERGE (Parse Only)

SORT and MERGE statements are parsed and SD entries are recognized in the grammar, but
no runtime implementation exists. The SORT file handler infrastructure is absent.

**Impact**: Programs that sort or merge files will fail at runtime. SORT is extremely
common in batch COBOL processing.

### 5.1.4 Alternate Record Keys (Not Parsed)

The grammar and semantic model do not support ALTERNATE RECORD KEY on indexed files.
Only the primary RECORD KEY is recognized.

**Impact**: Indexed files with secondary access paths cannot be compiled. Multi-key
access is common in master file maintenance programs.

### 5.1.5 Report Writer (Validation Stub)

`ReportWriterValidator` is explicitly documented as a stub ("full validation deferred
until Report Writer codegen is implemented"). Report Writer grammar (RD entries, report
group entries, TYPE/SUM clauses) is parsed but no code generation exists.

**Location**: `src/CobolSharp.Compiler/Semantics/ReportWriterValidator.cs`

**Impact**: Report Writer programs will parse but produce no report output.

### 5.1.6 National Data Type MOVE Stubs

The `PicRuntime` class contains 10 stub methods for National (PIC N / UTF-16) MOVE
operations. All delegate to alphanumeric-to-alphanumeric byte copy, which is
semantically incorrect for multi-byte national characters.

**Location**: `src/CobolSharp.Runtime/PicRuntime.cs`, lines 1303-1394

**Impact**: Programs using PIC N fields will get incorrect data movement results.

### 5.1.7 ACCEPT (Console Input Stub)

Plain ACCEPT (without FROM DATE/TIME/DAY) returns blank-filled output instead of
reading from console/stdin.

**Location**: `src/CobolSharp.Runtime/AcceptRuntime.cs`, line 29

**Impact**: Interactive COBOL programs that accept user input will receive blanks.

## 5.2 Runtime Issues

### 5.2.1 NC220M Infinite Loop

The NIST test NC220M causes an infinite loop at runtime. The root cause is not yet
diagnosed. This indicates a potential control flow lowering bug in PERFORM or GO TO
that could affect production programs with similar flow patterns.

**Risk**: Unknown scope. The same control flow pattern may exist in production code
that was never tested against this path.

### 5.2.2 NC121M Subscripted DIVIDE GIVING

DIVIDE ... GIVING with subscripted operands does not work correctly. The subscript
resolution path for DIVIDE GIVING targets is incomplete.

**NIST tests affected**: NC121M

**Impact**: Arithmetic results stored into table elements via DIVIDE may be incorrect.

## 5.3 Grammar Gaps

The ANTLR grammar has several structural gaps that prevent valid COBOL programs from
parsing:

| Gap | Description | Affected NIST Tests |
|-----|-------------|---------------------|
| VALUE THRU in level-88 | Level-88 condition names with VALUE ... THRU ... ranges cannot be parsed | NC201A, NC250A, NC252A |
| ASCENDING/DESCENDING KEY in OCCURS | OCCURS clause key specification is parsed but triggers COBOL0100 hint and is not semantically wired | NC233A, NC237A, NC238A, NC247A |
| START KEY IS syntax | Grammar requires two operands in `comparisonExpression`, but standard START KEY IS has one operand (`KEY IS >= data-name`) | Future START users |
| CALL implicit BY REFERENCE | `callByReference` rule requires explicit `BY` keyword; standard COBOL allows bare arguments | Programs using `CALL "X" USING var1 var2` |
| OCCURS DEPENDING ON | Variable-length tables parse but emit COBOL0104 "not yet supported" | Programs with variable-length records |
| INSPECT CONVERTING | Emits COBOL0105 "not yet supported" hint | NC223A, NC225A |
| INITIALIZE REPLACING | Emits COBOL0106 "not yet supported" hint | NC223A |
| Multi-target SET | `SET id1 id2 TO value` emits COBOL0108 hint | Programs using SET with multiple targets |
| PERFORM VARYING AFTER | Nested VARYING with AFTER clause emits COBOL0109 hint | Programs with multi-dimensional iteration |
| Abbreviated NOT conditions | `NOT =`, `NOT >`, `NOT <` operator forms emit COBOL0311 hint | Legacy programs using symbol-based NOT |
| EVALUATE ALSO | Multi-subject EVALUATE emits COBOL0107 hint | Programs with compound EVALUATE logic |

## 5.4 Reserved Word Conflicts

Two COBOL words conflict with the language grammar when used as paragraph names:

- **STATUS**: Reserved for FILE STATUS; emits COBOL0200 when used as a paragraph name
- **PROGRAM**: Reserved; emits COBOL0201 when used as a paragraph name

**Impact**: Legacy COBOL programs that use STATUS or PROGRAM as paragraph names will
fail to parse. This is a known limitation in the lexer/parser architecture where
context-sensitive keyword handling is incomplete.

## 5.5 TODO/FIXME/HACK Distribution

The codebase has a remarkably low count of deferred work markers:

| Marker | Count | Files |
|--------|-------|-------|
| TODO | 2 | `Program.cs` (CLI standard passing), `BoundTreeBuilder.cs` (function binding) |
| FIXME | 0 | -- |
| HACK | 0 | -- |

**Analysis**: Only 2 TODO comments exist in the entire C# codebase. This is unusually
clean for a compiler of this complexity. However, known gaps are tracked externally
(CLAUDE.md, PROJECT_PLAN.md Code Quality Audit table) rather than inline, which means
a developer reading the code will not see deferred work flagged at the point of impact.

The PROJECT_PLAN.md Code Quality Audit table (Session 10) lists 11 items, of which 3
are fixed and 8 remain:

| # | Issue | Status |
|---|-------|--------|
| 4 | OCCURS count extraction (RecordLayoutBuilder) | Deferred to Phase D |
| 5 | File-from-record FD resolution (BoundTreeBuilder) | Deferred to Phase F |
| 6 | Function calls bound as identifiers | Deferred to Phase K |
| 7 | Unresolved identifiers treated as string literals | Needs diagnostic |
| 8 | Unresolved REDEFINES silently ignored | Needs diagnostic |
| 9 | Level-88 condition names skipped in builder | Deferred |
| 10 | Runtime calls NOP'd in CilEmitter | Known limitation |
| 11 | COMP-1/COMP-2 mapped to Int32/Int64 | Known limitation |

Items 7 and 8 are silent failure modes that could produce incorrect compiled output
without any diagnostic.

## 5.6 Stub Implementations Summary

| Component | Stub Type | Behavior |
|-----------|-----------|----------|
| CALL statement | IR lowering stub | Emits DISPLAY warning, takes ON EXCEPTION path |
| RETURN statement | IR lowering stub | Emits DISPLAY warning, takes AT END path |
| ReportWriterValidator | Validation stub | No-op; no report validation occurs |
| National MOVE (10 methods) | Runtime stub | Delegates to alphanumeric byte copy |
| ACCEPT (plain) | Runtime stub | Returns blank-filled string |
| COMP-1/COMP-2 | Storage mapping | Mapped to Int32/Int64 instead of IEEE float |

## 5.7 NotSupportedException Usage

Five `NotSupportedException` throw sites exist in `CilEmitter.cs`. All are in code
generation paths and function as assertions against unrecognized IR node types or
binary operators. These are appropriate defensive coding (not gaps), but they indicate
that encountering an unsupported IR instruction at emit time will produce an unhandled
exception rather than a user-friendly diagnostic.

**Location**: `src/CobolSharp.Compiler/CodeGen/CilEmitter.cs`, lines 745, 829, 2604,
2634, 2748

## 5.8 Risk Assessment: Blockers to Production Use

### Critical Risks (Would Block Most Production Workloads)

1. **No CALL inter-program linkage.** Production COBOL systems are composed of dozens
   to hundreds of programs that CALL each other. Without this, only standalone programs
   can execute. This is the single largest gap.

2. **No SORT/MERGE runtime.** Batch processing relies heavily on SORT. Without it, a
   significant percentage of batch programs cannot run.

3. **NC220M infinite loop (undiagnosed).** An undiagnosed runtime infinite loop
   indicates a potential control flow correctness bug. Until root-caused, any program
   with similar control flow patterns is at risk of hanging.

### High Risks (Would Block Some Workloads)

4. **Grammar gaps for common constructs.** VALUE THRU (level-88 ranges), OCCURS
   DEPENDING ON (variable-length tables), PERFORM VARYING AFTER (nested loops), and
   implicit BY REFERENCE in CALL are all common in production COBOL. Each requires
   grammar work before affected programs can compile.

5. **No alternate record keys.** Multi-key indexed file access is standard in master
   file programs.

6. **COMP-1/COMP-2 mapped to integers.** These should be IEEE 754 single/double
   precision floats. Programs using floating-point computation will get incorrect
   results silently.

7. **Silent failure modes.** Unresolved identifiers treated as string literals (audit
   item 7) and unresolved REDEFINES silently ignored (audit item 8) mean the compiler
   can produce incorrect output without warning.

### Moderate Risks (Edge Cases or Niche Features)

8. **National data type stubs.** UTF-16 MOVE operations are semantically incorrect.
   National types are uncommon in legacy COBOL but may appear in modernized codebases.

9. **Report Writer.** Parse-only. Niche feature but required for conformance.

10. **Console ACCEPT.** Interactive programs cannot receive input. Most production
    COBOL is batch-oriented, so this is lower priority.

## 5.9 Dependencies and Their Risks

The compiler depends on two external packages:

| Dependency | Purpose | Risk Assessment |
|------------|---------|-----------------|
| **Antlr4.Runtime.Standard** | Parser runtime for the ANTLR-generated grammar | Mature, widely used, low risk. However, the grammar is split across 8 files using ANTLR `import`, and the build requires copying token files into subdirectories. Grammar maintenance (adding rules for gaps above) requires ANTLR expertise. |
| **Mono.Cecil** | CIL assembly emission | Mature, production-proven (used by Unity, Fody, PostSharp), actively maintained, low risk. No known issues for the project's use case. |

The test projects depend on xUnit and Microsoft.NET.Test.Sdk, which are standard and
low-risk.

**Runtime dependency risk**: The `CobolSharp.Runtime` assembly has **no external
NuGet dependencies**. All runtime support (PIC formatting, decimal arithmetic, file
I/O, INSPECT/STRING/UNSTRING) is implemented directly. This is a strength -- compiled
COBOL programs depend only on the runtime assembly and .NET itself.

**Architectural note on ANTLR**: The PROJECT_PLAN documents KTD-3 as "Hand-Written
Recursive Descent" parser, but the actual implementation uses ANTLR4. The plan's
rationale for hand-written parsing (PICTURE context-sensitivity, fixed-form handling,
COPY/REPLACE preprocessing) appears to have been addressed through other means. The
ANTLR grammar was split from a monolithic 2027-line file into 8 modular files. Grammar
gaps listed in Section 5.3 will require modifications to these grammar files, each
potentially introducing parse conflicts.

## 5.10 Summary Metrics

| Metric | Value |
|--------|-------|
| Functional stubs (IR-level) | 2 (CALL, RETURN) |
| Runtime stubs | 12 (10 National MOVE + ACCEPT + COMP-1/COMP-2) |
| Validation stubs | 1 (ReportWriterValidator) |
| Grammar gaps (documented) | 11 |
| Reserved word conflicts | 2 (STATUS, PROGRAM) |
| TODO comments | 2 |
| FIXME/HACK comments | 0 |
| Code quality audit open items | 8 of 11 |
| Silent failure modes | 2 (audit items 7, 8) |
| Undiagnosed runtime bugs | 2 (NC220M loop, NC121M subscript) |
| NIST tests at 100% | 39 of ~400 |
| Parse-error hint codes (COBOL0xxx) | 25+ distinct codes |
| External runtime dependencies | 0 |
| External compiler dependencies | 2 (ANTLR4, Mono.Cecil) |
