# PHASE 12 — M3 (COBOL-2014) surface deltas

- **Phase:** 12
- **Title:** M3 (COBOL-2014) deltas — dynamic length, TYPEDEF edges, `>>PROPAGATE`, IEEE floats, function pointers
- **Track:** feature-iso
- **Risk:** MEDIUM
- **Depends on:** P10 (M2 residual catalog: national/boolean, pointers, UDF, file-2002, RW/CONSTANT/concat) must be DONE. P3 (version-gating framework: EditionValidator waves + the harness-driven VCR audit + the "loud-guard" skeletons for float/E-symbol usages) must be DONE. The unified data model (P5/P6/P7 — `StorageForm`, the declared `IBindPass` pipeline, `RecordLayout`) is the substrate this phase layers onto.

### Goal (one paragraph)

Land the remaining COBOL-2014 *surface* deltas on the (rearchitected) data model, keeping the already-complete **OCCURS DYNAMIC** feature matrix-locked. Concretely: (1) verify OCCURS DYNAMIC across all four `--std` editions and drive its version-matrix rows; (2) implement **DYNAMIC LENGTH elementary items** (§8.5.1.10 / §13.18.19 — a variable-length `PIC X`/`PIC N` string, min length 0) *without* the 2023 `SET`-length enhancement (that layers into Phase 13); (3) confirm/settle the **TYPEDEF / `SAME AS` / `TYPE TO`** edition edges (TYPEDEF + `TYPE` + `SAME AS` are already DONE — `SAME AS` landed in P10 Step 16 with row `same-as-clause-2002` active; this phase only confirms the rows and catalogues the deferred `TYPE TO`); (4) re-edition **`>>PROPAGATE`** (§7.3.21) to be recognized ≤2014; (5) clear the **M3-4 catchall** — turn the Phase-3 *loud guards* for IEEE-754 float usages into real implementations with split 2002/2014 edges (the `FLOAT-SHORT/LONG/EXTENDED` trio is already live at 2002; add the `FLOAT-BINARY-*` / `FLOAT-DECIMAL-*` 2014 family and the external-float `E`-symbol PICTURE), add **FUNCTION-POINTER / PROGRAM-POINTER** data (prototype-dependent), and the conditional-expression enhancements. **"Increased limits" is DROPPED** — §4.2.15 delegates all size limits to the implementor, so there is nothing to gate. Every feature ships with a `tests/conformance/2014/` program (byte-compared) in the same commit, and every introduction gate ships as an `active` `tests/version-matrix/constructs.json` row so the "N per-edition compilers" matrix asserts it at all four editions.

### Exit criteria (copy from the roadmap — do not weaken)

1. The `tests/conformance/2014/` corpus is non-empty **and** every on-disk `.cob` is discovered by `CorpusRunnerTests` (the manifest-coverage integrity test is green).
2. The **dynamic-table** (`occurs-dynamic-2014`) and **dynamic-length** (`dynamic-length-item-2014`) version-matrix rows are `active` and **green at all four editions** (compile at 2014/2023, `COBOLNET0900` at 1985/2002).
3. The **full battery** is green: greenfield conformance (currently 3166) + unit (currently 281) + the FULL legacy guard (NIST 353 MATCH), with **zero regressions**, at every commit boundary.

### STATUS

`IN PROGRESS @ step 6` (branch `phase-12-m3-2014`)

> The executing session updates this line to `IN PROGRESS @ step N` after each step and `DONE` at phase end. Keep the
> per-step checkboxes in §4 in sync. On resume, read this line + the last DEVLOG entry + `git log --oneline -15` first.

> ### ⚠️ RE-SCOUT CORRECTIONS — read `PHASE-12-scout-notes.md` FIRST; it OVERRIDES this plan's anchors
> This plan was authored 2026-07-07, **before P8/P9/P10/P11 landed**, so many of its code-state and spec anchors
> drifted. A 6-scout + adversarial-verify re-scout (2026-07-17) produced the corrected reference
> **`docs/rearchitecture/PHASE-12-scout-notes.md`** — trust IT for anchors, this doc for step structure. The
> load-bearing corrections applied during execution:
> 1. **IEEE fidelity (Step 7) was INVERTED.** GR14-18 PIN `FLOAT-BINARY-*`/`FLOAT-DECIMAL-*` to ISO/IEC 60559:2020;
>    only the FLOAT trio is implementor-defined (GR13/GR21). Decision: implement `FLOAT-BINARY-32`→`float` and
>    `FLOAT-BINARY-64`→`double` (exact/conforming); declare `FLOAT-BINARY-128`/`FLOAT-DECIMAL-16`/`FLOAT-DECIMAL-34`
>    **processor-dependent non-support** (Annex A.3 items 17/19) — loud, never a silent misbind. NOT `double`-backed.
> 2. **PROGRAM-POINTER is DONE as a 2002 feature** (P10 Step 7). Step 8's PROGRAM-POINTER half + `{is2014()}?` gate
>    would be a regression; only the restricted `TO`-prototype form + `ADDRESS OF PROGRAM` spelling remain.
> 3. **FUNCTION-POINTER surface DONE, staged loud (0899)**; only its runtime semantics (SET Format 8 + carrier +
>    mandatory `TO`) remain (genuinely 2014). `COBOLNET1552` is unnecessary (dup of the live 0899 gate).
> 4. **Diagnostic band 1540-1559 COLLIDES** — 17/20 codes are live (high-water 1559, not 1538). Keep 1550/1551/1552
>    (already earmarked P12) and draw the rest from **1561-1599**. Renumber the plan's `1540/1541`/`1545`.
> 5. **`>>PROPAGATE` is LIVE in 2023**, not removed — Step 9 does an INTRODUCTION gate (~2002), NO top-end span.
> 6. **TYPE TO is NOT a pointer form** — it's the TYPE clause's optional word (§13.18.57.2). The §7 table also swaps
>    the TYPE(§13.18.57)/TYPEDEF(§13.18.58)/SAME-AS(§13.18.49) citations.
> 7. **Diagnostic registry** is `src/Cobol.Net.Editions/Diagnostics/DiagnosticCatalog.cs` (+ regenerate
>    `docs/DIAGNOSTICS.md`), NOT the plan's `Cobol.Net.Compiler/Diagnostics/`. Battery baseline = **3521 conf / 301
>    unit** (not 3166/281). `scripts/guard.ps1` does not exist — use `bash scripts/guard-fast.sh`.
> 8. **Steps 1 & 3 are largely satisfied already:** `occurs-dynamic-2014` is active (10 `dyn_*` enabled);
>    `tests/conformance/2002/float_usage.cob` already covers the whole trio — Step 3 needs no new redundant program.

