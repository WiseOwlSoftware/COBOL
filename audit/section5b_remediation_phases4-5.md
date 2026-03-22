# Remediation Plan: Phase 4 (Backend / CIL Emission) and Phase 5 (Diagnostics and Test Suite)

## Phase 4: Backend / CIL Emission (CIL-xx)

### CIL-01: Eliminate IrRuntimeCall String Dispatch

**Goal:** Replace the stringly-typed `IrRuntimeCall` dispatch mechanism with typed IR instructions for all file I/O operations.

**Scope:**
- `src/CobolSharp.Compiler/IR/IrInstruction.cs` (IrRuntimeCall class, new IR instruction types)
- `src/CobolSharp.Compiler/CodeGen/Binder.cs` (LowerOpen, LowerClose, file registration)
- `src/CobolSharp.Compiler/CodeGen/CilEmitter.cs` (EmitRuntimeCall, new Emit methods)

**Required changes:**
1. Define typed IR instructions: `IrOpenFile(string FileName, OpenMode Mode)`, `IrCloseFile(string FileName)`, `IrRegisterFileHandler(...)`, `IrInitFileRuntime`.
2. Replace all `IrRuntimeCall` emissions in `Binder.cs` (lines ~185, ~251, ~1132, ~1148) with the new typed instructions.
3. Add corresponding `case` arms in `CilEmitter.EmitInstruction` for each new instruction, moving the logic currently scattered across `EmitRuntimeCall`'s if-else chain.
4. Remove the `IrRuntimeCall` class and `EmitRuntimeCall` method entirely.

**Acceptance criteria:**
- No `IrRuntimeCall` instances exist in the IR after lowering.
- All OPEN (INPUT/OUTPUT/IO/EXTEND), CLOSE, Init, and RegisterFileHandler operations emit through typed IR instructions.
- All 176 integration tests pass. All 195 unit tests pass.

---

### CIL-02: Implement CALL Inter-Program Linkage

**Goal:** Replace the CALL stub in the Binder with actual CIL emission that invokes external programs via reflection or a program registry.

**Scope:**
- `src/CobolSharp.Compiler/CodeGen/Binder.cs` (LowerCall, lines ~1369-1384)
- `src/CobolSharp.Compiler/IR/IrInstruction.cs` (new IrCallProgram instruction)
- `src/CobolSharp.Compiler/CodeGen/CilEmitter.cs` (new EmitCallProgram method)
- `src/CobolSharp.Runtime/` (new ProgramRegistry or CallRuntime class)

**Required changes:**
1. Define `IrCallProgram` IR instruction carrying: target name (string or IrLocation for dynamic CALL), argument list with parameter modes (BY REFERENCE/CONTENT/VALUE), optional RETURNING location.
2. Implement `LowerCall` in `Binder.cs` to emit `IrCallProgram` instead of the current DISPLAY stub.
3. Create a `CallRuntime` class in the runtime that resolves program names to compiled assemblies, marshals BY REFERENCE arguments as byte-array slices, and handles ON EXCEPTION / NOT ON EXCEPTION semantics.
4. Implement `EmitCallProgram` in `CilEmitter.cs` to emit CIL that invokes `CallRuntime.Invoke(programName, args)`.
5. Wire ON EXCEPTION path to catch resolution failures; NOT ON EXCEPTION as the success continuation.

**Acceptance criteria:**
- Static CALL to a compiled subprogram with BY REFERENCE parameters produces working CIL.
- Dynamic CALL with string identifier resolves at runtime and raises exception on failure.
- ON EXCEPTION / NOT ON EXCEPTION paths execute correctly.
- CALL RETURNING stores result into the designated data item.

---

### CIL-03: Implement RETURN (Sort/Merge) CIL Emission

**Goal:** Replace the RETURN stub with actual SORT file retrieval via runtime support.

**Scope:**
- `src/CobolSharp.Compiler/CodeGen/Binder.cs` (LowerReturn, lines ~1350-1364)
- `src/CobolSharp.Compiler/IR/IrInstruction.cs` (new IrReturnRecord instruction)
- `src/CobolSharp.Compiler/CodeGen/CilEmitter.cs` (new EmitReturnRecord method)
- `src/CobolSharp.Runtime/` (SortRuntime or extension to FileRuntime)

