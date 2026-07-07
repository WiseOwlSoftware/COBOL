# PHASE 10 — M2 residual catalog: national/boolean, pointers, UDF, file-2002, RW / CONSTANT / concat

- **Phase:** 10
- **Title:** M2 residual catalog — close the mandatory COBOL-2002 non-OO surface on the unified data model + rearchitected runtime
- **Track:** feature-iso
- **Risk:** MEDIUM
- **Depends on (MUST be DONE before starting):** P5 (unified data model — `StorageForm` discriminator, `Model/` folder, `RecordLayout`, pass pipeline), P8 (runtime reorg — `RunUnit`, `FileConnector`/`FileRegistry`, `ManagedPointer`, role-based folders), P9 (OO rearchitecture — `Oo/` + `OoDriver`). Soft-adjacent: P6/P7 (binder phase + visitor dispatch) make the per-verb edits cleaner but are not hard blockers for any single step here.
- **Goal (one paragraph):** Every mandatory COBOL-2002 *non-OO* language feature is implemented end-to-end on the *rearchitected* substrate — national/boolean data ride `StorageForm.CharImage` (one UTF-16 char per position), pointers ride the `ManagedPointer` carrier, files ride `FileConnector`, UDFs ride the per-activation data model — with a rejecting diagnostic under every `--std` edition that lacks the feature, a discovered positive corpus entry, a version-matrix row, and a negative `.err` case per feature. The phase OPENS with a greenfield-vs-catalog reconciliation audit (a fresh `GreenfieldStatus` column sized against the *current* post-rearchitecture tree, not the DEVLOG-610 snapshot), so every subsequent wave is scoped against truth rather than the legacy-era ☑/◐ marks. It CLOSES with the M2 catalog marks flipped to greenfield truth and the full battery green.
- **Exit criteria:** Every track's positive corpus discovered by the greenfield runner (`CorpusRunnerTests` over `manifest.json`) + a version-matrix row + a negative `.err`; the M2 catalog (`docs/ISO2023_CONFORMANCE_PLAN.md` §3 and `docs/PHASE4_RECONCILIATION.md`) marks flipped to greenfield truth; national `CharImage` confirmed one-UTF-16-char-per-position by a runtime assertion + a golden; full battery green (2028+ greenfield conformance + 213+ unit + FULL legacy guard NIST 353 MATCH).

> **STATUS:** NOT STARTED
> _(The executing session updates this line: `NOT STARTED` → `IN PROGRESS @ step N (<short note>)` → `DONE`. Keep the per-step checkboxes in §4 current in the same commit that lands each step.)_

---

## 1. How to use this document

This is a **resumable** phase plan. A future session with no other context should be able to open this file, read the STATUS line + the §4 checkboxes, and continue from the first unchecked step. Every step names the exact files, the precise change, the reason, the verify command + expected result, and whether it is a **COMMIT BOUNDARY**. The battery MUST be green at every commit boundary.

**Canonical commands used throughout (all absolute; run from `E:/CobolSharp`):**

- Build the compiler + CLI:
  `dotnet build src/Cobol.Net.Cli/Cobol.Net.Cli.csproj -c Debug`
- Exercise one program end-to-end:
  `dotnet E:/CobolSharp/src/Cobol.Net.Cli/bin/Debug/net10.0/cobol.dll <src.cob> --std 2002 -o E:/Temp/out.dll --run`
  (swap `--std 85|2002|2014|2023` to check per-edition gating; add nothing for a clean run, expect exit 0 + stdout.)
- Greenfield conformance suite (the corpus runner + all differential/unit conformance tests):
  `dotnet test tests/Cobol.Net.Tests.Conformance/Cobol.Net.Tests.Conformance.csproj -c Debug`
- Greenfield unit suite:
  `dotnet test tests/Cobol.Net.Tests.Unit/Cobol.Net.Tests.Unit.csproj -c Debug`
- FULL legacy guard (the NIST 353-MATCH differential net; run on any grammar-touching or numeric-pipeline commit, and at phase end):
  `bash scripts/guard.sh`  (fast iteration: `bash scripts/guard-fast.sh`, proven byte-equivalent)

**Conformance corpus mechanics (verified as-built):**

- Positive corpus lives in `tests/conformance/{2002,2014,2023}/<name>.cob` (+ `<name>.out` golden). Each edition dir has a `manifest.json` with `"enabled"` and `"pending"` arrays. `CorpusRunnerTests` compiles+runs every ENABLED program at that dir's `--std` strict and byte-compares the `.out`; PENDING programs are catalogued but not asserted (the mass-red guard). An integrity fact asserts **every** on-disk `.cob` is listed (enabled ⊕ pending) — nothing is silently undiscovered. **To land a feature: add the `.cob`+`.out`, move the name from `pending`→`enabled`.**
- Negative corpus lives in `tests/conformance/negative/<name>.cob` + `<name>.err`; the `.err` pins the expected diagnostic (code + message) at the edition(s) the case names.
- Version-matrix rows live in `tests/version-matrix/constructs.json` (introduction/continuity gating) — each new 2002 construct that is a *compile-time introduction gate* gets a row so it enters the (construct × edition) matrix. Reserved-word interval rows go in the same file / `reserved-words.json`. A *runtime-observable* behavior (e.g. keyword-omitted forms) is NOT a matrix row — its golden + unit tests are its coverage (see M2-UDF-4 precedent).
- The catalog SSOTs to flip at the end: `docs/ISO2023_CONFORMANCE_PLAN.md` §3 (legacy-era marks) and `docs/PHASE4_RECONCILIATION.md` (the greenfield-truth table — this is the one to keep authoritative).

---

## 2. Rationale — the problems this phase fixes

The M2 (COBOL-2002) surface was largely built in the **retired legacy byte engine** and later re-landed piecemeal on the greenfield during "Phase 4"; the catalog carries three generations of stale marks. Concretely:

1. **The catalog marks lie in both directions.** `docs/ISO2023_CONFORMANCE_PLAN.md` §3 records LEGACY-era ☑/◐. `docs/PHASE4_RECONCILIATION.md` (DEVLOG 610) is truer but predates the rearchitecture (P1–P9) and several Phase-4 landings. The single largest historical finding — "M2-DATA done marks are legacy mirages" (national/boolean/pointers/floats staged LOUD `COBOLNET0899` in the greenfield) — has since been *partly* reclaimed, but nobody has re-audited against the **post-rearchitecture** tree where `StorageForm`, `RunUnit`, `FileConnector`, and `ManagedPointer` now own the substrate. **A wave sized against a stale mark is either wasted (re-implementing a landed feature) or a silent hole (skipping a regressed one).** → Step 1 is a fresh audit.

2. **National/boolean data must be re-confirmed on `StorageForm.CharImage`, not the old `StoreAsImage` flag.** P5 deleted the mutable `DataItem.StoreAsImage` and the emitter's `MarkStoreAsImage` write-back (DESIGN-data-model.md), replacing them with a single computed `StorageForm` discriminator where **`CharImage` subsumes every string-stored leaf including national and boolean**. The invariant "one UTF-16 char per national position, `ImageWidth == Length`" (DESIGN-data-model.md D-N1) is now a property of the `StorageFormPass`, not a scattered `IsNationalLike` doubling. This must be *proven*, not assumed (the exit criterion names it explicitly).

3. **Pointers must ride the ONE `ManagedPointer` carrier on the reorganized `Control/` runtime.** P8 split `ProgramRegistry.cs` and moved `ManagedPointer` to `Control/ManagedPointer.cs` under `RunUnit`. The 2002 pointer surface (`USAGE POINTER`/`PROGRAM-POINTER`, `NULL`, `SET`, `ADDRESS OF`, `BASED`, `ALLOCATE`/`FREE`) plus the `StorageCell`/`CellPointer` window model must be confirmed against the new home, and the residual `USAGE PROGRAM-POINTER` leg closed.