---

## 1. Preconditions & how to resume

Before touching code, in a fresh session:

1. Read `resume-prompt.md`'s top STATE banner (the live feature-drive state), then this file's STATUS line, then the newest `DEVLOG.md` entry (DESCENDING order — newest first), then `git log --oneline -15`.
2. Confirm the battery is green at HEAD (the P12 baseline):
   ```
   dotnet test E:/CobolSharp/tests/Cobol.Net.Tests.Conformance/Cobol.Net.Tests.Conformance.csproj -c Debug
   dotnet test E:/CobolSharp/tests/Cobol.Net.Tests.Unit/Cobol.Net.Tests.Unit.csproj -c Debug
   bash E:/CobolSharp/scripts/guard-fast.sh    # or scripts/guard.ps1 after the P0/DESIGN-test-build-ci rewrite
   ```
   If any is red at baseline, STOP — the phase assumes a green start.
3. The prebuilt CLI for ad-hoc reproduction (rebuild it after any compiler edit):
   ```
   dotnet build E:/CobolSharp/src/Cobol.Net.Cli/Cobol.Net.Cli.csproj -c Debug
   dotnet E:/CobolSharp/src/Cobol.Net.Cli/bin/Debug/net10.0/cobol.dll <src.cob> --std 2014 -o <out.dll> --run
   ```
4. **Spec-first, always.** For every semantic/syntax question cite `specs/ISO_COBOL.md` by §. The legacy oracle + NIST goldens are regression nets with known holes; they never define behavior (memory `feedback_use_the_spec`).

**Resumability:** every step below is a COMMIT BOUNDARY or names one. If interrupted mid-step, `git status`/`git diff` shows the partial work; each step's "verify" command tells you whether the last completed step is sound. No step leaves the battery red at a commit boundary. Grammar-touching steps (DYNAMIC LENGTH, float family, pointers) MUST run the FULL legacy guard (`scripts/guard.sh` / `guard-fast.sh`) before commit — a shared `.g4` change can perturb the NIST parse (memory `feedback_legacy_suite_on_shared_corpus`, `feedback_autonomous_grammar_nist`). Grammar changes are pre-authorized (memory `feedback_grammar_approval`).

---

## 2. Rationale — what this phase fixes (with as-built citations)

The Phase-3 version-gating framework deliberately left a set of 2014 constructs as **loud skeletons**: they parse (or their reserved words exist) and immediately raise a not-implemented/introduction diagnostic instead of silently mis-binding. Those `status:"pending"` rows in `tests/version-matrix/constructs.json` and the `pending`/skeleton notes in `src/Cobol.Net.Compiler/Binding/Model/PicInfo.cs` are the debt this phase clears. Specifically:

- **Floats half-implemented, matrix rows stale.** The `FLOAT-SHORT/LONG/EXTENDED` trio is already LIVE end-to-end (`PicInfo.cs:93-97,244-276,668-676` map the `Usage.FloatShort/FloatLong/FloatExtended` members to `float`/`double`; `DataBinder.cs:1158,1176` route them; the runtime `CobolFloat` carrier and the `NumX.Real` flag render them). BUT their version-matrix rows are still `status:"pending"` (`constructs.json` `usage-float-short-2002` line ~174, `usage-float-long-2002` ~496, `usage-float-extended-2002` ~506), so the matrix does **not** yet assert "compiles at 2002+, `COBOLNET0900` below." And the **2014 half of the D16 float split — `FLOAT-BINARY-32/64/128` and `FLOAT-DECIMAL-16/34` — is not wired at all**: the lexer has bare `FLOAT_BINARY`/`FLOAT_DECIMAL` tokens (`CobolLexer.g4:190-191`) but the `usageKeyword` rule (`CobolData.g4:302-329`) omits them, so `USAGE FLOAT-BINARY-32` does not parse. Row `usage-float-binary32-2014` is `pending`. The external-float `E`-symbol PICTURE (`pic-external-float-2002`, `constructs.json` ~457) "silent-misbinds in `PicInfo.Analyze`" per its own description.
- **DYNAMIC LENGTH elementary items entirely absent.** §8.5.1.10 / §13.18.19 (a min-length-zero variable-length `PIC X`/`N` string) has no grammar, no binding, no runtime. It is distinct from OCCURS DYNAMIC (a growable *table*), which is complete. The 2014-introduced clause is `DYNAMIC LENGTH [dynamic-length-structure-name-1] [LIMIT IS integer-1]`; the 2023 spec's only 2014→2023 delta for it is VCR row 60 ("SET enhanced to set its length" — E.3.3 item 17), which this phase explicitly DEFERS to Phase 13.
- **FUNCTION-POINTER / PROGRAM-POINTER data absent.** The grammar comment at `CobolData.g4:447-448` admits "BOOLEAN, DATA-POINTER, FUNCTION-POINTER, PROGRAM-POINTER … require lexer tokens not yet defined." §8.5.2.7 (function-pointer category) / §8.5.2.15 (program-pointer category) are unimplemented. `POINTER` (data-pointer) already exists.
- **`>>PROPAGATE` recognized but not re-editioned.** `ConditionalCompilationProcessor.cs:36` lists `PROPAGATE` among compilation-variable names, but the §7.3.21 `>>PROPAGATE` directive (which turns on automatic EC propagation across the run unit) is not gated to its correct edition span (≤2014).
- **TYPEDEF edges nearly done.** TYPEDEF + the `TYPE` clause are FEATURE-COMPLETE and review-hardened (matrix rows `typedef-def-2002`, `type-clause-2002` already `active`), and `SAME AS` LANDED in P10 Step 16 (§13.18.49 via the ONE `CloneItem`/`ExpandSameAs`; row `same-as-clause-2002` `active`). Only `TYPE TO` (the pointer-target form) remains deferred (conformance-plan M3-2). This phase only confirms those edges and catalogues `TYPE TO`.

The net effect: after P12 the 2014 introduction surface is either implemented-and-matrix-locked or explicitly deferred-with-a-cited-row — no silent mis-binds, no stale `pending` rows for shipped features.

---

## 3. Target end-state (what exists when this phase is DONE)