**Required changes:**
1. Define `IrReturnRecord` IR instruction with FileName, Record (IrLocation), optional INTO location.
2. Replace the DISPLAY stub in `LowerReturn` with `IrReturnRecord` emission plus AT END / NOT AT END conditional branching (mirroring READ pattern).
3. Implement runtime method `SortRuntime.ReturnRecord(fileName, area, offset, length)` that retrieves the next sorted record.
4. Add `EmitReturnRecord` to CilEmitter that calls the runtime method and stores data into the record buffer.

**Acceptance criteria:**
- RETURN statement retrieves sorted records from a sort file.
- AT END path executes when no more records are available.
- NOT AT END path executes on successful retrieval.

---

### CIL-04: Implement SORT/MERGE Lowering and CIL Emission

**Goal:** Lower SORT and MERGE statements from parse-only status to full CIL emission.

**Scope:**
- `src/CobolSharp.Compiler/CodeGen/Binder.cs` (new LowerSort, LowerMerge methods)
- `src/CobolSharp.Compiler/IR/IrInstruction.cs` (new IrSort, IrMerge instructions)
- `src/CobolSharp.Compiler/CodeGen/CilEmitter.cs` (new EmitSort, EmitMerge methods)
- `src/CobolSharp.Runtime/` (new SortRuntime class)

**Required changes:**
1. Define `IrSort` instruction carrying: sort file name, key specifications (ascending/descending + field locations), INPUT PROCEDURE or USING file names, OUTPUT PROCEDURE or GIVING file names.
2. Define `IrMerge` with the same structure minus INPUT PROCEDURE.
3. Implement `LowerSort` in Binder: resolve key fields to IrLocations, emit IrSort with USING/GIVING file names or INPUT/OUTPUT PROCEDURE method references.
4. Implement `SortRuntime` in the runtime project: buffer records, sort by key comparisons, feed to OUTPUT PROCEDURE or GIVING file.
5. Implement `EmitSort` and `EmitMerge` in CilEmitter to call the runtime sort/merge methods.

**Acceptance criteria:**
- `SORT file ON ASCENDING KEY ... USING ... GIVING ...` produces correct sorted output.
- INPUT PROCEDURE and OUTPUT PROCEDURE forms invoke the specified paragraph ranges.
- MERGE with multiple USING files produces correctly merged output.

---

### CIL-05: Fix Subscripted Operands in DIVIDE GIVING

**Goal:** Support subscripted data items (e.g., `TABLE-ITEM(IDX)`) as DIVIDE GIVING targets.

**Scope:**
- `src/CobolSharp.Compiler/CodeGen/Binder.cs` (arithmetic lowering for DIVIDE GIVING)
- `src/CobolSharp.Compiler/IR/IrInstruction.cs` (IrPicDivide, IrMoveAccumulatedToTarget)

**Required changes:**
1. Identify the code path in Binder that resolves DIVIDE GIVING target locations and ensure `ResolveExpressionLocation` (which handles subscripted references via IrElementRef) is used instead of `ResolveLocation` (which only handles static references).
2. Verify that `IrMoveAccumulatedToTarget`, `IrPicDivide`, and `IrPicDivideLiteral` correctly propagate IrElementRef locations to the emitter.
3. Add an integration test exercising `DIVIDE ... INTO ... GIVING TABLE-ITEM(IDX)`.

**Acceptance criteria:**
- NIST test NC121M passes (subscripted DIVIDE GIVING).
- No regression in existing arithmetic tests.

---

### CIL-06: Diagnose and Fix NC220M Infinite Loop

**Goal:** Identify and fix the runtime infinite loop in NIST test NC220M.

**Scope:**
- `src/CobolSharp.Compiler/CodeGen/Binder.cs` (PERFORM VARYING lowering)
- `src/CobolSharp.Compiler/CodeGen/CilEmitter.cs` (PERFORM loop emission)

**Required changes:**
1. Compile NC220M and capture the emitted IL (using the existing `[IL]` diagnostic output).
2. Identify the PERFORM VARYING loop whose exit condition never becomes true. Common causes: incorrect UNTIL condition evaluation order (TEST BEFORE vs TEST AFTER), incorrect increment step, or missing initial SET.
3. Fix the Binder or CilEmitter logic that generates the faulty loop structure.
4. Add a timeout guard in the integration test runner to prevent future hangs.

