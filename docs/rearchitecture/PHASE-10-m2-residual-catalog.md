# PHASE 10 — M2 residual catalog: national/boolean, pointers, UDF, file-2002, RW / CONSTANT / concat

- **Phase:** 10
- **Title:** M2 residual catalog — close the mandatory COBOL-2002 non-OO surface on the unified data model + rearchitected runtime
- **Track:** feature-iso
- **Risk:** MEDIUM
- **Depends on (MUST be DONE before starting):** P5 (unified data model — `StorageForm` discriminator, `Model/` folder, `RecordLayout`, pass pipeline), P8 (runtime reorg — `RunUnit`, `FileConnector`/`FileRegistry`, `ManagedPointer`, role-based folders), P9 (OO rearchitecture — `Oo/` + `OoDriver`). Soft-adjacent: P6/P7 (binder phase + visitor dispatch) make the per-verb edits cleaner but are not hard blockers for any single step here.
- **Goal (one paragraph):** Every mandatory COBOL-2002 *non-OO* language feature is implemented end-to-end on the *rearchitected* substrate — national/boolean data ride `StorageForm.CharImage` (one UTF-16 char per position), pointers ride the `ManagedPointer` carrier, files ride `FileConnector`, UDFs ride the per-activation data model — with a rejecting diagnostic under every `--std` edition that lacks the feature, a discovered positive corpus entry, a version-matrix row, and a negative `.err` case per feature. The phase OPENS with a greenfield-vs-catalog reconciliation audit (a fresh `GreenfieldStatus` column sized against the *current* post-rearchitecture tree, not a stale legacy-era snapshot), so every subsequent wave is scoped against truth rather than the legacy-era ☑/◐ marks. It CLOSES with the M2 catalog marks flipped to greenfield truth and the full battery green.
- **Exit criteria:** Every track's positive corpus discovered by the greenfield runner (`CorpusRunnerTests` over `manifest.json`) + a version-matrix row + a negative `.err`; the M2 catalog (`docs/ISO2023_CONFORMANCE_PLAN.md` §3 and `docs/PHASE4_RECONCILIATION.md`) marks flipped to greenfield truth; national `CharImage` confirmed one-UTF-16-char-per-position by a runtime assertion + a golden; full battery green (2028+ greenfield conformance + 213+ unit + FULL legacy guard NIST 353 MATCH).

> **STATUS:** IN PROGRESS @ Step 4 landed (2026-07-17 — the LAST unchecked feature step: the full ALPHABET-national/UCS-4/UTF-8/UTF-16 collating surface on the ONE collating subsystem — sparse `NationalCollatingTable`/`__COLLATE_NAT`, PCS + SORT/MERGE FOR forms, national relations/88s/CHAR-NATIONAL/ORD/HIGH-LOW-VALUE, the UCS-4≡native §8.5.1.4 derivation, Table-6 coded-set-only rejections; `alphabet_national` golden + 4 negatives + 2 new matrix rows + `sort-collating-national-2002` ACTIVATED — see the Step-4 section. Steps 6-rest/7–10 [confirm/close bookkeeping] remain)
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

**Conformance corpus mechanics (current):**

- Positive corpus lives in `tests/conformance/{2002,2014,2023}/<name>.cob` (+ `<name>.out` golden). Each edition dir has a `manifest.json` with `"enabled"` and `"pending"` arrays. `CorpusRunnerTests` compiles+runs every ENABLED program at that dir's `--std` strict and byte-compares the `.out`; PENDING programs are catalogued but not asserted (the mass-red guard). An integrity fact asserts **every** on-disk `.cob` is listed (enabled ⊕ pending) — nothing is silently undiscovered. **To land a feature: add the `.cob`+`.out`, move the name from `pending`→`enabled`.**
- Negative corpus lives in `tests/conformance/negative/<name>.cob` + `<name>.err`; the `.err` pins the expected diagnostic (code + message) at the edition(s) the case names.
- Version-matrix rows live in `tests/version-matrix/constructs.json` (introduction/continuity gating) — each new 2002 construct that is a *compile-time introduction gate* gets a row so it enters the (construct × edition) matrix. Reserved-word interval rows go in the same file / `reserved-words.json`. A *runtime-observable* behavior (e.g. keyword-omitted forms) is NOT a matrix row — its golden + unit tests are its coverage (see M2-UDF-4 precedent).
- The catalog SSOTs to flip at the end: `docs/ISO2023_CONFORMANCE_PLAN.md` §3 (legacy-era marks) and `docs/PHASE4_RECONCILIATION.md` (the greenfield-truth table — this is the one to keep authoritative).

---

## 2. Rationale — the problems this phase fixes

The M2 (COBOL-2002) surface was largely built in the **retired legacy byte engine** and later re-landed piecemeal on the greenfield during "Phase 4"; the catalog carries three generations of stale marks. Concretely:

1. **The catalog marks lie in both directions.** `docs/ISO2023_CONFORMANCE_PLAN.md` §3 records LEGACY-era ☑/◐. `docs/PHASE4_RECONCILIATION.md` is truer but predates the rearchitecture (P1–P9) and several Phase-4 landings, and has not been re-audited against the **post-rearchitecture** tree where `StorageForm`, `RunUnit`, `FileConnector`, and `ManagedPointer` now own the substrate. The known hazard: a "M2-DATA done mark" can be a legacy mirage — national/boolean/pointers/floats may stage LOUD `COBOLNET0899` in the greenfield even where the legacy catalog reads done. **A wave sized against a stale mark is either wasted (re-implementing a landed feature) or a silent hole (skipping a regressed one).** → Step 1 is a fresh audit.

2. **National/boolean data must be confirmed on `StorageForm.CharImage`, not any `StoreAsImage` flag.** The data model has no mutable `DataItem.StoreAsImage` flag or emitter `MarkStoreAsImage` write-back (DESIGN-data-model.md); a single computed `StorageForm` discriminator owns it, where **`CharImage` subsumes every string-stored leaf including national and boolean**. The invariant "one UTF-16 char per national position, `ImageWidth == Length`" (DESIGN-data-model.md D-N1) is a property of the `StorageFormPass`, not a scattered `IsNationalLike` doubling. This must be *proven*, not assumed (the exit criterion names it explicitly).

3. **Pointers must ride the ONE `ManagedPointer` carrier on the reorganized `Control/` runtime.** P8 split `ProgramRegistry.cs` and moved `ManagedPointer` to `Control/ManagedPointer.cs` under `RunUnit`. The 2002 pointer surface (`USAGE POINTER`/`PROGRAM-POINTER`, `NULL`, `SET`, `ADDRESS OF`, `BASED`, `ALLOCATE`/`FREE`) plus the `StorageCell`/`CellPointer` window model must be confirmed against the new home, and the residual `USAGE PROGRAM-POINTER` leg closed.

4. **Files must ride `FileConnector`.** P8 collapses `SequentialFile`/`RelativeFile`/`IndexedFile` behind `FileConnector` + a polymorphic `FileRegistry`, deleting the `Keyed*` fallthrough. The 2002 file surface (SHARING / LOCK MODE / RETRY / UNLOCK / line-sequential + 2002 FILE STATUS 5x/6x) exists but was built on the pre-connector dispatch; it must be re-confirmed on the connector.

5. **Genuine open residue remains, on ANY substrate:** ✅ `&`-concatenation LANDED (§8.8.3, Step 14 2026-07-16 — a compile-time fold, `concat-operator-2002` ACTIVE); CONSTANT entries (§13.10) + CONSTANT RECORD (§13.18.15) — **zero grammar/binder surface today** (`grep CONSTANT src/.../DataBinder.cs` → empty); Report Writer 2002 additions PRESENT WHEN format 1 + VARYING format 1 (**zero** hits in `DataBinder.Reports.cs`); ARITHMETIC IS STANDARD *behavior* (the `ArithmeticMode` enum is *captured* in `OptionsModel.cs` but **not consumed** by the numeric engine); ALPHABET national/UCS-4/UTF-8/UTF-16 phrases (no hits in `DataBinder.Switches.cs`); the EC `-N` twins + `EXCEPTION-FILE-N` (`IntrinsicCatalog.cs:133` = `IntrinsicBind.Deferred`, staged loud, blocked on national); the UDF residue (✅ BY VALUE header formals LANDED Step 10 2026-07-16 [§14.2.2/§14.2.3 GR10, both activation paths]; ✅ per-evaluation activation LANDED — 1509 NARROWED to VARYING BY / AFTER-level FROM / EVALUATE subjects; ✅ the RECURSIVE per-activation-vs-static data model LANDED (Step 10a 2026-07-16 — a recursive unit's / every FUNCTION's WS = STATIC fields, one §14.6.2.3.3 last-used copy; program/function LOCAL-STORAGE binds and is per-activation initial per §13.6.4 GR1/§14.6.2.3.2; CANCEL resets the statics per §14.9.5 GR3); still open: the per-shape `COBOLNET1510` RETURNING residues [float/boolean/pointer-class + group shapes] in `Binding/Procedure/Verbs/UdfBinder.cs`, OPTIONAL/OMITTED formals [0899 `optional-formal`], and the two Step-10a stages [0899 `recursive-contained-working-storage`, 0899 `recursive-working-storage-pointer-backed`]); ✅ the TYPEDEF residue LANDED (Step 16 2026-07-16 — SAME AS §13.18.49 via the ONE `CloneItem`; EXTERNAL type declarations live per §13.18.22 GR2/GR3/SR5 [the 1534 stage lifted]; heterogeneous strong relations were already live [1533]; still open by name: 0899 `strong-group-ordering-signed-leaf` [§8.8.4.2.12 element-wise signed ordering], 1535 `typedef-renames-staged`, 1531 INDEXED-type-≥2×).

6. **Per-edition obligation.** Every item above owes TWO things (owner directive, `ISO2023_CONFORMANCE_PLAN.md` §0): the complete per-edition ISO behavior AND the rejecting diagnostic under every `--std` edition that lacks it. A 2002 construct compiled `--std 85` must flag. Coverage = a positive golden + a version-matrix row + a negative `.err`.

---

## 3. Target end-state for this phase

When Phase 10 is DONE, the following are true and demonstrable:

**Data model / runtime confirmations (no net-new feature, but proven on the rearchitected tree):**
- `StorageForm.CharImage` covers national (`USAGE NATIONAL`/`PIC N`) and boolean (`USAGE BIT`/`PIC 1`) leaves; a runtime assertion + golden prove national is one UTF-16 char per position, `ImageWidth == Length`. National movement/compare/figurative/VALUE/level-88 all route through `Values/Text/CobolString` (national) and `Values/Text/CobolBool` (boolean) with the `pad`-char discipline.
- Boolean OPERATORS `B-AND`/`B-OR`/`B-XOR`/`B-NOT` bind through `Binding/Procedure/Verbs/ConditionBinder.cs` (`BoundBoolBinary`) and render through `Values/Text/CobolBool` (already present; confirmed on the new folders + a golden).
- Pointers (`USAGE POINTER`/`PROGRAM-POINTER`, `NULL`, `SET`, `ADDRESS OF`, `SET ADDRESS OF`, `UP/DOWN BY`, `BASED` deref, `ALLOCATE`/`FREE`) ride `Control/ManagedPointer` + `Control/StorageCell` (`CellPointer` window) under `RunUnit`; `USAGE PROGRAM-POINTER` distinguished from data `POINTER` (equality + `SET pp TO ENTRY`/procedure-pointer semantics per §13.18, staged-loud only where §13.18.5 GR3/GR4 deref would be undefined under .NET).
- 2002 file surface (SHARING / LOCK MODE / RETRY / UNLOCK / line-sequential / FILE STATUS 5x/6x) is confirmed on `IO/FileConnector` + `FileRegistry`; the sharing registry is on `RunUnit` (`IO/Sharing/PhysicalFileTable`).
- UDF residue closed: ✅ category-carrying non-numeric/group RETURNING (Step 9 — the blanket 1510 is now the per-shape residue staging), ✅ BY VALUE header formals modeled end-to-end (Step 10 — §14.2.2/§14.2.3 GR10, both activation paths), ✅ per-evaluation activation for condition windows (Step 10 — 1509 narrowed), ✅ the per-activation-vs-static data model for RECURSIVE (Step 10a — §13.5.4 GR1/§14.6.2.3.2/.3: a RECURSIVE unit's / every FUNCTION's WS emits STATIC [one last-used copy across all activations, reset by CANCEL per §14.9.5 GR3], LOCAL-STORAGE binds at program/function level and is per-activation initial [fresh instance for INITIAL/RECURSIVE units; an emitted Call-entry re-init for cached singletons]).

**Net-new features (files/classes that exist when done):**
- `src/Cobol.Net.Compiler/Binding/Model/` (or the P5 model folder): `ConstantEntry` support on `DataItem` (a computed init-only `IsConstant`/`ConstantValue`), a `ConstantEntryPass` (or fold into `BindEntry`); CONSTANT RECORD level-01 handling.
- ✅ `&`-concatenation (Step 14, 2026-07-16): the spec surface turned out to be a COMPILE-TIME LITERAL, not a runtime operator — §8.8.3.3 GR3 makes a concatenation expression "equivalent to a literal of the same class and value", and its operands are only literals/figurative constants — so the as-built shape is `Binding/ConcatFolder.cs` (the ONE fold chokepoint; §8.8.3.2 SR diagnostics 1540/1541/1545) + a `concatenationExpression` tier in `Core/CobolExpressions.g4` + the `AMPERSAND` token; NO `BoundConcat` node and NO emitter arm exist (the folded literal rides the pre-existing literal channels). Boolean `&` folds the same way (`B"01" & B"10"` → `B"0110"`), incl. the boolean-relation channel.
- ✅ Report Writer 2002 (Step 13, 2026-07-16): `PRESENT WHEN` (§13.18.41 F1) + `VARYING` (§13.18.64) + the §13.18.14 multiple/relative COLUMN vehicle — parsed in `Core/CobolReportWriter.g4`, condition chains + `ReportVaryingModel` on the report model (`DataBinder.Reports.cs`; bound via `ReportWriterBinder.BindReportGroupClauses` through the ONE ConditionBinder), compose-side guards/counters in `CodeGen/Verbs/ReportWriterEmitter.cs`, the evaluate-once-per-presentation walk in `Cobol.Net.Runtime/IO/ReportWriter.cs`. SR family = COBOLNET1559; multiple LINE repetition stages 0899 with the report-group OCCURS family.
- ARITHMETIC IS STANDARD: `OptionsModel.ArithmeticMode` consumed by the numeric renderer / `Runtime/Values/Numeric` intermediate-precision path.
- ALPHABET national/UCS-4/UTF-8/UTF-16 phrases: bound in `DataBinder.Switches.cs` / `SpecialNamesBinder`, feeding the national codec.
- ✅ EC `-N` twins + `EXCEPTION-FILE-N` (Step 11, 2026-07-16): `IntrinsicCatalog.cs` rows flipped from `Deferred` to `Runtime` (`EcFileN`/`EcLocationN`/`CharNational`), backed by `EcFunctions.FileN/LocationN` on the ONE `NationalOf` repertoire translator + `CobolIntrinsics.CharNational`.
- ✅ TYPEDEF residue (Step 16, 2026-07-16): `SAME AS` §13.18.49 via the single `CloneItem` (+ the shared `CopyEntryDescription`/`ExpandSameAs` on the ONE `ExpandTypes` pass; 1555/1556/1557 SR bands); EXTERNAL type declarations LIVE (§13.18.22 GR2/GR3/SR5 = 1558 — the 1534 stage lifted; records external-by-type ride the ordinary ExternalStore re-basing); strong-group heterogeneous relations were ALREADY live (§8.8.4.2.3 SR1 = 1533 at the ONE relation checkpoint — stale audit claim); SR4 reclassified to the named rule (`strong-compare-ordering`), and the §8.8.4.2.12 signed-leaf ordering staged 0899 `strong-group-ordering-signed-leaf`.

**Bookkeeping:**
- Each feature: a positive `<name>.cob`+`.out` moved `pending`→`enabled` in `tests/conformance/2002/manifest.json` (2014 where the construct is 2014-introduced), a `constructs.json` matrix row (compile-time gates only), and a `tests/conformance/negative/<name>.cob`+`.err`.
- `docs/PHASE4_RECONCILIATION.md` rows and `docs/ISO2023_CONFORMANCE_PLAN.md` §3 marks flipped to greenfield truth.

---

## 4. STEP-BY-STEP

> Ordering rationale: Step 1 (audit) reshapes every wave. Then waves are ordered lowest-risk-first and by dependency: confirmations of already-landed features (that only need re-proof on the new substrate) come before net-new implementation; national (Step 2) unblocks the `-N` intrinsic and EC legs (Steps 11/16); CONSTANT and `&`-concat are self-contained. Each wave ends at a COMMIT BOUNDARY with the battery green.

**Progress checkboxes (executing session keeps current):**

- [x] Step 1 — Reconciliation audit + `GreenfieldStatus` column (2026-07-16 — the audit table + evidence under Step 1 below; 9 PARTIAL · 3 NOT-STARTED · 1 STAGED-LOUD; 56 ISO-cited gaps)
- [x] Step 2 — National on `StorageForm.CharImage` (confirm + prove one-UTF-16-char/position) (2026-07-16 — `NationalStorageFormTests` pins CharImage Width==Length for PIC N(5)/USAGE NATIONAL; D-N2 byte-surface guards confirmed live per the Step-1 audit evidence; `national_data` golden green)
- [x] Step 3 — Boolean data + operators on the new folders (confirm) (2026-07-16 — the audit found the 2002 core already on StorageForm/Values-Text/CobolBool with `boolean_data`/`boolean_ops` ENABLED + negatives; the missing substrate pin added to `NationalStorageFormTests` (PIC 1(4) USAGE BIT CharImage); B-SHIFT/BX"…" stay the separately-catalogued 2023 residue)
- [x] Step 4 — ALPHABET national / UCS-4 / UTF-8 / UTF-16 phrases (2026-07-17 — the FULL national collating surface: two-branch §12.3.7.2 ALPHABET grammar [ISO FOR position + postfix superset], sparse `NationalCollatingTable`/`__COLLATE_NAT`, PCS/SORT-MERGE FOR forms + alphabet-name-2, national relations/88s/CHAR-NATIONAL/ORD/HIGH-LOW-VALUE wired, UCS-4≡native derivation documented, UTF-8/UTF-16 coded-set-only rejections; `alphabet_national` golden + 4 negatives + `alphabet-national-2002`/`program-collating-national-2002` rows + `sort-collating-national-2002` ACTIVATED — see the Step-4 section)
- [x] Step 5 — National wave: DISPLAY-OF/NATIONAL-OF + pins + goldens/negatives (2026-07-16 — see the Step-5 section for the landed list; Step 4 was deferred out of that wave and landed separately 2026-07-17)
- [x] Step 6 — Pointers on `ManagedPointer`/`StorageCell` under `RunUnit` (confirm) — **PROC-5-allocate slice LANDED 2026-07-16** (the §14.9.3 GR7 INITIALIZED lowering + the `allocate_initialized` golden); **the qualified/subscripted ADDRESS OF residue LIFTED 2026-07-16 (the pointers wave):** `ReferenceResolver.ResolveForAddressOf` resolves the operand through the ONE §8.4.2.2 qualification machinery and returns the in-class OCCURS displacement (`(idx−1)×width` — the SAME end-to-end cell-layout formula the Tier-B `PlaceForItem` window uses; a D10 transitional rendered-index string on `BoundAddressOf.OccursDisplacement`); the pre-scan `PtrScanAddressOfTargets` yields (head, qualifiers) for EVERY operand shape and resolves qualified heads via `FindItem`; ref-mod operands stay loud (a span, not an item). Golden `address_of_qualified` ENABLED (qualified / subscripted / variable-subscript / combined — each address re-based onto a BASED view and read back). REMAINING (named): BASED/ADDRESS OF inside a class definition stays staged (`OoBasedInClass` — the OO cell/bridge emission; deferred out of the wave by the tripwire rule), and INITIALIZE over pointer categories (§14.9.24 — NULL under DEFAULT/REPLACING) is a residue shared by data- AND program-pointers (neither kind is handled by InitializeBinder today).
- [x] Step 7 — `USAGE PROGRAM-POINTER` leg (net-new residue) (COMMIT) (2026-07-16 — the pointers wave: the CONSTANT/AS §8.9 interval treatment for BOTH new words (PROGRAM-POINTER 2002+, FUNCTION-POINTER 2014+ per reserved-words.json: lexer tokens + cobolWord + CheckedTokenTypes + `user-word-*` interval rows); `programPointerUsage`/`functionPointerUsage` grammar alternatives with the `TO prototype` tail; `PicCategory.ProgramPointer`/`Usage.ProgramPointer`/`PicInfo.ProgramPointerItem`/`StorageForm.ProgramPointerRef`; the `Control/ProgramPointer` runtime carrier = the OUTERMOST program's externalized identity resolved through the ONE `ProgramTable` (`EntryOf` — §8.4.3.13 GR1/GR4 with the sibling-module probe; `CallPointer` — NULL → loud EC-PROGRAM-NOT-FOUND, the documented implementor definition of §14.9.4.4's undefined invalid-address case); the NEW `setEntryStatement` grammar rule + `BindSetEntry` (Format 9 + the §8.4.3.13 ENTRY sender, literal + identifier forms, EC-gated via the EmitFree checking pattern); `BindSetProgramPointer` re-routes mirroring the data-pointer F4 pair (the setToValue + objectReference peeks); CALL-through-pointer via `BoundCallProgram.IsPointerTarget` → `ProgramRegistry.CallPointer` (§14.9.4 SR1 :26082); `ProgramPointer.SameTarget` relations (§8.8.4.1.3, the NULL figurative renders the Null carrier); PIC prohibited (§13.16.3 SR8) + VALUE prohibited (§13.18.63 SR9) = 0881; restricted TO-prototype (GR25) + FUNCTION-POINTER semantics STAGED LOUD (0899 descriptors `program-pointer-restricted`/`usage-function-pointer` — prototypes are P13). Gates: `usage-program-pointer-2002` ACTIVE + `usage-function-pointer-2014` pending via the bound-arm `UsageConstructId`. Golden `program_pointer` ENABLED (verified by running: init-NULL, ENTRY literal + identifier, CALL ×2 through the pointer, copy, relations, NULL reset, the GR4 not-found → NULL leg) + 5 negatives + legacy exclusion.)
- [◐] Step 8 — File-2002 (SHARING/LOCK/RETRY/UNLOCK/line-seq/5x-6x) on `FileConnector` — **sharing/record-lock
      wave LANDED 2026-07-16** (sequential-organization locking on the ordinal identity; the WRITE/REWRITE/DELETE
      conflict checks + lock discipline; RETRY on all five verbs; DELETE FILE '62'; see the Step-8 AS-BUILT below).
      REMAINING: the line-seq status protocol 06/09/71 + line-seq REWRITE, the 04/39 narrow statuses, the LINE
      SEQUENTIAL edition gate (COMMIT)