**Grammar (`src/Cobol.Net.Frontend/Grammar/Core/CobolData.g4` + `CobolLexer.g4`):**
- `usageKeyword` gains `{is2014()}? floatBinaryUsage` and `{is2014()}? floatDecimalUsage` alternatives (the `FLOAT-BINARY-32/64/128`, `FLOAT-DECIMAL-16/34` family), plus `{is2014()}? PROGRAM_POINTER` and `{is2014()}? functionPointerUsage` (FUNCTION-POINTER, optionally `TO function-prototype-name`).
- A new `dynamicLengthClause : {is2014()}? DYNAMIC LENGTH dataName? (LIMIT IS? integerLiteral)?` alternative in the data-description clause list.
- New lexer tokens as needed: `PROGRAM_POINTER : 'PROGRAM-POINTER'`, `FUNCTION_POINTER : 'FUNCTION-POINTER'` (only if a hyphenated single token is chosen; otherwise reuse `FUNCTION`/`POINTER` — see Step 8). `FLOAT_BINARY`/`FLOAT_DECIMAL`/`DYNAMIC`/`LENGTH`/`LIMIT` tokens already exist.

**Compiler:**
- `PicInfo.Usage` gains `FloatBinary32/64/128`, `FloatDecimal16/34`, `ProgramPointer`, `FunctionPointer` members; `PictureAnalyzer.ParseUsage` maps every new grammar-accepted usage keyword to a real member (no `COBOLNET0899` reachable for a shipped keyword). The float family maps to `float`/`double`/`decimal` (or `System.Decimal`-backed `CobolDec` for FLOAT-DECIMAL) native types.
- `DataItem` carries a `DynamicLength` fact (min-0 variable string) computed once — a new `StorageForm` case `DynamicString` (`Binding/Model/StorageForm.cs`) selected by `StorageFormPass`, with the limit/structure-name carried as init-only `DataItem` fields.
- External-float `E`-symbol PICTURE recognized in `PictureAnalyzer.Analyze` → `Usage.Display` external-float category rendered through `CobolFloat` string↔double.
- `>>PROPAGATE` gated in the preprocessor / turn-collection so it is accepted ≤2014 and drives EC propagation (or is a documented no-op-with-warning if EC-propagation semantics are Phase-13 scope — see Step 9).
- Diagnostic band **1540-1559** allocated to P12 (1538 is the current TYPEDEF high-water mark): 1540-1544 DYNAMIC LENGTH; 1545-1549 float family + E-symbol; 1550-1554 function/program pointers; 1555-1559 conditional-expression / `>>PROPAGATE`. Register each in the diagnostic registry (`Cobol.Net.Compiler/Diagnostics/` after the P-test rewrite; `EditionCodes`/bare-string today).

**Runtime (`src/Cobol.Net.Runtime`):**
- A `CobolDynString` (variable-length, min-0) value type under `Text/`, backing DYNAMIC LENGTH items. Reuses `CobolString` semantics where possible.
- `CobolFloat` extended for binary32/64/128 + decimal16/34 formats (128-bit / decimal use `System.Decimal` or a documented widened carrier; see Step 6 for the IEEE-754 fidelity note).
- `CobolPtr` (or `ManagedPointer`) extended (or a sibling `CobolFunctionPtr`/`CobolProgramPtr`) for program-/function-pointer values, per the single-managed-pointer-carrier rule (memory `feedback_managed_pointers`).

**Tests:**
- `tests/conformance/2014/` grows one `.cob` + `.out` per feature (all listed in `manifest.json` `enabled`): `dynamic_length_item`, `dynamic_length_limit`, `float_binary`, `float_decimal`, `external_float_pic`, `program_pointer`, `function_pointer` (prototype-dependent), plus `>>PROPAGATE` and conditional-expression programs as they land.
- `tests/version-matrix/constructs.json`: `usage-float-short/long/extended-2002` and `usage-float-binary32-2014` flipped `pending`→`active`; new `active` rows `dynamic-length-item-2014`, `usage-float-decimal-2014`, `usage-external-float-pic-2002`, `program-pointer-2014`, `function-pointer-2014`. `docs/VERSION_CHANGE_REFERENCE.md` 2014-introduction status cells updated (row 60 stays TODO — that is the 2023 SET-length delta).
- Unit tests under `tests/Cobol.Net.Tests.Conformance/` (or `.Unit/`) per feature (`DynamicLengthTests`, `FloatFamilyTests`, `PointerDataTests` extensions), mirroring the `TypedefStrongTests`/`OccursDynamicGuardTests` pattern (GreenfieldOnly for greenfield-only features).

---

## 4. STEP-BY-STEP

> Diagnostic band for this phase: **1540-1559.** Grammar-touching steps (5, 6, 8) require the FULL legacy guard before commit. Every feature-landing step adds its `tests/conformance/2014/*.cob` + `.out` + `manifest.json` entry **in the same commit** (memory `feedback_conformance_tests_per_feature`, `feedback_parse_and_emit_together`).

### [x] Step 0 — Baseline & branch (COMMIT BOUNDARY: none; setup only)

- **Do:** From `main` (default branch), create a working branch `phase-12-m3-2014`. Confirm the green baseline (§1.2). Record the baseline counts (conformance/unit/NIST) in the DEVLOG draft.
- **Why:** A known-green start so any red is attributable to this phase.
- **Verify:** the three battery commands in §1.2 all green.

### [x] Step 1 — Verify OCCURS DYNAMIC across all four editions; make its matrix row assert (COMMIT BOUNDARY)

- **Files:** `tests/version-matrix/constructs.json` (row `occurs-dynamic-2014` — already `active`, verify), `tests/conformance/2014/manifest.json` (the `dyn_*` programs already enabled — verify byte-match).
- **Do:**
  1. Run the OCCURS DYNAMIC corpus through the four-edition matrix. For each `dyn_*.cob`, confirm: `--std 2014` and `--std 2023` compile+run+byte-match the `.out`; `--std 1985` and `--std 2002` are REJECTED with `COBOLNET0900` (introduction gate). Use `EditionHarness.CompileFull(src, ed)` semantics or the CLI:
     ```
     dotnet .../cobol.dll tests/conformance/2014/dyn_declare.cob --std 2002 -o /tmp/x.dll   # expect COBOLNET0900
     dotnet .../cobol.dll tests/conformance/2014/dyn_declare.cob --std 2014 -o /tmp/x.dll --run   # expect the .out
     ```
  2. Confirm the `occurs-dynamic-2014` row is `active` (no `status:"pending"`) so `VersionMatrixTests.ActiveConstruct_CompilesIffEditionAllows` asserts it both ways. If it is `pending`, flip it to `active`.