**Acceptance criteria:**
- NC220M completes within 10 seconds and produces correct output.
- No regression in existing PERFORM tests.

---

### CIL-07: Implement WRITE BEFORE/AFTER ADVANCING with Dynamic Operand

**Goal:** Support WRITE ADVANCING with a data-item operand (not just literal integer).

**Scope:**
- `src/CobolSharp.Compiler/IR/IrInstruction.cs` (IrWriteAfterAdvancing — currently only carries `int AdvanceLines`)
- `src/CobolSharp.Compiler/CodeGen/Binder.cs` (WRITE lowering)
- `src/CobolSharp.Compiler/CodeGen/CilEmitter.cs` (EmitWriteAfterAdvancing)

**Required changes:**
1. Extend `IrWriteAfterAdvancing` to carry an optional `IrLocation` for the advance-count when it is a data item reference, and a `bool IsBefore` to distinguish BEFORE vs AFTER.
2. Update the Binder to emit the IrLocation form when the ADVANCING operand is an identifier.
3. Update `EmitWriteAfterAdvancing` to evaluate the IrLocation at runtime (DecodeNumeric to int) when present.
4. Add WRITE BEFORE ADVANCING support (currently only AFTER is handled).

**Acceptance criteria:**
- `WRITE rec AFTER ADVANCING WS-LINES LINES` where WS-LINES is a data item works correctly.
- `WRITE rec BEFORE ADVANCING` emits correct output.
- Literal ADVANCING continues to work unchanged.

---

### CIL-08: READ INTO and WRITE FROM with Non-Record Targets

**Goal:** Ensure READ INTO and WRITE FROM correctly move data between the record buffer and an arbitrary data item.

**Scope:**
- `src/CobolSharp.Compiler/CodeGen/Binder.cs` (LowerRead, LowerWrite)
- `src/CobolSharp.Compiler/CodeGen/CilEmitter.cs` (existing emit methods)

**Required changes:**
1. Verify that READ INTO emits: (a) read record from file into record buffer, (b) MOVE record buffer to INTO target using IrMoveFieldToField.
2. Verify that WRITE FROM emits: (a) MOVE FROM source to record buffer, (b) write record buffer to file.
3. Ensure both paths handle group-to-group moves with correct size semantics.
4. Add integration tests for READ INTO and WRITE FROM with group items of differing sizes.

**Acceptance criteria:**
- READ INTO copies the record to the specified data item after reading.
- WRITE FROM copies the source to the record buffer before writing.
- Size mismatches are handled per COBOL-85 MOVE rules (truncation/padding).

---

## Phase 5: Diagnostics and Test Suite (TST-xx)

### TST-01: Create Formal Diagnostic Catalog Document

**Goal:** Produce a machine-readable catalog of all diagnostic codes with stable IDs, severity, and COBOL-85 standard references.

**Scope:**
- `src/CobolSharp.Compiler/Diagnostics/DiagnosticDescriptors.cs` (source of truth)
- New file: `docs/diagnostics.md` (generated catalog)

**Required changes:**
1. Write a script or source generator that extracts all `DiagnosticDescriptor` fields from `DiagnosticDescriptors.cs` and produces a markdown table with columns: Code, Severity, Message Template, COBOL-85 Section, Phase (parser/binder/validator/codegen).
2. Add COBOL-85 standard section references as comments on each descriptor in the source file (e.g., `// COBOL-85 6.4.38 — MOVE statement`).
3. Verify that all diagnostic codes follow the established numbering scheme: CBL0601-0803 (data/file), CBL0901-0905 (MOVE), CBL1001-1004 (VALUE), CBL1101-3502 (statements/flow/scope).
4. Identify and fill any numbering gaps (e.g., CBL0602 is not defined but CBL0601 exists).

**Acceptance criteria:**
- Every diagnostic emitted by the compiler has a corresponding entry in `DiagnosticDescriptors.cs`.
- No diagnostic is emitted using raw string codes (all go through `DiagnosticDescriptor`).
- The catalog document lists all codes in order with severity and message template.

---

### TST-02: Audit Diagnostic Emission for Raw String Codes