- [x] Step 9 — UDF residue: category-carrying RETURNING (lift 1510) (net-new) (2026-07-16 — the §8.4.3.2.4 GR1 category channel: elementary alphanumeric/numeric-edited/national + character-form GROUP RETURNING carried end-to-end (the temp clones the FULL description — a group deep-clones its subtree unregistered via `CloneTempNode`; every operand chokepoint maps the result read to a `BoundFieldOperand` so the cloned category drives MOVE Table-16 / relation class dispatch / DISPLAY / the LENGTH fold — the relation chokepoint `ComparisonOperandOf` now routes through the ONE `IntrinsicBinder.OperandOf` mapping; delivery = the existing string CALL-ABI `CobolArgAdapt.StoreReturn(string)`, groups as AsImage/FromImage); `udf_returning_categories` golden ENABLED byte-exact + legacy GreenfieldOnly. STAGED loud by name (1510, per-shape texts in `UdfBinder.UdfReturningResidue`): FLOAT (the CALL-boundary string carrier has no float write half), BOOLEAN (no §8.8.2 boolean-expression function-result arm — a partial land would half-wire), pointer/object/index classes, and group residues (strong-typed identity, internal REDEFINES, variable-length, non-character binary/packed/COMP-5/float leaves))
- [x] Step 10 — UDF residue: BY VALUE formals + per-evaluation activation + recursion verification (COMMIT) (2026-07-16 — (a) BY VALUE header formals END-TO-END: the §14.2.2 using-phrase grammar (`usingParameter`/`usingByValue`, per-parameter — legacy consumers updated shape-only), `LinkageFormal.ByValue` + §14.2.3 GR4 transitivity threading, §14.2.2 SR2 = COBOLNET1553 (+ 0899 `by-value-formal-carrier` for the SR2-legal object/pointer/float shapes; 0899 `optional-formal` for the parsed OPTIONAL phrase), the callee-side §14.2.3 GR10 DETACHED value-copy cells `CobolArgAdapt.NumValue`/`TextValue` on the ONE ABI (copy-out skipped) for BOTH CALL targets and UDF activations, UDF GR5c argument modes + §8.4.3.2.3 SR10 = COBOLNET1554, matrix row `pd-header-by-value-2002` (parse-arm `VisitUsingByValue`); (b) per-evaluation activation: `BoundUdfEvaluated` + `UdfAttachPerEvaluation` at every conditionally/repeatedly-evaluated CONDITION window (PERFORM UNTIL/VARYING UNTIL, SEARCH/SEARCH ALL WHEN, EVALUATE object terms, non-first AND/OR operands; rendered as an IIFE so C# short-circuit realizes §8.8.4.13 r1) — COBOLNET1509 NARROWED to VARYING BY / AFTER-level FROM / EVALUATE subjects; (c) recursion verified per §8.6.6 :8821 (already implemented — `BinderDriver` registers FUNCTION-ID units Recursive; `udf_recursion` re-proven by running). Goldens `udf_by_value` + `udf_per_eval` (EXTERNAL activation counter) verified-then-baked; negatives 1553/1554. ⚠ The RECURSIVE WS-static data-model split (§14.6.2.3.2/.3) did NOT land in this step — taken by Step 10a)
- [x] Step 10a — RECURSIVE working-storage data model: static WS + program/function LOCAL-STORAGE (dispositioned into P10 at DEVLOG 864) (COMMIT) (2026-07-16 — **the §-derivation** (storage class × unit kind → copy semantics): §13.5.4 GR1 — WS of a non-INITIAL program / a FUNCTION is STATIC data; §14.6.2.3.3 — static+external are the ONLY last-used data ⇒ a RECURSIVE unit's (and every FUNCTION's, §8.6.6 :8821/§9.4 :12529) WS is ONE copy shared across all concurrent/successive activations, initial only per the §14.6.2.3.2 triggers (first activation in the run unit / after an INITIAL container's activation / after CANCEL §14.9.5 GR3); §13.6.4 GR1 — LOCAL-STORAGE is AUTOMATIC ⇒ initial state EVERY activation for EVERY unit kind; §13.5.4 GR2 — an INITIAL program's WS is INITIAL data (per-activation). **As built:** the discriminator `unit.Recursive && !unit.Initial && Children.Count==0` → `DataBinder.UnitStaticWs` (BinderDriver); WS/LS roots captured (`WorkingStorageRoots`/`LocalStorageRoots` — program/function LOCAL-STORAGE now BINDS via the ONE `BindEntries(EntrySection.LocalStorage)` path, previously silently unbound); the new `RouteStaticUnitStorage` bind pass (after the pointer pass) routes WS roots + Tier-B backings + INDEXED BY cells onto the ONE static-field channel the method-WS D3 pattern established (`StaticRootFields`/`StaticIndexCells` — RENAMED from `OoStatic*`, two producers one mechanism; EXTERNAL-backed classes excluded — run-unit ExternalStore cells, §14.9.5 GR8); `RecordStructEmitter` adds the `static` modifier + emits `__ResetStatics` (reassigns the SAME composed ValueInitializer/ImageInitOf initializers; cells → 1); registration passes `{ClassRef}.__ResetStatics` as the OPTIONAL 8th `ProgramTable.Register` arg (invoked at run-unit start = §14.6.2.3.2 case 1, and by `CancelNode` = case 3/GR3; the INITIAL-container cascade = case 2 rides `CancelContained`); a cached-SINGLETON unit with LS gets a `Call`-entry LS re-init (same initializers — automatic data is per-activation for every unit kind). Matrix row `local-storage-section-2002` ACTIVE (parse-arm `VisitLocalStorageSection`). **Staged LOUD (honest subset):** 0899 `recursive-contained-working-storage` (a RECURSIVE program WITH contained programs + WS — a containee's GLOBAL `__outer` ref-bridge aliases CONTAINER-INSTANCE fields, C# forbids instance→static refs; negative `recursive-contained-ws`) and 0899 `recursive-working-storage-pointer-backed` (BASED/ADDRESS-OF-taken WS in a recursive unit — the cell/AddrField are per-instance today). **Recorded residues (silent, pre-existing):** a recursive unit's FILE connectors/record areas stay per-activation-instance (file-state sharing across recursive activations untouched by this slice); an EXTERNAL WS table's INDEXED BY cells stay per-activation. Golden `recursive_ws` verified-by-RUNNING then baked (depth-3 shared-WS accumulation + per-depth LS re-init + each activation keeping its OWN LS across the nested call + CANCEL→WS-initial + a UDF WS call-counter 1,2,13); `udf_recursion`/`udf_per_eval` stay green unchanged (LINKAGE/EXTERNAL state by design); characterization 33/33 — non-recursive emission byte-identical (statics fire ONLY on Recursive&&!Initial; the LS re-init only when an LS section exists; the Register 8th arg omitted otherwise))
- [x] Step 11 — EC `-N` twins + `EXCEPTION-FILE-N` (net-new, needs Step 2) (COMMIT) (2026-07-16 — EXCEPTION-FILE-N §15.29 / EXCEPTION-LOCATION-N §15.31 (the ONLY -N EC twins the 2023 text defines) flipped `Deferred`→`Runtime` as `EcFunctions.FileN/LocationN` = the base renderings through the ONE `NationalOf` repertoire translator, category National; the same wave landed CHAR-NATIONAL §15.16 (`CharNational`, native national PCS = UTF-16 order) + ORD-over-national §15.70.4 r2 (the 0844 guard narrowed to CHAR, alphanumeric weights never applied to a national arg); `exception_file_n`+`char_national` ENABLED, `exception-file-n-2002` matrix row, `exception_file_n_below_2002` 85-window negative, ECT018N inline EC Fact; the 2023 file-connector-argument form stays loud → PHASE-13 Step 9)
- [x] Step 12 — ARITHMETIC IS STANDARD behavior @2002/2014 (residual-leg closure — the consumption core was already landed+golden-pinned, per the Step-1 audit) (COMMIT) (2026-07-16 — **the residual legs, all six audit gaps dispositioned:** (1) SDIDI exponentiation §8.8.1.5.4 = `CobolDec.Pow` (integer exponents by square-and-multiply over `Mul` — exactly r2a–r2d for 1–4 [SDIDI × is commutative with one per-op rounding], the r2e implementor-defined form beyond, r3 reciprocal, the §8.8.1.2 r6/r4 EC-SIZE-EXPONENTIATION legs; non-integer exponent = the r2e double approximation through `FromDouble`) + the `NumericRenderer.Power` mode branch; (2) decimal128 range ECs §8.8.1.5.2 r2 = the ONE `Clamp` in the `Round34Wide` funnel (adjusted exp > +6144 ⇒ EC-SIZE-OVERFLOW; below 10⁻⁶¹⁷⁶ re-rounds onto the subnormal quantum, nonzero→zero ⇒ EC-SIZE-UNDERFLOW); (3) float operands under the modes IMPLEMENTED (not staged): `CobolDec.FromDouble` = the §8.8.1.5.1 implementor-defined conversion (SHORTEST round-trip decimal identity, ≤17 digits ⇒ exact; Inf⇒EC-SIZE-OVERFLOW, NaN⇒EC-DATA-INCOMPATIBLE) — the StandardDecimal branch now runs BEFORE the D16 float branch in `CombineCore`/`Power`; (4) intrinsics §15.4.1 r1: MEAN's division evaluates in SDIDI (IntrinsicRenderer — the NOTE-2 relation `MEAN(a b c) = (a+b+c)/3` now TRUE), the exact-Int128 family is documented-equivalence consumption (the `CobolIntrinsics.Exact.cs` header derivation; >34-digit exact results recorded as the extra-precision residue), the prose-approximation family is implementor-defined in every mode, and ANNUITY/PRESENT-VALUE/VARIANCE/STANDARD-DEVIATION (inexact-division EAEs) staged LOUD = 0899 `arithmetic-standard-intrinsic` (IntrinsicBinder); (5) RW SUM §8.8.1.5.1 = documented-equivalence at the ReportWriterEmitter chokepoint (each GR3 accumulation is one ≤32-digit exact addition — digit-identical engines); (6) the 2002-vs-2014 edge RESOLVED to 2002 (Annex E.2 item 21 back-derivation + the M2 catalog + the OPTIONS reserved word @2002): rows renamed `options-paragraph-2002`/`options-arithmetic-native-2002`/`arithmetic-standard-2002` (dual-window 0900/0903-obsolete-2014/0807-2023), the STANDARD-DECIMAL/STANDARD-BINARY keywords gate 2014 on the `VisitArithmeticMethod` arm, and the six 2014-only OPTIONS clauses got per-clause 0900 rows+arms (`options-default-rounded/-intermediate-rounding/-entry-convention[conservative]/-float-binary/-float-decimal/-initialize-2014`); OptionsBinder routes inert below 2002. Golden `2002/arith_standard` (hand-derived: 2/7*7=2.00000; (1+1e-9)²=1.000000002000000001 exact; 9999999999³ = 999999999700000000029999999999 exact 30 digits; 10^3200×10^3000 ⇒ RANGE-EC via ON SIZE ERROR; COMP-2 0.1×3 = 0.300000000000000000 vs native …044-artifact; MEAN-EAE-OK) verified-by-RUNNING against its native twin (199997/…128/long-saturation/no-EC/…064/MEAN-EAE-DIFFERS) then baked; legacy GreenfieldOnly exclusion; negatives arith-standard-at-85 / arith-standard-decimal-at-2002 / options-default-rounded-at-2002 / arith-standard-intrinsic-staged; VCR row-28 anchor gains gate:arithmetic-standard-2002)
- [x] Step 13 — Report Writer 2002: PRESENT WHEN + VARYING format 1 (net-new) (COMMIT) (2026-07-16 — SUPERSET PARSE on the EXISTING RWCS (no second presentation path): `reportPresentWhenClause : PRESENT WHEN condition` + `reportVaryingClause : VARYING (cobolWord (FROM arith)? (BY arith)?)+` as `reportGroupClause` alternatives, and the 2002 §13.18.14/§13.18.35 Format-1 operand forms — `reportColumnClause : (COLUMN|COLUMNS|COL|COLS) (NUMBER|NUMBERS)? (IS|ARE)? (PLUSWORD? integerLiteral)+` (multi-operand + relative) / `reportLineClause` reshaped onto `reportLineOperand+` (legacy `SemanticBuilder` migrated shape-only to operand 0). New §8.9/§8.10 words: PRESENT/COLS/COLUMNS (reserved 'added 2002' — funnel rows pre-existed) + NUMBERS (§8.10 context word) = lexer tokens + `cobol-words.json` nameSlot rows. **Bind (`DataBinder.Reports.cs`):** PRESENT WHEN chains accumulate down a level-number stack (§13.18.41.4 GR2b — an absent ancestor absents every subordinate ⇒ presence = AND of the chain): a LINE carries 01→line-entry, a field the slice strictly below its line entry, a SUM entry the FULL chain; VARYING counters (`ReportVaryingModel`) capture FROM/BY parse contexts; `SOURCE IS counter` rebinds to `FieldVaryingSource` (§13.18.64.4 GR4 NOTE). Conditions/expressions bind in the procedure phase through the ONE `ConditionBinder`/`ExpressionBinder` (`ReportWriterBinder.BindReportGroupClauses`, memoized per distinct context; called at the top of `StatementBinder.Bind`). SRs = **COBOLNET1559** `report-group-clause-rule` (one code, the SameAsEntryRule bundling precedent; 1560-band stays PHASE-13): §13.15.3 SR16 (condition references LINE-/PAGE-COUNTER or a report-section-exclusive name — ByName-declared names resolve to storage and are exempt) + SR17 (GROUP INDICATE ⊥ PRESENT WHEN) + §13.18.64.3 SR1 (VARYING needs OCCURS/multi-LINE/multi-COLUMN) + SR2 (counter defined elsewhere) + SR3 (counter in its own FROM). **Emit (`ReportWriterEmitter` + `ConditionRenderer` wired in):** compose-side `if (chain)` guards per field (§13.18.41.4 GR2b/GR3f — an absent item places nothing and never advances the horizontal counter); multi-COLUMN placements unrolled per operand with `long __rv{uid}_{k}` compose-locals (GR3a first ← FROM aligned to scale 0 [`Rescale` truncation = the EC-REPORT-VARYING noninteger seam, §13.18.64.4 GR5, checking default-off]; GR3b += BY per repetition); relative operands place against the `__hc` horizontal counter (§13.18.14.4 GR7/GR8/GR9 — emitted only when a relative operand exists); `new BoundReportVaryingRef` (scale-0 BoundExpr leaf; the generated-visitor addition) renders the counter through the ONE MOVE conversion. Line delegates `new ReportGroupLine(…, () => chain)` + `AddSum(…, present)` — both params optional, plain emission byte-identical (characterization 33/33). **Engine (`CobolReport`):** `EvaluatePresent` evaluates every line condition ONCE per presentation BEFORE any LINE processing (GR2); the page-fit form + trial sum + GR5 first-line placement key on the first PRESENT line (§13.18.35.4 GR4/GR5 'which LINE clause is taken to be the first may depend on the conditions'; absent relative lines excluded from the trial per §13.18.41.4 GR3d); absent lines are SKIPPED so the next relative line re-anchors on LINE-COUNTER (the line collapse, GR2b); all-absent ⇒ return-before-flags (as though the description were omitted — no counters, no fit, no reset); `EndOfGroupSumReset` skips an absent SUM entry (§13.18.41.4 GR3g/§13.18.54.4 GR10 — not printed [the compose chain] and not reset). **Gates:** parse-arm `VisitReportPresentWhenClause`/`VisitReportVaryingClause` (recognition) + `VisitReportColumnClause` (fires on COL/COLS/COLUMNS/NUMBERS/ARE/multi-operand/PLUS — the 85 form was exactly `COLUMN NUMBER IS integer-1`) + `VisitReportLineClause` (LINES/NUMBERS/ARE/multi-operand); rows `report-present-when-2002`/`report-varying-2002`/`report-multi-column-2002` ACTIVE + `report-multi-line-2002` pending. **Staged LOUD (named):** multiple LINE repetition (0899 `report-multiple-line` — GR9-equivalent to the staged report-group OCCURS family; VARYING's OCCURS/multi-LINE vehicles ride it), a VARYING counter inside FROM/BY expressions (0899 `report-varying-counter-in-expression` — the SR3-legal BY self-reference), FUNCTION inside condition-1 (0899 `report-condition-function` — the UDF activation-hoist is statement-context machinery), GROUP INDICATE + relative COLUMN (0899 `report-indicate-relative-column`); LEFT/CENTER/RIGHT stays a no-grammar-surface item (deep-dive §5). Golden `2002/rw_present_when` (ONE fixture covers PRESENT WHEN line-collapse across present/absent presentations + field suppression + `COLUMNS ARE 12 16 20 … VARYING RV-IDX FROM WS-SEQ BY 2` with the counter as SOURCE — hand-derived grid, VERIFIED BY RUNNING, byte-exact) + negatives `present-when-at-85` (0900) / `report-varying-no-repetition` (1559 SR1) / `present-when-group-indicate` (1559 SR17) + legacy GreenfieldOnly exclusion)
- [x] Step 14 — `&`-concatenation operator §8.8.3 (net-new) (COMMIT) (2026-07-16 — SUPERSET PARSE: the `AMPERSAND` token + a `concatenationExpression` alternative INSIDE `nonNumericLiteral` (every literal position inherits it, §8.8.3.3 GR3); the construct is a COMPILE-TIME fold (`Binding/ConcatFolder.cs`) to the equivalent single literal — no `BoundConcat`, no emitter leg; §8.8.3.2 SRs = COBOLNET1540 class-mismatch / 1541 ALL-figurative / 1545 8,191-cap; figuratives fold ONE char each (§8.3.3.6.4 GR3a, PCS-aware H/L); the introduction gate is the VersionConformancePass parse arm `VisitConcatenationExpression` (position-blind recognition → 0900 below 2002) with the `concat-operator-2002` row flipped ACTIVE; `literal_concat` golden ENABLED (all class pairs + VALUE + 88 + hex + boolean relation + FUNCTION LENGTH) + `concat_below_2002`/`concat_class_mismatch` negatives + legacy GreenfieldOnly exclusion)
- [x] Step 15 — CONSTANT entries §13.10 + CONSTANT RECORD §13.18.15 (net-new) (COMMIT) (2026-07-16 — SUPERSET PARSE: `constantEntryBody` as a `dataDescriptionBody` alternative + `constantRecordClause` + the `occursBound` rule (integer literal OR integer constant-name, §13.10.3 SR2) + the CONSTANT and AS lexer tokens, BOTH §8.9-interval words (user words at 85, cobolWord-admitted, funnel-0901'd ≥2002 via `CheckedTokenTypes`; AS is nameSlot-ONLY by design — `AS (arith-expr)` must lex its parens in normal mode, the FU-1 ledger). The construct is a COMPILE-TIME substitution table (`DataBinder.Constants.cs` — no `DataItem`, §13.10.4 GR1/GR3): AS literal (GR1/GR2 + the SR1 single-numeric-literal reclassification + §8.8.3 concat pre-fold), AS arithmetic-expression (GR4 — a §7.3.6 evaluator on `decimal`: SR1a no exponentiation, SR1b literal/prior-constant operands, SR1c zero-divide, final integer truncation §7.3.6.3 GR3), AS LENGTH OF (GR6 via `ImageWidth`, SR3/SR10/SR12 checked); AS BYTE-LENGTH OF (GR5) + FROM compilation-variable STAGED LOUD (0899 band — the §15.14 byte-width authority and the preprocessor cv-store residues). Substitution chokepoints: `ExpressionBinder.FieldOperand`/`RefExpr` (same bound shapes as written literals), `ResolveReceiving` → COBOLNET1548, `ReferenceResolver.ResolveSubscriptName`, `OccursBoundValue` (both greenfield occurs sites; legacy `SemanticBuilder` migrated positionally), `ExpandPicConstants` (PIC repetition, SR2 sentence 2), `ExtractValue`/`RawValueOperandText` (01 + 88 VALUE). §13.10 SR family = COBOLNET1547; CONSTANT RECORD = `DataItem.IsConstantRecord` + §13.18.15.3 SR1 (WS/LS-only via the new `EntrySection` param on `BindEntries`) + SR2 receiving rejection (1548, ancestor walk) + §13.16.3 SR3/SR6/SR13 same-entry/subtree checks = COBOLNET1549. Gates: `VisitConstantEntryBody`/`VisitConstantRecordClause` parse arms; rows `constant-entry-2002` ACTIVE + `constant-record-2002` + `user-word-constant-2002`/`user-word-as-2002` interval rows. Goldens `constant_entry` (OCCURS bound + PIC repetition + VALUE 01/88 + subscript + arithmetic + relations + LENGTH OF + SR9 dup + concat-fold) / `constant_record` ENABLED + 9 negatives + legacy GreenfieldOnly. GLOBAL-phrase visibility into contained programs = recorded residue (parsed, not propagated))
- [x] Step 16 — TYPEDEF residue: EXTERNAL type / SAME AS / strong-group relations (net-new) (COMMIT) (2026-07-16 — **scout-vs-audit drift recorded first:** (a) the audit's "§13.18.57.4 GR5" citation for EXTERNAL typedefs was WRONG — GR5 is a Format-2 REPORT-GROUP rule; the real semantics are §13.18.22 SR1/SR5 + GR2/GR3/GR6 (a conformance surface + record-external attribution, NOT a cross-program type-identity model); (b) the "strong-group heterogeneous relations need wiring into CheckedRelational" claim was STALE — §8.8.4.2.3 SR1 (1533 `strong-compare-mismatch`) and SR4 (1535) were ALREADY live at the ONE relation checkpoint; (c) no `same-as`/`external-typedef` constructs.json rows pre-existed. **As built:** (1) **SAME AS §13.18.49** — `sameAsClause : SAME AS cobolWord ((OF|IN) cobolWord)*` on `dataDescriptionClause` (SAME + AS tokens both pre-existed; additive, LL-disjoint); `DataItem.SameAsName`/`SameAsQualifiers`; `ExpandSameAs` INSIDE the ONE `ExpandTypes` pass (after the TYPE loop, so targets copy their EXPANDED description), expanding via the SAME `CloneItem` (new `levelDelta` param = GR2b subordinate renumbering, may exceed 49 per GR2c; TYPE flows pass 0 — byte-stable) + the NEW shared `CopyEntryDescription` (also the §13.18.58.4 GR3 body for TYPE — template-root USAGE/SIGN/VALUE/JUST/BWZ now copy, a previously-silent GR3 gap; SYNC copies for SAME AS only — §13.18.57.4 GR1 excludes alignment, §13.18.49 GR1 does not); GR3/GR5 target-ancestor USAGE/SIGN applied to the subject; a copied TYPE identity carries strong typing (SameStrongType anchors, SR6 placement re-check). SRs → one code per family: **1555** subject-entry (§13.16.3 SR12 exclusion set; SR2 no subordinates/88s; SR8 77-elementary; SR9 no ancestor USAGE/SIGN), **1556** referenced-entry (SR7 elementary-or-level-1 [66 excluded]; SR5 no own OCCURS; SR1 not subject to OCCURS; SR10 no CONSTANT RECORD; SR6 file-section object-ref walk; unresolved/ambiguous), **1557** cycles (SR3 chain via the expanding-set + containment via the ancestor walk; SR4 rides the expanded-target walks + §13.18.57.3 SR1). (2) **EXTERNAL type declarations — the 1534 stage LIFTED:** `DataItem.IsExternalTypedef`/`HasExternalClause`/`ExternalFromType`; `ExpandType` enforces §13.18.22 GR2 (level-1 reference) + SR5 (strong-external pairing) = **1558** and marks GR3 records; `CallBindExternalAndGlobal` re-bases `ExternalFromType` roots onto the SAME run-unit ExternalStore cell as explicit EXTERNAL records (double-registration guarded). (3) **Strong-group relations:** SR4 reclassified from "staged residue" to the named spec rule (descriptor `strong-compare-ordering`, code 1535 byte-stable; the RENAMES-in-typedef stage split off as `typedef-renames-staged`); NEW staged leg 0899 `strong-group-ordering-signed-leaf` — ordering between same-type strong groups with a SIGNED numeric leaf needs §8.8.4.2.12 element-by-element ALGEBRAIC ordering the image comparison cannot honor (equality stays image-based — injective per fixed profile; unsigned/alnum orderings are image==element order). Goldens `typedef_same_as` (elementary+VALUE / group+qualified refs / NESTED renumbered copy / SAME AS+OCCURS / strong copy MOVE + = + <) and `typedef_external` (two programs, one ExternalStore cell, both directions) VERIFIED BY RUNNING then baked; negatives `same-as-at-85` (0900) + `strong-group-heterogeneous-compare` (1533); matrix row `same-as-clause-2002` ACTIVE (parse-arm `VisitSameAsClause`; EXTERNAL-typedef needs NO second row — TYPEDEF is unreachable below 2002 via `typedef-def-2002`); `SameAsTests` ×12 + `TypedefResidueTests` reworked (1534 tests → 1558 legs + the 0899 signed-ordering pin); legacy GreenfieldOnly ×2; characterization 33/33 byte-identical)
- [ ] Step 17 — Phase-end verification + catalog flip + STATUS=DONE (COMMIT)

---

### Step 1 — Reconciliation audit + `GreenfieldStatus` column

**Files to edit:** `docs/PHASE4_RECONCILIATION.md` (add/refresh a `GreenfieldStatus@P10` column and a per-track wave-sizing note), `docs/rearchitecture/PHASE-10-m2-residual-catalog.md` (this file — record the audit result in a table under this step).

**Change:** For EACH M2 non-OO track (UDF, DATA-3 national, DATA-4 boolean+ops, DATA-5 pointers, PROC-5 allocate, FILE-1 sharing/lock, FILE-2 line-seq, PROC-4 EC `-N` legs, ARITH-2 standard, RW-2002, concat, CONSTANT, TYPEDEF-residue), probe the **current** tree and classify each as one of: `LANDED-CONFIRMED-ON-NEW-SUBSTRATE`, `LANDED-NEEDS-RECONFIRM` (landed pre-rearch, must re-prove on `StorageForm`/`FileConnector`/`ManagedPointer`), `PARTIAL` (some legs land), `NOT-STARTED`, or `STAGED-LOUD` (recognized → named diagnostic). Cite file:line evidence for every verdict — do NOT trust the doc marks.

Probe commands (run these; record results):
- National/boolean substrate: `grep -rn "StorageForm\|CharImage\|National\|Boolean" src/Cobol.Net.Compiler/Binding/Model/ src/Cobol.Net.Runtime/Values/Text/` (paths per P5/P8 reorg; fall back to `Binding/`, `Runtime/Text/` if P8 folders not yet flipped).
- Pointers: `grep -rn "ManagedPointer\|StorageCell\|CellPointer\|ProgramPointer" src/Cobol.Net.Runtime/Control/`.
- Files: `grep -rn "FileConnector\|FileRegistry\|Sharing\|LockMode\|Retry\|LineSequential" src/Cobol.Net.Runtime/IO/ src/Cobol.Net.Compiler/Binding/Procedure/Verbs/FileLockBinder.cs`.
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

#### Step-1 AUDIT RESULT (2026-07-16 — 13 parallel read-only auditors, every verdict file:line-cited; the full evidence lists follow the table)

| Track | Verdict | Gaps | Wave sizing |
|---|---|---|---|
| UDF | PARTIAL | 3 | S — **Steps 9 + 10 BOTH LANDED 2026-07-16:** the category-carrying result channel (Step 9 — the 1510 reject is the per-shape `UdfReturningResidue` staging: float/boolean/pointer-class + the group residues), header BY VALUE formals end-to-end (Step 10 — grammar `usingParameter`/`usingByValue` → `LinkageFormal.ByValue` → GR5c `UdfArg` modes → the shared-ABI `CobolArgAdapt.NumValue`/`TextValue` detached cells; SR2=1553, SR10=1554, `pd-header-by-value-2002` row), and per-evaluation activation (Step 10 — `BoundUdfEvaluated` IIFE windows; 1509 NARROWED to VARYING BY / AFTER-level FROM / EVALUATE subjects). The invocation core, recursion (§8.6.6 — verified), prototypes, and EXIT FUNCTION ride the new substrate with enabled goldens. **Step 10a LANDED 2026-07-16:** the RECURSIVE WS-static data model (§13.5.4 GR1/§14.6.2.3.2/.3 — static WS on the ONE `StaticRootFields` channel + program/function LOCAL-STORAGE binding + the `__ResetStatics` CANCEL hook; golden `recursive_ws`). Remaining: the per-shape 1510 RETURNING residues, OPTIONAL/OMITTED formals (0899), the narrowed 1509 operand shapes, and the Step-10a stages (0899 `recursive-contained-working-storage` / `recursive-working-storage-pointer-backed`). |
| DATA-3-national | PARTIAL | 3 | S — **LANDED 2026-07-16 (Step 5): DISPLAY-OF/NATIONAL-OF are Runtime rows** (argument-2 turned out to be a substitution character per the 2023 §15.26.3 r2/§15.66.3 r2 text, not a codeset name — both forms fully implemented, no staged deferral); the SR12 national-form numeric/boolean leg stays staged-loud as the separately catalogued residual. **Step 4 LANDED 2026-07-17: the ALPHABET-national collating surface** (two-branch §12.3.7.2 grammar, sparse `NationalCollatingTable`/`__COLLATE_NAT`, PCS/SORT-MERGE FOR forms, national relations/88s/CHAR-NATIONAL/ORD/HIGH-LOW-VALUE; UCS-4≡native per §8.5.1.4; UTF-8/UTF-16 coded-set-only per Table 6). |
| DATA-4-boolean | PARTIAL | 3 | M — the wave must land the four 2023 B-SHIFT operators end-to-end (lexer tokens + a shift tier in the §8.8.2 booleanExpression grammar with an arithmetic shift-count operand, a new BoundBoolShift leaf through the generated visitor, CobolBool.ShiftL/LC/R/RC, a 2023-only VersionConformancePass gate row, an enabled 2023 golden + a below-2023 negative), plus the small BX\"…\" lexer leg and the national-form boolean representation, and fix the stale CobolExpressions.g4:145-150 staged-residue comment; the entire 2002 core (data, literals, four operators, conditions, COMPUTE F2) needs no rework — it already rides StorageForm/Place/CobolBool. |
| DATA-5-pointers | PARTIAL | 4 | M — the P10 wave must land USAGE PROGRAM-POINTER end-to-end (lexer token + usageClause alternative + a PicCategory/StorageForm.PointerRef-style carrier resolving through the existing RunUnit ProgramTable for SET … TO ENTRY, with a 2002 ConstructRegistry introduction row and an enabled golden), and unstage the two loud residues (class-unit BASED/ADDRESS OF cell emission; qualified/subscripted ADDRESS OF operands) on the already-proven ManagedPointer/StorageCell substrate. |
| PROC-5-allocate | PARTIAL | 2 | S — **LANDED 2026-07-16 (the Step-6 slice):** the §14.9.3 GR7 lowering (ALLOCATE based-item INITIALIZED → the allocation + EXACTLY the spec's `INITIALIZE data-name-1 WITH FILLER ALL TO VALUE THEN TO DEFAULT` expansion via `InitializeBinder.BindAllocateInitialized`, carried as a `BoundSequence` — the ONE INITIALIZE mechanism; the PtrBinder BoundUnsupported stage DELETED) + the ENABLED `2002/allocate_initialized` golden byte-verifying GR7 (VALUE members / numeric ZERO / edited zero / WITH-FILLER spaces), GR6 zero-fill (LOW-VALUE witness), and GR4b form-2 RETURNING; legacy GreenfieldOnly exclusion (legacy only zero-fills). Both gaps CLOSED. |
| FILE-1-sharing-lock | PARTIAL | 6 | M — **the sharing/record-lock wave LANDED 2026-07-16 (Step 8):** sequential connectors got the ordinal record identity (polymorphic on FileConnector — the CurrentRecordId switch deleted), the 51 conflict check + lock discipline landed on WRITE/REWRITE/DELETE via the governed registry entries (WriteShared/RewriteShared/DeleteShared; sequential READ via ReadShared with the GR10a FPI-unchanged pre-check + GR22 skip-scan), and RETRY binds+emits on READ/WRITE/REWRITE/DELETE/DELETE FILE (START carries NO retry/lock phrase per §14.9.41 — the audit anchor misread the DELETE FILE grammar site); goldens file_sharing_seq + file_sharing_mutate. REMAINING (ledgered): cross-run-unit sharing (§9.1.15), timed RETRY SECONDS/FOREVER (deadlock-bail 52 residue), COMMIT/ROLLBACK lock release, the keyed post-read GR10a FPI residue + unemitted keyed ADVANCING ON LOCK. |
| FILE-2-line-seq | PARTIAL | 6 | M — **the 62 + sequential-5x legs LANDED 2026-07-16 (Step 8):** DeleteFile produces 62 via OpenByAnotherConnector under the GR15 RETRY discipline (golden 2023/delete_file_sharing; the audit's "line-seq 62" framing was a mischaracterization — that condition is status 71, §9.1.13.10), and 51/52/53/54 all fire on SequentialConnector via the ordinal identity. REMAINING: the 06/09/71 line-sequential status protocol, the §14.9.35 line-seq REWRITE leg, and the LINE SEQUENTIAL edition gate (VersionConformancePass arm + version-matrix row) while reconciling the 2002-enabled oo_object_report golden. |
| PROC-4-ec-n | STAGED-LOUD | 6 | M — **LANDED 2026-07-16 (Step 11):** all four Deferred rows are Runtime (NATIONAL-OF via Step 5; EXCEPTION-FILE-N/EXCEPTION-LOCATION-N/CHAR-NATIONAL via Step 11 — `EcFunctions.FileN/LocationN` on the ONE `NationalOf` translator, `CharNational` on the native national PCS, + ORD-over-national §15.70.4 r2); the golden (`exception_file_n`, `char_national`) + `exception-file-n-2002` matrix row + `--std 85` negative shipped in the same change. Remaining gap = ONLY the 2023 file-connector-argument form (loud; VCR rows 68/69, PHASE-13 Step 9). |
| ARITH-2-standard | PARTIAL | 6 | M — the phase doc's planned net-new 'OptionsModel.ArithmeticMode consumed by the numeric renderer' build (Step 12, catalog line 76) is ALREADY LANDED and golden-pinned on the post-P7 NumericRenderer/CobolDec substrate, so the P10 wave shrinks to closing the residual spec legs: SDIDI exponentiation (§8.8.1.5.4), routing the §15.4.1 intrinsic set and the RW SUM clause through the mode, the decimal128-range size ECs (§8.8.1.5.2 r2), resolving the 2002-vs-2014 introduction edge against the 2002 standard (then either an arithmetic-standard-2002 registry row + 2002 golden + negative .err at --std 85, or a corrected catalog row), and rewriting the stale Step 12 text from 'consume the mode' to 'close the residual legs'. **Step 12 LANDED 2026-07-16 — all six gaps dispositioned (Pow §8.8.1.5.4 + range ECs §8.8.1.5.2 r2 + float→SDIDI §8.8.1.5.1 implemented; MEAN in SDIDI + exact-family/SUM documented equivalence; the four inexact-EAE intrinsics staged 0899; the introduction edge resolved to 2002 with the OPTIONS gate restructure); see the Step-12 as-built + the annotated gap list below.** |
| RW-2002 | NOT-STARTED | 7 | L — the wave must add the PRESENT lexer token plus six 2002 grammar formats (PRESENT WHEN, VARYING, OCCURS+STEP, multi-COLUMN with alignment, multi-LINE, expression/ROUNDED SOURCE), extend the ReportModel/DataBinder.Reports with presence conditions and varying counters, evaluate them at presentation time in the new-substrate Cobol.Net.Runtime/IO/ReportWriter, and ship 85-edition 0900 gates, matrix rows, and per-edition conformance goldens in the same change set. |
| CONCAT | NOT-STARTED | 6 | S — **LANDED 2026-07-16 (Step 14):** the & token + `concatenationExpression` tier inside `nonNumericLiteral` (every literal position), the bind-time fold per §8.8.3.3 GR3 (`ConcatFolder`; class rules incl. boolean → 1540/1541/1545), the concat-operator-2002 gate in the VersionConformancePass parse arm (0900 below 2002), the ONE `literal_concat` golden (subsumes the planned concat_literal/concat_boolean pair — the boolean leg is in it) + `concat_below_2002`/`concat_class_mismatch` negatives, row flipped ACTIVE. |
| CONSTANT | NOT-STARTED | 6 | M — the P10 wave must add the CONSTANT token and §13.10 grammar alternative, bind constant-names to folded compile-time values substituted wherever literals are legal (including data-division positions), implement §13.18.15 CONSTANT RECORD with immutability enforcement, wire the constant-entry-2002 registry Check so pre-2002 use fires COBOLNET0900 instead of a generic parse error, and activate the pending matrix row with conformance goldens. |
| TYPEDEF-residue | PARTIAL | 7 | M — the wave must implement SAME AS (§13.18.49) end-to-end (grammar rule + binder inline-expansion reusing the ExpandTypes clone machinery + SR checks + edition gate + goldens), un-stage COBOLNET1534 by modeling run-unit-shared EXTERNAL types with cross-source-unit same-type equivalence in SameStrongType, and un-stage the two mechanical residues (carry Renames66 through CloneItem per §13.18.58.4 GR1; per-reference INDEXED-BY index-name uniquing per §13.18.38), converting the SR4 rejection to its named check and adding 2014/2023 continuity goldens. |

**UDF — user-defined functions (FUNCTION-ID)** — `PARTIAL`

Evidence:
- src/Cobol.Net.Compiler/Binding/Procedure/Verbs/UdfBinder.cs:85 — COBOLNET1510 still a live staged reject: only elementary fixed-point numeric RETURNING implemented (guard at line 83)
- tests/Cobol.Net.Tests.Conformance/UdfInvocationTests.cs:388-418 — StagedReturningCategories_1510 asserts the reject (PIC X and group RETURNING both fail loud)
- src/Cobol.Net.Compiler/Binding/Procedure/Verbs/UdfBinder.cs:150-151 — invocation lowers onto BoundCallProgram{IsFunction=true} + caller-side __FNRES temp (new bound-tree substrate, P7 Step 10k collaborator)
- src/Cobol.Net.Compiler/Binding/BinderDriver.cs:187 — implicit recursive attribute for every FUNCTION-ID unit (§9.4 :12529)
- src/Cobol.Net.Runtime/Control/ProgramTable.cs:119-124 — Recursive ⇒ fresh per-activation instance via n.Factory (the P8 runtime-reorg substrate; RunUnit-owned ModuleStack push at line 131)
- tests/conformance/2002/manifest.json:80-87 — all 8 udf_* goldens (udf_invocation, udf_inline_expression, udf_value_args, udf_nested_args, udf_recursion, udf_exit_function, udf_prototype, udf_keyword_omitted) in the ENABLED array
- tests/Cobol.Net.Tests.Conformance/CorpusRunnerTests.cs:27-30 — the greenfield CorpusRunner consumes manifest.json enabled entries (compile + byte-compare .out at --std 2002)
- tests/conformance/2002/udf_recursion.cob:30 — self-recursive factorial through five nested activations proves per-activation LINKAGE/temp data
- src/Cobol.Net.Frontend/Grammar/CobolParserCore.g4:406-408 — usingClause is 'USING dataReferenceList' only; NO BY VALUE/BY REFERENCE alternative in the PD header
- src/Cobol.Net.Compiler/Binding/DataBinder.Linkage.cs:185 — 'the header BY VALUE phrase is not yet parsed'
- src/Cobol.Net.Compiler/Binding/Procedure/Verbs/UdfBinder.cs:158-166 — 'Header BY VALUE formals (GR5c) are not modeled for functions'; UdfArg maps only Reference/Content
- src/Cobol.Net.Compiler/Binding/Procedure/Verbs/UdfBinder.cs:103,194,235 — COBOLNET1506 (OMITTED args) and COBOLNET1509 (conditionally-evaluated positions) staged loud

Gaps:
- ~~category-carrying RETURNING (group / alphanumeric / edited / float result channel; currently staged COBOLNET1510, elementary fixed-point numeric only)~~ — **CLOSED 2026-07-16 (Step 9)** for the categories the channel now carries: alphanumeric/alphabetic, numeric-edited, national, and character-form groups (§8.4.3.2.4 GR1 — the temp clones the full description; §14.2.2 SR5 places no category restriction). Still 1510 by name (per-shape texts in `UdfBinder.UdfReturningResidue`): FLOAT (no CALL-boundary float write half), BOOLEAN (no §8.8.2 function-result arm), pointer/object/index, strong-typed/REDEFINES-bearing/variable-length/binary-leaf groups — ISO §8.4.3.2.4 GR1 / §14.8.3
- ~~BY VALUE formal parameters in the FUNCTION-ID PROCEDURE DIVISION USING header (grammar-absent — raw parse error, no named diagnostic)~~ — **CLOSED 2026-07-16 (Step 10)**: the §14.2.2 using-phrase parses per-parameter, `LinkageFormal.ByValue` + the §14.2.3 GR4 transitivity thread, SR2 = COBOLNET1553 (+ 0899 `by-value-formal-carrier` for the uncarried SR2-legal classes), the callee-side GR10 detached-cell value copy on the shared ABI (`CobolArgAdapt.NumValue`/`TextValue`, copy-out skipped) for BOTH CALL and UDF activation, GR5c argument modes + SR10 = COBOLNET1554, row `pd-header-by-value-2002`, golden `udf_by_value`, negatives 1553/1554 — ISO §14.2.2 / §14.2.3 GR10 / §8.4.3.2.4 GR5c
- OPTIONAL formals / OMITTED arguments for function activation (OMITTED args staged COBOLNET1506; the header OPTIONAL phrase now PARSES and stages 0899 `optional-formal`, Step 10) — ISO §14.8.2 / §14.2.3 GR3
- ~~per-evaluation activation for conditionally-evaluated reference positions: PERFORM UNTIL/VARYING, SEARCH WHEN, EVALUATE selection, non-first AND/OR operands (staged COBOLNET1509)~~ — **CLOSED 2026-07-16 (Step 10)** for every CONDITION window (`BoundUdfEvaluated` per-evaluation attach + IIFE render; golden `udf_per_eval` proves the runtime cardinality with an EXTERNAL activation counter; the Step-1 "same function twice in one statement hoisted once" claim was stale — each occurrence always activated separately per GR2). COBOLNET1509 remains ONLY for: a VARYING BY operand, an AFTER-level FROM operand, and an EVALUATE selection SUBJECT (the lowering re-binds subjects per WHEN while §14.9.13.4 GR3 evaluates them once) — ISO §8.4.3.2.4 GR1/GR6a / §8.8.4.13 r1–r2
- ~~the RECURSIVE per-activation-vs-static data-model deviation (§14.6.2.3.2/.3): WS should be last-used after the first activation; `Initial || Recursive ⇒ fresh instance per activation` re-initializes it — NOT taken by Step 10 (its goldens use EXTERNAL state instead)~~ — **CLOSED 2026-07-16 (Step 10a)**: a Recursive-and-not-Initial unit's (incl. every FUNCTION's) WS emits STATIC on the ONE `StaticRootFields` channel (§13.5.4 GR1 static data — one §14.6.2.3.3 last-used copy; INDEXED BY cells + Tier-B backings ride along), the fresh-per-activation instance keeps carrying LOCAL-STORAGE (now BOUND at program/function level — §13.6.4 GR1 automatic data; a cached singleton re-initializes LS at `Call` entry) + formals + PERFORM state, and `__ResetStatics` registers with `ProgramTable` for the §14.6.2.3.2 initial-state triggers (run-unit start / CANCEL §14.9.5 GR3 / INITIAL-container cascade). Honest-subset stages: 0899 `recursive-contained-working-storage`, 0899 `recursive-working-storage-pointer-backed`. Golden `recursive_ws`; matrix row `local-storage-section-2002` — ISO §13.5.4 / §13.6.4 / §14.6.2.3.2/.3 / §8.6.6 / §9.4

**DATA-3-national** — `PARTIAL`

Evidence:
- src/Cobol.Net.Compiler/Binding/Passes/StorageFormPass.cs:202 — national leaf classified to StorageForm.CharImage(item.ImageWidth, pic.Category)
- src/Cobol.Net.Compiler/Binding/Model/StorageForm.cs:49 — CharImage(int Width, PicCategory Category), ImageWidth => Width
- src/Cobol.Net.Compiler/Binding/Model/DataItem.cs:293 — ElementaryImageWidth = pic.Length for national (one UTF-16 char/position); :237-241 states 'never byte-doubled for national'
- src/Cobol.Net.Compiler/Binding/Passes/BindPipeline.cs:66 — StorageFormPass wired as the UsageCollected→StorageComputed pass
- src/Cobol.Net.Compiler/CodeGen/Verbs/MoveEmitter.cs:334-337 — national receiver: StrStore/StrStoreJustified left-justify, national-space pad, right-truncate (§14.6.8.5), A→N/9→N via D-N4
- src/Cobol.Net.Compiler/Binding/Procedure/Verbs/MoveBinder.cs:213-228 — §14.9.25.3 SR10 Table-16 national sender/receiver legality (COBOLNET0819)
- src/Cobol.Net.Compiler/CodeGen/Emit/ConditionRenderer.cs — national relations order under the NATIONAL sequence (the D-N3 ordinal identity, or `__COLLATE_NAT` under an explicit ALPHABET … FOR NATIONAL — Step 4); the ALPHANUMERIC PCS weight table deliberately excluded
- src/Cobol.Net.Runtime/Values/Text/CobolString.cs:78-79 — national space pad in the runtime compare substrate
- src/Cobol.Net.Compiler/CodeGen/DataDivision/ValueInitializer.cs:82-85 — national VALUE stores via StrStore (§13.18.63 SR5)
- src/Cobol.Net.Compiler/Binding/DataBinder.cs:1850-1855 — REDEFINES-over-national reject (tier Rejected, D-N2)
- src/Cobol.Net.Compiler/Binding/DataBinder.cs:1873-1883 — GateNationalRecords: FD/SD record national leaf → DiagnosticCatalog.NationalData error; wired at BindPipeline.cs:46
- src/Cobol.Net.Compiler/Binding/DataBinder.Linkage.cs:332-343 — EXTERNAL cell-backing refuses national leaves (RESIDUE-11); loud ExternalRecordNotCellBacked at :305
- src/Cobol.Net.Compiler/Binding/Procedure/Verbs/SortBinder.cs:144-146 — table-SORT national key → DiagnosticCatalog.NationalData (§14.9.40 GR5b); file-sort keys covered by the FD gate
- src/Cobol.Net.Editions/Diagnostics/DiagnosticCatalog.cs:87-90 — the named diagnostic: NationalData, code 'national-data', RecognizedNotImplemented band
- tests/conformance/2002/manifest.json:26 — 'national_data' in the ENABLED array; run+byte-compared by tests/Cobol.Net.Tests.Conformance/CorpusRunnerTests.cs:61-80 through the greenfield CompilerDriver
- tests/conformance/2002/national_data.cob:27-64 — exercises N-literal MOVE, truncate, national-space pad, VALUE N"OK", A→N, 9→N, SPACE/INITIALIZE fill, =/< compares; .out byte-exact
- tests/conformance/negative/manifest.json:32-33 — enabled negatives move-binary-to-national, move-national-to-an (the Table-16 prohibitions)
- src/Cobol.Net.Compiler/Binding/IntrinsicCatalog.cs:132-133 — DISPLAY-OF (§15.26) and NATIONAL-OF (§15.66) catalogued IntrinsicBind.Deferred
- src/Cobol.Net.Compiler/CodeGen/Emit/IntrinsicRenderer.cs:52-53 — a Deferred row renders EmitText.LoudValue('FUNCTION <name> (catalogued, not yet implemented)'), never a wrong value
- src/Cobol.Net.Compiler/Binding/PictureAnalyzer.cs:153-156,194-197 — national-form boolean/numeric PICTURE under USAGE NATIONAL staged loud via DiagnosticCatalog.NationalData (§13.18.60.3 SR12)
- src/CobolSharp.Runtime/Intrinsics/IntrinsicFunctions.cs:647-660 — DisplayOf/NationalOf exist ONLY in the legacy oracle runtime, not the new substrate

Gaps:
- ~~NATIONAL-OF intrinsic not implemented on the new substrate~~ — **CLOSED 2026-07-16 (Step 5)** — ISO §15.66
- ~~DISPLAY-OF intrinsic (the sanctioned national→alphanumeric narrowing) not implemented on the new substrate~~ — **CLOSED 2026-07-16 (Step 5)** — ISO §15.26
- national-form numeric and boolean data (numeric/boolean PICTURE with USAGE NATIONAL) staged loud in PictureAnalyzer, not implemented — ISO §13.18.60.3 SR12

**DATA-4-boolean** — `PARTIAL`

Evidence:
- src/Cobol.Net.Compiler/Binding/PictureAnalyzer.cs:140-165 — PIC 1 → PicCategory.Boolean; USAGE DISPLAY and BIT both accepted (SR5/SR13b, identical D-B1 string storage); non-boolean usages rejected COBOLNET0881
- src/Cobol.Net.Compiler/Binding/Passes/StorageFormPass.cs:202-203 — PicCategory.Boolean → StorageForm.CharImage: boolean data rides the NEW StorageForm data model (Binding/Model)
- src/Cobol.Net.Frontend/Grammar/Core/CobolLexer.g4:615,660 — BOOLLIT B"…" boolean literal token; :659 states BX"…" (hex) is deferred
- src/Cobol.Net.Frontend/Grammar/Core/CobolExpressions.g4:111,132-138 — booleanExpression precedence tiers (B-NOT > B-AND > B-XOR > B-OR, §8.8.2 rule 7b) + the boolExprAhead()-gated condition entry; src/Cobol.Net.Frontend/Grammar/CobolParserCore.g4:804 — COMPUTE Format 2 boolean alt (§14.9.8)
- src/Cobol.Net.Compiler/Binding/Bound/BoundTree.cs:237-263,282,387 — the BoundBoolExpr family (BoundBoolLiteral/Ref/All/Binary/Not/Error), BoundBooleanCondition, BoundComputeBoolean — a dedicated bound-tree value channel
- src/Cobol.Net.Compiler/Binding/Procedure/Verbs/ConditionBinder.cs:38,185,209 + ArithmeticBinder.cs:165,197 — BindBoolExpr / BindPrimaryBoolean / BoundComputeBoolean binding; ExpressionBinder.cs:74 — BooleanLiteralOperand
- src/Cobol.Net.Compiler/CodeGen/Emit/BooleanRenderer.cs:14-49 — renders via the generated exhaustive IBoundBoolExprVisitor (PHASE-07 Step 6 substrate) over structural Place (PlaceRenderer.Read, line 25); ConditionRenderer.cs:41,82 — CobolBool.IsTrue/Equal; ArithmeticEmitter.cs:122 — EmitComputeBoolean; RuntimeApi.cs:24-41 — nameof-anchored CobolBool calls
- src/Cobol.Net.Runtime/Values/Text/CobolBool.cs:23-113 — typed-native runtime (And/Or/Xor/Not/Equal/IsTrue/Resize + …All figurative forms) with §8.8.2 rules 9/10 length semantics; NO byte substrate
- src/Cobol.Net.Compiler/Validation/VersionConformancePass.cs:269,1101,1110 — BooleanData2002 + BooleanOperators2002 fire through the post-Step-E single gating funnel
- tests/conformance/2002/manifest.json:9-10 — boolean_data AND boolean_ops ENABLED (CorpusRunnerTests compiles+runs+byte-compares at --std 2002 strict); goldens cover MOVE/VALUE/JUSTIFIED/INITIALIZE/comparison (boolean_data.cob:19-49) and all four operators + ALL B"…" + boolean relation + simple boolean condition (boolean_ops.cob:21-49)
- tests/conformance/negative/manifest.json:13-14 — boolean-ordering-relation + boolean_operators_below_2002 enabled (must-reject witnesses for the edition gate and the equality-only relation rule)
- src/Cobol.Net.Editions/ReservedWords.Table.cs:46-49 — B-SHIFT-L/-LC/-R/-RC exist ONLY as 2023 reserved-word rows; grep of src/ finds no lexer token, no grammar tier, no bound node, no CobolBool method, no golden for any shift operator
- src/Cobol.Net.Compiler/Binding/PictureAnalyzer.cs:150-157 — national-form boolean (PIC 1 USAGE NATIONAL, §13.18.60.3 SR12) staged LOUD as an error before recovery to Display
- DOC DRIFT: src/Cobol.Net.Frontend/Grammar/Core/CobolExpressions.g4:145-150 still claims the boolean relation/condition forms are 'STAGED RESIDUE … NOT yet supported', contradicted by line 111 and the ENABLED boolean_ops golden exercising IF A B-AND B = B"0100" (boolean_ops.cob:36-49)

Gaps:
- B-SHIFT-L / B-SHIFT-LC / B-SHIFT-R / B-SHIFT-RC boolean shift operators (2023): reserved-word-only — no lexer operator token, no grammar tier, no BoundBool node, no CobolBool runtime method, no golden — ISO §8.7.2 / §8.8.2 (Annex E.2 item 25)
- BX"…" hexadecimal boolean literal: explicitly deferred at the lexer (CobolLexer.g4:659) — ISO §8.3.3.4
- national-form boolean data (PIC 1 with USAGE NATIONAL): recognized but staged as a loud not-implemented error, recovers to Display (PictureAnalyzer.cs:150-157) — ISO §13.18.60.3 SR12

**DATA-5-pointers** — `PARTIAL`

Evidence:
- src/Cobol.Net.Compiler/Binding/Model/PicInfo.cs:40,89,193-194,229,261 — PicCategory.Pointer / Usage.Pointer / PicInfo.PointerItem; CLR type 'ManagedPointer', init 'ManagedPointer.Null' (§8.4.3.10)
- src/Cobol.Net.Compiler/Binding/Passes/StorageFormPass.cs:198,234 — PicCategory.Pointer maps onto the NEW StorageForm data model as StorageForm.PointerRef ('ManagedPointer') — the substrate-riding pass/type
- src/Cobol.Net.Compiler/Binding/DataBinder.Ptr.cs:51-91 — PtrBindBasedAndAddressables: BASED roots get the ManagedPointer addr field + CobolPtr.Deref bridge (§13.18.5 GR2-4); ADDRESS-OF targets are forced onto per-instance StorageCells
- src/Cobol.Net.Compiler/Binding/Procedure/Verbs/SetBinder.cs:54-91,110-111,130 — BindSetPointer (F4, 0869 band) + the F10 pointer-arithmetic reroute host.Ptr.TryBindSetUpDown
- src/Cobol.Net.Compiler/Binding/Procedure/Verbs/PtrBinder.cs:26-52 — SET ADDRESS OF both directions (§14.9.39 F7 SR17/SR18)
- src/Cobol.Net.Compiler/CodeGen/Verbs/PtrEmitter.cs:26-47 (AddressOfText §8.4.3.11), 67-79 (F10 UP/DOWN BY GR18-20), 85-107 (ALLOCATE §14.9.3 GR1/2/4b/6), 112-138 (FREE §14.9.15 GR1 + EC-STORAGE-NOT-ALLOC)
- src/Cobol.Net.Compiler/CodeGen/Verbs/SetEmitter.cs:35-37 — EmitSetPointer renders ManagedPointer.Null / pointer copy
- src/Cobol.Net.Compiler/CodeGen/Emit/ConditionRenderer.cs:63-71 — NULL/pointer [NOT] EQUAL via ManagedPointer.SameTarget (§8.8.4.1.3 structural equality)
- src/Cobol.Net.Runtime/Control/ManagedPointer.cs:10-80 + CellPointer.cs:10-18 + CobolPtr.cs:24-106 — the ONE carrier + StorageCell window + Deref/UpBy/UpByScaled/Allocate/Free runtime (EC-DATA-PTR-NULL, EC-BOUND-PTR, EC-SIZE-ADDRESS loud)
- src/Cobol.Net.Runtime/Control/ExternalStore.cs:13 — ExternalStore.Cell forwards to RunUnit.Current.External (the ADDRESS-OF-EXTERNAL leg rides RunUnit instance state; PtrEmitter.cs:43-45 emits it)
- tests/conformance/2002/manifest.json:7,67-69 + :89 ('pending': []) — based_pointer, pointer_alloc, pointer_arith, pointer_data ALL in the ENABLED list (runnable goldens with sibling .out files)
- tests/Cobol.Net.Tests.Conformance/PointerDataTests.cs:35-66 + PointerAddressingTests.cs:31-122 + tests/Cobol.Net.Tests.Unit/CobolPtrTests.cs — the edition gate (0900 at 85), the 0869 band, and the review-caught legs locked
- src/Cobol.Net.Editions/ConstructRegistry.g.cs:23,65,98 — based-clause-2002 / usage-pointer-2002 / set-address-2002 introduction-gate rows (VersionConformancePass funnel)
- PROGRAM-POINTER absence: src/Cobol.Net.Editions/ReservedWords.Table.cs:338 is a reserved-word row ONLY; src/Cobol.Net.Frontend/Grammar/Core/CobolData.g4:456 is a COMMENT; usageClause (CobolData.g4:335-336) has POINTER but NO PROGRAM-POINTER alternative, no lexer token, zero binder/emitter/runtime hits (grep 'ProgramPointer|PROGRAM_POINTER' over src/Cobol.Net.* = empty)

Gaps:
- USAGE PROGRAM-POINTER (program-pointer data items — declaration, SET pointer TO ENTRY, program-address comparison): grep-empty beyond a reserved-word row and a grammar comment; a source using it is a raw parse error, not a named diagnostic — ISO §13.18.60 GR24 (+ §14.9.39 SET ENTRY formats)
- USAGE FUNCTION-POINTER (the sibling §13.18.60 phrase named in the same grammar comment) equally absent — ISO §13.18.60
- BASED data / ADDRESS OF inside a class definition's data divisions: staged LOUD via DiagnosticCatalog.OoBasedInClass (DataBinder.Ptr.cs:57-63) — the OO cell/bridge emission is a named residue — ISO §13.18.5 / §8.4.3.11
- Qualified or subscripted ADDRESS OF operands: staged LOUD COBOLNET0869 (PtrBinder.cs:65-67, 'a named increment residue') — ISO §8.4.3.11

**PROC-5-allocate** — `PARTIAL`

Evidence:
- src/Cobol.Net.Compiler/Binding/Procedure/Verbs/PtrBinder.cs:96-131 — BindAllocate: SR3 RETURNING pointer-category check (:104-109), SR2 CHARACTERS-requires-RETURNING rejection COBOLNET0869 (:114-120), Form-1 bind with INITIALIZED flag (:121), Form-2 based bind (:125-130)
- src/Cobol.Net.Compiler/Binding/Procedure/Verbs/PtrBinder.cs:127-129 — ALLOCATE based-item INITIALIZED binds to BoundUnsupported ('the §14.9.3 GR7 INITIALIZE lowering — a named increment residue')
- src/Cobol.Net.Compiler/CodeGen/StatementEmitter.cs:126 + src/Cobol.Net.Compiler/CodeGen/Emit/EmitCore.cs:65 — BoundUnsupported renders LoudStmt = NotImplemented.Run(feature): the staged-loud channel is a RUNTIME throw, not a compile diagnostic code
- src/Cobol.Net.Compiler/Binding/Procedure/Verbs/PtrBinder.cs:135-147 — BindFree, §14.9.15 SR1 data-pointer-only operands (0869)
- src/Cobol.Net.Compiler/Binding/Procedure/Verbs/PtrBinder.cs:187-196 — PtrResolveBased: §14.9.3 SR1 / §14.9.39 SR18 BASED-01/77 check, COBOLNET0869
- src/Cobol.Net.Compiler/CodeGen/Verbs/PtrEmitter.cs:85-107 — EmitAllocate: Form-2 GR3/GR4a via the implicit BasedPointerField (:96), GR4b RETURNING delivery (:97-98), GR1 fractional round-UP rescale (:101-104), GR6 zeroFill wiring (:105-106)
- src/Cobol.Net.Compiler/CodeGen/Verbs/PtrEmitter.cs:112-138 — EmitFree: GR1 three-way per operand, nonfatal EC-STORAGE-NOT-ALLOC through the TurnState-gated block (§14.6.13.1.4)
- src/Cobol.Net.Runtime/Control/CobolPtr.cs:81-86 — Allocate: GR1 size, GR2 <=0 -> NULL no EC, GR6 '\0' zero-fill; :94-106 — Free: GR1a release+null / GR1b NULL no-op / GR1c notAlloc out-flag; :24-40 — Deref GR3 EC-DATA-PTR-NULL / GR4 EC-BOUND-PTR incl. freed-cell dangling alias
- src/Cobol.Net.Runtime/Control/ — ManagedPointer.cs, CellPointer.cs, StorageCell.cs, RunUnit.cs all present: the post-P8 Runtime/Control substrate; wired via src/Cobol.Net.Compiler/CodeGen/Roslyn/RuntimeApi.cs:202-207 (PtrAllocate/PtrFree -> CobolPtr.Allocate/Free)
- src/Cobol.Net.Compiler/Binding/Model/DataItem.cs:177 (IsBased) + src/Cobol.Net.Compiler/Binding/Model/RedefinesModel.cs:86 (BasedPointerField) — the BASED interplay lives in the StorageForm data model (Binding/Model/)
- src/Cobol.Net.Compiler/Validation/VersionConformancePass.cs:818-819 — VisitAllocateStatement recognition-arm edition gate; :794-796 — the FREE gate; registry rows tests/version-matrix/constructs.json:52-71 (allocate-2002 / free-2002, status active, diagnosticCode COBOLNET0900)
- tests/conformance/2002/manifest.json:67 — pointer_alloc ENABLED; tests/conformance/2002/pointer_alloc.cob:14-24 exercises ALLOCATE B (Form 2), ALLOCATE 5 CHARACTERS RETURNING P (Form 1), SET ADDRESS OF, FREE; pointer_alloc.out:1-3 (B=HELLO/B2=WORLD/FREED=YES) byte-compared by CorpusRunnerTests.EnabledProgram_CompilesStrict_AndMatchesOutIfPresent (tests/Cobol.Net.Tests.Conformance/CorpusRunnerTests.cs:61-88)
- tests/conformance/negative/manifest.json:4-5,25 — allocate-chars-no-returning, allocate-non-based, free-non-pointer all ENABLED; asserted per-edition by CorpusRunnerTests.EnabledNegativeCase_RejectsWithItsDiagnostic (tests/Cobol.Net.Tests.Conformance/CorpusRunnerTests.cs:97-115); .err substrings match the binder texts (allocate-chars-no-returning.err:1 <-> PtrBinder.cs:117; allocate-non-based.err:1 <-> PtrBinder.cs:193)
- tests/Cobol.Net.Tests.Conformance/PointerAddressingTests.cs:74-84 — compile locks for the goldens' unreached legs: 2.5 CHARACTERS (GR1 round-up), 8 CHARACTERS INITIALIZED (GR6), ALLOCATE B RETURNING P (GR4b), 0 CHARACTERS -> NULL (GR2)
- tests/Cobol.Net.Tests.Unit/CobolPtrTests.cs:72-105 — Allocate size rules + zero-fill and Free three-way + dangling-alias-loud unit coverage
- Note: no positive golden is literally named allocate_* — the enabled runtime witness for this track is 2002/pointer_alloc (the PHASE-10 catalog row docs/rearchitecture/PHASE-10-m2-residual-catalog.md:514 names it)

Gaps:
- ~~ALLOCATE based-item INITIALIZED: the GR7 lowering (INITIALIZE ... WITH FILLER ALL TO VALUE THEN TO DEFAULT) is staged, not landed~~ — **CLOSED 2026-07-16**: `PtrBinder.BindAllocate` now lowers the form to `BoundSequence([BoundAllocate, InitializeBinder.BindAllocateInitialized(basedRef)])` — the spec's exact statement bound through the ONE INITIALIZE expansion (WITH FILLER + bare-ALL TO VALUE + THEN TO DEFAULT), sequenced after the allocation sets the implicit pointer (GR4a); the BoundUnsupported stage is deleted — ISO §14.9.3 GR7
- ~~No ENABLED runtime golden observes GR6 zero-fill or GR4b form-2 RETURNING end-to-end~~ — **CLOSED 2026-07-16**: `2002/allocate_initialized` ENABLED (byte-compared .out) — GR7 over a mixed BASED group (VALUE-carrying X + 9(3), FILLER, defaulted 9(4)/X(5), numeric-edited Z9 → the EDITED zero) + an elementary based item, GR4b (the RETURNING pointer windows the same initialized storage through a second based item), GR6 (6 CHARACTERS INITIALIZED = LOW-VALUE), FREE-to-NULL; edition-checked (85 rejects via the allocate-2002/based gates; 2023 byte-identical) — ISO §14.9.3 GR6/GR4b

**FILE-1-sharing-lock** — `PARTIAL`

Evidence:
- src/Cobol.Net.Runtime/IO/Sharing/PhysicalFileTable.cs:12 — the per-host sharing/record-lock table (P8-substrate type; GR7 ceilings 53/54 at :24-26, LockRecord 51/GR8 at :46-59)
- src/Cobol.Net.Runtime/IO/FileRegistry.cs:34 — PhysicalFileTable owned by the polymorphic run-unit FileRegistry instance (the P8 Step-5 registry)
- src/Cobol.Net.Runtime/IO/CobolFile.cs:17 — `private static FileRegistry _reg => RunUnit.Current.Files;` — the emitted facade is a pure delegator onto the RunUnit-owned instance (new substrate proof)
- src/Cobol.Net.Runtime/IO/FileRegistry.cs:354 RegisterSharing · :364-374 OpenShared (RETRY loop + 61) · :378-396 SharedOpenAttempt · :399-405 Table-19 Conflicts (§9.1.13.9 a-e) · :409-444 ReadLockGovern (WITH/NO/IGNORING LOCK, AUTOMATIC GR4, GR6 single-lock) · :448-455 Unlock (§14.9.47 GR1, 42-if-not-open) · :483-498 RetryLoop (§14.7.9)
- src/Cobol.Net.Runtime/IO/FileRegistry.cs:140-146 — CLOSE deregisters the sharing-active connector and releases its record locks (§9.1.16 :11754)
- src/Cobol.Net.Compiler/CodeGen/Verbs/SequentialIoEmitter.cs:95-112 (RegisterSharing emit) · :131-160 (OpenShared emit on SHARING/RETRY OPEN) · :207-213 (EmitUnlock)
- src/Cobol.Net.Compiler/CodeGen/Verbs/KeyedIoEmitter.cs:124 — ReadLockGovern emitted after every keyed READ
- src/Cobol.Net.Compiler/Binding/Model/FileModel.cs:140-146 — SharingMode (§12.4.5.15) + LockModeInfo (§12.4.5.9) on the unified data model
- src/Cobol.Net.Compiler/Binding/Procedure/Verbs/FileLockBinder.cs:27-38 (BindUnlock → BoundUnlock, BoundTree.cs:558); KeyedIoBinder.cs:88-93 (READ Lock/Retry/AdvancingOnLock); SequentialIoBinder.cs:25-46 (OPEN SharingOverride+Retry)
- src/Cobol.Net.Compiler/Validation/VersionConformancePass.cs:791-792 (UnlockStatement2002 gate) · :1065 (RetryPhrase2002 gate) — the Step-E funnel
- tests/conformance/2002/manifest.json:13 — "file_sharing" in the ENABLED array; tests/conformance/2002/file_sharing.out exists, so the run contract applies (compile strict at --std 2002 + run + byte-compare, tests/Cobol.Net.Tests.Conformance/CorpusRunnerTests.cs:63-69)
- tests/Cobol.Net.Tests.Unit/CobolFileLockTests.cs:15-80 — lock primitives (51/GR8/UNLOCK/54 ceiling) via the CobolFile facade → the RunUnit registry
- tests/conformance/negative/sharing_below_2002.cob + sharing-all-no-lockmode.cob/.err — edition-gate and validation negative goldens
- ~~Gap anchors~~ (audit-time; the retryPhrase-on-START claim was WRONG — CobolIO.g4:425 is the DELETE FILE site, and the ISO START general format carries no RETRY or lock phrase at all: §14.9.41 + §12.4.5.9 GR6 "any I-O statement except START")

Gaps (the first three CLOSED by the P10 Step-8 sharing/record-lock wave, 2026-07-16 — see the Step-8 AS-BUILT):
- ~~Sequential-organization record locking suppressed~~ — **CLOSED**: the record-lock identity went polymorphic on `FileConnector` (`LastReadRecordId`/`MutationTargetRecordId(image)`/`LastWrittenRecordId` — RRN / prime key / the sequential ORDINAL position; the per-organization switch `CurrentRecordId` is deleted); the sequential READ routes through `FileRegistry.ReadShared` (the pre-read ordinal conflict check — 51 with the FPI UNCHANGED per §14.9.30 GR10a and the '43' gate cleared via `NoteReadLockConflict`; GR11 lock discipline via the ONE `ApplyReadLockDiscipline`; GR22 ADVANCING ON LOCK skip-scan), and sharing-active sequential streams open `FileShare.ReadWrite` so the Table-19 registry — not the OS handle — arbitrates (§9.1.15). Golden `2002/file_sharing_seq` — ISO §9.1.16
- ~~WRITE/REWRITE/DELETE never check a record lock~~ — **CLOSED**: `WriteShared`/`RewriteShared`/`DeleteShared` (FileRegistry, all organizations polymorphically) pre-check the record-operation conflict on the mutation target (§14.9.35 GR11 → 51, record unrewritten; §14.9.10 GR6 → 51, record not removed) and apply the completion lock discipline (§14.9.35 GR12a-c, §14.9.10 GR7a-b, §14.9.51 GR10/GR11 — WRITE defines NO 51 leg: §9.1.13.8's 51 is an "access" conflict, and §14.9.51 GR33/GR42 state locks are ignored in invalid-key detection, so its RETRY covers only the implementor cross-run-unit "resources" case, GR16). Emitters route ONLY lock-relevant statements (the ONE `SequentialIoEmitter.LockGoverned` predicate) — unshared emission byte-identical (characterization 33/33). Golden `2002/file_sharing_mutate` — ISO §9.1.16 (:11752) / §14.7.9 GR4. (START needs nothing: no RETRY/lock phrase in its format.)
- ~~RETRY phrase silently dropped on WRITE/REWRITE/DELETE~~ — **CLOSED**: `BoundWrite`/`BoundRewrite`/`BoundKeyedWrite`/`BoundKeyedRewrite` carry `Lock`+`Retry`, `BoundKeyedDelete`/`BoundKeyedDeleteFile` carry `Retry`; all ride the ONE `RetryLoop` (§14.7.9 — n TIMES exhausts to 51/62, SECONDS/FOREVER deadlock-bail 52) — ISO §14.7.9
- Cross-run-unit file sharing unenforced — PhysicalFileTable is in-process per-run-unit state (cleared on Reset); connectors take no OS-level share locks, so SHARING NO OTHER etc. bind nothing against a concurrent run unit — ISO §9.1.15
- RETRY SECONDS/FOREVER performs no timed retry — a single re-check then deadlock-bail to 52 (documented single-run-unit residue, defensible only until cross-run-unit sharing exists) — ISO §14.7.9 GR2/GR3
- Record-lock release on COMMIT/ROLLBACK + the implicit LOCK MODE IS AUTOMATIC WITH LOCK ON MULTIPLE for APPLY COMMIT files is absent from the lock subsystem (no commit hooks into PhysicalFileTable) — ISO §9.1.16 (:11756-11760) / §9.1.18
- Keyed READ conflict detection stays POST-read (the NEXT/PREVIOUS record identity is only knowable after the read), so a keyed 51 leaves the FPI advanced — a §14.9.30 GR10a residue the sequential pre-check path does not share; keyed ADVANCING ON LOCK is bound + edition-gated but not emitted (skip-scan lands with a keyed governed-read entry) — ISO §14.9.30 GR10a/GR22

**FILE-2-line-seq — LINE SEQUENTIAL organization + the 5x/6x status family on SequentialConnector** — `PARTIAL`

Evidence:
- src/Cobol.Net.Runtime/IO/SequentialConnector.cs:19 — _lineSequential lives on the NEW FileConnector-derived connector (P8 substrate)
- src/Cobol.Net.Runtime/IO/SequentialConnector.cs:233-236 — line-seq WRITE = WriteLine(TrimEnd) (matches ISO §14.9.46 GR21 trailing-space rule, specs/ISO_COBOL.md:33525)
- src/Cobol.Net.Runtime/IO/SequentialConnector.cs:296-302 — line-seq READ = ReadLine + Fit; :300-301 silently TRUNCATES a longer-than-area line (no status 06 partial-transfer protocol, specs/ISO_COBOL.md:11474 + :30623 NOTE 4)
- src/Cobol.Net.Runtime/IO/SequentialConnector.cs:365-377 — line-sequential REWRITE unconditionally reports '30' PermanentError (runtime status, not a named compile diagnostic)
- src/Cobol.Net.Runtime/IO/FileRegistry.cs:89-95 — Register(...) news SequentialConnector(lineSequential) on the polymorphic registry
- src/Cobol.Net.Compiler/CodeGen/Verbs/SequentialIoEmitter.cs:74-78 — FileOrganization.LineSequential flows into the registration; DataBinder.cs:762 maps ORGANIZATION LINE; CobolIO.g4:116 grammars it
- src/Cobol.Net.Runtime/IO/FileStatus.cs:60-77 — the full 51/52/53/54/61/62 constant family with §9.1.13.8/.9 citations; NO 06/09/71 constants exist
- PRODUCTION SITES: 51 = src/Cobol.Net.Runtime/IO/Sharing/PhysicalFileTable.cs:51 + FileRegistry.cs:423; 52 = FileRegistry.cs:496 (RetryLoop deadlock-bail) + :373; 53 = PhysicalFileTable.cs:56; 54 = PhysicalFileTable.cs:55; 61 = FileRegistry.cs:389-390 (SharedOpenAttempt, Table-19 Conflicts :399-405)
- 62 = DECLARED-ONLY: grep over src/ finds FileStatus.cs:77 as the sole hit; FileRegistry.DeleteFile (FileRegistry.cs:328-343) never consults the PhysicalFileTable open set — only '41'/'05'/'37'/'30' are producible
- FileRegistry.cs:512-518 — CurrentRecordId returns "" for a sequential connector ('sequential has no per-record identity in this model (residue)') and :415 suppresses locking on empty recId; ReadLockGovern is emitted ONLY by KeyedIoEmitter.cs:124 — so 51/53/54 (and record-path 52) can never fire on SequentialConnector; 61 + OPEN-path 52 DO reach sequential via SequentialIoEmitter.cs:152 (FileOpenShared)
- ENABLED goldens: tests/conformance/2002/file_sharing.cob (+ .out; manifest.json:13 'enabled') drives 51/RETRY-51/IGNORING/UNLOCK-00/61 on the new registry; tests/Cobol.Net.Tests.Unit/CobolFileLockTests.cs:72-98 pins 54/53 ceilings; tests/Cobol.Net.Tests.Conformance/FileIoDifferentialTests.cs:167-181 + :205 are enabled Facts reading back LINE SEQUENTIAL streams
- NO EDITION GATE: LINE SEQUENTIAL is a 2023 introduction (specs/ISO_COBOL.md:1219 new-features list; ORGANIZATION clause at :15606-15613); docs/COBOLNET_FILES_DESIGN.md:136-138 requires rejection at 85 — but VersionConformancePass.cs has no line-sequential arm (grep empty), tests/version-matrix/constructs.json has no row (grep empty), DataBinder.cs:758-766 MapOrganization is unconditional, and the ENABLED 2002 golden tests/conformance/2002/oo_object_report.cob:30 (manifest.json:53) compiles it at --std 2002 strict
- docs/COBOLNET_FILES_DESIGN.md:106 + :173 — statuses 06/09 are explicitly DEFERRED (open Q4), i.e. a known doc-acknowledged hole, not staged-loud

Gaps (the first two CLOSED by the P10 Step-8 sharing/record-lock wave, 2026-07-16):
- ~~Status 62 has no production site~~ — **CLOSED**: `FileRegistry.DeleteFile` now checks `OpenByAnotherConnector` (any OTHER connector — sharing-registered or not, per §9.1.13.9 item 2's plain "another file connector" — whose `HostPath` matches and `IsOpen`) after the GR13 self-open '41' check, under the GR15 RETRY discipline (n TIMES → 62, FOREVER → 52); golden `2023/delete_file_sharing` (62/62/52/00/05/35). NOTE the audit line's "line-seq 62" framing was a MISCHARACTERIZATION: per ISO §9.1.13.9 item 2, 62 is the DELETE FILE file-sharing conflict — the "line-sequential WRITE with out-of-repertoire characters" condition it described is status **71** (§9.1.13.10 / §14.9.51 GR23), which remains open below — ISO §9.1.13.9 item 2 / §14.9.10 GR15
- ~~51/53/54 unreachable on SequentialConnector~~ — **CLOSED**: the sequential record's lock identity is its ordinal position (FileConnector's polymorphic id members + `FileRegistry.ReadShared`/`WriteShared`/`RewriteShared`); all of 51 (conflict), 52 (RETRY deadlock-bail), 53/54 (GR7 ceilings via `LockRecord`/`PreflightNewLock`) now fire on sequential connectors — golden `2002/file_sharing_seq`, unit `ReadShared_SequentialOrdinals_Conflict51_AndAdvancingOnLockSkips` — ISO §9.1.16 / §9.1.13.8
- Line-sequential status 06 (READ succeeded but no line delimiter detected — the long-record partial-transfer protocol; the connector silently truncates instead) — ISO §9.1.13 item 5 / §14.9.30 item 17 NOTE 4
- Line-sequential statuses 09 (READ) and 71 (WRITE/REWRITE) for characters outside the implementor-defined line-seq character set — no constants, no charset definition, no checks — ISO §9.1.13 items 7/§9.1.13 '71' / §14.9.46 item 23 / §14.9.35 GR d
- Line-sequential REWRITE reports a blanket '30' instead of the line-sequential REWRITE rules (in-place replacement with the 06-condition failure leg) — ISO §14.9.35 item 17
- No edition gate for ORGANIZATION LINE SEQUENTIAL (a 2023 introduction; the FILES design mandates at minimum rejection at 85, and the enabled 2002 golden oo_object_report.cob currently rides the ungated hole) — ISO §12.4.5.10 ORGANIZATION clause (2023 new-features list) + the version-matrix introduction invariant

**PROC-4-ec-n** — `STAGED-LOUD`

Evidence:
- E:\CobolSharp\src\Cobol.Net.Compiler\Binding\IntrinsicCatalog.cs:135 — EXCEPTION-FILE-N catalogued IntrinsicType.National, IntrinsicBind.Deferred, IntroducedIn 2002 (§15.29)
- E:\CobolSharp\src\Cobol.Net.Compiler\Binding\IntrinsicCatalog.cs:140 — EXCEPTION-LOCATION-N catalogued Deferred (§15.31)
- E:\CobolSharp\src\Cobol.Net.Compiler\Binding\IntrinsicCatalog.cs:136-138 — deliberate staging comment: '-N national twins stay Deferred-loud — no national runtime exists; faking national as UTF-16 alphanumeric would be the wrong data class (§15.29/§15.31; EC scout hazard H8)'
- E:\CobolSharp\src\Cobol.Net.Compiler\Binding\IntrinsicCatalog.cs:128,133 — the other -N/national intrinsic legs CHAR-NATIONAL (§15.16) and NATIONAL-OF (§15.66) are also Deferred
- E:\CobolSharp\src\Cobol.Net.Compiler\Binding\Procedure\Verbs\IntrinsicBinder.cs:125,138-143 — catalogued rows BIND with recognition diagnostics: COBOLNET1501 (unknown function), COBOLNET1502/1503 (D8 edition window — -N twins reject below --std 2002), COBOLNET1504 (arity)
- E:\CobolSharp\src\Cobol.Net.Compiler\CodeGen\Emit\IntrinsicRenderer.cs:52-53 and 250-251 — the Deferred bind renders EmitText.LoudValue("FUNCTION EXCEPTION-FILE-N (catalogued, not yet implemented)") in both the numeric and string channels
- E:\CobolSharp\src\Cobol.Net.Compiler\CodeGen\Emit\EmitCore.cs:68-69 — LoudValue emits NotImplemented.Value<T>(feature)
- E:\CobolSharp\src\Cobol.Net.Runtime\Control\Signals\NotImplemented.cs:11-12,30 — the named diagnostic: NotImplementedCobolFeatureException 'COBOL.NET: a COBOL feature that is not yet implemented was reached at run time: FUNCTION EXCEPTION-FILE-N (catalogued, not yet implemented)'
- E:\CobolSharp\src\Cobol.Net.Runtime\Exceptions\EcFunctions.cs:6-31 — the alphanumeric bases (EXCEPTION-STATUS §15.33 / -LOCATION §15.30 / -STATEMENT §15.32 / no-arg EXCEPTION-FILE §15.28) ARE implemented on the new ExceptionState substrate; the doc-comment (lines 9-10) states the -N twins stay catalogued-loud
- E:\CobolSharp\src\Cobol.Net.Compiler\Binding\IntrinsicCatalog.cs:44 — IntrinsicType.National folds to PicCategory.Alphanumeric in the result-category channel (no national result category exists yet)
- grep 'exception_file_n|char_national|national_of' over E:\CobolSharp\tests = no matches — no conformance golden exercises any -N leg (the only hits, tests\CobolSharp.Tests.Unit\Runtime\IntrinsicFunctionTests.cs:1114-1157, test the LEGACY src\CobolSharp.Runtime IntrinsicFunctions, not the greenfield)
- E:\CobolSharp\docs\rearchitecture\PHASE-11-intrinsics-backlog-tierc-codec.md:354-355 — ISO defines NO -N twin for EXCEPTION-STATEMENT/-STATUS; the only EC -N twins are the two catalogued rows
- E:\CobolSharp\docs\rearchitecture\PHASE-10-m2-residual-catalog.md:312-330 (Step 11) and PHASE-11-intrinsics-backlog-tierc-codec.md:341-361 (Step 4) — both plans schedule the Deferred→Runtime flip, blocked on national data (P10 Step 2)

Gaps (all but #3 CLOSED by Step 11, 2026-07-16):
- ~~EXCEPTION-FILE-N runtime body~~ CLOSED — `EcFunctions.FileN()` = `CobolIntrinsics.NationalOf(File())` (the §15.29.4 r1c "converted … to the runtime national character set" IS the ONE repertoire translation) — ISO §15.29
- ~~EXCEPTION-LOCATION-N runtime body~~ CLOSED — `EcFunctions.LocationN()` likewise — ISO §15.31
- The 2023 file-connector-argument form of EXCEPTION-FILE/EXCEPTION-FILE-N (renders loud on base AND twin; VCR rows 68/69, PHASE-13 Step 9) — ISO §15.28/§15.29 (2023, E.3.3 items 25/26) — **still the one open gap (staged loud, recorded location: IntrinsicRenderer.cs `EcFile`/`EcFileN` arms)**
- ~~Other -N/national legs~~ CLOSED — NATIONAL-OF landed at Step 5; CHAR-NATIONAL landed at Step 11 (`CobolIntrinsics.CharNational`, native national PCS = UTF-16 code-unit order; the non-native ALPHABET … FOR NATIONAL weights channel [`CollateNat`/`__COLLATE_NAT`] landed at Step 4) + ORD over a national argument (§15.70.3/§15.70.4 r2; the 0844 guard narrowed to CHAR with a §15.15.3 citation) — ISO §15.16/§15.66/§15.70
- ~~A true national result-category channel~~ CLOSED at Step 5 — `IntrinsicSig.ResultCategory` maps National→`PicCategory.National` (IntrinsicCatalog.cs)
- ~~Verification legs~~ CLOSED — `exception_file_n`+`char_national` ENABLED goldens (FUNCTION LENGTH pins the national character-position counts), `exception-file-n-2002` constructs.json row (reject 1502 below 2002), `exception_file_n_below_2002` negative @85, ECT018N inline EC Fact @2023 — ISO §15.29/§15.31

**ARITH-2-standard — ARITHMETIC IS STANDARD (plain, dual-window 2014→2023) / STANDARD-DECIMAL (2014) behavior at bind+emit. Direct answers: (1) ArithmeticMode IS consumed in CodeGen — the phase doc's captured-not-consumed mark is STALE (landed Phase-4 track (e), DEVLOG 611): NumericRenderer.StandardDecimal (CodeGen/Emit/NumericRenderer.cs:203) routes every +,-,*,/ through CobolDec.Add/Sub/Mul/Div with the §11.9.11 INTERMEDIATE ROUNDING mode (lines 189-198, 205), comparisons through CobolDec.Compare (ConditionRenderer.cs:111), statement temps typed CobolDec (ArithmeticEmitter.cs:181), and the final transfer through CobolDec.ToUnscaled with the receiver's ROUNDED mode (RuntimeApi.cs:106-108); bind-side, StandardBinary is a loud COBOLNET0806 error (DataBinder.cs:196-198) and the §14.7 r2 composite check is native-only (StatementValidation.cs:107). (2) §8.8.1.3 in the 2023 text is NATIVE arithmetic (implementor-defined intermediates — here the Int128 scaled-integer engine that clips nested quotients toward the receiver scale, specs/ISO_COBOL.md:9065-9067); 'standard arithmetic' (the 2002/2014 §8.8.1.3 numbering, dropped by 2023) requires every operation to evaluate in the standard intermediate data item — for fixed-point operands the decimal128-equivalent SDIDI: 34-significant-digit per-operation rounding under INTERMEDIATE ROUNDING, exact operand lift, decimal128-range size ECs (2023 §8.8.1.5, spec 9203-9250). Observable difference the goldens pin: COMPUTE W = 2 / 7 * 7 into PIC 9V9(5) gives 2.00000 under STANDARD/STANDARD-DECIMAL vs the native-clipped 1.99997.** — `PARTIAL`

Evidence:
- src/Cobol.Net.Compiler/Binding/OptionsModel.cs:21 — Arithmetic captured (§11.9.5, default Native); :54 enum Native/Standard/StandardBinary/StandardDecimal
- src/Cobol.Net.Compiler/Binding/OptionsBinder.cs:44-45,61-65 — ARITHMETIC clause parsed into ArithmeticMode
- src/Cobol.Net.Compiler/CodeGen/Emit/NumericRenderer.cs:203 — StandardDecimal => ctx.Data.Options.Arithmetic is StandardDecimal or Standard (CodeGen CONSUMES the mode; refutes the phase doc's captured-not-consumed)
- src/Cobol.Net.Compiler/CodeGen/Emit/NumericRenderer.cs:189-198 — +,-,*,/ emit CobolDec.Add/Sub/Mul/Div(…, IntermediateMode); :205 IntermediateMode = Options.IntermediateRounding (§11.9.11); :209 exact SDIDI lift CobolDec.From (§8.8.1.5.2); :283 Dec unary minus
- src/Cobol.Net.Compiler/CodeGen/Emit/ConditionRenderer.cs:111 — comparisons via CobolDec.Compare
- src/Cobol.Net.Compiler/CodeGen/Verbs/ArithmeticEmitter.cs:181 — CobolDec-typed statement temporaries
- src/Cobol.Net.Compiler/CodeGen/Roslyn/RuntimeApi.cs:106-108 — final transfer CobolDec.ToUnscaled(scale, receiver ROUNDED mode) (§14.7 NOTE 1)
- src/Cobol.Net.Runtime/Values/Numeric/CobolDec.cs:17-23 — the SDIDI record struct (Sig×10^Exp, decimal128-equivalent, §8.8.1.5); :58,68-82 exact 256-bit Mul/Div; :70 EC-SIZE-ZERO-DIVIDE; :134-166 one 34-digit round per op incl. PROHIBITED ⇒ EC-SIZE-TRUNCATION (§11.9.11)
- src/Cobol.Net.Compiler/Binding/DataBinder.cs:186-198 — bind-time rationale: plain STANDARD routes to the same CobolDec engine (fixed-point standard intermediate = the DECIMAL form); STANDARD-BINARY → COBOLNET0806 loud error (§8.8.1.4.1 NOTE 1)
- src/Cobol.Net.Compiler/Binding/Validation/StatementValidation.cs:107 — §14.7 r2 composite-of-operands applies only under Native (mode consumed at bind validation)
- src/Cobol.Net.Compiler/Validation/VersionConformancePass.cs:997-1005 — ArithmeticStandard2014 dual-window gate (0900 below 2014, 0807 removed-at-2023)
- tests/version-matrix/constructs.json — arithmetic-standard-decimal-2014 (positive at 2014+; expectDiagnostic 0900 on the keyword arm since Step 12); the dual-window row is now arithmetic-standard-2002 (0900 below 2002 / 0903 obsolete 2014 / 0807 at 2023) and the NATIVE row options-arithmetic-native-2002 (the Step-12 introduction-edge resolution)
- tests/conformance/2014/arithmetic_standard_decimal.cob:15 + .out:2 — ENABLED golden: COMPUTE W = 2/7*7 → W=200000 (the standard result; native clips to 1.99997) — enabled at tests/CobolSharp.Tests.Integration/ConformanceTests.cs:125 and tests/conformance/2014/manifest.json:4
- tests/conformance/2014/options_paragraph.cob:8,16 + .out — ENABLED plain-STANDARD golden (same 2/7*7 divergence + DEFAULT ROUNDED), ConformanceTests.cs:118
- specs/ISO_COBOL.md:9065-9067 — 2023 §8.8.1.3 = NATIVE (implementor-defined); :9203-9250 — §8.8.1.5 SDIDI requirements (decimal128 equivalence, 34 digits, INTERMEDIATE ROUNDING, range ECs)
- GAP anchors: src/Cobol.Net.Compiler/CodeGen/Emit/NumericRenderer.cs:271-279 — Power() has no Dec/StandardDecimal branch (native double Math.Pow + FromDouble even under STANDARD-DECIMAL); src/Cobol.Net.Compiler/Binding/Procedure/Verbs/IntrinsicBinder.cs:517-527 — the only mode-aware intrinsic logic is a float-argument diagnostic, no SDIDI routing of §15.4.1 functions; src/Cobol.Net.Compiler/CodeGen/Verbs/ReportWriterEmitter.cs:169 — SUM accumulates via the native NumericRenderer.Align path; src/Cobol.Net.Runtime/Values/Numeric/CobolDec.cs:123-166 — no decimal128 exponent-range (±6144) check, EC-SIZE-OVERFLOW/UNDERFLOW never signaled; docs/rearchitecture/PHASE-10-m2-residual-catalog.md:76,334-353,518 — stale 'consume the mode' net-new plan + a 2002 introduction claim contradicting the shipped 2014 registry edge (constructs.json:1483)

Gaps (ALL SIX dispositioned by Step 12, 2026-07-16 — see the Step-12 checkbox for the full as-built):
- ~~Exponentiation (**) under STANDARD/STANDARD-DECIMAL still evaluates on the native double path~~ — **CLOSED**: `CobolDec.Pow` per §8.8.1.5.4 (r2a–d exactly; r2e square-and-multiply beyond; r3 reciprocal; the r4/§8.8.1.2 r6 EC-SIZE-EXPONENTIATION legs; non-integer exponent = the r2e double approximation via FromDouble) + the mode-first `NumericRenderer.Power` branch; golden legs W2/W3 — ISO §8.8.1.5.4
- ~~Integer and numeric intrinsic functions are not routed through the SDIDI~~ — **CLOSED (per-family disposition)**: MEAN's division evaluates in SDIDI (the §15.4.1 NOTE-2 relation golden leg); the exact-Int128 family is documented-equivalence consumption (CobolIntrinsics.Exact.cs header — every EAE step exact in both engines; >34-digit exact results = the recorded extra-precision residue); the prose-approximation family (SQRT/trig/log/E/PI) has no EAE ⇒ implementor-defined in every mode; ANNUITY/PRESENT-VALUE/VARIANCE/STANDARD-DEVIATION staged LOUD (0899 `arithmetic-standard-intrinsic`, negative `arith-standard-intrinsic-staged`) — ISO §8.8.1.5.1 / §15.4.1
- ~~Report Writer SUM clause accumulation stays on the native scaled-integer path~~ — **CLOSED (documented equivalence AT the chokepoint)**: each §13.18.54 GR3 accumulation is ONE ≤32-digit fixed-point addition — exact and digit-identical in the Int128 and SDIDI engines (ReportWriterEmitter comment) — ISO §8.8.1.5.1
- ~~EC-SIZE-OVERFLOW / EC-SIZE-UNDERFLOW at the decimal128 range bounds are never signaled~~ — **CLOSED**: the ONE `Clamp` in the `Round34Wide` funnel (adjusted exponent > +6144 ⇒ EC-SIZE-OVERFLOW; below the 10⁻⁶¹⁷⁶ subnormal quantum re-rounds onto it, a nonzero value rounding to zero ⇒ EC-SIZE-UNDERFLOW); golden leg W4 (RANGE-EC via ON SIZE ERROR) — ISO §8.8.1.5.2 rule 2
- ~~The 2002 introduction edge is unresolved~~ — **CLOSED (resolved to 2002)**: Annex E.2 item 21 (Standard Arithmetic obsolete-but-present in 2014 ⇒ removed 2023 ⇒ it predates 2014; 85 had no ARITHMETIC clause ⇒ 2002) + the M2 catalog + the OPTIONS reserved word @2002 — rows `options-paragraph-2002` / `options-arithmetic-native-2002` / `arithmetic-standard-2002` (0900 below 2002 / 0903 obsolete at 2014 / 0807 at 2023); the STANDARD-DECIMAL/STANDARD-BINARY keywords gate 2014 on the VisitArithmeticMethod arm; the six 2014-only OPTIONS clauses got per-clause 0900 rows (ENTRY-CONVENTION conservatively 2014 — the recorded ambiguity: the in-repo evidence chain establishes only ARITHMETIC at 2002); golden `2002/arith_standard` + negatives — ISO §11.9.5 / Annex E.2 item 21
- ~~Plain-STANDARD float-operand divergence staged behind the float-usage staging~~ — **CLOSED (IMPLEMENTED, not staged)**: a float operand converts into SDIDI form via `CobolDec.FromDouble` (the §8.8.1.5.1 implementor-defined conversion = the shortest round-trip decimal identity; Inf ⇒ EC-SIZE-OVERFLOW, NaN ⇒ EC-DATA-INCOMPATIBLE) and the operations are SDIDI — the StandardDecimal branch now precedes the D16 float branch; golden leg W5 (COMP-2 0.1 × 3 = 0.300000000000000000 vs the native …044 binary artifact). STANDARD-BINARY remains the deliberate documented-unsupported COBOLNET0806 posture (§8.8.1.4.1 NOTE 1) with its 2014 introduction edge on the pending `arithmetic-standard-binary-2014` row — ISO §8.8.1.5.1

**RW-2002** — `NOT-STARTED` → ✅ **LANDED (Step 13, 2026-07-16)** — the PRESENT WHEN / VARYING / multiple-COLUMN gaps below CLOSED on the EXISTING RWCS (grammar alternatives + condition chains on the report model + compose-side guards/counters + the engine's evaluate-once-per-presentation walk; the full as-built record is the Step-13 checkbox above). Still open from the original gap list (named, staged or catalogued): report-group OCCURS incl. STEP (0899 `report-occurs-in-group`, unchanged), multiple LINE repetition (NEW 0899 `report-multiple-line` + the pending `report-multi-line-2002` row), COLUMN LEFT/CENTER/RIGHT alignment (no grammar surface — deep-dive §5), and the SOURCE 2002 format (SOURCES ARE / multi-operand / arithmetic-expression / ROUNDED — untouched by this wave). Original audit record follows.

Evidence:
- E:\CobolSharp\src\Cobol.Net.Frontend\Grammar\Core\CobolReportWriter.g4:4 — header scopes the entire RW grammar to 'COBOL-85, ISO 1989:1985' rules only
- E:\CobolSharp\src\Cobol.Net.Frontend\Grammar\Core\CobolReportWriter.g4:77-92 — reportGroupClause alternatives list has NO presentWhen/varying alternative; PRESENT WHEN or VARYING in a report group entry is a raw parse error, not a named diagnostic
- E:\CobolSharp\src\Cobol.Net.Frontend\Grammar\Core\CobolReportWriter.g4:111-113,121-123,126-128 — LINE, COLUMN, SOURCE are the '85 single-operand formats (no LINES ARE repetition, no LEFT/CENTER/RIGHT or multi-column, no SOURCES ARE/arithmetic-expression/ROUNDED)
- E:\CobolSharp\src\Cobol.Net.Frontend\Grammar\Core\CobolLexer.g4:588 — VARYING token exists only for PERFORM/SEARCH; no PRESENT token anywhere in the lexer (grep-empty)
- E:\CobolSharp\src\Cobol.Net.Compiler\Binding\DataBinder.Reports.cs:1-579 — zero PRESENT/VARYING matches in the entire greenfield report binder
- E:\CobolSharp\src\Cobol.Net.Compiler\Binding\DataBinder.Reports.cs:315-317 — the ONE 2002-adjacent leg that is recognized: OCCURS in a report group stages loud via DiagnosticCatalog.ReportOccursInGroup (the shared occursClause parses there; the 2002 STEP phrase does NOT parse)
- E:\CobolSharp\src\Cobol.Net.Editions\Diagnostics\DiagnosticCatalog.cs:44,119-122 — ReportOccursInGroup = COBOLNET0899 'report-occurs-in-group', ISO §13.18.38, RecognizedNotImplemented
- E:\CobolSharp\src\Cobol.Net.Editions\ReservedWords.Table.cs:331 — PRESENT registered as reserved since 2002; reserved-word machinery only, no clause implementation behind it
- E:\CobolSharp\tests\version-matrix\constructs.json — no PRESENT WHEN or report-VARYING construct row (grep-empty); no *.cob under tests\ uses PRESENT WHEN; the only RW goldens are the '85-module NIST RW101A-RW104A (tests\nist\valid\RW101A.txt-RW104A.txt)
- E:\CobolSharp\specs\ISO_COBOL.md:706,734,703,672,700 — spec anchors verified: PRESENT WHEN §13.18.41, VARYING §13.18.64, OCCURS (report format, STEP) §13.18.38, COLUMN §13.18.14, LINE §13.18.35

Gaps:
- PRESENT WHEN clause on report group lines/items (grammar rule + PRESENT lexer token + binder + absent-item state in the report model + presentation-time condition evaluation in Cobol.Net.Runtime/IO/ReportWriter) — ISO §13.18.41
- VARYING clause Format 1 on report group entries (repetition counters driving OCCURS'd report items at presentation time) — ISO §13.18.64
- OCCURS repeating entries in report groups including the STEP phrase (the STEP phrase does not parse; the bare OCCURS shape is the one staged-loud sliver: COBOLNET0899 report-occurs-in-group) — ISO §13.18.38
- COLUMN clause 2002 format: multiple column positions, LEFT/CENTER/RIGHT alignment, PLUS-relative repetition — ISO §13.18.14
- LINE clause 2002 format: LINE NUMBERS ARE / LINES ARE multi-operand repetition — ISO §13.18.35
- SOURCE clause 2002 format: SOURCES ARE, multiple operands, arithmetic-expression sources, the ROUNDED phrase (with the EC-REPORT-SUM-SIZE interaction) — ISO §13.18.53
- Edition gating + matrix coverage: no VersionConformancePass introduction gate (2002+ construct → 0900 at --std cobol85) and no version-matrix constructs.json row for any RW-2002 construct — ISO §8.9 interval encoding per the version-test-matrix invariant, over §13.18.41/§13.18.64/§13.18.38

**CONCAT — the & concatenation operator (ISO §8.8.3)** — `NOT-STARTED` → ✅ **LANDED (Step 14, 2026-07-16)** — all six gaps below CLOSED: (1) grammar — `AMPERSAND` (CobolLexer.g4, §8.7.3) + the `concatenationExpression`/`concatOperand` tier as the FIRST alternative of `nonNumericLiteral` (every literal position inherits it per §8.8.3.3 GR3); (2) binder — NO `BoundConcat` (the spec's construct is a compile-time literal, GR3): `Binding/ConcatFolder.cs` folds to the equivalent single literal with the §8.8.3.2 class rules (alnum/national/boolean + figurative adaptation GR1a/GR1b) as COBOLNET1540/1541/1545; (3) emitter — none needed, the folded literal rides the existing literal channels; (4) gating — `Constructs.ConcatOperator2002` fires from the VersionConformancePass parse arm (`VisitConcatenationExpression`, recognition, position-blind) → 0900 below 2002, verified via the CLI at `--std 85`; (5) goldens — `literal_concat` ENABLED (subsumes the planned concat_literal/concat_boolean pair) + the `concat-operator-2002` matrix row ACTIVE + `concat_below_2002`/`concat_class_mismatch` negatives; (6) the boolean leg — B"…" & B"…" folds and routes through the boolean relation channel (`ConditionBinder.IsBooleanValueOperand`/`BindBoolOperandValue` concat arms). Original audit record follows.

Evidence:
- grep 'BoundConcat' over E:\CobolSharp\src — zero matches (only hit repo-wide is docs\rearchitecture\PHASE-10-m2-residual-catalog.md)
- grep "AMPERSAND|'&'" over all src/**/*.g4 — zero matches; no & token or concatenation-expression rule in the grammar
- grep -i 'concat' over src\Cobol.Net.Frontend (lexer/parser/error strategy) — zero matches; no W1.5 parse-hint, so & in source is a generic parse error, not a named diagnostic
- src\Cobol.Net.Editions\ConstructRegistry.g.cs:37 — row new("concat-operator-2002", "concatenation expression (&)", 2002, null, null, "COBOLNET0900", "ISO §8.8.3; D6; PENDING (Phase 4g)") — inert seed data
- src\Cobol.Net.Editions\Constructs.g.cs:35 — Constructs.ConcatOperator2002 defined; grep shows ZERO references anywhere else in src (no gate/pass consumes it, so COBOLNET0900 can never fire for this construct)
- tests\version-matrix\constructs.json:281-291 — row concat-operator-2002 has "status": "pending" with a seeded source snippet; not exercised
- Glob tests/**/*concat* — no conformance goldens exist (catalog names concat_literal/concat_boolean as the planned goldens, PHASE-10-m2-residual-catalog.md:520)
- src\Cobol.Net.Compiler\Binding\IntrinsicCatalog.cs:176 + CodeGen\Emit\IntrinsicRenderer.cs:265 — the 2023 FUNCTION CONCAT (§15.18) IS implemented but is a distinct construct; the §8.8.3 operator it references has zero surface

Gaps:
- grammar: no & lexer token and no concatenation-expression rule in any literal position — ISO §8.8.3
- binder: no BoundConcat node / no compile-time folding of literal & literal into the single resulting literal (alphanumeric/national/boolean class-compatibility rules) — ISO §8.8.3
- emitter: no rendering leg for concatenation expressions — ISO §8.8.3
- edition gating: Constructs.ConcatOperator2002 wired to NO gate — the COBOLNET0900 introduction diagnostic below 2002 never fires; the registry row is inert — ISO §8.8.3 (VCR introduction invariant)
- goldens: zero conformance tests (planned concat_literal/concat_boolean absent) and the version-matrix row is status:pending — ISO §8.8.3
- boolean-literal concatenation leg (the § covers boolean operands too, per PHASE4_RECONCILIATION.md:1372) — ISO §8.8.3

**CONSTANT — constant entries (§13.10 level-01 CONSTANT AS/FROM) + CONSTANT RECORD (§13.18.15)** — `NOT-STARTED`

Evidence:
- E:\CobolSharp\src\Cobol.Net.Editions\ConstructRegistry.g.cs:36 — registry row 'constant-entry-2002' explicitly marked 'ISO §13.10 + §13.18.15; D5; PENDING (Phase 6)' with code COBOLNET0900; metadata only
- E:\CobolSharp\src\Cobol.Net.Editions\Constructs.g.cs:34 — the compile-checked id ConstantEntry2002 exists, but grep across src/Cobol.Net.Compiler finds ZERO ConstructRegistry.Check call sites for it (only the two generated registry files reference it)
- E:\CobolSharp\src\Cobol.Net.Frontend\Grammar\Core\CobolData.g4:386 — the ONLY CONSTANT-adjacent grammar text is a comment about VALUE constant-name operands; a case-insensitive grep of all .g4 under src/Cobol.Net.Frontend/Grammar returns no CONSTANT token or rule (only figurativeConstant and Antlr IntStreamConstants hits), and a whole-word CONSTANT grep of src/Cobol.Net.Frontend excluding Generated/ is empty
- E:\CobolSharp\src\Cobol.Net.Compiler\Binding\Bound\BoundTree.cs:222 — the sole 'Constant' hit in the bound tree is a figurativeConstant comment; no ConstantEntry/ConstantRecord bound node or binder path exists
- E:\CobolSharp\tests\version-matrix\constructs.json:270 — the row is status "pending" (its sample source '01 K CONSTANT AS 42.' is catalogued, never compiled)
- E:\CobolSharp\tests\Cobol.Net.Tests.Conformance\VersionMatrixTests.cs:57 — the matrix generator filters to Status == "active", so no enabled test exercises CONSTANT; not STAGED-LOUD because nothing recognizes the syntax to fire the reserved COBOLNET0900 — a CONSTANT entry today fails as a generic parse error

Gaps:
- CONSTANT lexer token + level-01 constant-entry grammar rule (01 constant-name CONSTANT [IS GLOBAL] AS {literal | arithmetic-expression}) — ISO §13.10
- CONSTANT FROM compilation-variable-name leg (the >>DEFINE directive tie-in) — ISO §13.10 (FROM phrase)
- Binder: constant-name symbol kind + compile-time value folding/substitution at every reference position where a literal is permitted (procedure division AND data division, e.g. PIC replication / OCCURS bounds) — ISO §13.10 general rules
- CONSTANT RECORD clause grammar + binder: structured-constant marking, level-1/WS-LS-only + clause-conflict syntax rules, INITIALIZE-equivalent content, and receiving-operand immutability rejection — ISO §13.18.15 (+ §13.18.1 SR13 cross-clause conflicts)
- Live edition gate: no ConstructRegistry.Check(Constructs.ConstantEntry2002) call site exists, so pre-2002 use cannot fire the registered COBOLNET0900 (the registry row at ConstructRegistry.g.cs:36 is inert) — ISO §13.10 (2002 introduction gating)
- Enabled test coverage: flip the constructs.json 'constant-entry-2002' row from pending to active (VersionMatrixTests.cs:57 filter) + a conformance golden exercising CONSTANT AS, CONSTANT FROM, and CONSTANT RECORD — ISO §13.10 + §13.18.15

**TYPEDEF-residue** — `PARTIAL`

Evidence:
- src/Cobol.Net.Compiler/Binding/DataBinder.cs:852 — ExpandTypes bind pass (TYPEDEF core rides the new Binding/Model tree; registered in Binding/Passes/BindPipeline.cs:36)
- src/Cobol.Net.Compiler/Binding/Model/StrongTypeModel.cs:19-73 — StrongRoot/IsStrongGroup/TypeAnchor/SameStrongType overlay on the new data model (P5.11b extraction)
- tests/conformance/2002/manifest.json:73-79 — seven ENABLED typedef goldens (typedef_88, typedef_indexed, typedef_nested_strong, typedef_odo, typedef_strong_ok, typedef_weak_elem, typedef_weak_group) consumed by the greenfield CorpusRunnerTests (tests/Cobol.Net.Tests.Conformance/CorpusRunnerTests.cs:27)
- src/Cobol.Net.Compiler/Binding/Validation/StatementValidation.cs:248-258 — STRONG relation rule §8.8.4.2.3 SR1 (same-type gate, StrongCompareMismatch) at the ONE relation checkpoint covering IF/EVALUATE/PERFORM UNTIL/SEARCH WHEN — LANDED on new substrate
- src/Cobol.Net.Compiler/Binding/Validation/StatementValidation.cs:79-95 — §14.9.25.3 SR2 strong-MOVE gate (COBOLNET1533)
- src/Cobol.Net.Compiler/Binding/Procedure/Verbs/ConditionBinder.cs:359-365 — §8.8.4.4.3 SR1 strong-group class-condition ban (StrongClassCondition)
- src/Cobol.Net.Compiler/Binding/DataBinder.cs:1011-1028 — §13.18.57.3 SR3/SR4 REDEFINES/RENAMES-of-strong bans (COBOLNET1532); SR6 checked at clone time (comment line 1007-1008)
- src/Cobol.Net.Compiler/Binding/DataBinder.cs:1327-1329 — EXTERNAL on a type declaration STAGED-LOUD COBOLNET1534 ('recognized but not yet implemented, D17 residue')
- src/Cobol.Net.Compiler/Binding/Model/StrongTypeModel.cs:52-53 — same-type test scoped to ONE source element; 'cross-program EXTERNAL equivalence is a follow-up' stated in code
- src/Cobol.Net.Compiler/Binding/DataBinder.cs:387-390 — level-66 RENAMES inside a TYPEDEF STAGED-LOUD COBOLNET1535 (not cloned into TYPE references)
- src/Cobol.Net.Compiler/Binding/Validation/StatementValidation.cs:259-265 — §8.8.4.2.3 SR4 ordering-compare of a boolean/object/pointer-bearing strong group rejected loud COBOLNET1535 (spec-conformant rejection, framed as D17 residue); equality positive-companion compiles clean (tests/Cobol.Net.Tests.Conformance/TypedefResidueTests.cs:134-150)
- src/Cobol.Net.Compiler/Binding/DataBinder.cs:964 — INDEXED-BY type referenced >=2x STAGED-LOUD COBOLNET1531
- tests/Cobol.Net.Tests.Conformance/TypedefResidueTests.cs:20-110 — all three staged codes (1534/1535/1531) test-pinned
- SAME AS grep-empty in greenfield: no grammar rule (src/Cobol.Net.Frontend/Grammar/Core/CobolIO.g4:189 is the unrelated I-O SAME AREA clause; CobolData.g4 has no sameAsClause), no binder site, no diagnostic; sole mention is a legacy doc comment src/CobolSharp.Compiler/Semantics/DialectConfig.cs:83; spec section confirmed specs/ISO_COBOL.md:21731 ('## 13.18.49 SAME AS clause')
- tests/conformance/2014/manifest.json — zero typedef rows (2002-vs-2014 refinement provisional per src/Cobol.Net.Frontend/Grammar/Core/CobolData.g4:258-261)

Gaps:
- SAME AS clause entirely absent (no grammar rule, no binder expansion, no staged diagnostic — a '01 X SAME AS Y.' entry dies as a raw parse error, not a named rejection) — ISO §13.18.49
- EXTERNAL on a type declaration: run-unit-shared type not modeled, staged loud COBOLNET1534 instead of implemented — ISO §13.18.57.4 GR5 / §13.18.22
- Cross-source-unit same-type equivalence for EXTERNAL types in the §8.5.3 same-type test (SameStrongType matches type-names within ONE source element only) — ISO §8.5.3.3
- Level-66 RENAMES declared inside a TYPEDEF template is not cloned into TYPE references (CloneItem drops Renames66), staged loud COBOLNET1535 — ISO §13.18.58.4 GR1
- A type whose OCCURS carries INDEXED BY cannot be referenced more than once (index-name clones would collide; no per-reference uniquing), staged loud COBOLNET1531 — ISO §13.18.38
- Ordering-relation semantics for strong groups containing boolean/object-reference/pointer elements: the equality-only SR4 rejection fires (COBOLNET1535) which matches the spec prohibition, but it is implemented/labeled as staged residue rather than the named SR4 check — ISO §8.8.4.2.3 SR4
- TYPEDEF/TYPE edition gate is 2002-provisional with zero 2014/2023 typedef goldens (no continuity coverage in the version matrix corpus) — ISO §13.18.58 (edition introduction per Annex E)

Step-16 disposition (2026-07-16): SAME AS gap CLOSED (grammar + ExpandSameAs on the ONE clone machinery; 1555/1556/1557); the EXTERNAL-type gap CLOSED — the audit's §13.18.57.4 GR5 citation was WRONG (GR5 is a report-group rule; the real §13.18.22 GR2/GR3/SR5 semantics are a conformance surface + record-external attribution, implemented as 1558 + the ordinary ExternalStore re-basing — no cross-program type-identity model is required, and the §8.5.3 same-type "follow-up" note in StrongTypeModel stands as the recorded EXTERNAL-equivalence residue); the SR4 labeling gap CLOSED (named descriptor `strong-compare-ordering`, code byte-stable); NEW named stage 0899 `strong-group-ordering-signed-leaf` (§8.8.4.2.12 signed-leaf element ordering). Still open from this row: the RENAMES-in-typedef clone (1535 `typedef-renames-staged`), the INDEXED-type-≥2× uniquing (1531), and the 2014/2023 continuity goldens.


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

**Files:** `src/Cobol.Net.Compiler/Binding/Procedure/Verbs/ConditionBinder.cs` (`BindBoolXor`/`BindBoolAnd`/`BindBoolFactor`/`MakeBoolBinary`→`BoundBoolBinary`), `src/Cobol.Net.Compiler/CodeGen/Emit/BooleanRenderer.cs`, `src/Cobol.Net.Runtime/Values/Text/CobolBool.cs` (confirm on the P8 `Values/Text/` home, not `Text/`), goldens `tests/conformance/2002/boolean_data.cob` + `boolean_ops.cob` (both ENABLED).

**Change:** Confirm the boolean core (`PIC 1`/`USAGE BIT`, `B"…"`, MOVE/VALUE/INITIALIZE/DISPLAY/compare, JUSTIFIED) and the four operators bind + render on the reorganized runtime folders. Confirm boolean item↔item compare zero-extends via `CobolString.Compare(l, r, pad: '0')` OR `CobolBool.Equal` (both correct per §8.8.4.2.8; NOT space-padding). Confirm boolean-in-COMPUTE stays in the `BoundBoolExpr` channel (never the numeric channel) and boolean-in-arithmetic (`ADD B1 TO X`) is rejected. Confirm equality-only relations (ordering operator on boolean → the `0844`/`15xx` band diagnostic).

**Why:** Boolean core + operators are present (in `Binding/Procedure/Verbs/ConditionBinder.cs`) but predate the runtime folder reorg; re-prove and re-home.

**Verify:**
- `dotnet …/cobol.dll tests/conformance/2002/boolean_ops.cob --std 2002 -o E:/Temp/b.dll --run` → matches `boolean_ops.out`.
- Negative: `tests/conformance/negative/boolean-ordering-relation.cob` still emits its `.err` code at 2002.
- `--std 85` on a `PIC 1` program → boolean usage/literal rejection.

_(Folds into Step 5.)_

---

### Step 4 — ALPHABET national / UCS-4 / UTF-8 / UTF-16 phrases — **LANDED 2026-07-17**

**Scout finding (audit drift, the five-of-six pattern):** the audit's "zero surface" was PARTIAL drift — the grammar
already parsed a postfix `FOR (ALPHANUMERIC|NATIONAL)` on the ALPHABET clause (nonstandard position; edition-gated
by `VisitAlphabetClause`), but the binder ignored the class entirely and UCS-4/UTF-8/UTF-16 had no surface at all.
Also, the original plan's "feed the encoding into the CODE-SET/codec boundary" has NO consumption point: the
CODE-SET clause has no compiler surface (fails loud at parse), so the coded-set role of UTF-8/UTF-16 is
inert-by-construction — nothing to stage.

**Landed (spec-derived first — §12.3.7.2 two-branch format, §12.3.7 GR7 f/g/h + Table 6, §12.3.6, §14.9.40.2,
§8.5.1.4, §15.16.4, §15.70.4 r2):**
- **Grammar** (shared .g4, superset; legacy consumers shape-only-fixed): `alphabetClause` takes the FOR phrase in
  its ISO position (between the name and IS) AND keeps the historical postfix as an accepted superset;
  `programCollatingSequenceClause` + `sortCollatingPhrase` gained the two-name IS form / the ONE shared
  `collatingForPhrase` subrule (`FOR ALPHANUMERIC|NATIONAL IS? alphabet-name`). **UCS-4/UTF-8/UTF-16 are §8.9
  CONTEXT-SENSITIVE words** (ALPHABET-clause scope): recognized BY TEXT as plain cobolWord entries — no lexer
  tokens minted (no ANTLR auto-mint exposure; they stay user-definable elsewhere).
- **Binder** (`DataBinder.Switches.cs`): the FOR-class splits the registries — `Alphabets` (alphanumeric) vs
  `NationalAlphabets` (`NationalAlphabetDef`: Table 6's per-name collating-capability). NATIVE/UCS-4 → identity
  (null table); UTF-8/UTF-16 → coded-set-ONLY (referencing them as a collating sequence = 0898); literal phrase →
  the SPARSE `NationalCollatingTable` (GR7 k1–k6 over the 65,536-code-unit native national set; k3's unspecified
  tail computed arithmetically, never a dense table; GR10 figuratives = the native NATIONAL extremes U+FFFF/U+0000;
  SR14c literal-class checks). PCS resolution fills `Collating` + `NationalCollating` with §12.3.6 SR1/SR2
  class-validation; SORT/MERGE name-2/FOR-forms resolve + class-validate (§14.9.40.3 SR2) — the validated national
  sequence is intentionally NOT carried into the sort: national KEYS cannot exist (D-N2 + the staged table-sort
  key), the staged legs are the fence (carried slot lands with RESIDUE-11).
- **UCS-4 ≡ NATIVE derivation (documented, not staged):** GR7 f makes UCS-4's collating sequence the ISO 10646
  appearance order; §8.5.1.4 (:8057/:8067) makes each UTF-16 code element its OWN character position with "no
  special handling or recognition of surrogate pairs" — so the codepoint-vs-code-unit divergence above U+FFFF
  (weighing a surrogate PAIR as one supplementary codepoint) is UNREACHABLE in COBOL's per-position comparison
  model, and ISO 10646 order over the 65,536 single-position characters IS the native code-unit order (D-N3).
  The implementor correspondence (item 188) is the BMP identity.
- **Runtime + emit:** `NationalCollation` (`Values/Text/`, sparse Weight/CharAt) emitted as `__COLLATE_NAT`;
  `CobolString.Compare(a,b,national)`; national relations + condition-names take `NatCollateArg` (§12.3.6 GR11 /
  §8.8.4.2.9); `CharNational(n, nat)` + `Ord(s, nat)` behind the H5-twin `BoundIntrinsicCall.CollateNat`;
  national HIGH-/LOW-VALUE fills read the explicit sequence's extremes (§12.3.7 GR8/GR9) via
  `FigurativeConstants` (the native national pin stays byte-stable — the flagged GR6/GR7 divergence).
- **Gates/matrix:** rows `alphabet-national-2002` (the coded-set phrases, by-text parse-arm gate) +
  `program-collating-national-2002` (PCS name-2/FOR forms) ADDED; `sort-collating-national-2002` ACTIVATED
  (pending→active; its probe now uses a spec-valid `FOR NATIONAL IS NATIVE` name-2); the
  `special-names-for-national-2002` probe rewritten spec-valid (`STANDARD-1 FOR NATIONAL` violates the branch
  format the binder now enforces).
- **Proof:** golden `2002/alphabet_national.cob` (ENABLED + legacy `GreenfieldOnly`) — a `N"CBA"` national PCS
  visibly REVERSES national relations, places unspecified characters per k3, drives CHAR-NATIONAL(1)='C',
  ORD(A)=3/ORD(space)=36, LOW-VALUE='C', HIGH-VALUE=U+FFFF, and the level-88 — hand-derived, byte-matched on run.
  Negatives: `alphabet-national-at-85` (0900), `alphabet-utf8-collating`, `alphabet-standard1-for-national`,
  `alphabet-ucs4-for-alphanumeric` (all 0898). Unit: `NationalCollationTests` (the sparse math + the ALSO
  representative). Rejections ride the EXISTING 0898 national-rule band (no free code below the 1560 P13 band).
- **Out (documented):** the LOCALE alphabet phrase (no locale subsystem; no lexer token — fails loud at parse);
  code-name-1/code-name-2 (SR15 — none supported, 0898); MAX/MIN under a NON-native sequence — a pre-existing
  residue shared by BOTH classes (`CobolIntrinsics.MaxString`/`MinString` compare ordinal and equally ignore the
  ALPHANUMERIC PCS today; the one fix covers both when the intrinsic-collation leg lands).

---

### Step 5 — National/boolean wave commit

**LANDED 2026-07-16 (the DISPLAY-OF/NATIONAL-OF wave; Step 4 ALPHABET-national/UCS-4/UTF-8/UTF-16 was explicitly OUT of this wave and landed separately 2026-07-17 — see the Step-4 section):**
- **DISPLAY-OF (§15.26) / NATIONAL-OF (§15.66) implemented** as Runtime rows on `CobolIntrinsics` (`DisplayOf`/`NationalOf` in `Cobol.Net.Runtime/Intrinsics/CobolIntrinsics.Text.cs`), riding the ONE `Repertoire` translator extracted from CONVERT's §15.19.4 r1/r3 arm — never a second converter. Argument-2 is the one-character SUBSTITUTION CHARACTER per the 2023 text (§15.26.3 r2 / §15.66.3 r2 — no codeset facility in the format), so BOTH argument forms are fully implemented; the argument-2-unspecified form substitutes '?' + EC-DATA-CONVERSION through the existing ambient `ExceptionState.DataConversionError` channel (§15.26.4 r2/r3, §14.6.13.1.1).
- `IntrinsicSig.ResultCategory` now maps `IntrinsicType.National` → `PicCategory.National` (§15.2 type 4), so Table-16 MOVE legality and the string channels see the correct class; `IsStringOperand`/`BindLengthFold`/the nested-argument visitor accept national results.
- **Binder SR checks** (`IntrinsicBinder.CheckRepertoireArgs`, new code **COBOLNET1546**): §15.26.3 r1/r2 + §15.66.3 r1/r2/r3 (argument classes, the one-character argument-2, the zero-length-literal bar).
- The **N"…" literal Latin-1-only staged guard LIFTED** (`ExpressionBinder.NationalLiteralOperand` — the §8.1.2 correspondence now exists; content = the full national repertoire, one UTF-16 char/position, D-N1; the 8,191 SR1 cap stays).
- **Pins:** `tests/Cobol.Net.Tests.Unit/NationalStorageFormTests.cs` (Step-2 national + Step-3 boolean CharImage Width==Length). **Goldens:** `tests/conformance/2002/national_intrinsics.cob`/`.out` ENABLED (both directions, nesting, category-national comparison, out-of-repertoire U+4E16 substitution both forms, FUNCTION LENGTH over both results) + a legacy `GreenfieldOnly` exclusion. **Negatives:** `display-of-wrong-category`, `national-of-wrong-category` (COBOLNET1546). **Matrix:** the D8 per-name catalog window covers the 2002 introduction (DESIGN-version-conformance-pipeline §1.1 ledger item 2 — no `constructs.json` row needed); pinned at `--std 85` by `EditionGate_RepertoireFunctionsAt85_RejectedByName` (COBOLNET1502).

**Files (original plan):** `tests/conformance/2002/manifest.json` (confirm `national_data`, `boolean_data`, `boolean_ops` enabled; add `alphabet_national`), `tests/version-matrix/constructs.json` (national/boolean/alphabet rows present), `tests/conformance/negative/` (national-narrowing reject, boolean-VALUE-mismatch, boolean-ordering, alphabet-at-85).

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

**Files:** `src/Cobol.Net.Runtime/Control/ManagedPointer.cs`, `Control/StorageCell.cs`, `Control/ExternalStore.cs` (P8 split of `ProgramRegistry.cs`), `src/Cobol.Net.Compiler/Binding/DataBinder.Ptr.cs`, `Binding/Procedure/Verbs/PtrBinder.cs`, `CodeGen/Verbs/PtrEmitter.cs`, goldens `based_pointer` / `pointer_alloc` / `pointer_arith` (all ENABLED).

**Change:** Confirm `USAGE POINTER`, `NULL`, `SET p TO NULL|q`, `[NOT] EQUAL`, `ADDRESS OF`, `SET ADDRESS OF`, `SET … UP/DOWN BY`, `BASED` deref, and `ALLOCATE`/`FREE` all resolve to the ONE `ManagedPointer` carrier now living under `Control/` and owned by `RunUnit` (not a process-global). Confirm the `StorageCell`+`CellPointer` window model (structural §8.8.4.2 equality; deref bridge §13.18.5 GR3/GR4 loud) survived the `ExternalStore`→`RunUnit` instance move. Confirm `FREE`'s three-way GR1 (nonfatal `EC-STORAGE-NOT-ALLOC` through the TurnState-gated block; dangling alias loud at deref).

**PROC-5-allocate slice — LANDED 2026-07-16:** `ALLOCATE based-item INITIALIZED` lowers per §14.9.3 GR7 to the allocation + the spec's own `INITIALIZE data-name-1 WITH FILLER ALL TO VALUE THEN TO DEFAULT` bound expansion (`InitializeBinder.BindAllocateInitialized`, a `BoundSequence` — no second INITIALIZE mechanism, no new emitter leg); the fourth pointer golden `allocate_initialized` (ENABLED, GreenfieldOnly in the legacy runner) byte-verifies GR7 + GR6 zero-fill + GR4b form-2 RETURNING at runtime.

**Why:** The pointer surface must ride the `RunUnit`-owned `ManagedPointer`/`ExternalStore` (P8 makes them instance state, not a process-global `ProgramRegistry`); re-prove no regression on the new home.

**Verify:** run all three pointer goldens (`--std 2002 --run`) byte-exact; `tests/conformance/negative/allocate-non-based.cob`, `based-redefines-conflict.cob` still emit their `.err` codes. Concurrent-run-unit sanity (if P8 enabled it): two `RunUnit`s each with their own allocated storage do not collide.

_(No commit yet — pairs with Step 7.)_

---

### Step 7 — `USAGE PROGRAM-POINTER` leg (residue)

**Files:** `src/Cobol.Net.Frontend/Grammar/Core/CobolData.g4` (usage keyword — `PROGRAM-POINTER` token likely already lexed/reserved), `src/Cobol.Net.Compiler/Binding/PicInfo.cs` / the P5 `PictureAnalyzer` (usage marker), `Binding/Procedure/Verbs/PtrBinder.cs` (`SET pp TO ENTRY|procedure-name`, `SET pp TO NULL`, equality), `src/Cobol.Net.Runtime/Control/ManagedPointer.cs` (program-pointer variant — carries a resolvable program/entry identity, distinct from a data address).

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

> **AS-BUILT — the sharing/record-lock wave (2026-07-16; the FILE-1 closure + the FILE-2 62/5x legs):**
> **(1) Polymorphic record-lock identity** — `FileConnector` grew three virtual members (`LastReadRecordId`,
> `MutationTargetRecordId(image)`, `LastWrittenRecordId`; §9.1.16 applies to EVERY organization — the READ/REWRITE/
> WRITE/DELETE lock rules are ALL-FORMATS rules): relative = the RRN, indexed = the prime record key, sequential =
> the record's 1-based ORDINAL position (reads count from OPEN; a sharing-active connector's write base is seeded
> at shared open — OUTPUT 0, EXTEND the pre-existing record count per §14.9.51 GR18). The registry's per-organization
> `CurrentRecordId` switch is DELETED (singular pattern).
> **(2) Governed verb entries on the ONE registry** — `ReadShared` (sequential; the next ordinal is knowable BEFORE
> the read, so the §14.9.30 GR9 conflict check precedes it: 51 leaves the FPI UNCHANGED per GR10a while
> `NoteReadLockConflict` clears only the '43' gate; GR22 ADVANCING ON LOCK skip-scans read-and-discard; GR11 lock
> actions via the shared `ApplyReadLockDiscipline` — GR11a single-lock auto-release, GR11b multiple+NO-LOCK release,
> GR11c/d AUTOMATIC/MANUAL acquire, §12.4.5.9 GR1a/b1 no-LOCK-MODE ⇒ no acquire, GR3 NoOther ⇒ no acquire);
> `WriteShared` (§14.9.51 GR10 single release + GR11 WITH LOCK on the written record; NO 51 leg — §9.1.13.8's 51 is
> an "access" conflict and GR33/GR42 ignore locks in invalid-key detection, so WRITE's RETRY covers only the
> implementor cross-run-unit GR16 case); `RewriteShared` (§14.9.35 GR11 pre-op conflict → 51 with the record
> unrewritten + GR12a2 begin-release / GR12a1/GR12b completion-release / GR12c WITH-LOCK acquire, with a §12.4.5.9
> GR7 `PreflightNewLock` so a ceiling denial precedes the update per GR14); `DeleteShared` (§14.9.10 GR6 → 51 +
> GR7a2 begin / GR7a1+GR7b completion releases); `DeleteFile(name, retry…)` (§14.9.10 GR15 / §9.1.13.9 item 2 —
> `OpenByAnotherConnector` over host paths → **62**, RETRY per §14.7.9). `ReadLockGovern` (keyed post-read) now
> shares `ApplyReadLockDiscipline` and clears the '43' gate on a conflict. Keyed conflict detection stays post-read
> (identity knowable only after a NEXT/PREVIOUS read) — the GR10a FPI residue + unemitted keyed ADVANCING ON LOCK
> are ledgered under FILE-1.
> **(3) Bind + emit** — `BoundRead`/`BoundWrite`/`BoundRewrite` + `BoundKeyedWrite`/`BoundKeyedRewrite` carry
> `Lock`+`Retry`, `BoundKeyedDelete`/`BoundKeyedDeleteFile` carry `Retry` (the sequential READ also binds
> `AdvancingOnLock`, gated by a new `BoundRead` arm in the VersionConformancePass mirroring the keyed arm); the ONE
> `SequentialIoEmitter.LockGoverned` predicate routes ONLY lock-relevant statements to the governed entries —
> unshared programs' generated text is byte-identical (characterization 33/33). ISO START carries NO RETRY/lock
> phrase (§14.9.41 format; §12.4.5.9 GR6 excepts START) — the audit's "retryPhrase on START" anchor misread the
> DELETE FILE grammar site.
> **(4) OS-handle posture** — a sharing-REGISTERED sequential connector opens its streams `FileShare.ReadWrite` so
> the §9.1.13.9 Table-19 registry (not the OS handle) arbitrates; unshared connectors keep the exclusive posture
> byte-for-byte.
> **Witnesses:** goldens `2002/file_sharing_seq` + `2002/file_sharing_mutate` + `2023/delete_file_sharing` (outputs
> verified by RUNNING before baking; GreenfieldOnly exclusions added), unit `CobolFileLockTests` 18/18, negatives
> unchanged (SR2/SR4/SR8/edition gates all pre-existed — no new diagnostics minted), characterization 33/33.
> **Still open in Step 8:** line-seq 06/09/71 + line-seq REWRITE, the 04/39 narrow statuses, the LINE SEQUENTIAL
> edition gate, and the EC-side confirm sweep.

**Files:** `src/Cobol.Net.Runtime/IO/FileConnector.cs`, `IO/FileRegistry.cs`, `IO/SequentialConnector.cs`/`RelativeConnector.cs`/`IndexedConnector.cs`, `IO/Sharing/PhysicalFileTable.cs`, `IO/FileStatus.cs` (P8 reorg), `src/Cobol.Net.Compiler/Binding/Procedure/Verbs/FileLockBinder.cs`, `Binding/Procedure/Verbs/KeyedIoBinder.cs`, `Binding/DataBinder.cs` (`MapOrganization`), `CodeGen/Verbs/KeyedIoEmitter.cs`, golden `tests/conformance/2002/file_sharing.cob` (ENABLED).

**Change:** Confirm SHARING clause / OPEN SHARING phrase, LOCK MODE (AUTOMATIC/MANUAL/EXCLUSIVE), RETRY (§14.7.9), UNLOCK, and line-sequential organization + 2002 FILE STATUS 5x/6x all route through the polymorphic `FileConnector`/`FileRegistry` (the `Keyed*` static fallthrough was deleted in P8). Confirm the sharing/lock registry is now `RunUnit`-owned (`IO/Sharing/PhysicalFileTable`). Confirm the EC bridge (`ExceptionCatalog` `EC-I-O-FILE-SHARING` / `EC-I-O-RECORD-OPERATION`, `IoEcOfStatus` 5x/6x arms, `__IoCheckEc` continues-not-throws for 5x/6x) is intact. Close the named residue: narrow FILE STATUS 04/39.

**Why:** The file-2002 surface exists but was built on the pre-connector dispatch; P8 collapses the three organizations behind `FileConnector` and moves the sharing registry to `RunUnit`. Re-prove + finish 04/39.

**Verify:** `file_sharing.cob` golden byte-exact; `tests/conformance/negative/close-with-lock.cob` `.err` at the right edition; new negative for a 51/61 shared-lock conflict producing the continuable EC. Add a golden exercising a 04 (record-length) or 39 (fixed-attribute conflict) status. `bash scripts/guard.sh` (file I/O touches the NIST SQ/RL/IX corpus — run the FULL guard).

**COMMIT BOUNDARY.** Suggested message:
```
feat(cobolnet): Phase 10 wave C — file-2002 confirmed on IO/FileConnector; narrow FILE STATUS 04/39

SHARING/LOCK MODE/RETRY/UNLOCK/line-sequential + 5x/6x statuses re-proven on the polymorphic
FileConnector/FileRegistry with the RunUnit-owned sharing registry; 04/39 narrow statuses added.
Full legacy guard NIST 353 MATCH. Battery green.
```

---

### Step 9 — UDF residue: category-carrying non-numeric/group RETURNING (lift `COBOLNET1510`) — DONE 2026-07-16

> **AS-BUILT (2026-07-16):** landed SMALLER than the recipe — no new bound node, no CallEmitter/CallAbi change.
> The existing shapes already carried category: `IntrinsicBinder.OperandOf` maps the result's `BoundNumRef` to a
> `BoundFieldOperand` whose `Place.Item` IS the cloned description (Table-16 legality, relation class dispatch,
> DISPLAY, the LENGTH fold all read it), and the CALL ABI's string carrier trio + `CobolArgAdapt.StoreReturn(string)`
> already deliver text/group images. The actual edits: (1) `UdfBinder` — the blanket 1510 became the per-shape
> `UdfReturningResidue` staging (float/boolean/pointer-class/index + the group residues: strong-typed, internal
> REDEFINES, variable-length, non-character leaves); (2) `DataBinder.CreateCompilerTemp` — a GROUP model deep-clones
> its subtree via the new UNREGISTERED `CloneTempNode` (a temp's subordinates are never referenceable, §8.4.3.2.3
> SR1 — registering the callee's LINKAGE names in the caller's scope would ambiguate legal names; contrast the
> TYPEDEF `CloneItem`, whose clones ARE referenceable); (3) `ConditionBinder.ComparisonOperandOf` — the computed
> fallback routes through the ONE `OperandOf` mapping so a relation operand's UDF result compares by its category;
> (4) `DataBinder.ConformanceForest` — prunes a temp's WHOLE subtree (a group temp's cloned children must not
> re-fire data-attribute gates in the caller's unit). Golden: `udf_returning_categories` (alnum MOVE/DISPLAY/
> LENGTH/relation, group MOVE/child-access/DISPLAY, edited DISPLAY + move-to-alnum, national MOVE/relation) +
> the rewritten `ReturningCategories_CarriedVsStaged1510` matrix in UdfInvocationTests. One golden (not the
> recipe's two) — all four legs in one runnable witness.

**Files:** `src/Cobol.Net.Compiler/Binding/Procedure/Verbs/UdfBinder.cs` (the `1510` staging), `Binding/DataBinder.Linkage.cs` (`UserFunctionSignature`), `Binding/Bound/BoundTree.cs` (a category-carrying result operand), `CodeGen/Verbs/CallEmitter.cs` (RETURNING carrier emit), `src/Cobol.Net.Runtime/Control/CallAbi.cs` (`CobolArgAdapt.StoreReturn` for text/group).

**Change:** Today only elementary fixed-point numeric RETURNING is implemented; alphanumeric/national/boolean and group RETURNING stage LOUD as `COBOLNET1510` because the result reads through `BoundNumRef` (numeric classifiers + numeric relation rendering). Lift it: make the UDF result temp carry the RETURNING item's *category* (route through the same operand renderers a normal reference uses — `OperandText`/`CobolString` for text, group-image codec for group), so an alphanumeric result compares as text and a group RETURNING clones a fully-described temp (not a Pic-less undeclarable one). Reuse the P5 single `CloneItem`/`CreateCompilerTemp` for the group result temp.

**Why:** Named in scope ("category-carrying non-numeric/group RETURNING — lift 1510"). This is the last correctness gap in the otherwise-complete UDF track.

**Verify:**
- New goldens `tests/conformance/2002/udf_alpha_returning.cob` (a `FUNCTION-ID` returning `PIC X(n)`) and `udf_group_returning.cob` → run byte-exact.
- Confirm the previously-`1510` programs now compile+run instead of the loud diagnostic.
- Existing `udf_*` goldens still green.

_(No commit — pairs with Step 10.)_

---

### Step 10 — UDF residue: BY VALUE header formals + per-evaluation activation + recursion verification — DONE 2026-07-16

**As built (the wave's actual scope — BY VALUE end-to-end, the 1509 per-evaluation lift, and the recursion
verification; the WS-static data-model split stayed OUT — see the deferral note):**

(a) **BY VALUE header formals end-to-end** (ISO §14.2.2 using-phrase :23636 / §14.2.3 GR4+GR10 / §8.4.3.2.4
GR5c): the PD-header using-phrase now parses per-parameter (`usingParameter` / `usingByReference` /
`usingByValue` in `CobolParserCore.g4` — the CALL `callArgument` precedent; the legacy oracle's three
`usingClause` consumers updated shape-only, all-BY-REFERENCE semantics preserved). `LinkageFormal` carries
`ByValue`; `CallBindLinkage` threads the GR4 transitivity, enforces §14.2.2 SR2 (**COBOLNET1553** — class
numeric/message-tag/object/pointer) and stages the SR2-legal-but-uncarried object/pointer/float shapes loud
(0899 `by-value-formal-carrier`); the OPTIONAL phrase parses and stages loud (0899 `optional-formal`). The
callee side delivers the GR10 VALUE COPY through the shared ABI: `CobolArgAdapt.NumValue`/`TextValue` —
DETACHED cells conformed to the formal (the "COMPUTE without ROUNDED" store via `CobolNum.Store`), the
copy-out loop skipped for BY VALUE — stores never reach the caller, on BOTH CALL targets and UDF
activations (one mechanism). UDF caller side: `UdfArg` is formal-aware (GR5c — every argument shape passes
`CobolPassMode.Value` to a BY VALUE formal) and §8.4.3.2.3 SR10 rejects non-numeric/object/pointer
arguments (**COBOLNET1554**). Registry row `pd-header-by-value-2002` (parse-arm `VisitUsingByValue`; 0900
at 85 proven, clean at 2002). Method-header BY VALUE/OPTIONAL parse and stage loud (the INVOKE channel is
P13+ work).

(b) **Per-evaluation activation — the 1509 guard NARROWED** (§8.4.3.2.4 GR1 :6963 / GR2 :6971 / GR6a :6995;
§8.8.4.13 r2): a new `BoundUdfEvaluated` condition node carries the drained activations of every
conditionally- or repeatedly-evaluated CONDITION window — PERFORM UNTIL and VARYING UNTIL (per iteration,
§14.9.28), SEARCH / SEARCH ALL WHEN (per pass, §14.9.37.4 GR5b), EVALUATE selection-object terms (per WHEN
consideration, §14.9.13.4 GR4a–d), and non-first AND/OR operands (§8.8.4.13 r1; XOR exempt) — attached by
`UdfBinder.UdfAttachPerEvaluation` at each binder window and rendered as an immediately-invoked
`Func<bool>` (ConditionRenderer), so the activation executes exactly when the condition text evaluates
(loop headers re-run it; C#'s `&&`/`||` short-circuit realizes r1 exactly). The statement hoist remains for
exactly-once positions. **COBOLNET1509 narrowed** to three genuinely-remaining OPERAND shapes
(`UdfStagePerEvaluationResidue`): a VARYING BY operand (per augment, GR12), an AFTER-level FROM
(re-evaluated per outer augment, GR13e.2), and an EVALUATE selection SUBJECT (once-per-statement per
§14.9.13.4 GR3, but the chained-selection lowering re-binds subjects per WHEN — a hoist would
over-activate). Audit-anchor drift recorded: the Step-1 claim "multiple invocations of one function in one
statement are hoisted ONCE" was stale — each occurrence already registers its own activation (GR2);
the actual gap was the conditional/repeated windows.

(c) **Recursion verified** (§8.6.6 :8821 "Functions and methods are always recursive" / §9.4 :12529):
`BinderDriver` already registers every FUNCTION-ID unit `Recursive` (citation strengthened), so
`ProgramTable`'s §14.9.4.4 GR3f re-entry rejection never fires for a function self-activation and the
per-activation instance model (D3/D4) applies; proven live by `udf_recursion` (5! = 120, five nested
activations — re-verified by running).

**⚠ Deferred (NOT in this wave):** the RECURSIVE **WS-static vs per-activation data-model split**
(§14.6.2.3.2/.3 — static WS should be last-used after the first activation while LOCAL-STORAGE/formals are
per-activation; today `Initial || Recursive ⇒ fresh instance per activation` re-initializes WS each
activation). ~~It remains the recorded deviation in §"Genuine open residue" below~~ — **taken by Step 10a
(2026-07-16, the RECURSIVE-WS slice; see the Step-10a checkbox for the full as-built)**; the `udf_per_eval`
golden deliberately proves activation counts through EXTERNAL data (last-used per run unit, §14.6.2.3.3),
not WS — which is why it stays green unchanged under the new model.

**Verify (all done):** goldens `udf_by_value` (BY VALUE mutation invisible / BY REFERENCE contrast /
literal-arg / CALL leg) and `udf_per_eval` (two-in-one-statement GR2; per-iteration UNTIL; AND/OR
short-circuit non-activation; per-WHEN EVALUATE object; per-pass SEARCH WHEN — an EXTERNAL activation
counter) verified by running then baked; negatives `by-value-formal-class` (1553) +
`udf-by-value-arg-class` (1554); `udf_recursion` still green; matrix row `pd-header-by-value-2002`;
GreenfieldOnly exclusions same change set; characterization 33/33; greenfield unit green.

---

### Step 11 — EC `-N` twins + `EXCEPTION-FILE-N` (needs Step 2) — **LANDED 2026-07-16**

**As built:** the 2023 §15 text defines exactly TWO -N EC twins — `EXCEPTION-FILE-N` (§15.29) and
`EXCEPTION-LOCATION-N` (§15.31); none exists for EXCEPTION-STATEMENT/-STATUS (P11 backlog note confirmed against
the §15.1/§15.2 table). Both flipped `Deferred`→`Runtime` in `IntrinsicCatalog.cs` as `EcFileN`/`EcLocationN`,
implemented in `src/Cobol.Net.Runtime/Exceptions/EcFunctions.cs` as **the base renderings projected national
through the ONE `CobolIntrinsics.NationalOf` repertoire translator** (`FileN() = NationalOf(File())`,
`LocationN() = NationalOf(Location())` — each -N section's "converted at runtime to the runtime national
character set" IS that §15.66.4 conversion; under D-N4 the Latin-1 code points keep their values, so the
compiler-observable delta is the result CATEGORY = National, carried by `IntrinsicSig.ResultCategory` and driving
Table-16 MOVE/compare legality). `IntrinsicRenderer.RenderString` gained the `EcFileN`/`EcLocationN` arms; the
2023 file-connector-argument form (E.3.3 items 25/26) renders loud on base AND twin (VCR rows 68/69 → PHASE-13
Step 9). Same wave, same channel: **CHAR-NATIONAL §15.16** landed (`CobolIntrinsics.CharNational` — the native
national PCS is UTF-16 code-unit order; the non-native ALPHABET … FOR NATIONAL weights overload
[`CharNational(n, NationalCollation)`] landed at Step 4) and **ORD over a national argument** landed per §15.70.3/§15.70.4 r2 (the 0844 CHAR/ORD guard narrowed
to CHAR with a §15.15.3 citation; a national ORD argument never routes to the alphanumeric `__COLLATE` weights).
Edition window: **IntroducedIn 2002 for both twins + CHAR-NATIONAL** (the EC model and national data are both
2002 introductions; the 2023 Annex E.3.3 delta is only the optional argument) — D8 `COBOLNET1502` below 2002.
`EXCEPTION-FILE` (non-national) confirmed unchanged (ECT018 + the base arm untouched).

**Verified (all green 2026-07-16):**
- Goldens `tests/conformance/2002/exception_file_n.cob`+`.out` (r1a "00" pre-EC, r1c "10TF" at EC-I-O-AT-END,
  FUNCTION LENGTH = 4/1 character positions, category-national MOVE + N"" compare; §15.31.3 r1 one national
  space) and `char_national.cob`+`.out` (ordinal→char, N"" equality, LENGTH=1, wide U+4E16 leg, ORD=19991) —
  ENABLED in the 2002 manifest; legacy `GreenfieldOnly` exclusions (no legacy EC model / national category).
- `exception-file-n-2002` constructs.json row (compile 2002/2014/2023, reject 1502 @85) + regenerated registry.
- Negative `tests/conformance/negative/exception_file_n_below_2002.cob`+`.err` (COBOLNET1502 @85).
- Inline EC-net Fact `IoAtEnd_ExceptionFileN_NationalTwin` (ECT018N @2023).
- The M4-2b catalog note ("EXCEPTION-FILE-N also blocked on national (a)") updated in
  `ISO2023_CONFORMANCE_PLAN.md` + `PHASE4_RECONCILIATION.md` (M4-2b LANDED; SMALLEST-ALGEBRAIC was already a
  Fold row).

**COMMIT BOUNDARY.** Suggested message:
```
feat(cobolnet): Phase 10 wave E — EC national intrinsics (EXCEPTION-FILE-N + -N twins) on the national channel

Flipped the national exception intrinsics from Deferred/loud to Runtime now that national CharImage
is confirmed; national-string returns via Runtime/Intrinsics. Golden + matrix. Battery green.
```

---

### Step 12 — ARITHMETIC IS STANDARD @2002/2014 — DONE (residual-leg closure; the consumption core predated P10)

**As built (2026-07-16; the Step-1 audit had already refuted this step's original "captured-not-consumed" premise
— the CombineCore/CobolDec/Store/Compare consumption was landed and golden-pinned pre-P10, so the wave closed the
six residual audit gaps instead; the gap-by-gap disposition is annotated in the ARITH-2 audit entry above and the
full detail in the Step-12 checkbox line):**

- **Runtime (`CobolDec.cs`):** `Pow` (§8.8.1.5.4 — integer exponents by square-and-multiply over `Mul`, exactly
  r2a–r2d for 1–4; r3 reciprocal; §8.8.1.2 r6 / r4 EC-SIZE-EXPONENTIATION; non-integer exponents = the r2e
  implementor-defined double approximation), `FromDouble` (§8.8.1.5.1 — the shortest-round-trip decimal identity;
  Inf ⇒ EC-SIZE-OVERFLOW, NaN ⇒ EC-DATA-INCOMPATIBLE), and the `Clamp` range check in the ONE `Round34Wide`
  funnel (§8.8.1.5.2 r2 — ±6144 adjusted-exponent overflow / 10⁻⁶¹⁷⁶-quantum underflow).
- **Emitters:** the StandardDecimal branch now precedes the D16 float branch in `NumericRenderer.CombineCore` /
  `Power` (float operands convert in via `DecOperand` → `FromDouble`); `IntrinsicRenderer`'s MEAN evaluates its
  division in SDIDI (§15.4.1 r1 / NOTE 2); `ReportWriterEmitter` carries the SUM-clause documented-equivalence
  derivation (§8.8.1.5.1 — every GR3 accumulation exact in both engines).
- **Staged LOUD:** 0899 `arithmetic-standard-intrinsic` (IntrinsicBinder) — ANNUITY / PRESENT-VALUE / VARIANCE /
  STANDARD-DEVIATION under the standard modes (inexact-division EAEs on the double engine cannot honor §15.4.1
  r1's equality). STANDARD-BINARY stays the documented-unsupported 0806 posture (§8.8.1.4.1 NOTE 1).
- **Edition gates (the 2002-vs-2014 edge resolved to 2002 — Annex E.2 item 21 + the M2 catalog + the OPTIONS
  reserved word @2002):** rows `options-paragraph-2002` (0804), `options-arithmetic-native-2002`,
  `arithmetic-standard-2002` (dual-window: 0900 below 2002, 0903-obsolete at 2014, 0807 at 2023 — VCR row 28
  anchors it), the 2014 keyword rows `arithmetic-standard-decimal-2014` / `arithmetic-standard-binary-2014`
  (pending — no compiling cell) on the `VisitArithmeticMethod` arm, and per-clause 2014 rows + arms for
  DEFAULT ROUNDED / INTERMEDIATE ROUNDING / ENTRY-CONVENTION (conservative 2014 — recorded ambiguity) /
  FLOAT-BINARY / FLOAT-DECIMAL / INITIALIZE. `OptionsBinder` routes inert below 2002.
- **Proof:** golden `tests/conformance/2002/arith_standard.cob` (hand-derived, verified by RUNNING against its
  native twin): 2/7*7 chain, exact `**` at 19 and 30 digits, the decimal128 RANGE-EC via ON SIZE ERROR, the
  COMP-2 float→SDIDI witness (0.1 × 3 exact vs the native binary artifact), and the §15.4.1 NOTE-2 MEAN
  relation. Legacy `GreenfieldOnly` exclusion. Negatives: `arith-standard-at-85`,
  `arith-standard-decimal-at-2002`, `options-default-rounded-at-2002`, `arith-standard-intrinsic-staged`.
- **Recorded residues (silent, documented — not staged):** exact-family intrinsic results past 34 significant
  digits keep MORE precision than the per-op-rounded SDIDI (FACTORIAL 31–33, >34-digit SUM chains — runtime
  magnitudes, bind-undecidable; CobolIntrinsics.Exact.cs header); NUMVAL's long-carrier cap is mode-independent
  and pre-existing.

---

### Step 13 — Report Writer 2002: PRESENT WHEN format 1 + VARYING format 1 — ✅ LANDED 2026-07-16

**As built** (the recipe below predates the spec read; the §13.18.41/§13.18.64 text overrode three of its guesses):
the full record is the Step-13 checkbox in §4. The three recipe corrections: (1) the citation is **§13.18.41**
(not "§13.18.44-ish") and VARYING is **§13.18.64** — a REPETITION counter over a repeating entry (a multiple
LINE/COLUMN clause or report-group OCCURS, SR1), NOT a "report loop variable"; there is no loop statement — the
counter steps once per COLUMN/LINE operand (GR3a/GR3b) and is referable only within its entry (SR2). Landing
VARYING therefore REQUIRED the §13.18.14 multiple/relative COLUMN forms (landed; multiple LINE stages 0899 with
the OCCURS repetition family it is GR9-equivalent to). (2) No `{is2002()}?` grammar gate — the two-arm
VersionConformancePass owns ALL edition gating (Exec Step E); the clauses superset-parse and the ParseArm fires
0900 below 2002. (3) ONE golden (`rw_present_when` — it exercises BOTH clauses; `rw_varying` folded in) and the
row ids are `report-present-when-2002` / `report-varying-2002` / `report-multi-column-2002` (+ pending
`report-multi-line-2002`); negatives `present-when-at-85` / `report-varying-no-repetition` /
`present-when-group-indicate` (the §13.15.3 SR16/SR17 + §13.18.64.3 SR1–SR3 families = COBOLNET1559).

**Verified:** golden byte-exact by RUNNING (hand-derived RWCS grid — the absent line COLLAPSES: presentation 2's
TAIL re-anchors at LINE-COUNTER+1 directly under ROW-2); `--std 85` rejects with 0900; the RW '85 surface is
untouched (plain compose/construction emission byte-identical; characterization + rw suites green).

---

### Step 14 — `&`-concatenation operator §8.8.3 — ✅ LANDED 2026-07-16

**As built** (the recipe below predates the spec read; the §8.8.3 text overrode two of its guesses):
- **The construct is a COMPILE-TIME LITERAL, not a runtime operator.** §8.8.3.1's operands are only
  literals / figurative constants / concatenation expressions (never identifiers), and §8.8.3.3 GR3 makes the
  result "equivalent to a literal of the same class and value … used anywhere a literal of that class may be
  used" — so there is NO `BoundConcat` node and NO emitter/`OperandText` leg: `Binding/ConcatFolder.cs` (the
  ONE fold chokepoint) collapses the parse-tree `concatenationExpression` into the equivalent single literal,
  and every consumer rides the pre-existing literal channels. Boolean `&` (`B"01" & B"10"` → `B"0110"`) folds
  the same way — there is no "runtime concat for bit strings" (both operands are literals by grammar).
- **Grammar:** `AMPERSAND : '&'` (CobolLexer.g4, §8.7.3 — no continuation interaction; the separator-space
  rule is token-stream lenient like `::`) + `concatenationExpression : concatOperand (AMPERSAND concatOperand)+`
  as a distinct additive FIRST alternative of `nonNumericLiteral` (no shared-rule restructure; `&` appears in
  no other rule, so prediction is exact). SUPERSET PARSE at every edition.
- **Gate:** NOT an `{is2002()}?` predicate — per the Exec-Step-E doctrine the ONE funnel is the
  VersionConformancePass parse arm: `VisitConcatenationExpression` → `Check(Constructs.ConcatOperator2002)`
  (position-blind recognition; 0900 below 2002; `constructs.json` row ACTIVE, `expectDiagnostic` 0900).
- **SR diagnostics (one code per rule, DiagnosticCatalog):** COBOLNET1540 `concat-class-mismatch` (SR1
  same-class; figurative adaptation GR1a/GR1b; NULL has no character value), COBOLNET1541
  `concat-all-figurative` (SR1 — no ALL-prefixed figurative), COBOLNET1545 `concat-result-too-long`
  (SR2–SR4 8,191 cap). Figuratives fold ONE character (§8.3.3.6.4 GR3a; H/L use the PCS extremes when
  active, else the native U+00FF/U+0000 pins; boolean class admits only ZERO).
- **Consumers wired through the fold:** `ExpressionBinder.LiteralOperand/ConcatOperand/NumLiteral` (0844 in a
  numeric context), `ConditionBinder` (comparisons + the boolean channel), `EvaluateBinder`, `InspectBinder`,
  `IntrinsicBinder` (FUNCTION arguments), `CallBinder` (CALL/CANCEL literal-1), `ControlFlowBinder` (STOP),
  `OoBinder` (INVOKE method-name + literal args; its inline hex decode unified into `CobolLiteral.DecodeHex`),
  `DataBinder` VALUE capture (`ExtractValue`/`RawValueOperandText` incl. level-88s and Report Writer VALUE),
  `DataBinder.Switches` ALPHABET/CLASS literals, `OptionsBinder` INITIALIZE fill.

**Verified:**
- Golden `tests/conformance/2002/literal_concat.cob/.out` ENABLED (subsumes the planned
  `concat_literal`/`concat_boolean` pair): every legal class pair (alnum incl. X"…" hex, national, boolean),
  figurative operands, a VALUE-clause use, a level-88 VALUE, MOVE/DISPLAY/IF/EVALUATE uses, a boolean
  relation, a multi-operand chain, and `FUNCTION LENGTH("AB" & "CD")` → 4 — run byte-exact. Legacy
  `ConformanceTests` GreenfieldOnly exclusion (the frozen legacy grammar has no `&` token).
- `--std 85` → 0900 (CLI-verified + `concat_below_2002` negative); `"AB" & N"CD"` → 1540
  (`concat_class_mismatch` negative at 2002/2014/2023); matrix row active across all four editions.
- **Grammar change → FULL legacy guard** rides the supervising battery (`scripts/guard.sh` NIST 353 MATCH).

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

### Step 16 — TYPEDEF residue: EXTERNAL type declaration / SAME AS / strong-group heterogeneous relations — ✅ LANDED 2026-07-16 (as-built below; the checkbox entry carries the full detail)

**Files (as built):** `src/Cobol.Net.Frontend/Grammar/Core/CobolData.g4` (`sameAsClause`) + `Cst/DataDescriptionCst.cs` (`SameAsTargetName`/`SameAsQualifiers`), `src/Cobol.Net.Compiler/Binding/DataBinder.cs` (`ExpandSameAs` + the shared `CopyEntryDescription` inside the ONE `ExpandTypes` pass; `CloneItem` levelDelta; the ExpandType §13.18.22 legs), `Binding/Model/DataItem.cs` (`SameAsName`/`SameAsQualifiers`/`IsExternalTypedef`/`HasExternalClause`/`ExternalFromType`), `Binding/DataBinder.Linkage.cs` (`CallBindExternalAndGlobal` external-by-type re-basing), `Binding/Validation/StatementValidation.cs` (the relation checkpoint: SR4 reclassified + the §8.8.4.2.12 signed-leaf stage), `Validation/VersionConformancePass.cs` (`VisitSameAsClause`), `Editions/Diagnostics/DiagnosticCatalog.cs` (1555/1556/1557/1558 + `strong-compare-ordering`/`typedef-renames-staged`/`strong-group-ordering-signed-leaf`).

**Scout corrections that reshaped the plan (recorded — three prior waves found the same class of drift):**
1. The plan's leg 1 ("register + resolve cross-unit, the OO class-table precedent") over-modeled the feature: §13.18.22 GR2/GR3 make an EXTERNAL typedef a CONFORMANCE surface + a record-external attribute — the records ride the EXISTING run-unit ExternalStore matching (GR6, by externalized name), no cross-unit type registry exists or is needed. The old 1534 message's "§13.18.57.4 GR5" citation was wrong (GR5 = a report-group rule).
2. The plan's leg 3 ("wire into CheckedRelational, the 1532 band") was ALREADY DONE pre-wave: §8.8.4.2.3 SR1 fires as 1533 `strong-compare-mismatch` at the ONE relation checkpoint (and SR4 as 1535). The wave's real work was the SR4 reclassification + the §8.8.4.2.12 signed-ordering stage + the corpus negative.
3. The plan's `external-typedef-2002` matrix row is NOT warranted: TYPEDEF itself is unreachable below 2002 (`typedef-def-2002`), so an EXTERNAL-typedef row would double-fire 0900 on one entry. Only `same-as-clause-2002` was added.

**Verified:** `typedef_same_as` + `typedef_external` goldens RUN-verified then baked (enabled; legacy GreenfieldOnly); negatives `same-as-at-85` (0900) + `strong-group-heterogeneous-compare` (1533 — the band is 1533, not the plan's guessed 1532); `SameAsTests` (1555/1556/1557 per-SR + qualified/chained/EXTERNAL-GLOBAL positives); `TypedefResidueTests` (1558 ×2 + the 0899 signed-ordering stage + equality companion); existing typedef goldens green; characterization 33/33.

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
- **If a grammar change destabilizes the SLL/LL parse** (Steps 7, 13, 14, 15): the risk is a DFA ambiguity on a shared core rule. Mitigation: keep every new alternative ADDITIVE with a unique leading token; run the FULL legacy guard on the grammar-touching commit BEFORE enabling any new golden; if the 85 surface shifts even one byte, revert the grammar edit and re-approach bind-side (the M2-UDF-4 keyword-omitted precedent: resolve semantically at bind, not in the grammar).
- **If `StorageForm` (P5) is not actually complete** when P10 starts: Steps 2, 9, 15, 16 hard-depend on it. Do NOT fake it with the old `StoreAsImage` flag (P5 deleted it, `feedback_no_transitional_hacks`). Block on P5; the pointer/file/UDF-VALUE/RW/concat waves (6, 7, 8, 10, 13, 14) are mostly independent of `StorageForm` and can proceed first — reorder §4 accordingly and note it in STATUS.
- **Numeric-pipeline steps (12)**: the ARITHMETIC-STANDARD wiring must leave `NATIVE` byte-invariant. If the guard diverges, the mode threading leaked into the default path — gate it strictly on `mode != Native`.
- **Idempotency:** re-running a completed step is safe (goldens already enabled, matrix rows already present) — the manifest integrity fact and matrix drift test will simply pass.

---

## 7. ISO feature work in this phase — spec sections, editions, conformance artifacts

All tracks are **COBOL-2002 introductions** (carried unchanged through 2014/2023 unless a VCR delta is noted), so each owes: (a) the complete 2002+ behavior AND (b) a rejecting diagnostic at `--std 85`. `VERSION_CHANGE_REFERENCE.md` has NO 85→2002 rows — derive 85↔2002 gating from the 2002 standard / the §3 catalog.

| Track | Spec § (ISO/IEC 1989:2023, `specs/ISO_COBOL.md`) | Edition | Positive golden(s) → `manifest.json` enabled | Version-matrix row(s) `constructs.json` | Negative `.err` |
|---|---|---|---|---|---|
| National data | §13.16.6 / §13.18 USAGE NATIONAL; Table 16 MOVE; §8.8.4.2.9 compare; §14.9.11.4 | 2002 | `national_data` (enabled — confirm) | `national-usage-2002`, `pic-n-2002` | national-narrowing reject; national-at-85 |
| ALPHABET national encodings — **LANDED (Step 4)** | §12.3.7.2 ALPHABET FOR NATIONAL (NATIVE/UCS-4/UTF-8/UTF-16/literal-phrase); §12.3.6 PCS FOR forms; §14.9.40.2 SORT/MERGE FOR forms; §12.3.7 GR7 Table 6 | 2002 | `alphabet_national` ✓ | `alphabet-national-2002` ✓, `program-collating-national-2002` ✓, `sort-collating-national-2002` ✓ (activated) | alphabet-national-at-85 ✓; alphabet-utf8-collating ✓; alphabet-standard1-for-national ✓; alphabet-ucs4-for-alphanumeric ✓ (0898) |
| Boolean data + operators | §13.18.40 USAGE BIT/PIC 1; §8.8.2/§8.8.4.2.8 boolean expr/compare; §14.9.8 F2 COMPUTE | 2002 (B-SHIFT = 2023) | `boolean_data`, `boolean_ops` (enabled — confirm) | `usage-bit-2002`, `boolean-operator-2002` | boolean-ordering-relation; bit-usage-numeric-pic |
| Pointers / ALLOCATE / FREE / BASED | §13.18 USAGE POINTER/PROGRAM-POINTER; §8.8.4.2 equality; §13.18.5 GR3/4 deref; §14.9.5 ALLOCATE, §14.9.16 FREE | 2002 | `based_pointer`, `pointer_alloc`, `pointer_arith` (confirm), `program_pointer` (new) | `usage-program-pointer-2002`, `allocate-2002`, `free-2002` (confirm) | allocate-non-based; program-pointer-deref |
| File-2002 | §12.4.5.15 SHARING; §14.7.9 RETRY; §9.1.13.8/.9 status 5x/6x; line-sequential org | 2002 | `file_sharing` (confirm) + a 04/39-status golden | `file-sharing-clause-2002`, `user-word-sharing-2002` | close-with-lock; shared-lock-conflict |
| UDF residue | §8.4.3.2.4 GR1/GR5; §14.9.4 GR5c BY VALUE; §14.6.2.3.2/.3 static/per-activation; §9.4 recursive | 2002 | `udf_returning_categories` ✅ (Step 9), `udf_by_value` ✅ (Step 10), `recursive_ws` ✅ (Step 10a — the static/per-activation model) | `pd-header-by-value-2002` ✅, `local-storage-section-2002` ✅ | udf-returning-as-receiver |
| EC national intrinsics — **LANDED (Step 11)** | §15.29 EXCEPTION-FILE-N + §15.31 EXCEPTION-LOCATION-N (the only `-N` twins) + §15.16 CHAR-NATIONAL | 2002 | `exception_file_n` + `char_national` ✓ | `exception-file-n-2002` ✓ (+ `exception_file_n_below_2002` negative) | (n/a — runtime) |
| ARITHMETIC IS STANDARD | §11.9.5 ARITHMETIC clause; §8.8.1.5 standard-decimal intermediate | 2002 (obsolete 2014; removed 2023 — Annex E.2 item 21) | `arith_standard` (LANDED, Step 12) | `arithmetic-standard-2002` (LANDED) | arith-standard-at-85 (LANDED) |
| Report Writer 2002 — **LANDED (Step 13)** | §13.18.41 PRESENT WHEN (format 1); §13.18.64 VARYING (format 1) + the §13.18.14 multiple/relative COLUMN vehicle | 2002 | `rw_present_when` ✓ (the one golden covers both clauses) | `report-present-when-2002` ✓, `report-varying-2002` ✓, `report-multi-column-2002` ✓ (+ `report-multi-line-2002` pending) | present-when-at-85 ✓; report-varying-no-repetition ✓ (1559 SR1); present-when-group-indicate ✓ (1559 SR17) |
| `&`-concatenation — **LANDED (Step 14)** | §8.8.3 concatenation expression | 2002 | `literal_concat` ✓ (the one golden subsumes the planned pair — boolean leg included) | `concat-operator-2002` ✓ ACTIVE | concat_below_2002 ✓ (+ `concat_class_mismatch` SR1 negative) |
| CONSTANT entries | §13.10 constant entry; §13.18.15 CONSTANT RECORD | 2002 | `constant_entry`, `constant_record` | `constant-entry-2002`, `constant-record-2002` | constant-as-receiver; constant-at-85 |
| TYPEDEF residue — ✅ LANDED (Step 16) | §13.18.49 SAME AS; §13.18.22 EXTERNAL type; §8.8.4.2.3 SR1/SR4 + §8.8.4.2.12 strong-group relations | 2002 | `typedef_same_as` ✓, `typedef_external` ✓ | `same-as-clause-2002` ✓ ACTIVE (no `external-typedef` row — TYPEDEF is unreachable below 2002, the `typedef-def-2002` gate covers the composition) | strong-group-heterogeneous-compare ✓ (1533) + same-as-at-85 ✓ (0900) |

**Diagnostic bands (reuse the established greenfield bands; do not collide):** `0814` national literal, `0819` national/boolean usage/MOVE legality, `0844` boolean relation misuse, `0869`/`0881` pointer bands, `0870` binary-usage-PICTURE, `0900`/`0901` edition-introduction / reserved-word funnel, `15xx` (`1510` UDF RETURNING category — being LIFTED; `1532` strong-type relations). New constraints pick the next free code in the relevant band and register a one-code-one-rule descriptor (per the DESIGN-test-build-ci diagnostic-registry direction) rather than reusing `0899`.

**Owner process rules that bind this phase:** every feature ships its conformance test IN THE SAME COMMIT (`feedback_conformance_tests_per_feature`); grammar changes are pre-authorized but require the FULL legacy guard in the same change set (`feedback_grammar_approval`, `feedback_legacy_suite_on_shared_corpus`); implement COMPLETELY to spec + design, tests verify never scope (`feedback_spec_scopes_not_tests`); cite the § in code for every semantic decision (`feedback_use_the_spec`); verify by RUNNING, not just compiling (`feedback_verify_demo_output`).