- **Why:** Exit criterion 2 requires the dynamic-table row green at all four editions. OCCURS DYNAMIC is already implemented; this step only proves + locks the matrix contract, catching any regression from the rearchitecture waves.
- **Verify:**
  ```
  dotnet test tests/Cobol.Net.Tests.Conformance -c Debug --filter "VersionMatrixTests|OccursDynamicGuardTests|OccursDifferentialTests|OdoDifferentialTests|CorpusRunnerTests"
  ```
  All green.
- **Commit:** `test(cobolnet): P12 step 1 — OCCURS DYNAMIC matrix-locked at all four editions (DEVLOG NNN)` (only if a row flip or a new assertion was needed; otherwise fold into Step 2).

### [x] Step 2 — Flip the live float trio matrix rows `pending`→`active` (COMMIT BOUNDARY)

- **Files:** `tests/version-matrix/constructs.json` (rows `usage-float-short-2002`, `usage-float-long-2002`, `usage-float-extended-2002`).
- **Do:** Remove `"status": "pending"` (defaults to `active`) from the three trio rows — the feature is already live (`PictureAnalyzer.ParseUsage`). Update each row's `description` to drop the "PENDING … Phase 6 implements" clause and state "LIVE (P12 step 2)". Keep `introducedIn: 2002`, `expectDiagnostic: "COBOLNET0900"`.
- **Why:** The rows lag the code; the matrix must assert what ships (`VersionMatrixTests` only asserts `active` rows). Fixes the stale-metadata smell.
- **Verify:**
  ```
  dotnet test tests/Cobol.Net.Tests.Conformance -c Debug --filter "VersionMatrixTests"
  ```
  The three float-trio cases now compile at 2002/2014/2023 and reject with `COBOLNET0900` at 1985. Green.
- **Commit:** `test(cobolnet): P12 — activate the live FLOAT-SHORT/LONG/EXTENDED matrix rows (DEVLOG NNN)`.

### [x] Step 3 — Add a real float-trio conformance program to the 2014 corpus (COMMIT BOUNDARY)

- **Files:** create `tests/conformance/2014/float_trio.cob` + `.out`; edit `tests/conformance/2014/manifest.json` (add `float_trio` to `enabled`). (Note: the trio is a 2002 feature, but per the OCCURS-DYNAMIC precedent, 2014-and-earlier float corpus programs live under `2014/`; alternatively add a `2002/` program — pick per where the manifest coverage is thinnest. Recommendation: `2002/float_trio` since introducedIn=2002, so the 2002 corpus exercises it at its own edition.)
- **Do:** Write a program that declares `FLOAT-SHORT`/`FLOAT-LONG`/`FLOAT-EXTENDED` items, does arithmetic (`COMPUTE`, `ADD`), and `DISPLAY`s results. Generate the `.out` by running the compiled program once and **verifying the output is arithmetically CORRECT** (not merely non-crashing — memory `feedback_verify_demo_output`); cite the IEEE-754 rounding you expect. Unique `PROGRAM-ID` (memory `feedback_unique_programid_per_test`).
- **Why:** Exit criterion 1 (non-empty, discovered corpus) + `feedback_conformance_tests_per_feature`.
- **Verify:**
  ```
  dotnet .../cobol.dll tests/conformance/2002/float_trio.cob --std 2002 -o /tmp/ft.dll --run   # matches .out
  dotnet test tests/Cobol.Net.Tests.Conformance -c Debug --filter "CorpusRunnerTests"
  ```
  `Manifest_CoversEveryProgram_NoOverlap` and `EnabledProgram_CompilesStrict_AndMatchesOutIfPresent` green.
- **Commit:** `feat(cobolnet): P12 — float-trio 2002 conformance program (DEVLOG NNN)`.

### [x] Step 4 — DYNAMIC LENGTH: runtime carrier + data model (COMMIT BOUNDARY)

- **Files:**
  - create `src/Cobol.Net.Runtime/Text/CobolDynString.cs`. A variable-length, min-0 string over `CobolString` semantics: a `string Value` with a `Limit` (max chars; `int.MaxValue`/`-1` if implementor-default per §13.18.19.4 GR2) and `Class` (Alphanumeric for `X`, National for `N`, §13.18.19.4 GR1). Constructor, `Length`, MOVE-in truncation-to-limit, `ToString`.
  - `src/Cobol.Net.Compiler/Binding/Model/StorageForm.cs`: add a `DynamicString` case; add the limit/structure-name as init-only `DataItem` fields `int DynLengthLimit`, `string? DynLengthStructureName`.
- **Do:** Model the value only (no grammar yet). `StorageFormPass` selects `DynamicString` when the entry has the DYNAMIC LENGTH fact; `RecordLayout` treats its physical width as variable (limit-bounded).
- **Why:** Establish the storage representation ONCE before the emitter/binder consume it (invariant 1 — typed-native, no byte substrate; the string is a native `CobolDynString` field, never a byte window). Keeps the "one storage-form decision" discipline (DESIGN-data-model.md).
- **Verify:** `dotnet build src/Cobol.Net.Runtime` and `src/Cobol.Net.Compiler` succeed; add a `CobolDynString` unit test (round-trip, truncation-to-limit, min-0). `dotnet test tests/Cobol.Net.Tests.Unit --filter CobolDynString`.
- **Commit:** `feat(cobolnet): P12 — CobolDynString runtime carrier + DynamicString storage form (DEVLOG NNN)`.

### [x] Step 5 — DYNAMIC LENGTH: grammar + binder + emitter + conformance (COMMIT BOUNDARY — FULL LEGACY GUARD)