**Goal:** Ensure every diagnostic in the compiler is emitted through a `DiagnosticDescriptor`, not via raw `ReportError`/`ReportWarning` with inline string codes.

**Scope:**
- `src/CobolSharp.Compiler/Diagnostics/DiagnosticBag.cs`
- All callers of `ReportError`, `ReportWarning`, `Report` across the compiler

**Required changes:**
1. Search all `.cs` files for calls to `ReportError(string code, ...)` and `ReportWarning(string code, ...)`.
2. For each call using a raw string code (e.g., `"COBOL0500"`), create a corresponding `DiagnosticDescriptor` in `DiagnosticDescriptors.cs` and convert the call to use `Report(descriptor, ...)`.
3. Consider making `ReportError(string, ...)` and `ReportWarning(string, ...)` `[Obsolete]` to prevent future raw-code usage, or remove them entirely and require all reporting to go through descriptors.

**Acceptance criteria:**
- Zero calls to `ReportError` or `ReportWarning` with raw string codes remain in the codebase.
- All diagnostic codes are centralized in `DiagnosticDescriptors.cs`.

---

### TST-03: Add Negative Diagnostic Tests for All Statement Validators

**Goal:** Ensure every CBL diagnostic code has at least one unit test that verifies the diagnostic is emitted for invalid input.

**Scope:**
- `tests/CobolSharp.Tests.Unit/Semantics/BoundTreeValidatorTests.cs` (28 tests currently)
- `tests/CobolSharp.Tests.Unit/Semantics/ArithmeticDiagnosticTests.cs` (4 tests currently)
- `tests/CobolSharp.Tests.Unit/Semantics/DiagnosticTestBase.cs` (shared infrastructure)

**Required changes:**
1. Inventory all `DiagnosticDescriptor` entries in `DiagnosticDescriptors.cs` (currently ~80 descriptors).
2. For each descriptor, verify a corresponding test exists that compiles invalid COBOL source and asserts the diagnostic code appears in the output.
3. Add missing negative tests. Priority areas with low coverage:
   - CBL1301-1304 (STRING validation)
   - CBL1401-1406 (UNSTRING validation)
   - CBL1501-1503 (INSPECT validation)
   - CBL1601-1605 (START validation)
   - CBL3201-3206 (FILE STATUS validation)
   - CBL3301-3310 (CALL/USING/RETURNING validation)
   - CBL3401-3406 (Report Writer validation)
4. Each test must use the `DiagnosticTestBase` infrastructure to compile a minimal COBOL snippet and assert the expected diagnostic code.

**Acceptance criteria:**
- Every CBL diagnostic descriptor has at least one test that triggers it.
- Tests assert on the specific diagnostic code, not just error/warning presence.

---

### TST-04: Add Positive Semantic Tests for Core COBOL-85 Rules

**Goal:** Add tests that verify valid COBOL constructs compile without diagnostics, covering COBOL-85 rules that currently only have negative (error) tests.

**Scope:**
- `tests/CobolSharp.Tests.Unit/Semantics/` (new test classes or additions to existing)

**Required changes:**
1. For each major statement category (MOVE, arithmetic, IF/EVALUATE, PERFORM, STRING/UNSTRING, INSPECT, file I/O), add at least one test that compiles a valid program and asserts zero errors.
2. Cover edge cases: MOVE of figurative constants, MOVE CORRESPONDING with matching subordinates, COMPUTE with nested expressions, PERFORM VARYING with AFTER, EVALUATE TRUE with multiple WHEN clauses.
3. Add positive tests for data definition rules: valid OCCURS with DEPENDING ON, valid REDEFINES, valid level-88 conditions with VALUE.

**Acceptance criteria:**
- At least 20 new positive tests exist covering the major statement categories.
- All positive tests compile the COBOL source and assert `HasErrors == false`.

---

### TST-05: Organize Integration Tests by COBOL Feature Area

**Goal:** Split the monolithic `EndToEndTests.cs` (169 tests) into feature-focused test classes for maintainability.

**Scope:**
- `tests/CobolSharp.Tests.Integration/EndToEndTests.cs`
- New test files in `tests/CobolSharp.Tests.Integration/`