4. **Files must ride `FileConnector`.** P8 collapsed `SequentialFile`/`RelativeFile`/`IndexedFile` behind `FileConnector` + a polymorphic `FileRegistry`, deleting the `Keyed*` fallthrough. The 2002 file surface (SHARING / LOCK MODE / RETRY / UNLOCK / line-sequential + 2002 FILE STATUS 5x/6x) landed in Phase 4d on the OLD dispatch; it must be re-confirmed on the connector.

5. **Genuine open residue remains, on ANY substrate:** `&`-concatenation (§8.8.3, `concat-operator-2002` PENDING); CONSTANT entries (§13.10) + CONSTANT RECORD (§13.18.15) — **zero grammar/binder surface today** (`grep CONSTANT src/.../DataBinder.cs` → empty); Report Writer 2002 additions PRESENT WHEN format 1 + VARYING format 1 (**zero** hits in `DataBinder.Reports.cs`); ARITHMETIC IS STANDARD *behavior* (the `ArithmeticMode` enum is *captured* in `OptionsModel.cs` but **not consumed** by the numeric engine); ALPHABET national/UCS-4/UTF-8/UTF-16 phrases (no hits in `DataBinder.Switches.cs`); the EC `-N` twins + `EXCEPTION-FILE-N` (`IntrinsicCatalog.cs:133` = `IntrinsicBind.Deferred`, staged loud, blocked on national); the UDF residue (non-numeric/group RETURNING staged `COBOLNET1510` at `StatementBinder.Udf.cs:82`; BY VALUE header formals unmodeled; the RECURSIVE per-activation-vs-static data model deviation); the TYPEDEF residue (EXTERNAL type declaration, strong-group heterogeneous relations, SAME AS via `CloneItem`).

6. **Per-edition obligation.** Every item above owes TWO things (owner directive, `ISO2023_CONFORMANCE_PLAN.md` §0): the complete per-edition ISO behavior AND the rejecting diagnostic under every `--std` edition that lacks it. A 2002 construct compiled `--std 85` must flag. Coverage = a positive golden + a version-matrix row + a negative `.err`.

---

## 3. Target end-state for this phase

When Phase 10 is DONE, the following are true and demonstrable:

**Data model / runtime confirmations (no net-new feature, but proven on the rearchitected tree):**
- `StorageForm.CharImage` covers national (`USAGE NATIONAL`/`PIC N`) and boolean (`USAGE BIT`/`PIC 1`) leaves; a runtime assertion + golden prove national is one UTF-16 char per position, `ImageWidth == Length`. National movement/compare/figurative/VALUE/level-88 all route through `Values/Text/CobolString` (national) and `Values/Text/CobolBool` (boolean) with the `pad`-char discipline.
- Boolean OPERATORS `B-AND`/`B-OR`/`B-XOR`/`B-NOT` bind through `StatementBinder.Boolean.cs` (`BoundBoolBinary`) and render through `Values/Text/CobolBool` (already present as-built; confirmed on the new folders + a golden).
- Pointers (`USAGE POINTER`/`PROGRAM-POINTER`, `NULL`, `SET`, `ADDRESS OF`, `SET ADDRESS OF`, `UP/DOWN BY`, `BASED` deref, `ALLOCATE`/`FREE`) ride `Control/ManagedPointer` + `Control/StorageCell` (`CellPointer` window) under `RunUnit`; `USAGE PROGRAM-POINTER` distinguished from data `POINTER` (equality + `SET pp TO ENTRY`/procedure-pointer semantics per §13.18, staged-loud only where §13.18.5 GR3/GR4 deref would be undefined under .NET).
- 2002 file surface (SHARING / LOCK MODE / RETRY / UNLOCK / line-sequential / FILE STATUS 5x/6x) is confirmed on `IO/FileConnector` + `FileRegistry`; the sharing registry is on `RunUnit` (`IO/Sharing/PhysicalFileTable`).
- UDF residue closed: category-carrying non-numeric/group RETURNING (lifts `COBOLNET1510`), BY VALUE header formals modeled, and the per-activation-vs-static data model conformed for RECURSIVE (§14.6.2.3).