- **Files:**
  - `src/Cobol.Net.Frontend/Grammar/Core/CobolData.g4`: add `dynamicLengthClause : {is2014()}? DYNAMIC LENGTH dataName? (LIMIT IS? integerLiteral)?` and wire it into the data-description-entry clause alternation (beside `occursClause`). Tokens `DYNAMIC`/`LENGTH`/`LIMIT` already exist (`CobolLexer.g4:366,444,410`) — no lexer change, so this is LL-safe: `DYNAMIC LENGTH` is disjoint from `OCCURS DYNAMIC` (different leading token).
  - `src/Cobol.Net.Compiler/Binding/DataBinder.cs` (`BindEntry`): decode the clause. Enforce §13.18.19.3 SR1 (PICTURE must be exactly one `N` or one `X`) → **COBOLNET1540** on violation; SR4 (LIMIT ≤ structure max when a structure-name is given — if structures are unimplemented, reject a structure-name with **COBOLNET1541** "DYNAMIC LENGTH STRUCTURE not supported", a staged-loud guard); set the `StorageForm.DynamicString` fact.
  - Emitter (`RecordStructEmitter`): emit a `CobolDynString` field initialized to empty (min length 0, §13.18.19.4 GR1). MOVE into it truncates to limit; reference/DISPLAY reads current length.
  - create `tests/conformance/2014/dynamic_length_item.cob` + `.out` and `tests/conformance/2014/dynamic_length_limit.cob` + `.out`; add both to `manifest.json` `enabled`.
- **Do:** Implement the min-0 variable string. A `MOVE "ABC" TO DL-ITEM` sets length 3; `MOVE SPACES`/`MOVE ""`... `FUNCTION LENGTH(DL-ITEM)` returns the current length; overflow past LIMIT truncates (§13.18.19.4 GR2). **Do NOT** implement `SET DL-ITEM LENGTH …` — that is the 2023 delta (VCR row 60, Phase 13); if encountered, it should be a clean unimplemented diagnostic, not a silent accept.
- **Why:** §8.5.1.10 / §13.18.19 is a core 2014 introduction. Implement COMPLETELY to the spec (memory `feedback_spec_scopes_not_tests`) minus the explicitly-deferred 2023 SET-length enhancement.
- **Verify:**
  ```
  dotnet build src/Cobol.Net.Frontend   # regenerates ANTLR; a failed regen FAILS the build
  dotnet .../cobol.dll tests/conformance/2014/dynamic_length_item.cob --std 2002 -o /tmp/x.dll   # expect COBOLNET0900
  dotnet .../cobol.dll tests/conformance/2014/dynamic_length_item.cob --std 2014 -o /tmp/x.dll --run   # matches .out
  dotnet test tests/Cobol.Net.Tests.Conformance -c Debug --filter "CorpusRunnerTests|DynamicLengthTests"
  bash scripts/guard-fast.sh    # FULL LEGACY GUARD — grammar changed; expect NIST 353 MATCH
  ```
- **Commit:** `feat(cobolnet): P12 — DYNAMIC LENGTH elementary items (§8.5.1.10/§13.18.19, 2014); NIST 353 MATCH (DEVLOG NNN)`.

### [x] Step 6 — DYNAMIC LENGTH: version-matrix row (COMMIT BOUNDARY)

- **Files:** `tests/version-matrix/constructs.json` (new `active` row `dynamic-length-item-2014`); `docs/VERSION_CHANGE_REFERENCE.md` (mark the 2014-introduction status; keep row 60 — the 2023 SET-length delta — as TODO with a "P13" note).
- **Do:** Add the row: `introducedIn: 2014`, `removedIn: null`, `expectDiagnostic: "COBOLNET0900"`, `vcr: "2014 introduction (§8.5.1.10 / §13.18.19 DYNAMIC LENGTH)"`, a minimal `source` (a `01 D PIC X DYNAMIC LENGTH LIMIT IS 20.` program that `DISPLAY`s and `STOP RUN`s).
- **Why:** Exit criterion 2 (dynamic-length row green at all four editions). The matrix computes the expected outcome from `introducedIn`.
- **Verify:** `dotnet test tests/Cobol.Net.Tests.Conformance --filter "VersionMatrixTests"` — the new row compiles at 2014/2023, rejects at 1985/2002. Green.
- **Commit:** fold into Step 5's commit if done together, else `test(cobolnet): P12 — dynamic-length-item version-matrix row, active at 4 editions (DEVLOG NNN)`.

### [ ] Step 7 — IEEE float family (`FLOAT-BINARY-*` / `FLOAT-DECIMAL-*`) + external-float `E` PICTURE (COMMIT BOUNDARY — FULL LEGACY GUARD)

- **Files:**
  - `src/Cobol.Net.Frontend/Grammar/Core/CobolData.g4` `usageKeyword`: add `| {is2014()}? floatBinaryUsage | {is2014()}? floatDecimalUsage`, with `floatBinaryUsage : FLOAT_BINARY (integerLiteral)?` (accept the `-32/-64/-128` suffix; the token is `FLOAT-BINARY`, the numeric part follows) and `floatDecimalUsage : FLOAT_DECIMAL (integerLiteral)?`. Confirm the lexer splits `FLOAT-BINARY-32` as `FLOAT_BINARY` + `-32` or a single token; if the hyphen-number is lexed together, add explicit tokens `FLOAT_BINARY_32/64/128`, `FLOAT_DECIMAL_16/34` in `CobolLexer.g4` (reproduce a quick `dotnet .../cobol.dll` probe to see how `FLOAT-BINARY-32` tokenizes before choosing).
  - `src/Cobol.Net.Compiler/Binding/Model/PicInfo.cs`: add `Usage.FloatBinary32/64/128`, `Usage.FloatDecimal16/34`; extend `PictureAnalyzer.ParseUsage` with an arm per keyword, each calling `ConstructRegistry.Check(edition, "usage-float-binary32-2014", where)` etc. Map binary32→`float`, binary64→`double`, binary128→`decimal` (or a documented widened `double` with a fidelity note), decimal16→`decimal`, decimal34→`decimal`. Extend the `IsFloat`/`IsSingle`/`CsTypeName`/`ZeroLiteral` switches (`PicInfo.cs:244-276`) for every new member — **exhaustively** (memory `feedback_scan_all_similar`; a missed arm is a silent mis-type).
  - External-float `E` symbol: in `PictureAnalyzer.Analyze`, recognize the `E` picture symbol (§13.18.40 external floating-point) → a `Usage.Display` external-float category; render through `CobolFloat` string↔double. Add **COBOLNET1545** for an ill-formed external-float picture (mantissa/exponent SR violations, §13.18.40.3).
  - `src/Cobol.Net.Runtime` `CobolFloat`: add binary128 / decimal16 / decimal34 format handling (see fidelity note below).
  - create `tests/conformance/2014/float_binary.cob`+`.out`, `float_decimal.cob`+`.out`, `tests/conformance/2002/external_float_pic.cob`+`.out`; manifest entries.