**Required changes:**
1. Create feature-area test classes: `ArithmeticEndToEndTests.cs`, `FileIOEndToEndTests.cs`, `ControlFlowEndToEndTests.cs`, `StringEndToEndTests.cs`, `DataMovementEndToEndTests.cs`, `InspectEndToEndTests.cs`.
2. Extract the shared `CompileAndRun` helper into a base class (e.g., `EndToEndTestBase`).
3. Move each test from `EndToEndTests.cs` to the appropriate feature-area class.
4. Preserve the existing test method names and assertions exactly.

**Acceptance criteria:**
- `EndToEndTests.cs` is removed or contains only tests that do not fit a feature category.
- Each feature-area test class inherits from a shared base with `CompileAndRun`.
- All 169 integration tests continue to pass with no behavioral changes.

---

### TST-06: Add CIL Emission Unit Tests

**Goal:** Create unit tests that verify CilEmitter produces correct IL for individual IR instruction types, independent of full compilation.

**Scope:**
- New file: `tests/CobolSharp.Tests.Unit/CodeGen/CilEmitterTests.cs`

**Required changes:**
1. Create a test helper that constructs a minimal `IrModule` with a single method containing specific IR instructions, runs `CilEmitter.EmitAssembly`, and inspects the resulting `AssemblyDefinition` via Mono.Cecil.
2. Add tests for:
   - `IrLoadConst` (int, string, bool, decimal) produces correct CIL opcodes.
   - `IrBranch` / `IrJump` / `IrBranchIfFalse` produce correct branch targets.
   - `IrPerformThru` produces the dispatch loop with correct switch structure.
   - `IrMoveFieldToField` routes through correct PicRuntime method based on source/destination categories.
   - `IrComputeStore` evaluates expression trees and stores results.
   - `IrGoToDepending` produces cascaded comparisons with correct return values.
3. Each test asserts on the emitted IL instruction sequence using Mono.Cecil's `MethodBody.Instructions`.

**Acceptance criteria:**
- At least 10 CIL emission unit tests exist covering the major instruction categories.
- Tests verify IL structure without requiring program execution.

---

### TST-07: Add NIST Test Coverage Tracking

**Goal:** Create an automated mechanism to track which NIST COBOL-85 tests pass, fail, or are skipped, with trend reporting.

**Scope:**
- `tests/CobolSharp.Tests.Integration/` (NIST test infrastructure)
- New file or script for reporting

**Required changes:**
1. Create a test results summary that lists all known NIST tests (NC1xxA/M, NC2xxA/M series) with their current status (pass/fail/skip/not-implemented).
2. Add a CI-friendly output format (e.g., CSV or JSON) that records test results per run.
3. Document which COBOL-85 module each NIST test covers (Nucleus, Sequential I/O, Relative I/O, Indexed I/O, etc.).
4. Flag tests that regress between runs.

**Acceptance criteria:**
- A single command produces a summary of all NIST test statuses.
- The summary distinguishes between "test exists and passes," "test exists and fails," and "test not yet implemented."
- Current baseline: 39 NIST tests at 100%.

---

### TST-08: Add Diagnostic Severity and Code Stability Tests

**Goal:** Prevent accidental changes to diagnostic codes or severities that would break tooling or user expectations.

**Scope:**
- `src/CobolSharp.Compiler/Diagnostics/DiagnosticDescriptors.cs`
- New file: `tests/CobolSharp.Tests.Unit/Diagnostics/DiagnosticCatalogTests.cs`

**Required changes:**
1. Create a test that reflects over `DiagnosticDescriptors` and asserts:
   - All codes match the pattern `CBL[0-9]{4}`.
   - No two descriptors share the same code.
   - Severity is one of Error, Warning, Info.
   - Message templates contain valid `{0}`, `{1}` format placeholders (no malformed templates).
2. Create a snapshot test that serializes all descriptors (code + severity + template) to a baseline file. Any change to an existing descriptor requires an explicit baseline update, preventing accidental regressions.
3. Add a test that verifies every `DiagnosticDescriptor` field name matches its code (e.g., field `CBL0901` has `Code == "CBL0901"`).

**Acceptance criteria:**
- Any addition of a new diagnostic passes automatically.
- Any change to an existing diagnostic's code, severity, or template causes a test failure until the baseline is updated.
- No duplicate codes exist.