**Net-new features (files/classes that exist when done):**
- `src/Cobol.Net.Compiler/Binding/Model/` (or the P5 model folder): `ConstantEntry` support on `DataItem` (a computed init-only `IsConstant`/`ConstantValue`), a `ConstantEntryPass` (or fold into `BindEntry`); CONSTANT RECORD level-01 handling.
- `&`-concatenation: a `BoundConcat` bound node (or reuse `BoundBoolBinary`'s pattern), a grammar concat tier in `Core/CobolExpressions.g4`, and an emitter arm; boolean-`&` and (2002) non-numeric-literal `&` both covered.
- Report Writer 2002: `PRESENT WHEN` (RD entry, format 1) + `VARYING` (format 1) parsed in `Core/CobolReportWriter.g4`, bound in `DataBinder.Reports.cs` / `ReportSectionBinder`, rendered in `CSharpEmitter.ReportWriter.cs` / the RW runtime.
- ARITHMETIC IS STANDARD: `OptionsModel.ArithmeticMode` consumed by the numeric renderer / `Runtime/Values/Numeric` intermediate-precision path.
- ALPHABET national/UCS-4/UTF-8/UTF-16 phrases: bound in `DataBinder.Switches.cs` / `SpecialNamesBinder`, feeding the national codec.
- EC `-N` twins + `EXCEPTION-FILE-N`: `IntrinsicCatalog.cs` rows flipped from `Deferred` to `Runtime`, backed by `Runtime/Intrinsics` national-string returns; `EcFunctions` national variants.
- TYPEDEF residue: EXTERNAL type declaration + `SAME AS` (via the single `CloneItem`/`CloneSubtree`) + strong-group heterogeneous `=`/`<>` relations (§8.8.4.2 SR4), on the P5 `TypedefExpander`.

**Bookkeeping:**
- Each feature: a positive `<name>.cob`+`.out` moved `pending`→`enabled` in `tests/conformance/2002/manifest.json` (2014 where the construct is 2014-introduced), a `constructs.json` matrix row (compile-time gates only), and a `tests/conformance/negative/<name>.cob`+`.err`.
- `docs/PHASE4_RECONCILIATION.md` rows and `docs/ISO2023_CONFORMANCE_PLAN.md` §3 marks flipped to greenfield truth.

---

## 4. STEP-BY-STEP

> Ordering rationale: Step 1 (audit) reshapes every wave. Then waves are ordered lowest-risk-first and by dependency: confirmations of already-landed features (that only need re-proof on the new substrate) come before net-new implementation; national (Step 2) unblocks the `-N` intrinsic and EC legs (Steps 11/16); CONSTANT and `&`-concat are self-contained. Each wave ends at a COMMIT BOUNDARY with the battery green.

**Progress checkboxes (executing session keeps current):**

- [ ] Step 1 — Reconciliation audit + `GreenfieldStatus` column
- [ ] Step 2 — National on `StorageForm.CharImage` (confirm + prove one-UTF-16-char/position)
- [ ] Step 3 — Boolean data + operators on the new folders (confirm)
- [ ] Step 4 — ALPHABET national / UCS-4 / UTF-8 / UTF-16 phrases (net-new)
- [ ] Step 5 — National wave: matrix rows + negatives + catalog flip (COMMIT)
- [ ] Step 6 — Pointers on `ManagedPointer`/`StorageCell` under `RunUnit` (confirm)
- [ ] Step 7 — `USAGE PROGRAM-POINTER` leg (net-new residue) (COMMIT)
- [ ] Step 8 — File-2002 (SHARING/LOCK/RETRY/UNLOCK/line-seq/5x-6x) on `FileConnector` (confirm) (COMMIT)
- [ ] Step 9 — UDF residue: category-carrying RETURNING (lift 1510) (net-new)
- [ ] Step 10 — UDF residue: BY VALUE formals + RECURSIVE per-activation data model (COMMIT)
- [ ] Step 11 — EC `-N` twins + `EXCEPTION-FILE-N` (net-new, needs Step 2) (COMMIT)
- [ ] Step 12 — ARITHMETIC IS STANDARD behavior @2002/2014 (net-new) (COMMIT)
- [ ] Step 13 — Report Writer 2002: PRESENT WHEN + VARYING format 1 (net-new) (COMMIT)
- [ ] Step 14 — `&`-concatenation operator §8.8.3 (net-new) (COMMIT)
- [ ] Step 15 — CONSTANT entries §13.10 + CONSTANT RECORD §13.18.15 (net-new) (COMMIT)
- [ ] Step 16 — TYPEDEF residue: EXTERNAL type / SAME AS / strong-group relations (net-new) (COMMIT)
- [ ] Step 17 — Phase-end verification + catalog flip + STATUS=DONE (COMMIT)

---

### Step 1 — Reconciliation audit + `GreenfieldStatus` column

**Files to edit:** `docs/PHASE4_RECONCILIATION.md` (add/refresh a `GreenfieldStatus@P10` column and a per-track wave-sizing note), `docs/rearchitecture/PHASE-10-m2-residual-catalog.md` (this file — record the audit result in a table under this step).

**Change:** For EACH M2 non-OO track (UDF, DATA-3 national, DATA-4 boolean+ops, DATA-5 pointers, PROC-5 allocate, FILE-1 sharing/lock, FILE-2 line-seq, PROC-4 EC `-N` legs, ARITH-2 standard, RW-2002, concat, CONSTANT, TYPEDEF-residue), probe the **current** tree and classify each as one of: `LANDED-CONFIRMED-ON-NEW-SUBSTRATE`, `LANDED-NEEDS-RECONFIRM` (landed pre-rearch, must re-prove on `StorageForm`/`FileConnector`/`ManagedPointer`), `PARTIAL` (some legs land), `NOT-STARTED`, or `STAGED-LOUD` (recognized → named diagnostic). Cite file:line evidence for every verdict — do NOT trust the doc marks.

Probe commands (run these; record results):
- National/boolean substrate: `grep -rn "StorageForm\|CharImage\|National\|Boolean" src/Cobol.Net.Compiler/Binding/Model/ src/Cobol.Net.Runtime/Values/Text/` (paths per P5/P8 reorg; fall back to `Binding/`, `Runtime/Text/` if P8 folders not yet flipped).
- Pointers: `grep -rn "ManagedPointer\|StorageCell\|CellPointer\|ProgramPointer" src/Cobol.Net.Runtime/Control/`.
- Files: `grep -rn "FileConnector\|FileRegistry\|Sharing\|LockMode\|Retry\|LineSequential" src/Cobol.Net.Runtime/IO/ src/Cobol.Net.Compiler/Binding/Bound/StatementBinder.FileLock.cs`.
- Genuine gaps: `grep -rn "CONSTANT" src/Cobol.Net.Compiler/Binding/DataBinder.cs` (expect empty), `grep -rn "PRESENT\|VARYING" src/Cobol.Net.Compiler/Binding/DataBinder.Reports.cs` (expect empty), `grep -rn "concat\|BoundConcat" src/Cobol.Net.Compiler/` (expect empty), `grep -rn "ArithmeticMode\." src/Cobol.Net.Compiler/CodeGen/` (expect empty — captured not consumed).

**Why:** Sizes every wave against truth (exit criterion "the M2 catalog marks flipped to greenfield truth" begins here). Prevents re-implementing landed features or skipping regressed ones.

**Verify:** No code change → battery unaffected. Sanity: `dotnet test tests/Cobol.Net.Tests.Conformance/Cobol.Net.Tests.Conformance.csproj -c Debug` still green (baseline capture — record the exact test counts here for later delta checks).

**COMMIT BOUNDARY.** Suggested message:
```
docs(cobolnet): Phase 10 step 1 — M2 residual reconciliation audit + GreenfieldStatus@P10 column

Fresh greenfield-truth audit of the M2 non-OO catalog against the post-rearchitecture
tree (StorageForm / FileConnector / ManagedPointer). Wave-sizing recorded in
PHASE-10 §4; PHASE4_RECONCILIATION rows annotated. No code change.
```

---

### Step 2 — National data on `StorageForm.CharImage`; prove one-UTF-16-char/position

**Precondition:** P5 `StorageForm` exists (`src/Cobol.Net.Compiler/Binding/Model/StorageForm.cs`) with a `CharImage` case and a `StorageFormPass`.

**Files:** `src/Cobol.Net.Compiler/Binding/Passes/StorageFormPass.cs` (confirm national → `CharImage`), `src/Cobol.Net.Compiler/Binding/Model/RecordLayout.cs` (confirm `ImageWidth == Length` for national, NOT doubled), `src/Cobol.Net.Runtime/Values/Text/CobolString.cs` (national move/compare/pad), `src/Cobol.Net.Compiler/CodeGen/DataDivision/*` (national VALUE/figurative init), and the golden `tests/conformance/2002/national_data.cob`/`.out` (already ENABLED — confirm still green + extend).

**Change:** (a) Confirm the `StorageFormPass` classifies `USAGE NATIONAL`/`PIC N` leaves as `CharImage` with `ImageWidth == Length` (one UTF-16 char per position, D-N1) — NOT the legacy 2-bytes/char doubling. If P5 left a `National` distinction, ensure it is a *category* fact on `PicInfo`, not a second `StorageForm` case (per DESIGN-data-model.md OPEN item: "national stays CHARACTER-width; a future 2-byte layout would be a NEW StorageForm case"). (b) Add a runtime/unit assertion that a national item's backing `string` length equals its declared `Length`. (c) Confirm the byte-surface guards (national under REDEFINES / EXTERNAL cells / FD records / SORT keys → the sanctioned-narrowing reject, D-N2) survived the pass rewrite.

**Why:** The exit criterion names "national CharImage confirmed one-UTF-16-char-per-position." This is the single most rearchitecture-sensitive M2 fact.

**Verify:**
- `dotnet E:/CobolSharp/src/Cobol.Net.Cli/bin/Debug/net10.0/cobol.dll tests/conformance/2002/national_data.cob --std 2002 -o E:/Temp/nat.dll --run` → byte-matches `national_data.out`.
- New unit test in `tests/Cobol.Net.Tests.Unit` asserting `ImageWidth == Length` for a `PIC N(5)` item (expect 5, not 10).
- `--std 85` on a `PIC N` program → `COBOLNET0814`-band national-literal / `0819` usage rejection (national is a 2002 introduction).

_(No separate commit — folds into Step 5.)_

---

### Step 3 — Boolean data + B-AND/B-OR/B-XOR/B-NOT operators on the reorganized runtime

**Files:** `src/Cobol.Net.Compiler/Binding/Bound/StatementBinder.Boolean.cs` (as-built: `BindBoolXor`/`BindBoolAnd`/`BindBoolFactor`/`MakeBoolBinary`→`BoundBoolBinary`), `src/Cobol.Net.Compiler/CodeGen/Emit/BooleanRenderer.cs`, `src/Cobol.Net.Runtime/Values/Text/CobolBool.cs` (confirm on the P8 `Values/Text/` home, not `Text/`), goldens `tests/conformance/2002/boolean_data.cob` + `boolean_ops.cob` (both ENABLED).

**Change:** Confirm the boolean core (`PIC 1`/`USAGE BIT`, `B"…"`, MOVE/VALUE/INITIALIZE/DISPLAY/compare, JUSTIFIED) and the four operators bind + render on the reorganized runtime folders. Confirm boolean item↔item compare zero-extends via `CobolString.Compare(l, r, pad: '0')` OR `CobolBool.Equal` (both correct per §8.8.4.2.8; NOT space-padding). Confirm boolean-in-COMPUTE stays in the `BoundBoolExpr` channel (never the numeric channel) and boolean-in-arithmetic (`ADD B1 TO X`) is rejected. Confirm equality-only relations (ordering operator on boolean → the `0844`/`15xx` band diagnostic).

**Why:** Boolean core + operators are as-built (`StatementBinder.Boolean.cs` present) but predate the runtime folder reorg; re-prove and re-home.

**Verify:**
- `dotnet …/cobol.dll tests/conformance/2002/boolean_ops.cob --std 2002 -o E:/Temp/b.dll --run` → matches `boolean_ops.out`.
- Negative: `tests/conformance/negative/boolean-ordering-relation.cob` still emits its `.err` code at 2002.
- `--std 85` on a `PIC 1` program → boolean usage/literal rejection.

_(Folds into Step 5.)_

---

### Step 4 — ALPHABET national / UCS-4 / UTF-8 / UTF-16 phrases (net-new)

**Files:** `src/Cobol.Net.Frontend/Grammar/Core/CobolSpecialNames.g4` (ALPHABET phrase alternatives), `src/Cobol.Net.Compiler/Binding/DataBinder.Switches.cs` (or the P6 `SpecialNamesBinder`), `src/Cobol.Net.Compiler/Binding/CollatingModel.cs`, `src/Cobol.Net.Runtime/Values/Text/` (national codec hookup).

**Change:** Parse + bind the 2002 ALPHABET phrases `NATIONAL`, `UCS-4`, `UTF-8`, `UTF-16` (§13.16.6 / SPECIAL-NAMES ALPHABET clause). Feed the selected encoding into the national CODE-SET / codec boundary (in-memory stays UTF-16; the alphabet governs the external-encoding boundary concern). Where a phrase implies an external encoding the .NET runtime cannot losslessly round-trip for a given operation, stage LOUD with a named diagnostic rather than silently mis-encode. Gate `{is2002()}?`.

**Why:** Named in scope; unblocks correct national collation + the `-N` intrinsic legs. Currently zero surface (`grep` in Switches empty).

**Verify:**
- New golden `tests/conformance/2002/alphabet_national.cob`+`.out` (declare `ALPHABET A IS UTF-8`, use in national context) → run matches.
- `--std 85` → ALPHABET-national phrase rejected (`0900` introduction band).
- Add `constructs.json` row `alphabet-national-2002` (IntroducedIn 2002).

_(Folds into Step 5 commit.)_

---

### Step 5 — National/boolean wave commit

**Files:** `tests/conformance/2002/manifest.json` (confirm `national_data`, `boolean_data`, `boolean_ops` enabled; add `alphabet_national`), `tests/version-matrix/constructs.json` (national/boolean/alphabet rows present), `tests/conformance/negative/` (national-narrowing reject, boolean-VALUE-mismatch, boolean-ordering, alphabet-at-85).

**Verify (full):** `dotnet test tests/Cobol.Net.Tests.Conformance/…` + `…Tests.Unit/…` green; `bash scripts/guard-fast.sh` NIST 353 MATCH.

**COMMIT BOUNDARY.** Suggested message:
```
feat(cobolnet): Phase 10 wave A — national/boolean on StorageForm.CharImage + ALPHABET national encodings

National (USAGE NATIONAL/PIC N) confirmed one-UTF-16-char/position on StorageForm.CharImage
(ImageWidth==Length, D-N1); boolean data + B-AND/B-OR/B-XOR/B-NOT re-homed to Values/Text/CobolBool;
ALPHABET NATIONAL/UCS-4/UTF-8/UTF-16 phrases bound. Goldens + matrix rows + negatives. Battery green.
```

---

### Step 6 — Pointers on `ManagedPointer`/`StorageCell` under `RunUnit`

**Files:** `src/Cobol.Net.Runtime/Control/ManagedPointer.cs`, `Control/StorageCell.cs`, `Control/ExternalStore.cs` (P8 split of `ProgramRegistry.cs`), `src/Cobol.Net.Compiler/Binding/DataBinder.Ptr.cs`, `Binding/Bound/StatementBinder.Ptr.cs`, `CodeGen/CSharpEmitter.Ptr.cs`, goldens `based_pointer` / `pointer_alloc` / `pointer_arith` (all ENABLED).

**Change:** Confirm `USAGE POINTER`, `NULL`, `SET p TO NULL|q`, `[NOT] EQUAL`, `ADDRESS OF`, `SET ADDRESS OF`, `SET … UP/DOWN BY`, `BASED` deref, and `ALLOCATE`/`FREE` all resolve to the ONE `ManagedPointer` carrier now living under `Control/` and owned by `RunUnit` (not a process-global). Confirm the `StorageCell`+`CellPointer` window model (structural §8.8.4.2 equality; deref bridge §13.18.5 GR3/GR4 loud) survived the `ExternalStore`→`RunUnit` instance move. Confirm `FREE`'s three-way GR1 (nonfatal `EC-STORAGE-NOT-ALLOC` through the TurnState-gated block; dangling alias loud at deref).

**Why:** Pointers landed (DEVLOG 613/617) on the pre-P8 static `ProgramRegistry`; P8 made `ManagedPointer`/`ExternalStore` instance-on-`RunUnit`. Re-prove no regression.

**Verify:** run all three pointer goldens (`--std 2002 --run`) byte-exact; `tests/conformance/negative/allocate-non-based.cob`, `based-redefines-conflict.cob` still emit their `.err` codes. Concurrent-run-unit sanity (if P8 enabled it): two `RunUnit`s each with their own allocated storage do not collide.

_(No commit yet — pairs with Step 7.)_

---

### Step 7 — `USAGE PROGRAM-POINTER` leg (residue)

**Files:** `src/Cobol.Net.Frontend/Grammar/Core/CobolData.g4` (usage keyword — `PROGRAM-POINTER` token likely already lexed/reserved), `src/Cobol.Net.Compiler/Binding/PicInfo.cs` / the P5 `PictureAnalyzer` (usage marker), `Binding/Bound/StatementBinder.Ptr.cs` (`SET pp TO ENTRY|procedure-name`, `SET pp TO NULL`, equality), `src/Cobol.Net.Runtime/Control/ManagedPointer.cs` (program-pointer variant — carries a resolvable program/entry identity, distinct from a data address).

**Change:** Distinguish `USAGE PROGRAM-POINTER` (§13.18.60) from a data `POINTER`: it holds a program/entry reference (`SET pp TO ENTRY "NAME"` / `SET pp TO name`), supports `= NULL`/`= pp2` equality, and is a valid CALL target (`CALL pp`). Deref-as-data is undefined → loud. Gate `{is2002()}?`. Note: full `FUNCTION-POINTER` is an M3-4 leg (out of P10 scope) — implement only the 2002 `PROGRAM-POINTER`.

**Why:** Named in scope ("incl USAGE PROGRAM-POINTER"); the reconciliation lists it as staged-loud (M3-4 leg) — the 2002 portion is P10.

**Verify:**
- New golden `tests/conformance/2002/program_pointer.cob`+`.out` (`SET pp TO ENTRY "SUB"`, `CALL pp`, verify the sub runs) → matches.
- `--std 85` → `PROGRAM-POINTER` usage rejected.
- Negative: `tests/conformance/negative/program-pointer-deref.cob` → loud diagnostic.
- `constructs.json` row `usage-program-pointer-2002`.

**COMMIT BOUNDARY.** Suggested message:
```
feat(cobolnet): Phase 10 wave B — pointers confirmed on Control/ManagedPointer under RunUnit + USAGE PROGRAM-POINTER

Data-pointer surface (POINTER/NULL/SET/ADDRESS OF/BASED/ALLOCATE/FREE) re-proven on the P8
RunUnit-owned ManagedPointer/StorageCell; USAGE PROGRAM-POINTER (SET TO ENTRY, CALL pp, equality)
landed as the 2002 residue. Golden + matrix row + negative. Battery green.
```

---

### Step 8 — File-2002 on `FileConnector`

**Files:** `src/Cobol.Net.Runtime/IO/FileConnector.cs`, `IO/FileRegistry.cs`, `IO/SequentialConnector.cs`/`RelativeConnector.cs`/`IndexedConnector.cs`, `IO/Sharing/PhysicalFileTable.cs`, `IO/FileStatus.cs` (P8 reorg), `src/Cobol.Net.Compiler/Binding/Bound/StatementBinder.FileLock.cs`, `StatementBinder.KeyedIo.cs`, `Binding/DataBinder.cs` (`MapOrganization`), `CodeGen/CSharpEmitter.KeyedIo.cs`, golden `tests/conformance/2002/file_sharing.cob` (ENABLED).

**Change:** Confirm SHARING clause / OPEN SHARING phrase, LOCK MODE (AUTOMATIC/MANUAL/EXCLUSIVE), RETRY (§14.7.9), UNLOCK, and line-sequential organization + 2002 FILE STATUS 5x/6x all route through the polymorphic `FileConnector`/`FileRegistry` (the `Keyed*` static fallthrough was deleted in P8). Confirm the sharing/lock registry is now `RunUnit`-owned (`IO/Sharing/PhysicalFileTable`). Confirm the EC bridge (`ExceptionCatalog` `EC-I-O-FILE-SHARING` / `EC-I-O-RECORD-OPERATION`, `IoEcOfStatus` 5x/6x arms, `__IoCheckEc` continues-not-throws for 5x/6x) is intact. Close the named residue: narrow FILE STATUS 04/39.

**Why:** File-2002 landed Phase 4d on the pre-P8 dispatch; P8 collapsed the three organizations behind `FileConnector` and moved the sharing registry to `RunUnit`. Re-prove + finish 04/39.

**Verify:** `file_sharing.cob` golden byte-exact; `tests/conformance/negative/close-with-lock.cob` `.err` at the right edition; new negative for a 51/61 shared-lock conflict producing the continuable EC. Add a golden exercising a 04 (record-length) or 39 (fixed-attribute conflict) status. `bash scripts/guard.sh` (file I/O touches the NIST SQ/RL/IX corpus — run the FULL guard).

**COMMIT BOUNDARY.** Suggested message:
```
feat(cobolnet): Phase 10 wave C — file-2002 confirmed on IO/FileConnector; narrow FILE STATUS 04/39

SHARING/LOCK MODE/RETRY/UNLOCK/line-sequential + 5x/6x statuses re-proven on the polymorphic
FileConnector/FileRegistry with the RunUnit-owned sharing registry; 04/39 narrow statuses added.
Full legacy guard NIST 353 MATCH. Battery green.
```

---

### Step 9 — UDF residue: category-carrying non-numeric/group RETURNING (lift `COBOLNET1510`)

**Files:** `src/Cobol.Net.Compiler/Binding/Bound/StatementBinder.Udf.cs` (the `1510` staging at `:82`), `Binding/DataBinder.Linkage.cs` (`UserFunctionSignature`), `Binding/Bound/BoundTree.cs` (a category-carrying result operand), `CodeGen/CSharpEmitter.Call.cs` (RETURNING carrier emit), `src/Cobol.Net.Runtime/Control/CallAbi.cs` (`CobolArgAdapt.StoreReturn` for text/group).

**Change:** Today only elementary fixed-point numeric RETURNING is implemented; alphanumeric/national/boolean and group RETURNING stage LOUD as `COBOLNET1510` because the result reads through `BoundNumRef` (numeric classifiers + numeric relation rendering). Lift it: make the UDF result temp carry the RETURNING item's *category* (route through the same operand renderers a normal reference uses — `OperandRenderer`/`CobolString` for text, group-image codec for group), so an alphanumeric result compares as text and a group RETURNING clones a fully-described temp (not a Pic-less undeclarable one). Reuse the P5 single `CloneItem`/`CreateCompilerTemp` for the group result temp.

**Why:** Named in scope ("category-carrying non-numeric/group RETURNING — lift 1510"). This is the last correctness gap in the otherwise-complete UDF track.

**Verify:**
- New goldens `tests/conformance/2002/udf_alpha_returning.cob` (a `FUNCTION-ID` returning `PIC X(n)`) and `udf_group_returning.cob` → run byte-exact.
- Confirm the previously-`1510` programs now compile+run instead of the loud diagnostic.
- Existing `udf_*` goldens still green.

_(No commit — pairs with Step 10.)_

---

### Step 10 — UDF residue: BY VALUE header formals + RECURSIVE per-activation data model

**Files:** `src/Cobol.Net.Compiler/Binding/DataBinder.Linkage.cs` (`LinkageFormal` gains a `mode` = REFERENCE/CONTENT/VALUE), `src/Cobol.Net.Frontend/Grammar/Core/CobolControlFlow.g4` (PD header `BY VALUE` phrase — likely already parses for CALL), `Binding/Bound/StatementBinder.Call.cs` (marshal BY VALUE), `src/Cobol.Net.Runtime/Control/CallAbi.cs` (BY VALUE copy-in), and the per-activation data model in `src/Cobol.Net.Runtime/Control/ProgramTable.cs` / `RunUnit` (static WS shared across activations vs per-activation LOCAL-STORAGE/formals).

**Change:** (a) Model BY VALUE header formals (§14.9.4 GR5c) — a private copy conformed to the formal, no write-back — currently `LinkageFormal` carries no mode (as-built note M2-UDF-1 deviation #4). (b) Conform the RECURSIVE per-activation data model (§14.6.2.3.2/.3): a function's WORKING-STORAGE is STATIC (last-used after the first activation), while LOCAL-STORAGE / formals are per-activation. Today `Initial || Recursive ⇒ fresh instance per activation` re-initializes WS per activation (a deviation predating UDFs). Split: shared static WS + per-activation automatic storage. This fixes RECURSIVE for both `PROGRAM-ID … RECURSIVE` and every UDF (always implicitly recursive, §9.4).

**Why:** Named in scope ("BY VALUE formals + the per-activation-vs-static data model (conforms §14.6.2.3.2/.3, fixes RECURSIVE)").

**Verify:**
- New golden `tests/conformance/2002/recursive_static_ws.cob` — a RECURSIVE program whose WS counter persists across activations (spec: static) while a LOCAL-STORAGE item resets → proves the split.
- New golden `udf_by_value.cob` — BY VALUE arg mutated inside the function is NOT visible to the caller (contrast BY REFERENCE).
- `udf_recursion.cob` (5! = 120) still green.
- `constructs.json` rows `call-by-value-2002`, `local-storage-section` (if not already present).

**COMMIT BOUNDARY.** Suggested message:
```
feat(cobolnet): Phase 10 wave D — UDF residue closed: category RETURNING + BY VALUE + RECURSIVE per-activation data

Non-numeric/group RETURNING lifts COBOLNET1510 (category-carrying result temp via the one CloneItem);
BY VALUE header formals modeled on CallAbi; RECURSIVE static-WS vs per-activation LOCAL-STORAGE split
(conforms §14.6.2.3). Goldens + matrix rows. Battery green.
```

---

### Step 11 — EC `-N` twins + `EXCEPTION-FILE-N` (needs Step 2)

**Files:** `src/Cobol.Net.Compiler/Binding/IntrinsicCatalog.cs` (`EXCEPTION-FILE-N` `:133` `Deferred`→`Runtime`; any other `-N` twins), `src/Cobol.Net.Runtime/Intrinsics/CobolIntrinsics.Text.cs` (national-string returns), `src/Cobol.Net.Runtime/Exceptions/EcFunctions.cs` (national variants), `CodeGen/Emit/IntrinsicRenderer.cs` (`:44` LoudValue → national channel).

**Change:** Flip the national exception intrinsics (`EXCEPTION-FILE-N`, and any `EXCEPTION-*-N` twins) from `IntrinsicBind.Deferred`/loud to `Runtime`, returning a national (`CharImage`) string built from the exception state. These were blocked on national data (Step 2). Confirm `EXCEPTION-FILE` (non-national) still returns the alphanumeric form.

**Why:** Named in scope ("the EC -N twins + EXCEPTION-FILE-N"); `IntrinsicCatalog.cs:133` confirms `Deferred` today, blocked on national.

**Verify:**
- New golden `tests/conformance/2002/exception_file_n.cob` — force a file EC, `DISPLAY FUNCTION EXCEPTION-FILE-N` → national output matches.
- The M4-2b catalog note ("EXCEPTION-FILE-N also blocked on national (a)") is now unblocked — update it.

**COMMIT BOUNDARY.** Suggested message:
```
feat(cobolnet): Phase 10 wave E — EC national intrinsics (EXCEPTION-FILE-N + -N twins) on the national channel

Flipped the national exception intrinsics from Deferred/loud to Runtime now that national CharImage
is confirmed; national-string returns via Runtime/Intrinsics. Golden + matrix. Battery green.
```

---

### Step 12 — ARITHMETIC IS STANDARD behavior @2002/2014

**Files:** `src/Cobol.Net.Compiler/Binding/OptionsModel.cs` (`ArithmeticMode` enum — already present, `Native`/`Standard`/`StandardBinary`/`StandardDecimal`), `src/Cobol.Net.Compiler/Binding/OptionsBinder` (OPTIONS paragraph ARITHMETIC clause capture — confirm), `src/Cobol.Net.Compiler/CodeGen/Emit/NumericRenderer.cs` (consume the mode), `src/Cobol.Net.Runtime/Values/Numeric/` (intermediate-precision path per the selected mode).

**Change:** The `ArithmeticMode` is *captured* but *not consumed* (OptionsModel.cs:10 "captured for the features that will consume them"). Wire it into the numeric engine: `STANDARD`/`STANDARD-DECIMAL` select the standard intermediate-data-item precision rules (§8.8.1.2 / §14.4.2 standard arithmetic — a defined intermediate result item), vs `NATIVE` (implementor-defined, the current behavior). Thread `OptionsModel.Arithmetic` → `NumericRenderer` → the `CobolDec` intermediate-scale computation.

**Why:** Named in scope ("ARITHMETIC IS STANDARD behavior @2002/2014"). 2002 introduction, unchanged 2014, one edition delta noted in VCR for 2023.

**Verify:**
- New golden `tests/conformance/2002/arithmetic_standard.cob` — a computation whose result differs between NATIVE and STANDARD intermediate precision; `--std 2002` with `OPTIONS. ARITHMETIC IS STANDARD.` → the standard result.
- `constructs.json` row `arithmetic-standard-2002`.
- Full legacy guard (numeric-pipeline change) — `bash scripts/guard.sh` NIST 353 MATCH (NATIVE default must be byte-invariant).

**COMMIT BOUNDARY.** Suggested message:
```
feat(cobolnet): Phase 10 wave F — ARITHMETIC IS STANDARD consumed by the numeric engine (@2002/2014)

OptionsModel.ArithmeticMode now drives NumericRenderer's intermediate-precision path (standard
intermediate data item per §14.4.2); NATIVE default byte-invariant (full guard MATCH). Golden + matrix.
```

---

### Step 13 — Report Writer 2002: PRESENT WHEN format 1 + VARYING format 1

**Files:** `src/Cobol.Net.Frontend/Grammar/Core/CobolReportWriter.g4` (RD entry `PRESENT WHEN` + `VARYING` phrases), `src/Cobol.Net.Compiler/Binding/DataBinder.Reports.cs` (or the P6 `ReportSectionBinder`), `Binding/ReportModel*`, `Binding/Bound/StatementBinder.ReportWriter.cs`, `CodeGen/CSharpEmitter.ReportWriter.cs`, `src/Cobol.Net.Runtime/IO/ReportWriter.cs`.

**Change:** Add the 2002 Report Writer additions: `PRESENT WHEN condition` (§13.18.44-ish RD report-group clause, format 1 — a group/field is presented only when the condition holds) and `VARYING identifier FROM … BY …` (format 1 — a report loop variable). Zero surface today (`grep PRESENT|VARYING DataBinder.Reports.cs` empty). Gate `{is2002()}?`.

**Why:** Named in scope ("Report Writer 2002 additions (PRESENT WHEN format 1 + VARYING format 1)").

**Verify:**
- New golden `tests/conformance/2002/rw_present_when.cob`+`.out` and `rw_varying.cob`+`.out` → run byte-exact.
- `--std 85` → PRESENT WHEN / VARYING rejected (`0900`); NIST RW corpus (85) still green (`bash scripts/guard.sh` — RW touches the NIST RW baselines).
- `constructs.json` rows `rw-present-when-2002`, `rw-varying-2002`.

**COMMIT BOUNDARY.** Suggested message:
```
feat(cobolnet): Phase 10 wave G — Report Writer 2002: PRESENT WHEN + VARYING (format 1)

RD PRESENT WHEN condition + VARYING loop bound/rendered through the RW pipeline, gated 2002.
Goldens + matrix + negatives; NIST-85 RW baselines byte-invariant (full guard). Battery green.
```

---

### Step 14 — `&`-concatenation operator §8.8.3

**Files:** `src/Cobol.Net.Frontend/Grammar/Core/CobolExpressions.g4` (a concatenation tier — `&` between literals/operands), `src/Cobol.Net.Frontend/Grammar/Core/CobolLexer.g4` (the `&` token — confirm; note `&` may need care vs continuation), `src/Cobol.Net.Compiler/Binding/Bound/BoundTree.cs` (`BoundConcat` node), the binder tier (parallel to `StatementBinder.Boolean.cs`'s `MakeBoolBinary`), `CodeGen/Emit/OperandRenderer.cs` (render concatenation via `CobolString`/`CobolBool`).

**Change:** Implement the §8.8.3 concatenation operator `&`: a *concatenation expression* joins two literals/figurative constants (2002 defines it primarily for VALUE-clause and boolean literals; boolean `&`-concatenation §8.8.2/§8.8.3 concatenates bit strings). Bind to `BoundConcat`, render as a compile-time-folded literal where both operands are literals (the common case — VALUE `"AB" & "CD"`), and as a runtime concat for boolean bit-string concatenation. Gate `{is2002()}?`. Heed the DEVLOG-621 lesson: do NOT restructure a shared core expression rule in a DFA-hazardous way — add the concat tier as a distinct additive alternative with the full legacy guard.

**Why:** Named in scope ("the &-concatenation operator §8.8.3"); reconciliation lists `concat-operator-2002` PENDING (Phase 4g). Zero surface today.

**Verify:**
- New golden `tests/conformance/2002/concat_literal.cob` (`01 X PIC X(4) VALUE "AB" & "CD".` → `ABCD`) and `concat_boolean.cob` (`B"01" & B"10"` → `0110`) → run byte-exact.
- `--std 85` → `&` concat rejected (`0900`).
- `constructs.json` row `concat-operator-2002`.
- **Grammar change → FULL legacy guard** `bash scripts/guard.sh` NIST 353 MATCH.

**COMMIT BOUNDARY.** Suggested message:
```
feat(cobolnet): Phase 10 wave H — &-concatenation operator §8.8.3 (literals + boolean bit strings)

BoundConcat: compile-time-folded literal concat (VALUE "AB"&"CD") + runtime boolean bit-string concat,
gated 2002, additive expression tier (no shared-rule restructure). Golden + matrix + negative.
Full legacy guard NIST 353 MATCH.
```

---

### Step 15 — CONSTANT entries §13.10 + CONSTANT RECORD §13.18.15

**Files:** `src/Cobol.Net.Frontend/Grammar/Core/CobolData.g4` (`level-number CONSTANT AS constant-expression` / `01 name CONSTANT RECORD`), `src/Cobol.Net.Compiler/Binding/DataBinder.cs` (`BindEntry` — recognize CONSTANT), `Binding/Model/DataItem.cs` (init-only `IsConstant` + `ConstantValue`), a `ConstantEntryPass` or fold into `BindEntry`, `CodeGen/DataDivision/*` (emit as a `const`/`static readonly` C# value, NOT a storage field), reference resolution (a CONSTANT reference is a value, never a receiving operand).

**Change:** Implement CONSTANT data entries (§13.10 — `01 PI CONSTANT AS 3.14159.` — a named compile-time constant; no storage) and CONSTANT RECORD (§13.18.15 — a level-01 whose subordinate items are all constants / a fixed record template). A CONSTANT reference resolves to its value at every read site and is rejected as a receiving operand (§13.10 SR). Zero surface today (`grep CONSTANT DataBinder.cs` empty). Gate `{is2002()}?`. Note: `CONSTANT` may collide as a user word at 85 → add to `_dataNameTokens` / `cobolWord` (funnel-`0901` at 2002+) per the PROTOTYPE/SHARING precedent.

**Why:** Named in scope ("CONSTANT entries §13.10 + CONSTANT RECORD §13.18.15").

**Verify:**
- New golden `tests/conformance/2002/constant_entry.cob` (`01 MAX-ROWS CONSTANT AS 100.` used as an OCCURS/PERFORM bound + a computation) → run byte-exact.
- Negative `tests/conformance/negative/constant-as-receiver.cob` → "CONSTANT may not be a receiving operand" diagnostic; `constant-at-85.cob` → `0900`/`0901`.
- `constructs.json` rows `constant-entry-2002`, `constant-record-2002`; reserved-word interval row for `CONSTANT`.
- **Grammar change → FULL legacy guard.**

**COMMIT BOUNDARY.** Suggested message:
```
feat(cobolnet): Phase 10 wave I — CONSTANT entries §13.10 + CONSTANT RECORD §13.18.15

Named compile-time constants (no storage; init-only DataItem.IsConstant/ConstantValue; emitted as C#
const), rejected as receiving operands; CONSTANT word funnel-0901'd ≥2002. Goldens + matrix + negatives.
Full legacy guard NIST 353 MATCH.
```

---

### Step 16 — TYPEDEF residue: EXTERNAL type declaration / SAME AS / strong-group heterogeneous relations

**Files:** `src/Cobol.Net.Compiler/Binding/DataBinder.cs` (`ExpandTypes`/`CloneItem` — the P5 `TypedefExpander`), `Binding/Model/StrongTypeModel.cs` (P5 split of `DataItem.SameStrongType`/`StrongRoot`), `src/Cobol.Net.Frontend/Grammar/Core/CobolData.g4` (`SAME AS` clause; EXTERNAL on a TYPEDEF), `Binding/Bound/StatementBinder.cs` (strong-group `=`/`<>` relation rule §8.8.4.2 SR4).

**Change:** Close the three TYPEDEF residue legs (weak + strong TYPE already landed per DEVLOG 659, `1532` strong gates present at `DataBinder.cs:810`):
1. **EXTERNAL type declaration** — a TYPEDEF marked EXTERNAL is visible across compilation units (a shared type template); register + resolve cross-unit (reuse the OO class-table / UDF-prototype cross-assembly precedent).
2. **SAME AS** (§13.18.44 SAME AS clause) — `03 B SAME AS A.` clones A's subtree via the ONE `CloneItem`/`CloneSubtree` (fresh Uid, cloned OccursSpec not shared — the D17 risk#1 discipline).
3. **Strong-group heterogeneous relations** — two strongly-typed groups of *different* types in `=`/`<>` (§8.8.4.2 SR4): rejected unless same strong root; wire into `CheckedRelational` (the `1532` band, per the boolean-increment residue #10 note "rides the TYPEDEF residue").

**Why:** Named in scope ("the TYPEDEF residue (EXTERNAL type declaration, strong-group heterogeneous relations, SAME AS via CloneItem)").

**Verify:**
- New goldens `tests/conformance/2002/typedef_same_as.cob`, `typedef_external.cob` (2 units sharing an EXTERNAL type) → run byte-exact.
- Negative `tests/conformance/negative/strong-group-heterogeneous-compare.cob` → `COBOLNET1532` (or the chosen band).
- Existing `typedef_weak_elem`/`typedef_weak_group` goldens still green.
- `constructs.json` rows `same-as-clause-2002`, `external-typedef-2002`.

**COMMIT BOUNDARY.** Suggested message:
```
feat(cobolnet): Phase 10 wave J — TYPEDEF residue: EXTERNAL type / SAME AS / strong-group relations

SAME AS clone via the single CloneItem; EXTERNAL type cross-unit resolution (OO/UDF precedent);
strong-group heterogeneous =/<> gated (§8.8.4.2 SR4, 1532 band). Goldens + matrix + negative. Battery green.
```

---

### Step 17 — Phase-end verification + catalog flip + STATUS=DONE

**Files:** `docs/PHASE4_RECONCILIATION.md` (flip every P10 track's `GreenfieldStatus` to `LANDED-CONFIRMED`), `docs/ISO2023_CONFORMANCE_PLAN.md` §3 (tick the M2 items), `DEVLOG.md` (a Phase-10-complete entry, newest-first), this file's STATUS line → `DONE`.

**Change:** Run the FULL battery (see §5). Confirm every exit criterion. Flip the catalog marks to greenfield truth. Record the final test counts.

**Verify:** §5 full battery all green.

**COMMIT BOUNDARY.** Suggested message:
```
docs(cobolnet): Phase 10 COMPLETE — M2 residual catalog closed on the rearchitected substrate

All M2 non-OO residual tracks landed/confirmed on StorageForm/FileConnector/ManagedPointer/RunUnit;
catalog flipped to greenfield truth; national CharImage one-UTF-16-char/position proven. Full battery
green (NNNN conformance + NNN unit + legacy guard NIST 353 MATCH). STATUS=DONE. (DEVLOG NNN)
```

---

## 5. Verification — the full battery at phase end

Run ALL of the following; every one must be green before STATUS=DONE:

1. `dotnet build src/Cobol.Net.Cli/Cobol.Net.Cli.csproj -c Debug` — clean.
2. `dotnet test tests/Cobol.Net.Tests.Conformance/Cobol.Net.Tests.Conformance.csproj -c Debug` — 2028+ tests green (record the exact new count; must be ≥ the Step-1 baseline + the new goldens).
3. `dotnet test tests/Cobol.Net.Tests.Unit/Cobol.Net.Tests.Unit.csproj -c Debug` — 213+ green.
4. `bash scripts/guard.sh` — FULL legacy guard: **NIST 353 MATCH** + the 11 `LEGACY_DIVERGENT` ISO-rebaselined goldens (0 unexpected divergence, 0 regression).
5. **Byte-exact / behavior-neutrality checks** (the confirmation waves must not have regressed):
   - Re-run every P10 golden with `--std 2002 --run` and byte-compare its `.out`.
   - `CorpusRunnerTests` integrity fact: every on-disk `.cob` in `tests/conformance/{2002,2014}` is manifest-listed (nothing silently undiscovered).
   - Per-edition negatives: every `tests/conformance/negative/<p10>.cob` emits its pinned `.err` code at the edition its manifest entry names (85 rejection for every 2002 introduction).
   - National invariant: the unit assertion `ImageWidth == Length` for national items passes (one UTF-16 char/position).
   - NATIVE-arithmetic byte-invariance: guard MATCH proves the ARITHMETIC-STANDARD wiring left `NATIVE` (the default) byte-identical.
6. **Version matrix:** every new construct row in `tests/version-matrix/constructs.json` has its introduction gate exercised (a compile at the lacking edition rejects, at the introducing edition accepts).

---

## 6. Rollback / resumability

- **Resume point:** read the STATUS line + the §4 checkboxes. The first unchecked step is the resume point. Every step is independently committable; a partial wave (a step that folds into a later commit, e.g. Steps 2–4 → Step 5) leaves the tree buildable but with the not-yet-enabled golden still in `pending` — so the battery stays green mid-wave. NEVER move a golden `pending`→`enabled` until its feature runs byte-exact.
- **If a confirmation step (2, 3, 6, 8) finds a REGRESSION from the rearchitecture** (P5–P9 broke a landed M2 feature): that is a P5–P9 bug surfaced late. Fix it here (it is in P10's scope to leave the feature working on the new substrate), and add a DEVLOG note that the rearchitecture phase's characterization net had a hole — feed it back to the P0 characterization corpus so it cannot recur.
- **If a grammar change destabilizes the SLL/LL parse** (Steps 7, 13, 14, 15): the risk is a DFA ambiguity on a shared core rule (the DEVLOG-621 lesson). Mitigation: keep every new alternative ADDITIVE with a unique leading token; run the FULL legacy guard on the grammar-touching commit BEFORE enabling any new golden; if the 85 surface shifts even one byte, revert the grammar edit and re-approach bind-side (the M2-UDF-4 keyword-omitted precedent: resolve semantically at bind, not in the grammar).
- **If `StorageForm` (P5) is not actually complete** when P10 starts: Steps 2, 9, 15, 16 hard-depend on it. Do NOT fake it with the old `StoreAsImage` flag (P5 deleted it, `feedback_no_transitional_hacks`). Block on P5; the pointer/file/UDF-VALUE/RW/concat waves (6, 7, 8, 10, 13, 14) are mostly independent of `StorageForm` and can proceed first — reorder §4 accordingly and note it in STATUS.
- **Numeric-pipeline steps (12)**: the ARITHMETIC-STANDARD wiring must leave `NATIVE` byte-invariant. If the guard diverges, the mode threading leaked into the default path — gate it strictly on `mode != Native`.
- **Idempotency:** re-running a completed step is safe (goldens already enabled, matrix rows already present) — the manifest integrity fact and matrix drift test will simply pass.

---

## 7. ISO feature work in this phase — spec sections, editions, conformance artifacts

All tracks are **COBOL-2002 introductions** (carried unchanged through 2014/2023 unless a VCR delta is noted), so each owes: (a) the complete 2002+ behavior AND (b) a rejecting diagnostic at `--std 85`. `VERSION_CHANGE_REFERENCE.md` has NO 85→2002 rows — derive 85↔2002 gating from the 2002 standard / the §3 catalog.

| Track | Spec § (ISO/IEC 1989:2023, `specs/ISO_COBOL.md`) | Edition | Positive golden(s) → `manifest.json` enabled | Version-matrix row(s) `constructs.json` | Negative `.err` |
|---|---|---|---|---|---|
| National data | §13.16.6 / §13.18 USAGE NATIONAL; Table 16 MOVE; §8.8.4.2.9 compare; §14.9.11.4 | 2002 | `national_data` (enabled — confirm) | `national-usage-2002`, `pic-n-2002` | national-narrowing reject; national-at-85 |
| ALPHABET national encodings | SPECIAL-NAMES ALPHABET (NATIONAL/UCS-4/UTF-8/UTF-16) | 2002 | `alphabet_national` | `alphabet-national-2002` | alphabet-national-at-85 |
| Boolean data + operators | §13.18.40 USAGE BIT/PIC 1; §8.8.2/§8.8.4.2.8 boolean expr/compare; §14.9.8 F2 COMPUTE | 2002 (B-SHIFT = 2023) | `boolean_data`, `boolean_ops` (enabled — confirm) | `usage-bit-2002`, `boolean-operator-2002` | boolean-ordering-relation; bit-usage-numeric-pic |
| Pointers / ALLOCATE / FREE / BASED | §13.18 USAGE POINTER/PROGRAM-POINTER; §8.8.4.2 equality; §13.18.5 GR3/4 deref; §14.9.5 ALLOCATE, §14.9.16 FREE | 2002 | `based_pointer`, `pointer_alloc`, `pointer_arith` (confirm), `program_pointer` (new) | `usage-program-pointer-2002`, `allocate-2002`, `free-2002` (confirm) | allocate-non-based; program-pointer-deref |
| File-2002 | §12.4.5.15 SHARING; §14.7.9 RETRY; §9.1.13.8/.9 status 5x/6x; line-sequential org | 2002 | `file_sharing` (confirm) + a 04/39-status golden | `file-sharing-clause-2002`, `user-word-sharing-2002` | close-with-lock; shared-lock-conflict |
| UDF residue | §8.4.3.2.4 GR1/GR5; §14.9.4 GR5c BY VALUE; §14.6.2.3.2/.3 static/per-activation; §9.4 recursive | 2002 | `udf_alpha_returning`, `udf_group_returning`, `udf_by_value`, `recursive_static_ws` | `call-by-value-2002`, `local-storage-section` | udf-returning-as-receiver |
| EC national intrinsics | §15.29 EXCEPTION-FILE-N (+ `-N` twins); Table 13 EC codes | 2002 | `exception_file_n` | `exception-file-n-2002` | (n/a — runtime) |
| ARITHMETIC IS STANDARD | §11.9.5 ARITHMETIC clause; §14.4.2 standard intermediate arithmetic | 2002 (2014 same; a 2023 VCR delta) | `arithmetic_standard` | `arithmetic-standard-2002` | arithmetic-standard-at-85 |
| Report Writer 2002 | RD PRESENT WHEN (format 1); VARYING (format 1) | 2002 | `rw_present_when`, `rw_varying` | `rw-present-when-2002`, `rw-varying-2002` | rw-present-when-at-85 |
| `&`-concatenation | §8.8.3 concatenation expression | 2002 | `concat_literal`, `concat_boolean` | `concat-operator-2002` | concat-at-85 |
| CONSTANT entries | §13.10 constant entry; §13.18.15 CONSTANT RECORD | 2002 | `constant_entry`, `constant_record` | `constant-entry-2002`, `constant-record-2002` | constant-as-receiver; constant-at-85 |
| TYPEDEF residue | §13.18.44 TYPEDEF/SAME AS; §8.8.4.2 SR4 strong-group relations; EXTERNAL type | 2002 | `typedef_same_as`, `typedef_external` | `same-as-clause-2002`, `external-typedef-2002` | strong-group-heterogeneous-compare |

**Diagnostic bands (reuse the established greenfield bands; do not collide):** `0814` national literal, `0819` national/boolean usage/MOVE legality, `0844` boolean relation misuse, `0869`/`0881` pointer bands, `0870` binary-usage-PICTURE, `0900`/`0901` edition-introduction / reserved-word funnel, `15xx` (`1510` UDF RETURNING category — being LIFTED; `1532` strong-type relations). New constraints pick the next free code in the relevant band and register a one-code-one-rule descriptor (per the DESIGN-test-build-ci diagnostic-registry direction) rather than reusing `0899`.

**Owner process rules that bind this phase:** every feature ships its conformance test IN THE SAME COMMIT (`feedback_conformance_tests_per_feature`); grammar changes are pre-authorized but require the FULL legacy guard in the same change set (`feedback_grammar_approval`, `feedback_legacy_suite_on_shared_corpus`); implement COMPLETELY to spec + design, tests verify never scope (`feedback_spec_scopes_not_tests`); cite the § in code for every semantic decision (`feedback_use_the_spec`); verify by RUNNING, not just compiling (`feedback_verify_demo_output`).