- **IEEE-754 fidelity note (cite in the DEVLOG + a code comment):** .NET has no native binary128 or IEEE decimal128/decimal64. Per §4.2.15 (limits/precision implementor-defined) and §13.18.60.4 GR13 (float representations implementor-defined), backing FLOAT-BINARY-128 by `double` and FLOAT-DECIMAL-16/34 by `System.Decimal` is a **conforming implementor choice** — document it as such (`feedback_bare_end`: no unsourced conformance claim; cite §4.2.15 + §13.18.60.4). Do NOT silently pretend full 128-bit precision.
- **Do:** Wire the 2014 float family end-to-end (parse → bind → emit → run) mirroring the live trio. Flip `usage-float-binary32-2014` `pending`→`active`; add `usage-float-decimal-2014` and `usage-external-float-pic-2002` `active` rows.
- **Why:** Clears the D16 float-split 2014 half + the external-float PICTURE — the "IEEE-754 float usages made loud in Phase 3 now implemented with split 2002/2014 edges" scope item.
- **Verify:**
  ```
  dotnet build src/Cobol.Net.Frontend
  dotnet .../cobol.dll tests/conformance/2014/float_binary.cob --std 2002 -o /tmp/x.dll     # COBOLNET0900 (2014 gate)
  dotnet .../cobol.dll tests/conformance/2014/float_binary.cob --std 2014 -o /tmp/x.dll --run  # matches .out
  dotnet test tests/Cobol.Net.Tests.Conformance --filter "VersionMatrixTests|FloatFamilyTests|CorpusRunnerTests|WideNumericTests|StandardDecimalTests"
  bash scripts/guard-fast.sh    # grammar changed — NIST 353 MATCH
  ```
- **Commit:** `feat(cobolnet): P12 — FLOAT-BINARY-*/FLOAT-DECIMAL-* (2014) + external-float E PICTURE; matrix rows active (DEVLOG NNN)`.

### [ ] Step 8 — FUNCTION-POINTER / PROGRAM-POINTER data (COMMIT BOUNDARY — FULL LEGACY GUARD)

- **Files:**
  - `CobolLexer.g4`: add `PROGRAM_POINTER : 'PROGRAM-POINTER'`; for FUNCTION-POINTER prefer reusing `FUNCTION`+`POINTER` context or add `FUNCTION_POINTER : 'FUNCTION-POINTER'` (decide by a tokenization probe — hyphenated words are single tokens here, so a dedicated token is cleanest).
  - `CobolData.g4` `usageKeyword`: add `| {is2014()}? PROGRAM_POINTER | {is2014()}? functionPointerUsage`, with `functionPointerUsage : FUNCTION_POINTER (TO functionPrototypeName)?` (§8.5.2.7 — the prototype-dependent form). Update the stale comment at `CobolData.g4:447-448`.
  - `PictureAnalyzer.ParseUsage` + the `Usage` enum (`Binding/Model/PicInfo.cs`): `Usage.ProgramPointer`, `Usage.FunctionPointer`; map to the runtime pointer carrier.
  - Runtime: extend the single `ManagedPointer`/`CobolPtr` carrier (memory `feedback_managed_pointers` — ONE managed-ref carrier, never a parallel registry) to hold a program/function reference, or add `CobolFunctionPtr`/`CobolProgramPtr` value types under `Control/` following the singular-pattern rule.
  - `SET` binding: SET a program-pointer from `ENTRY`/a program-name; SET a function-pointer from a function-prototype (§8.5.2.7 / §14.9.31 SET formats). Gate any not-yet-supported SET format loudly (**COBOLNET1550/1551**).
  - create `tests/conformance/2014/program_pointer.cob`+`.out`; `function_pointer.cob`+`.out` (prototype-dependent — if function prototypes are not yet bindable from P10, land PROGRAM-POINTER fully and stage FUNCTION-POINTER loud with **COBOLNET1552** "FUNCTION-POINTER requires a function prototype (M3-4 follow-up)", and record it as a `pending` matrix row + a `pending` manifest entry so it is catalogued, not silently missing).
- **Do:** Implement PROGRAM-POINTER data + SET fully (it only needs a program name / ENTRY). Implement FUNCTION-POINTER to the extent function prototypes exist post-P10; otherwise stage it loud and pending with a cited follow-up.
- **Why:** §8.5.2.7 / §8.5.2.15 pointer categories are 2014 surface. Prototype-dependence is called out in the phase scope ("FUNCTION-POINTER (prototype-dependent)").
- **Verify:**
  ```
  dotnet build src/Cobol.Net.Frontend
  dotnet .../cobol.dll tests/conformance/2014/program_pointer.cob --std 2014 -o /tmp/x.dll --run   # matches .out
  dotnet .../cobol.dll tests/conformance/2014/program_pointer.cob --std 2002 -o /tmp/x.dll          # COBOLNET0900
  dotnet test tests/Cobol.Net.Tests.Conformance --filter "PointerDataTests|PointerAddressingTests|VersionMatrixTests|CorpusRunnerTests"
  bash scripts/guard-fast.sh    # grammar changed — NIST 353 MATCH
  ```
- **Commit:** `feat(cobolnet): P12 — PROGRAM-POINTER (full) + FUNCTION-POINTER (prototype-dependent) data, 2014 (DEVLOG NNN)`.

### [ ] Step 9 — `>>PROPAGATE` re-editioning + conditional-expression enhancements (COMMIT BOUNDARY)

- **Files:**
  - `>>PROPAGATE`: `src/Cobol.Net.Frontend/Preprocessor/` (the directive handler; `ConditionalCompilationProcessor.cs:36` already lists `PROPAGATE` as a variable name — that is the `>>DEFINE`-style *variable*, NOT the §7.3.21 directive). Add/route the `>>PROPAGATE ON|OFF` directive (§7.3.21) so it is recognized ≤2014 (its introduction edition — confirm against §7.3.21 and the VCR before gating; it controls automatic EC propagation to a calling runtime element). If EC-propagation *runtime semantics* are Phase-13/EC-remnant scope, this phase makes the directive **recognized and edition-gated with a warning that propagation is honored per the EC model**, not a parse error — cite the exact behavior in the DEVLOG (`feedback_bare_end`).
  - Conditional-expression enhancements (§8.8.4): identify the specific 2014 deltas from `docs/VERSION_CHANGE_REFERENCE.md` + §8.8.4 (e.g. any relaxed abbreviated-combined-relation forms). Implement only genuinely-2014 introductions; gate each. **"Increased limits" is explicitly DROPPED** (§4.2.15 delegates limits to the implementor — add a one-line note in the DEVLOG that this scope item is intentionally a no-op).
  - Tests: `tests/conformance/2014/propagate_directive.cob`+`.out`; a conditional-expression program if a real 2014 delta is implemented.
- **Do:** Re-edition `>>PROPAGATE`; implement any real §8.8.4 2014 conditional-expression delta; document the increased-limits no-op.
- **Why:** Scope items ">>PROPAGATE §7.3.21 re-editioned ≤2014" and "conditional-expression enhancements". Keeps the four-editions-in-one gating honest.
- **Verify:**
  ```
  dotnet .../cobol.dll tests/conformance/2014/propagate_directive.cob --std 2014 -o /tmp/x.dll --run
  dotnet test tests/Cobol.Net.Tests.Conformance --filter "CorpusRunnerTests|AbbreviatedConditionDifferentialTests|ExceptionConditionConformanceTests"
  ```
- **Commit:** `feat(cobolnet): P12 — >>PROPAGATE re-editioned (§7.3.21, ≤2014) + §8.8.4 conditional-expr deltas; increased-limits DROPPED per §4.2.15 (DEVLOG NNN)`.

### [ ] Step 10 — TYPEDEF / `SAME AS` / `TYPE TO` edges: confirm + catalogue `TYPE TO` (COMMIT BOUNDARY)

- **Files:** `tests/version-matrix/constructs.json` (rows `typedef-def-2002`, `type-clause-2002`, `same-as-clause-2002` — confirm `active`); the `TYPE TO` deferral row.
- **Do:** TYPEDEF + `TYPE` are DONE, and **`SAME AS` LANDED in P10 Step 16** (§13.18.49 on the ONE `CloneItem`/`ExpandSameAs`; row `same-as-clause-2002` ACTIVE; golden `typedef_same_as`; SR bands 1555/1556/1557 — this step no longer owns it). Verify the three matrix rows are `active` and the `typedef_*` goldens green; no code change expected. **`TYPE TO`** (the pointer-target form) stays DEFERRED — record it as a `pending` matrix row (`type-to-2014`) with a cited owning-follow-up so the matrix catalogues it (never a silent gap — `VersionMatrixTests.PendingRow_HasActivationContract` requires a vcr + source).
- **Why:** Scope item "TYPEDEF / SAME AS / TYPE TO edges (provisional 2002/2014 per Phase 3)". The edges must be either asserted (`active`) or catalogued-deferred (`pending`), not stale.
- **Verify:** `dotnet test tests/Cobol.Net.Tests.Conformance --filter "TypedefStrongTests|TypedefConditionTests|TypedefResidueTests|TypedefReviewFixTests|VersionMatrixTests"` green.
- **Commit:** `test(cobolnet): P12 — confirm TYPEDEF/TYPE matrix edges; catalogue SAME AS / TYPE TO as pending (DEVLOG NNN)` (or a `feat` if SAME AS implemented).

### [ ] Step 11 — Sync docs + conformance ledger (COMMIT BOUNDARY)

- **Files:** `docs/ISO2023_CONFORMANCE_PLAN.md` (§3.8 M3: mark M3-4 items DONE/deferred per what landed; M3-1 already ☑, M3-2 already ◑), `docs/VERSION_CHANGE_REFERENCE.md` (2014-introduction status cells; row 60 stays TODO→"P13"), `docs/DOC_INDEX.md` (if a new subsystem doc was added), `resume-prompt.md` top STATE banner (P12 progress — memory `feedback_plan_updates`), and the relevant deep-dive (`docs/COBOLNET_DATA_MODEL_DESIGN.md` for DYNAMIC LENGTH / float family, per `feedback_follow_design_docs_and_spec` — keep the deep-dive current in the SAME change set).
- **Do:** Reconcile every ledger with what actually shipped; note any spec-fidelity residues (e.g. the binary128/decimal-fidelity implementor choice, deferred function-prototype FUNCTION-POINTER, deferred SET-length).
- **Verify:** `dotnet test tests/Cobol.Net.Tests.Conformance --filter "VersionMatrixTests"` (the registry drift tests bind constructs.json to the ledger — they must stay green).
- **Commit:** `docs(cobolnet): P12 — sync conformance ledger + VCR + resume banner for the 2014 deltas (DEVLOG NNN)`.

### [ ] Step 12 — Adversarial find→verify review of P12 (COMMIT BOUNDARY)

- **Do:** Every prior feature's post-implementation adversarial review found real defects (OCCURS DYNAMIC: 7 confirmed; TYPEDEF: 7 confirmed). Run a find→verify sweep over the P12 code: float exhaustiveness (every `Usage.*` member handled in every `switch`), DYNAMIC LENGTH edge cases (empty MOVE → length 0; overflow truncation; `FUNCTION LENGTH`; REDEFINES/OCCURS interaction — reject if illegal per §13.18.19.3), pointer SET formats, and the edition gates at all four `--std`. Fix each confirmed defect; add a golden/guard per fix (memory `feedback_scan_all_similar`).
- **Verify:** the full battery (see §5). Add the review's new tests.
- **Commit:** `fix(cobolnet): P12 adversarial review — N confirmed defects, all fixed (DEVLOG NNN)`.

### [ ] Step 13 — Merge to `main`, push (COMMIT BOUNDARY)

- **Do:** Ensure the full battery is green (§5). Update this file's STATUS line to `DONE`. Merge the branch to `main`; push (memory `feedback_fully_autonomous_push` — commit AND push every checkpoint; never ask "should I push"). Write the final DEVLOG entry (newest-first, real timestamp from `date "+%Y-%m-%d %H:%M %Z"`).

---

## 5. Verification (run at phase end + at every grammar commit boundary)

Full battery — all must be green with **zero regressions** vs the Step-0 baseline:

```
# Greenfield conformance (target: ≥3166 + the P12 additions, all green)
dotnet test E:/CobolSharp/tests/Cobol.Net.Tests.Conformance/Cobol.Net.Tests.Conformance.csproj -c Debug

# Unit (target: ≥281 + P12 additions)
dotnet test E:/CobolSharp/tests/Cobol.Net.Tests.Unit/Cobol.Net.Tests.Unit.csproj -c Debug

# FULL LEGACY GUARD — NIST differential; expect 353 MATCH (+ the known LEGACY_DIVERGENT set unchanged)
bash E:/CobolSharp/scripts/guard-fast.sh        # or scripts/guard.ps1 (post DESIGN-test-build-ci)
```

Byte-exact / behavior-neutrality checks:

- **Neutrality:** the NIST 353 MATCH count and the greenfield conformance count must not drop. A grammar change that perturbs NIST is a regression until a spec citation says otherwise (memory `feedback_diff_is_a_bug`). The three grammar steps (5, 7, 8) each ran the full legacy guard at their commit — re-run once more at phase end.
- **Matrix four-edition proof:** `VersionMatrixTests` asserts each `active` P12 row compiles iff `introducedIn ≤ V` and rejects with `COBOLNET0900` below. Confirm `occurs-dynamic-2014` and `dynamic-length-item-2014` are both `active` and green (exit criterion 2).
- **Corpus discovery:** `CorpusRunnerTests.Manifest_CoversEveryProgram_NoOverlap("2014")` green — no on-disk `.cob` unlisted, no phantom manifest entry (exit criterion 1).
- **Output correctness (not just non-crash):** each new `.out` was generated by RUNNING the program and hand-verified against the spec-derived expected result (memory `feedback_verify_demo_output`), not captured blindly.

---

## 6. Rollback / resumability + risks

**Resume mid-phase:** each step is atomic and its "verify" command tells you whether the last commit is sound. `git log --oneline` shows which step-commits landed; re-run §5 to locate the frontier. The STATUS line + the §4 checkboxes are the ledger — trust them over memory.

**Rollback a bad step:** `git revert <commit>` a single step (they are independent except: Step 6 depends on Step 5's grammar; Step 7's matrix rows depend on Step 7's code). No step deletes another's work.

**Risks & mitigations:**
- *Grammar perturbs NIST.* Mitigation: each grammar step runs the FULL legacy guard before commit; `{is2014()}?`-gate every new alternative so a pre-2014 parse is unaffected (memory `feedback_grammar_version_factoring`). `DYNAMIC LENGTH` is LL-disjoint from `OCCURS DYNAMIC` (different leading token); the float/pointer usages are new alternatives in `usageKeyword`, added incrementally.
- *IEEE-754 fidelity overclaim.* Mitigation: back binary128/decimal by `double`/`System.Decimal` and CITE §4.2.15 + §13.18.60.4 GR13 as the implementor-defined licence in code + DEVLOG (memory `feedback_bare_end`, `feedback_spec_fidelity_discipline`).
- *FUNCTION-POINTER blocked on prototypes.* Mitigation: land PROGRAM-POINTER fully; stage FUNCTION-POINTER loud + `pending` (catalogued, never silent) with a cited follow-up.
- *Float exhaustiveness gap.* Mitigation: Step 12's adversarial review specifically checks every `Usage.*` member is handled in every `switch` (the historical silent-mis-type class — memory `feedback_scan_all_similar`).
- *`>>PROPAGATE` runtime semantics scope creep.* Mitigation: this phase re-editions the directive (recognized ≤2014); if full EC-propagation runtime behavior is EC-remnant/Phase-13 scope, make it recognized-with-honored-per-EC-model, and DOCUMENT the boundary — do not silently no-op.

---

## 7. ISO feature work in this phase (spec sections, editions, tests)

| Feature | Spec § | Introduced | Conformance test(s) | Matrix row(s) |
|---|---|---|---|---|
| OCCURS DYNAMIC (verify + lock) | §13.18.38 Format 4 / §8.5.1.9 | 2014 | `2014/dyn_*` (exist) | `occurs-dynamic-2014` (active) |
| DYNAMIC LENGTH elementary item | §8.5.1.10 / §13.18.19 | 2014 | `2014/dynamic_length_item`, `dynamic_length_limit` | `dynamic-length-item-2014` (active) — **NOT** the 2023 SET-length (VCR row 60 → P13) |
| FLOAT-SHORT/LONG/EXTENDED (activate) | §13.18.59 / §13.18.60.4 GR13 | 2002 | `2002/float_trio` | `usage-float-{short,long,extended}-2002` (flip active) |
| FLOAT-BINARY-32/64/128, FLOAT-DECIMAL-16/34 | §13.18.59 | 2014 | `2014/float_binary`, `float_decimal` | `usage-float-binary32-2014` (flip active), `usage-float-decimal-2014` (new active) |
| External-float `E` PICTURE | §13.18.40 | 2002 | `2002/external_float_pic` | `usage-external-float-pic-2002` (new active) |
| PROGRAM-POINTER data | §8.5.2.15 | 2014 | `2014/program_pointer` | `program-pointer-2014` (active) |
| FUNCTION-POINTER data (prototype-dependent) | §8.5.2.7 | 2014 | `2014/function_pointer` | `function-pointer-2014` (active if prototypes ready, else pending) |
| `>>PROPAGATE` directive | §7.3.21 | ≤2014 (confirm) | `2014/propagate_directive` | (directive — gate, no matrix data-row unless a construct row fits) |
| Conditional-expression enhancements | §8.8.4 | 2014 | as a real delta is found | per delta |
| `SAME AS` / `TYPE TO` | §13.18.57 / §13.18.58 | 2002/2014 | deferred | `same-as-2014`, `type-to-2014` (pending, catalogued) unless owner schedules SAME AS |
| TYPEDEF / `TYPE` (confirm) | §13.18.57 / §13.18.58 | 2002 | `typedef_*` (exist) | `typedef-def-2002`, `type-clause-2002` (active) |
| Increased limits | §4.2.15 | — | **DROPPED** (implementor-defined) | none |

**Editions:** every 2014-introduced construct compiles at `--std 2014` and `--std 2023`, and is rejected `COBOLNET0900` at `--std 1985` and `--std 2002`. Every 2002-introduced construct (float trio, external-float PIC) compiles at 2002/2014/2023, rejected at 1985. The matrix (`VersionMatrixTests`) computes and asserts this from each row's `introducedIn`.

**Diagnostic band:** 1540-1559 (1540-1544 DYNAMIC LENGTH; 1545-1549 float family + external-float PIC; 1550-1554 pointers; 1555-1559 conditional-expr / `>>PROPAGATE`).
