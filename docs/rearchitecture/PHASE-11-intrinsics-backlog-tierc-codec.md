# PHASE 11 — Deferred-intrinsics backlog to zero + Tier-C REDEFINES confined-byte codec

- **Phase:** P11
- **Track:** feature-iso
- **Risk:** MEDIUM
- **Depends on:** P10 (M2 residual catalog — national/boolean data + operations, pointers, UDF, file-2002, RW/CONSTANT/concat). Transitively P5 (unified data model: `StorageForm`/`StorageFormPass`), P8 (runtime reorg), P9 (M2 OO).
- **Can run parallel to:** P12 (M3 2014 deltas). These are leaf functions in one catalog file + one REDEFINES classifier seam; they do not touch the emitter dispatch spine, so P12's data-model deltas and this phase's intrinsic rows rarely collide.

## STATUS

```
STATUS: IN PROGRESS @ step 6 (Steps 0-2 + 5 DONE — Step 1 @ 2a0ab666 [Unsupported + the five A.4.9 flips];
Step 2 @ e159a719 [boolean conversions + the Boolean result-category channel]; Steps 3-4 SATISFIED BY P10;
Step 5 [the Y2K windowing trio on the ONE YearToYyyy core + SECONDS-PAST-MIDNIGHT on the RunUnit.Clock
seam, scale 7] landed next commit. Deferred 17→6. Resume: read PHASE-11-scout-notes.md FIRST, then execute
Step 6 [the four TEST-* validators §15.90/91/93/94 — the scout's exact verdict-code tables; the TEST twins
live in CobolIntrinsics.Exact.cs beside TestNumvalF, NOT Text.cs])
```
> The executing session updates this line to `IN PROGRESS @ step N` and finally `DONE`. Keep it in sync with the per-step checkboxes in §4.

> **Step 0 baseline (2026-07-17, tree at `45fe74dd`, all green):** greenfield conformance **3467/3467** ·
> greenfield unit **292/292** · the legacy guard `guard-fast.sh` **NIST 353 MATCH, 0 regressions** (legacy
> unit 1196 + integration 636 ALL GREEN) · solution build 0 warnings 0 errors.
>
> **The Step-1 enumeration is DONE:** the live `IntrinsicBind.Deferred` set is **17 rows** (catalog lines
> 133–174) = §3.1's 22 minus the 5 P10-landed rows (CHAR-NATIONAL, DISPLAY-OF, NATIONAL-OF,
> EXCEPTION-FILE-N, EXCEPTION-LOCATION-N — already `Runtime`, verified in the catalog source).
>
> **The P10-lesson anchor re-scout is DONE** (11 parallel scouts over the spec + code, 2026-07-17):
> **`PHASE-11-scout-notes.md`** carries the full verified findings — exact §/GR/SR anchors with spec line
> numbers, hand-derived golden values per family, the end-to-end code seams (catalog row → binder →
> renderer → runtime, with file:line), the conformance/matrix/negative test wiring (the P10
> `exception_file_n` worked example), and the complete Tier-C guard-site inventory. **Its ⚠ gotcha /
> discrepancy blocks OVERRIDE this doc where they conflict** (e.g. `ComputeTier` is at
> `DataBinder.cs:2376`, not ~1752; BOOLEAN-OF-INTEGER(0, n) is an ambiguity resolved accept-0; left
> truncation to argument-2 bits is NORMAL per §15.13.4 r1 + Annex D.10). Do NOT re-scout — read the notes.

---

## Goal (one paragraph)

Drive the intrinsic-function catalog to **zero `IntrinsicBind.Deferred` rows** — every catalogued-but-unimplemented
function is either **implemented** (a real `CobolIntrinsics`/`CobolDate` runtime body + a renderer arm + a value-exercising
conformance golden) or **dispositioned** (the 5 Annex A.4.9 locale-module functions + the LOCALE keyword variants resolve to a
uniform *documented non-support* diagnostic per ratified decision 3), with each promotion carrying a **window-enforcement
negative row** (the function under an earlier `--std` emits the per-edition diagnostic). In the same phase, **decide Tier-C**:
consolidate the ~10 scattered "Tier-C byte island, deferred" loud guards into **one** `RedefinesClassifier` verdict (the
single-sourced rejection that satisfies the invariant on its own), and — recommended — implement the **sanctioned confined
`byte[]` codec** behind `StorageForm.TierCWindow` + a `GroupImageCodec` byte path, the one `byte[]` boundary the typed-native
invariant permits. The full battery stays green at every commit boundary.

## Exit criteria (from the roadmap)

1. **Zero `Deferred` intrinsic rows** — each row implemented OR dispositioned with an enforced edition window.
2. **Tier-C decided** — either the confined-`byte[]` codec is green, or the rejection is single-sourced into one verdict
   (the ~10 scattered guards collapsed).
3. **Every promotion has a conformance golden** exercising the *actual value* (not just "compiles" / "not loud").
4. **Every promotion has a window-enforcement negative row** (later-edition function under an earlier `--std` → the per-edition
   diagnostic).
5. **Full battery green** (greenfield conformance + 213 unit + full legacy guard NIST 353 MATCH).

## Scope

**IN**
- Every current `IntrinsicBind.Deferred` row in `src/Cobol.Net.Compiler/Binding/IntrinsicCatalog.cs`, by family (the 2002 set,
  the 2014 residue, the 2023 rows in scope).
- ~~`DISPLAY-OF` / `NATIONAL-OF` + the EC `-N` national twins (`EXCEPTION-FILE-N`, `EXCEPTION-LOCATION-N`)~~ —
  **ALREADY LANDED IN P10** (Step 5: DISPLAY-OF/NATIONAL-OF; Step 11: the EC `-N` twins as `EcFunctions.FileN/LocationN`
  on the ONE `NationalOf` translator + the `exception_file_n` golden/matrix/negative). Steps 3–4 below are SATISFIED
  except their locale/codepage prose — nothing left for P11 here.
- ~~`CHAR-NATIONAL`~~ **ALREADY LANDED IN P10 Step 11** (`CharNational`, native national PCS + ORD-over-national
  §15.70.4 r2; `char_national` golden); `BOOLEAN-OF-INTEGER`, `INTEGER-OF-BOOLEAN` — still promoted on the P10 boolean base.
- The 5 A.4.9 locale functions (`LOCALE-COMPARE`, `LOCALE-DATE`, `LOCALE-TIME`, `LOCALE-TIME-FROM-SECONDS`, `STANDARD-COMPARE`)
  **+** the `LOCALE` keyword variants of `LOWER-CASE`/`UPPER-CASE`/`TEST-NUMVAL-C` → the **decision-3 documented non-support
  diagnostic path**.
- `SMALLEST-ALGEBRAIC` (already a `Fold`; add its golden + window-enforcement row).
- The Tier-C confined `byte[]` codec into `StorageForm.TierCWindow` + a `GroupImageCodec` byte path — **or** the single-sourced
  rejection.

**OUT**
- 2023-only intrinsics that ride **P13** (e.g. the `EXCEPTION-FILE(file-connector-name)` 2023 optional-argument form —
  VCR row 68 — stays loud here; already handled by P13).
- JSON/XML (non-ISO; deleted/quarantined in P1).
- The unified-data-model refactor itself (`StorageForm`/`StorageFormPass` are consumed here, authored in P5).

---

## 2. Rationale — the problems this phase fixes

### 2.1 The `Deferred` catalog rows are latent gaps behind a loud guard

`IntrinsicCatalog.Build()` (`src/Cobol.Net.Compiler/Binding/IntrinsicCatalog.cs`) catalogues every ISO §15 function so that
**edition gating and arity checks apply**, but rows with `IntrinsicBind.Deferred` have no runtime body and no renderer recipe.
At emit time they degrade to a **loud not-implemented guard**:

- `IntrinsicRenderer.RenderNum` (`.../CodeGen/Emit/IntrinsicRenderer.cs:45`):
  `if (sig.Bind == IntrinsicBind.Deferred …) return new NumX(EmitText.LoudValue("long", $"FUNCTION {sig.Name} (catalogued, not yet implemented)"), 0);`
- `IntrinsicRenderer.RenderString` (`.../IntrinsicRenderer.cs:245`): the string-channel twin.

Loud is correct (never a wrong value — COBOLNET_DESIGN §1.4), but a `Deferred` row means a **conformant program fails at run
time**. The roadmap (`docs/COMPLETION_ROADMAP_COUNCIL.md` Phase 5, lines 67-69) requires each row **implemented or
dispositioned** with a **non-provisional or explicitly-blocked** window, and **window-enforcement negative rows** (the critics'
"dangling gap": a later-edition function silently accepted under an earlier `--std`).

> **NOTE — the "43" figure is stale.** The roadmap and the phase brief say "43 Deferred". Several 2002/2014 rows have
> already been promoted (ABS, SIGN, E/PI/EXP/EXP10, FRACTION-PART, the FORMATTED-* family, NUMVAL-F, the EXCEPTION-* alphanumeric
> quartet, etc.). **The executing session MUST re-enumerate the actual Deferred set first** (step 1). At the time of writing the
> live set is the **22 rows** listed in §3.1. Work the *actual* set, not the number.

### 2.2 The window-enforcement mechanism already exists — the gap is test coverage

`BindIntrinsicCore` already enforces the edition window generically:
`IntrinsicCatalog.cs` rows carry `IntroducedIn`/`RemovedIn`, and `Binding/Procedure/Verbs/IntrinsicBinder.cs:122-125` emits **COBOLNET1502**
(introduced-later) / **COBOLNET1503** (removed) by name+edition. So "window-enforcement negative rows" are **conformance test
cases**, not new binder code — each promoted function needs a `--std <earlier>` fixture asserting the 1502/1503 diagnostic. Some
`IntroducedIn` values are **provisional** (the catalog comment, `IntrinsicCatalog.cs:50-52`); firming them is part of each
promotion (see §7 for the per-family window authority).

### 2.3 The A.4.9 locale functions cannot be *implemented* — they must be *dispositioned*

`LOCALE-COMPARE/-DATE/-TIME/-TIME-FROM-SECONDS` (`IntrinsicCatalog.cs:144-147`) and `STANDARD-COMPARE` (`:149`) belong to the
optional **A.4.9 locale module**. Ratified **decision 3** (`docs/COMPLETION_ROADMAP_COUNCIL.md:119`) is **documented
non-support** for A.4.9. §4.2.7 (the documentation route, cited at `COMPLETION_ROADMAP_COUNCIL.md:266`) makes documented
non-support of an optional module **conformance-legal**. So these rows must move from `Deferred` (a *loud runtime* "not
implemented") to a **bind-time documented-non-support diagnostic** naming A.4.9 — a distinct, permanent verdict, not a TODO. The
same applies to the `LOCALE` keyword variants of `LOWER-CASE`/`UPPER-CASE`/`TEST-NUMVAL-C` (§15.57/§15.97/§15.94), which are
implemented today **without** locale support and currently reject a `LOCALE` phrase only as a generic arity error.

### 2.4 Tier-C REDEFINES is a declared-but-unimplemented tier scattered across ~10 guards

`RedefinesTier.ByteCanonical` (`src/Cobol.Net.Compiler/Binding/Model/RedefinesModel.cs:49`) — a genuine mixed-USAGE pun (a
COMP/COMP-1/2/3/5/INDEX leaf observed cross-view) whose shared area is one class-scoped `byte[]` — is **declared but
unimplemented**. `ComputeTier` (`DataBinder.cs:1752`) maps it to `Rejected` with a loud reason, and **the downstream call
sites re-check the same fact** and each emit their own loud guard (grep `Tier-C`):

`DataBinder.cs`, `DataBinder.Linkage.cs`, `OoClassTable.cs`, `ReferenceResolver.cs`, `Binding/Procedure/ExpressionBinder.cs`,
`Binding/Procedure/Verbs/InitializeBinder.cs`, `Binding/Procedure/Verbs/OoBinder.cs`, `Binding/Procedure/Verbs/SortBinder.cs`,
`CodeGen/DataDivision/GroupValueSlicer.cs`, `CodeGen/DataDivision/RecordStructEmitter.cs`, `CodeGen/Emit/OperandText.cs`,
`CodeGen/Emit/NumericRenderer.cs`, `CodeGen/Verbs/AcceptDisplayEmitter.cs`, `CodeGen/Verbs/CallEmitter.cs`, `CodeGen/Verbs/InspectEmitter.cs`,
`CodeGen/Verbs/MoveEmitter.cs`, `CodeGen/Verbs/SequentialIoEmitter.cs`, `CodeGen/Verbs/SortEmitter.cs`, `CodeGen/Verbs/StringEmitter.cs`.

They are all keyed off one predicate (`DataItem.IsImageCapable` / the `Rejected` verdict). The data-model design
(`docs/rearchitecture/DESIGN-data-model.md` §2.3) recommends **single-source the rejection now, implement the codec later**; the
phase brief permits **either** (exit: "codec green **or** single-sourced rejection"). This phase does the single-source
consolidation **unconditionally** (§4 step group C — the safety net), then implements the codec behind that one seam **as an
additive, fully-specified increment** (step group D — recommended).

---

## 3. Target end-state for this phase

### 3.1 The live Deferred set (re-verify with step 1) and its disposition

| # | Function | § | Row (catalog line) | Disposition | New `RuntimeMethod` / verdict |
|---|---|---|---|---|---|
| 1 | `BOOLEAN-OF-INTEGER` | §15.13 | `:124` | implement | `BooleanOfInteger` |
| 2 | `INTEGER-OF-BOOLEAN` | §15.45 | `:143` | implement | `IntegerOfBoolean` |
| 3 | `BYTE-LENGTH` | §15.14 | `:125` | implement (compile-time fold, byte size) | `Fold` (BYTE-LENGTH) |
| 4 | `CHAR-NATIONAL` | §15.16 | `:126` | **LANDED (P10 Step 11)** | `CharNational` ✓ |
| 5 | `DISPLAY-OF` | §15.26 | `:130` | **LANDED (P10 Step 5)** | `DisplayOf` ✓ |
| 6 | `NATIONAL-OF` | §15.66 | `:131` | **LANDED (P10 Step 5)** | `NationalOf` ✓ |
| 7 | `EXCEPTION-FILE-N` | §15.29 | `:133` | **LANDED (P10 Step 11)** | `EcFileN` ✓ |
| 8 | `EXCEPTION-LOCATION-N` | §15.31 | `:138` | **LANDED (P10 Step 11)** | `EcLocationN` ✓ |
| 9 | `DATE-TO-YYYYMMDD` | §15.23 | `:127` | implement (windowing) | `DateToYyyymmdd` |
| 10 | `DAY-TO-YYYYDDD` | §15.25 | `:128` | implement (windowing) | `DayToYyyyddd` |
| 11 | `YEAR-TO-YYYY` | §15.100 | `:129` | implement (windowing) | `YearToYyyy` |
| 12 | `SECONDS-PAST-MIDNIGHT` | §15.80 | `:148` | implement (runtime clock) | `SecondsPastMidnight` |
| 13 | `TEST-DATE-YYYYMMDD` | §15.90 | `:150` | implement (validation) | `TestDateYyyymmdd` |
| 14 | `TEST-DAY-YYYYDDD` | §15.91 | `:151` | implement (validation) | `TestDayYyyyddd` |
| 15 | `TEST-NUMVAL` | §15.93 | `:152` | implement (validation) | `TestNumval` |
| 16 | `TEST-NUMVAL-C` | §15.94 | `:153` | implement (validation) | `TestNumvalC` |
| 17 | `CONCATENATE` | §15.18¹ | `:156` | implement (window `[2002,2023)`) | `Concat` (reuse) |
| 18 | `LOCALE-COMPARE` | §15.51 | `:144` | **disposition A.4.9** | `Unsupported(A49)` |
| 19 | `LOCALE-DATE` | §15.52 | `:145` | **disposition A.4.9** | `Unsupported(A49)` |
| 20 | `LOCALE-TIME` | §15.53 | `:146` | **disposition A.4.9** | `Unsupported(A49)` |
| 21 | `LOCALE-TIME-FROM-SECONDS` | §15.54 | `:147` | **disposition A.4.9** | `Unsupported(A49)` |
| 22 | `STANDARD-COMPARE` | §15.85 | `:149` | **disposition A.4.9** | `Unsupported(A49)` |

¹ `CONCATENATE` is the 2002/2014 name; the row carries `RemovedIn = 2023`. The 2023 standard's `CONCAT` (§15.18) is a separate,
already-implemented row. The renderer can reuse the `Concat` runtime body (both concatenate argument images) while the two rows
keep distinct windows.

**Plus**, not `Deferred` but in scope: `SMALLEST-ALGEBRAIC` (§15.83, `:178`, already `Fold` via `BindAlgebraicFold`) needs a
value golden + a window-enforcement row; and the **`LOCALE` keyword variants** of `LOWER-CASE`/`UPPER-CASE`/`TEST-NUMVAL-C` get
the A.4.9 disposition bind path.

### 3.2 Files that exist / change when this phase is DONE

**Catalog + binder (Compiler):**
- `src/Cobol.Net.Compiler/Binding/IntrinsicCatalog.cs` — no row is `IntrinsicBind.Deferred`. A **new** `IntrinsicBind` case
  `Unsupported` (or a `Module` tag field) carries the A.4.9 rows. `RuntimeMethod` filled for every implemented row; windows
  firmed (provisional→attributed where §7 gives authority, else explicitly annotated blocked).
- `src/Cobol.Net.Compiler/Binding/Procedure/Verbs/IntrinsicBinder.cs` — a new bind arm mapping `Bind == Unsupported` to the
  A.4.9 diagnostic **COBOLNET1518**; the `LOCALE`-phrase detection for `LOWER-CASE`/`UPPER-CASE`/`TEST-NUMVAL-C`; `BYTE-LENGTH`
  folds through the `Fold` path (a `BindByteLengthFold` beside `BindLengthFold`); category resolution for national-result rows.

**Renderers (Compiler CodeGen):**
- `src/Cobol.Net.Compiler/CodeGen/Emit/IntrinsicRenderer.cs` — `RenderNum` and `RenderString` gain one arm per new
  `RuntimeMethod`. `Unsupported` rows are never reached (the binder produced a `BoundExprError`), so the `Deferred` loud fallback
  arms remain only as the never-hit backstop.

**Runtime (Runtime):**
- `src/Cobol.Net.Runtime/Intrinsics/CobolIntrinsics.Text.cs` — `DisplayOf`, `NationalOf`, `CharNational`, `BooleanOfInteger`,
  `IntegerOfBoolean`, `TestNumval`, `TestNumvalC` (string-analysis validators).
- `src/Cobol.Net.Runtime/Intrinsics/CobolDate.cs` — `DateToYyyymmdd`, `DayToYyyyddd`, `YearToYyyy`, `SecondsPastMidnight`,
  `TestDateYyyymmdd`, `TestDayYyyyddd`.
- `src/Cobol.Net.Runtime/Exceptions/EcFunctions.cs` — `FileN()`, `LocationN()` (national projections of the existing `File()`/
  `Location()`).

**Tier-C (Compiler):**
- `src/Cobol.Net.Compiler/Binding/Model/RedefinesModel.cs` + a `RedefinesClassifier` (in the `Binding/Passes/` pass folder or `DataBinder`): one
  `RejectTierC(class, reason)` verdict owning the reason table; **all** ~10 inline guards route through the single predicate
  (`Storage is StorageForm.TierCWindow` / `!item.IsImageCapable`).
- **If the codec is implemented (step group D):** `src/Cobol.Net.Compiler/Binding/Model/StorageForm.cs` — `TierCWindow.Read/
  Write` become real; `src/Cobol.Net.Compiler/CodeGen/DataDivision/GroupImageCodec.cs` gains a `byte[]` framing path; a runtime
  `Values/Tables/CobolByteImage.cs` (or `IO/RecordFraming`-adjacent) codec carries the COMP/packed/binary byte encoders.

**Docs:**
- `docs/COBOLNET_INTRINSICS_DESIGN.md` — the catalog table updated (Deferred→implemented/dispositioned; window authority).
- `docs/VERSION_CHANGE_REFERENCE.md` — intrinsic rows firmed (provisional markers resolved or explicitly blocked).
- `docs/rearchitecture/DESIGN-data-model.md` §2.3 — Tier-C status updated (rejection single-sourced; codec landed or scheduled).
- `docs/DOC_INDEX.md` — if any doc's subject materially changes.

### 3.3 Conformance goldens that exist when DONE

Under `tests/conformance/2002/`, `2014/`, `2023/`: one value-exercising `.cob` + `.out` per implemented function (families may
share a program). Under `tests/conformance/negative/`: one window-enforcement `.cob` per promoted function (or family
representative). New xUnit facts in `tests/Cobol.Net.Tests.Conformance/IntrinsicFunctionDifferentialTests.cs` (spec-pinned
values) and a locale-disposition + Tier-C fact set.

---

## 4. STEP-BY-STEP

> **Discipline (non-negotiable):** ONE function/family at a time — compile, run, verify the *value*, then next
> (`feedback_iterate_one_at_a_time`, `feedback_one_test_at_a_time`). Derive the expected result from `specs/ISO_COBOL.md` §15.n
> and cite it in the test + row (`feedback_use_the_spec`, `feedback_bare_end`). Run the battery before every commit
> (`feedback_run_guard_script`, `feedback_test_after_every_change`). Every implemented function ships its conformance golden in
> the **same commit** (`feedback_conformance_tests_per_feature`, `feedback_parse_and_emit_together`). DEVLOG entry per commit
> (`feedback_devlog_per_commit`).

### Verification primitives (used throughout)

- **CLI (single program):**
  `dotnet E:/CobolSharp/src/Cobol.Net.Cli/bin/Debug/net10.0/cobol.dll <src.cob> --std <edition> -o E:/Temp/claude/out.dll --run`
  (rebuild the CLI first if you touched Compiler/Runtime: `dotnet build E:/CobolSharp/src/Cobol.Net.Cli`).
- **Greenfield conformance + unit battery:**
  `dotnet test E:/CobolSharp/tests/Cobol.Net.Tests.Conformance` and `dotnet test E:/CobolSharp/tests/Cobol.Net.Tests.Unit`
  (or the greenfield suite the repo standardizes on).
- **Full legacy guard (must stay NIST 353 MATCH):** `bash E:/CobolSharp/scripts/guard-fast.sh` (fast) / `guard.sh` (full).
- **Expected result** for every value golden is derived from the spec and, where a sound oracle exists, cross-checked against the
  legacy differential (`AssertSameAsLegacy`) — but the spec is authority.

---

### Step 0 — Preflight (no commit)

Rebuild everything green as the baseline; capture the battery counts.

- `dotnet build E:/CobolSharp/CobolSharp.sln` (or the greenfield projects) → 0 errors.
- `dotnet test .../Cobol.Net.Tests.Conformance` + `.../Cobol.Net.Tests.Unit` → record PASS counts.
- `bash scripts/guard-fast.sh` → **NIST 353 MATCH**, 0 regressions.

If any is red, STOP — this phase assumes P10 landed green. Record the baseline counts in the DEVLOG working note.

---

### Step 1 — Enumerate the actual Deferred set and land the catalog scaffolding (COMMIT BOUNDARY)

**Why:** the "43" is stale (§2.1). Work the real set, and introduce the one structural change all A.4.9 rows need before any
promotion: a non-`Deferred` disposition for documented non-support, so "zero Deferred" is *reachable* without faking a runtime.

**Do:**
1. Enumerate: `grep -n "IntrinsicBind.Deferred" src/Cobol.Net.Compiler/Binding/IntrinsicCatalog.cs`. Reconcile against §3.1; if a
   row moved, update §3.1's row-line references in your working note.
2. In `IntrinsicCatalog.cs`, extend the bind classifier for documented non-support. Prefer a **new `IntrinsicBind` case** for
   the singular-pattern (`feedback_singular_pattern`):
   ```csharp
   public enum IntrinsicBind { Runtime, Fold, Deferred, Unsupported }
   ```
   `Unsupported` = "catalogued, edition/arity gating applies, but the containing optional module is documented non-support"
   (distinct from `Deferred` = "will be implemented"). Document it in the enum's XML doc citing §4.2.7 / A.4.9.
3. Flip the five A.4.9 rows (`LOCALE-COMPARE/-DATE/-TIME/-TIME-FROM-SECONDS`, `STANDARD-COMPARE`) from `Deferred` to
   `Unsupported`. Leave `RuntimeMethod` empty. (Their bind arm lands in step 8; until then they still fail loud — no golden
   regresses because no program uses them yet.)
4. Build; run the unit + conformance battery (no behavior change yet for existing programs).

**Verify:** battery green; `grep -c "IntrinsicBind.Deferred"` decreased by 5.

**COMMIT:** `feat(cobolnet): P11 step 1 — re-enumerate Deferred set; add IntrinsicBind.Unsupported for A.4.9 documented non-support`

---

### Step 2 — Family: boolean conversions `BOOLEAN-OF-INTEGER` / `INTEGER-OF-BOOLEAN` (COMMIT BOUNDARY)

ISO §15.13 (BOOLEAN-OF-INTEGER: an unsigned integer → a boolean value of `argument-2` bit-length) and §15.45
(INTEGER-OF-BOOLEAN: a boolean → its unsigned integer value). Boolean data landed in P10 (`CobolBool`, `BooleanOperatorTests`).

**Runtime** (`CobolIntrinsics.Text.cs`, near the boolean helpers):
```csharp
// §15.13 — argument-1 (integer) as a boolean value argument-2 bits wide (a '0'/'1' string, MSB first).
public static string BooleanOfInteger(long value, long length) { … }   // spec: argument-2 = result length in bits
// §15.45 — argument-1 (a boolean value) as its unsigned binary magnitude.
public static long IntegerOfBoolean(string boolImage) { … }
```
Derive the exact mapping from §15.13.4 / §15.45.4 (the boolean value is a bit string; the integer is its unsigned binary value).

**Renderer** (`IntrinsicRenderer.cs`): `BooleanOfInteger` is a **boolean-result** function (`IntrinsicType.Boolean`) — render
through the string/boolean channel; `IntegerOfBoolean` is integer-result → `RenderNum` arm at scale 0.

**Catalog:** flip both rows to `Runtime`, set `RuntimeMethod`, keep `IntroducedIn 2002` (both are 2002 core — firm, see §7).

**Golden** `tests/conformance/2002/intrinsics_boolean_conv.cob`: `DISPLAY FUNCTION INTEGER-OF-BOOLEAN(B01)` etc. with a known
value; `.out` derived from the spec bit semantics.

**Negative window row** `tests/conformance/negative/boolean-of-integer-at-85.cob`: the function under `--std 85` → assert the
compile emits **COBOLNET1502** naming COBOL-2002 (via `EditionHarness`/`EditionGateDiagnosticTests` pattern).

**Verify:** CLI runs the golden to the spec value; `dotnet test .../Conformance`; `guard-fast.sh` MATCH.

**COMMIT:** `feat(cobolnet): P11 — FUNCTION BOOLEAN-OF-INTEGER/INTEGER-OF-BOOLEAN (§15.13/§15.45) implemented + goldens + 85-window rows`

---

### Step 3 — Family: national conversions `DISPLAY-OF` / `NATIONAL-OF` / `CHAR-NATIONAL` (COMMIT BOUNDARY) — **SATISFIED BY P10 (Steps 5 + 11); skip, re-verify only**

> **P10 landed this whole family** — DISPLAY-OF/NATIONAL-OF at P10 Step 5 (argument-2 turned out to be a
> SUBSTITUTION CHARACTER per the 2023 §15.26.3 r2/§15.66.3 r2 text, NOT a codepage name — the codepage prose
> below is superseded), CHAR-NATIONAL + ORD-over-national (§15.70.4 r2) at P10 Step 11 (`national_intrinsics` +
> `char_national` goldens; the 0844 guard narrowed to CHAR). The section is kept for the design record only.

ISO §15.26 (DISPLAY-OF: national → alphanumeric in the runtime code page), §15.66 (NATIONAL-OF: alphanumeric → national),
§15.16 (CHAR-NATIONAL: an ordinal → the national character at that position). National storage landed in P10 (`CobolString`
national support; national is one UTF-16 char per position, D-N1).

**Runtime** (`CobolIntrinsics.Text.cs`):
```csharp
// §15.66 — argument-1 (alphanumeric) → national; optional argument-2 code-page name (default: the runtime alphanumeric CP).
public static string NationalOf(string s, string? codepage = null) { … }
// §15.26 — argument-1 (national) → alphanumeric; optional argument-2 code-page name.
public static string DisplayOf(string national, string? codepage = null) { … }
// §15.16 — the national character whose ordinal position is argument-1 (1-based, per the national program collating repertoire).
public static string CharNational(long ordinal) { … }
```
Because the model represents national as a UTF-16 `string` (D-N1), `NationalOf`/`DisplayOf` are identity-with-category-retag for
the default code page — **but** derive the actual conversion rule from §15.26.4 / §15.66.4; if a non-default `argument-2`
code-page is given and unsupported, emit the loud path (or a bind-time non-support if you choose to gate code-page names — cite
the § and keep it loud, never wrong).

**Binder** (`Binding/Procedure/Verbs/IntrinsicBinder.cs`): result categories — `NATIONAL-OF`/`CHAR-NATIONAL` → `IntrinsicType.National`
(category `National`); `DISPLAY-OF` → `Alphanumeric`. National-result rows render through the same string channel (a .NET string
carries the national image; the bound node's category drives downstream MOVE/DISPLAY, already handled by P10's national paths).
Remove the `COBOLNET0844` guard (national forms — CHAR-NATIONAL §15.16 / ORD over national — "not yet implemented") at
`Binding/Procedure/Verbs/IntrinsicBinder.cs:214` for the CHAR-NATIONAL leg (keep the ORD-over-national guard until/unless you implement it — cite the §).

**Renderer** (`IntrinsicRenderer.cs` `RenderString`): arms for `DisplayOf`, `NationalOf`, `CharNational`.

**Catalog:** three rows → `Runtime`, `IntroducedIn 2002`.

**Goldens** `tests/conformance/2002/intrinsics_national_conv.cob`: round-trip `NATIONAL-OF`/`DISPLAY-OF` a known literal and
`DISPLAY` the result; `CHAR-NATIONAL` of a known ordinal. `.out` per spec.

**Negative window rows:** each under `--std 85` → COBOLNET1502.

**Verify:** CLI + conformance + guard-fast MATCH.

**COMMIT:** `feat(cobolnet): P11 — FUNCTION DISPLAY-OF/NATIONAL-OF/CHAR-NATIONAL (§15.26/§15.66/§15.16) on the P10 national base + goldens + window rows`

---

### Step 4 — Family: EC national twins `EXCEPTION-FILE-N` / `EXCEPTION-LOCATION-N` (COMMIT BOUNDARY) — **SATISFIED BY P10 Step 11; skip, re-verify only**

> **P10 Step 11 landed exactly this design** (2026-07-16): `EcFunctions.FileN()/LocationN()` = the national
> projection through the ONE `CobolIntrinsics.NationalOf` translator; rows `Runtime` with `EcFileN`/`EcLocationN`;
> golden `tests/conformance/2002/exception_file_n.cob` (not `intrinsics_ec_national` — the P10 catalog's name),
> matrix row `exception-file-n-2002`, negative `exception_file_n_below_2002` @85. The 2023 optional-argument form
> stays loud (VCR 68/69 → PHASE-13 Step 9). Kept for the design record only.

ISO §15.29 / §15.31 — national projections of the existing alphanumeric `EXCEPTION-FILE` (§15.28) / `EXCEPTION-LOCATION`
(§15.30), which are already implemented (`EcFunctions.File()` / `.Location()`, rendered at `IntrinsicRenderer.cs:284-286/282`).
The catalog comment (`IntrinsicCatalog.cs:134-136`) deferred these because "no national runtime exists" — **now resolved by P10**.

**Runtime** (`EcFunctions.cs`): `public static string FileN() => `national projection of `File()`; `LocationN()` likewise
(same value, national category — the runtime string is the national image).

**Binder/renderer:** rows → `Runtime` with `RuntimeMethod` `EcFileN`/`EcLocationN`, category `National`; `RenderString` arms;
keep the `EcNoteFunction()` EC-gate flag (the `RuntimeMethod.StartsWith("Ec")` check at `Binding/Procedure/Verbs/IntrinsicBinder.cs:220`
already covers `EcFileN`/`EcLocationN`).

**Note:** there is **no** `-N` twin for `EXCEPTION-STATEMENT`/`-STATUS` in ISO §15 — do not invent rows. The only `-N` EC twins
are these two.

**Golden** `tests/conformance/2002/intrinsics_ec_national.cob`: trigger a file EC, `DISPLAY FUNCTION EXCEPTION-FILE-N`; `.out`
per the runtime register. **Negative window rows** at `--std 85`.

**Verify + COMMIT:** `feat(cobolnet): P11 — FUNCTION EXCEPTION-FILE-N/EXCEPTION-LOCATION-N (§15.29/§15.31) national EC twins + goldens + window rows`

---

### Step 5 — Family: date windowing + clock `DATE-TO-YYYYMMDD` / `DAY-TO-YYYYDDD` / `YEAR-TO-YYYY` / `SECONDS-PAST-MIDNIGHT` (COMMIT BOUNDARY)

ISO §15.23 / §15.25 / §15.100 (sliding-window century expansion; optional `argument-2` window size, default 50; optional
`argument-3` "current year" base) and §15.80 (SECONDS-PAST-MIDNIGHT from the runtime clock).

**Runtime** (`CobolDate.cs`): `DateToYyyymmdd(long yymmdd, long window = 50, long baseYear = <current>)`, `DayToYyyyddd(...)`,
`YearToYyyy(long yy, long window = 50, long baseYear = <current>)`, `SecondsPastMidnight()`. Derive the exact windowing algebra
from §15.100.4 (the pivot: `YYYY = base_century + yy`, adjusted by the window). `SECONDS-PAST-MIDNIGHT` reads the same clock seam
as `CURRENT-DATE` (`CobolDate.CurrentDate()` — the injectable clock; P8's `RunUnit.Clock`/`IClock` if it landed, else the
existing static seam). Keep it deterministic under the test clock.

**Renderer** (`RenderNum` for the integer-result rows; `SecondsPastMidnight` is numeric): arms passing the optional trailing
args (`OptionalTrailing`, MinArgs/MaxArgs already in the rows — respect them).

**Catalog:** rows → `Runtime`. **Windows (§7):** `DATE-TO-YYYYMMDD`/`DAY-TO-YYYYDDD`/`YEAR-TO-YYYY` are 2002 (year-2000
windowing amendment); `SECONDS-PAST-MIDNIGHT` is 2002. Firm to 2002 (non-provisional per the 2002 date-function set).

**Golden** `tests/conformance/2002/intrinsics_date_window.cob`: known `yymmdd` + explicit window → expected `yyyymmdd`; assert the
window boundary case (e.g. `yy=49` vs `yy=50`). `SECONDS-PAST-MIDNIGHT` under the fixed test clock. **Negative window rows** at
`--std 85`.

**Verify + COMMIT:** `feat(cobolnet): P11 — FUNCTION DATE-TO-YYYYMMDD/DAY-TO-YYYYDDD/YEAR-TO-YYYY/SECONDS-PAST-MIDNIGHT (§15.23/25/100/80) + goldens + window rows`

---

### Step 6 — Family: validators `TEST-DATE-YYYYMMDD` / `TEST-DAY-YYYYDDD` / `TEST-NUMVAL` / `TEST-NUMVAL-C` (COMMIT BOUNDARY)

ISO §15.90 / §15.91 (return 0 if the standard date/day integer is valid, else a nonzero code) and §15.93 / §15.94 (return 0 if
`argument-1` is a valid NUMVAL/NUMVAL-C string, else the 1-based position of the first offending character). `NUMVAL`/`NUMVAL-C`
runtime already exists (`CobolIntrinsics.Numval`/`NumvalC`) — the TEST twins share the parser and report the validity verdict
instead of the value (mirror the already-implemented `TestNumvalF`/`TestFormattedDatetime`).

**Runtime:** `CobolDate.TestDateYyyymmdd(long)`, `CobolDate.TestDayYyyyddd(long)`; `CobolIntrinsics.TestNumval(string, bool
commaMode)`, `CobolIntrinsics.TestNumvalC(string, string currency, bool commaMode)` (in `.Text.cs`, beside `TestNumvalF`).
Derive the verdict codes from §15.93.4 / §15.94.4 (they are specific, not just 0/nonzero).

**Renderer** (`RenderNum`): integer-result arms; `TestNumvalC` injects the default currency exactly like `NumvalC`
(`IntrinsicRenderer.cs:124-128` + the bind-time default-currency injection at `Binding/Procedure/Verbs/IntrinsicBinder.cs:179` — extend
the `args.Count == 1` injection to `TEST-NUMVAL-C`).

**Catalog:** rows → `Runtime`. **Windows (§7):** `TEST-DATE-YYYYMMDD`/`TEST-DAY-YYYYDDD` carry **direct in-spec 2002 attribution**
(`@D.31.3.1`, per `COMPLETION_ROADMAP_COUNCIL.md:69`) — firm to 2002, non-provisional. `TEST-NUMVAL`/`TEST-NUMVAL-C` are 2002 —
firm to 2002.

**Golden** `tests/conformance/2002/intrinsics_test_validators.cob`: a valid and an invalid argument for each, asserting 0 and the
exact nonzero code. **Negative window rows** at `--std 85`.

**Verify + COMMIT:** `feat(cobolnet): P11 — FUNCTION TEST-DATE-YYYYMMDD/TEST-DAY-YYYYDDD/TEST-NUMVAL/TEST-NUMVAL-C (§15.90/91/93/94) + goldens + window rows`

---

### Step 7 — `CONCATENATE` (2002–2014, removed 2023) + `BYTE-LENGTH` fold + `SMALLEST-ALGEBRAIC` golden (COMMIT BOUNDARY)

**`CONCATENATE` (§15.18 name history):** the row already carries `IntroducedIn 2002, RemovedIn 2023` (`IntrinsicCatalog.cs:156`).
Flip `Deferred`→`Runtime`, `RuntimeMethod = "Concat"` (reuse the 2023 `CONCAT` body — both concatenate argument images). The
generic window gate already emits **COBOLNET1503** at `--std 2023` (removed) and **COBOLNET1502** at `--std 85`.

**`BYTE-LENGTH` (§15.14):** the number of *bytes* the argument occupies — **≠** `FUNCTION LENGTH` (character positions), the D7
distinction. It is a **compile-time fold** (like LENGTH): a national leaf is 2 bytes/position (D-N1), DISPLAY is 1, COMP/COMP-3/
binary use their storage width. Add `BindByteLengthFold` beside `BindLengthFold` (`Binding/Procedure/Verbs/IntrinsicBinder.cs:465`) folding
from `RecordLayout`/`PicInfo.StorageWidth` byte geometry (the `RecordLayout` width/offset authority, else `DataItem`/`PicInfo` storage
width). Keep the row `Fold`, flip out of `Deferred` (set a sentinel `RuntimeMethod` or route by name like LENGTH). Runtime-length
arguments (ref-mod / ODO) stay loud by name (same discipline as LENGTH, §15.14.4).

**`SMALLEST-ALGEBRAIC` (§15.83):** already implemented as `Fold` (`BindAlgebraicFold`). Add its **value golden** + a
**window-enforcement negative row** at `--std 2014` (it is a 2023 introduction, `IntroducedIn 2023`).

**Goldens** `tests/conformance/2002/intrinsics_concatenate.cob`, `.../2002/intrinsics_byte_length.cob` (include a national leaf to
prove ≠ LENGTH), `.../2023/intrinsics_smallest_algebraic.cob`. **Negative rows:** `concatenate-at-2023.cob` (COBOLNET1503),
`concatenate-at-85.cob` (1502), `byte-length-at-85.cob` (1502), `smallest-algebraic-at-2014.cob` (1502).

**Verify + COMMIT:** `feat(cobolnet): P11 — FUNCTION CONCATENATE (window [2002,2023)) + BYTE-LENGTH fold (§15.14) + SMALLEST-ALGEBRAIC golden; window rows`

---

### Step 8 — A.4.9 locale disposition: functions + LOCALE keyword variants (COMMIT BOUNDARY)

**Why:** ratified decision 3 — documented non-support of the A.4.9 locale module (§2.3). This makes "zero Deferred" true for the
five locale functions and closes the locale keyword variants.

**Do:**
1. `Binding/Procedure/Verbs/IntrinsicBinder.cs` `BindIntrinsicCore` — after the window gate, before the special-bind dispatch, add:
   ```csharp
   if (sig.Bind == IntrinsicBind.Unsupported)
   {
       data.Edition.Error("COBOLNET1518", $"FUNCTION {sig.Name} is in the optional locale module "
           + "(ISO/IEC 1989:2023 Annex A.4.9), which COBOL.NET does not support (documented non-support; "
           + "conformant per ISO §4.2.7). Use a supported alternative (e.g. STANDARD-1/2 collating, "
           + "FORMATTED-DATE/-TIME).");
       return new BoundExprError($"FUNCTION {sig.Name} (A.4.9 locale, not supported)");
   }
   ```
   COBOLNET1518 is a **bind-time compile error** (loud, addressable, documented) — not a runtime guard. It renders no C#, so no
   `Deferred` runtime fallback is reached.
2. **LOCALE keyword variants** of `LOWER-CASE` (§15.57), `UPPER-CASE` (§15.97), `TEST-NUMVAL-C` (§15.94): these functions are
   implemented *without* locale support. Detect a `LOCALE` phrase in the argument list (a bare-word argument via
   `IntrinsicBinder.KeywordWordOf`, like TRIM's `LEADING`/`TRAILING` detection — P7 Step 12 made arguments real parse
   trees) and, when present, emit
   COBOLNET1518 naming A.4.9 (the LOCALE *phrase*, not the whole function). Absent the phrase, bind exactly as today (zero
   regression to the existing goldens).
3. Confirm `IntrinsicRenderer` never renders an `Unsupported` row (the binder returned an error) — the existing `Deferred` arms
   at `IntrinsicRenderer.cs:45/245` are the only backstop and are now unreachable for these rows.

**Goldens** `tests/conformance/negative/locale-compare-a49.cob`, `.../locale-date-a49.cob`, `.../standard-compare-a49.cob`,
`.../lower-case-locale-a49.cob` (the keyword variant), each `--std 2023` asserting **COBOLNET1518** + the "Annex A.4.9" /
"documented non-support" wording. Add an xUnit `LocaleDispositionTests` fact set mirroring `EditionGateDiagnosticTests`.

**Verify:** `grep -c "IntrinsicBind.Deferred" IntrinsicCatalog.cs` → **0**. Conformance + guard-fast MATCH.

**COMMIT:** `feat(cobolnet): P11 — A.4.9 locale functions + LOCALE keyword variants → documented non-support (COBOLNET1518, decision 3); zero Deferred rows`

> **Exit criteria 1, 3, 4 are met at this commit** (zero Deferred; every promotion has a value golden + a window row). Update the
> STATUS line and the DEVLOG. The remaining steps decide Tier-C (exit criterion 2).

---

### Step group C — Tier-C: single-source the rejection (MANDATORY) (COMMIT BOUNDARY)

**Why:** collapse the ~10 scattered "Tier-C byte island, deferred" guards (§2.4) into **one** verdict + **one** predicate. This
alone satisfies exit criterion 2 (single-sourced rejection) and is the seam the optional codec (step group D) plugs into.

**Do:**
1. Introduce `RedefinesClassifier.RejectTierC(RedefinesClass cls, string reason)` as the ONE site
   that stamps `cls.Tier = Rejected` / `cls.RejectReason` with a **reason table** keyed by the offending leaf kind (float,
   COMP-5, BINARY-*, INDEX, national). Move the reason strings from `ComputeTier` (`DataBinder.cs:1752`) into that table,
   preserving each ISO citation (risk #3 mitigation — keep every guard's citation).
2. Make **one predicate** the single query every downstream site uses: `item.Storage is
   StorageForm.TierCWindow` (or the group's `!IsImageCapable` / the `Rejected` verdict). Route **all** the inline
   guards through it, each emitting a message from the **one** reason table rather than a bespoke string:
   `DataBinder.cs`, `DataBinder.Linkage.cs`, `OoClassTable.cs`, `ReferenceResolver.cs`, `Binding/Procedure/ExpressionBinder.cs`,
   `Binding/Procedure/Verbs/InitializeBinder.cs`, `Binding/Procedure/Verbs/OoBinder.cs`, `Binding/Procedure/Verbs/SortBinder.cs`,
   `CodeGen/DataDivision/GroupValueSlicer.cs`, `CodeGen/DataDivision/RecordStructEmitter.cs`, `CodeGen/Emit/OperandText.cs`,
   `CodeGen/Emit/NumericRenderer.cs`, `CodeGen/Verbs/AcceptDisplayEmitter.cs`, `CodeGen/Verbs/CallEmitter.cs`, `CodeGen/Verbs/InspectEmitter.cs`,
   `CodeGen/Verbs/MoveEmitter.cs`, `CodeGen/Verbs/SequentialIoEmitter.cs`, `CodeGen/Verbs/SortEmitter.cs`, `CodeGen/Verbs/StringEmitter.cs`. Keep each guard's *statement context* (the verb it
   guards) — only the *reason text* is centralized.
3. **Do not change behavior** — every program that was loud-rejected before is loud-rejected now, with the same or better
   message. Add a `TierCRejectionTests` fact set: one program per previously-guarded shape (a COMP-5 REDEFINES of a DISPLAY area
   in MOVE / CALL USING / ACCEPT / SORT / INVOKE), each asserting a loud reject with the single code and the leaf-naming reason.

**Verify:** conformance + unit + **full legacy guard** (`guard.sh`) NIST 353 MATCH — this touches the emitter loud paths, so run
the FULL guard, not just fast (`feedback_legacy_suite_on_shared_corpus`). No golden flips.

**COMMIT:** `refactor(cobolnet): P11 — single-source the Tier-C REDEFINES rejection into RedefinesClassifier.RejectTierC; collapse ~10 scattered guards + TierCRejectionTests`

> **Exit criterion 2 is met at this commit** if you stop here (single-sourced rejection). Step group D is the recommended
> follow-on that turns the rejection into real support.

---

### Step group D — Tier-C: implement the confined `byte[]` codec (RECOMMENDED, additive) (multiple COMMIT BOUNDARIES)

**Why:** Tier-C is *legal* COBOL (a COMP/binary/packed/index leaf type-punning a DISPLAY area via REDEFINES). A commercial-quality
compiler supports it. This is the **one** sanctioned `byte[]` boundary of hard-invariant #1 (`DESIGN-data-model.md` §2.3 /
`Binding/Model/RedefinesModel.cs:47-49`). It is additive: the single seam from step group C is where `TierCWindow` becomes real.

> **Decision gate:** if the executing session is time-boxed or P12 is racing, **stop after step group C** (exit met) and record
> the codec as a scheduled increment in `DESIGN-data-model.md` §2.3 + a DEVLOG note. If proceeding, do the sub-steps below, each
> a commit, each battery-green. No NIST program requires this, so the goldens are hand-authored spec-pinned cases.

**D.1 — The runtime byte codec (COMMIT).** `src/Cobol.Net.Runtime/Values/Tables/CobolByteImage.cs` (or beside `IO/RecordFraming`
if P8 placed framing there): a class-scoped `byte[]` backing + per-usage window accessors — `ReadDisplay/WriteDisplay(offset,
len)`, `ReadComp3/WriteComp3` (packed-decimal), `ReadBinary/WriteBinary(offset, width, signed)` (COMP/COMP-5 two's-complement),
`ReadIndex/WriteIndex`. Encodings from ISO §13.18.60 USAGE GR4 (representation implementor-defined — pick the documented COBOL.NET
representation and cite it) + the packed/binary byte layouts. Unit tests in `Cobol.Net.Tests.Unit` for each encoder round-trip.

**D.2 — `StorageForm.TierCWindow` becomes real (COMMIT).** In `src/Cobol.Net.Compiler/Binding/Model/StorageForm.cs`, give
`TierCWindow(RedefinesClass, Offset, Length, Usage)` real `Read`/`Write` that render a `CobolByteImage` window accessor for the
leaf's usage. `ComputeTier`/`RedefinesClassifier` now classify a genuine mixed-usage pun as **`ByteCanonical`** (not `Rejected`):
one class-scoped `byte[]` canonical (`RedefinesClass.Width` in bytes; the backing `_redef_X` becomes `byte[]`).

**D.3 — `GroupImageCodec` byte path (COMMIT).** In `src/Cobol.Net.Compiler/CodeGen/DataDivision/GroupImageCodec.cs` (the
split-out group image codec that owns `AsImage`/`FromImage`), add the `byte[]` framing: a Tier-C class emits a `byte[]` backing + `AsImage`/
`FromImage` that marshal through `CobolByteImage`, parallel to the existing Tier-B string codec in `GroupImageCodec.cs`.

**D.4 — Remove the reject, keep the verdict for the truly-unmodelable (COMMIT).** `RejectTierC` now fires only for **Tier-D**
(spec-forbidden: object/pointer/strongly-typed SR12/14; ODO/variable-length SR5/17). The ~10 guards now permit a Tier-C class and
route MOVE/CALL/ACCEPT/SORT/INVOKE/STRING/INSPECT/FILE-STATUS through the `byte[]` codec instead of the loud path. Convert each
`TierCRejectionTests` case that was a *legal* mixed-usage pun into a **value** golden (the pun reads back the reinterpreted bits);
keep the Tier-D cases as reject goldens.

**Goldens** `tests/conformance/2002/redefines_tierc_*.cob`: a `05 N PIC 9(4) COMP` redefining a `05 A PIC X(?)` DISPLAY area,
`MOVE`/`DISPLAY` proving the byte reinterpretation matches the documented representation. Cross-check against the legacy oracle
where it implements the same pun (differential); otherwise spec-pinned.

**Verify (each sub-step):** unit (encoders) + conformance + **full legacy guard** NIST 353 MATCH (the guards changed — full
guard).

**COMMITS:** one per sub-step, e.g. `feat(cobolnet): P11 Tier-C D.1 — CobolByteImage confined byte[] codec + encoder unit tests`,
`… D.2 — StorageForm.TierCWindow read/write`, `… D.3 — GroupImageCodec byte framing`, `… D.4 — accept legal Tier-C puns; Tier-D
stays rejected; value goldens`.

---

### Step 9 — Docs + STATUS close-out (COMMIT BOUNDARY)

- `docs/COBOLNET_INTRINSICS_DESIGN.md` — catalog table: Deferred→implemented/dispositioned; window authority per §7; the A.4.9
  disposition and COBOLNET1518.
- `docs/VERSION_CHANGE_REFERENCE.md` — intrinsic rows firmed (provisional resolved or explicitly "blocked on standards
  acquisition" per decision 1); the window-enforcement rows referenced.
- `docs/rearchitecture/DESIGN-data-model.md` §2.3 — Tier-C status (rejection single-sourced; codec landed **or** scheduled with
  rationale, per `feedback_follow_design_docs_and_spec` / `feedback_update_adr_on_design_corrections`).
- `docs/DOC_INDEX.md` — sync if any doc's subject changed.
- Update this file's **STATUS** to `DONE`; append the final battery counts.

**COMMIT:** `docs(cobolnet): P11 — intrinsics backlog to zero + Tier-C decision; sync intrinsics/VCR/data-model docs (DEVLOG NNN)`

---

## 5. Verification (phase end)

Run the **full** battery and confirm each exit criterion by evidence, not assertion:

1. `grep -rn "IntrinsicBind.Deferred" src/Cobol.Net.Compiler/Binding/IntrinsicCatalog.cs` → **0 matches** (criterion 1).
2. `dotnet test E:/CobolSharp/tests/Cobol.Net.Tests.Conformance` → all green, including every new `intrinsics_*` / `redefines_
   tierc_*` golden and the `LocaleDispositionTests` / `TierCRejectionTests` facts (criteria 3, 4, 2).
3. `dotnet test E:/CobolSharp/tests/Cobol.Net.Tests.Unit` → 213+ green (the new byte-codec encoder tests add rows).
4. `bash E:/CobolSharp/scripts/guard.sh` (FULL, not fast) → **NIST 353 MATCH**, 0 regressions (criterion 5). The full guard is
   mandatory because step groups C/D touch emitter loud paths and REDEFINES classification.
5. **Behavior-neutrality of the Tier-C consolidation (step C):** the pre-C and post-C reject messages differ only in centralized
   wording; assert via `TierCRejectionTests` that every previously-guarded shape is still loud (or, post-D, correctly valued).
6. **Value correctness spot-check:** run 3-4 goldens through the CLI `--run` and eyeball the value against the hand-derived spec
   result (`feedback_verify_demo_output` — verify the output is *correct*, not just non-crashing).
7. **Window enforcement:** each `tests/conformance/negative/*-at-<edition>.cob` compiles to the expected COBOLNET1502/1503/1518
   and **fails the compile** (exit 65), not a runtime error.

---

## 6. Rollback / resumability

- **Resume point:** the STATUS line + the per-step commit boundaries. Each step 2-8 is one function family = one atomic commit;
  a mid-step interruption leaves the previous family committed and green. To resume, `git log --oneline | grep "P11"`, read the
  last commit, continue at the next family in §3.1.
- **Independence:** steps 2-7 are order-independent (disjoint catalog rows + disjoint runtime methods); step 1 (the `Unsupported`
  enum) must precede step 8; step group C must precede step group D; step 8 must follow step 1. Steps 2-7 may be parallelized
  across worktrees if desired (`feedback_worktree_workflows_stale`) — but each worktree runs its own guard before merge, and a
  shared-file conflict is only `IntrinsicCatalog.cs`/`IntrinsicRenderer.cs` (merge is mechanical: distinct rows/arms).
- **Risks + mitigations:**
  1. *Provisional windows firmed wrong.* Mitigation: §7 gives the authority per family; where none exists, mark the window
     "blocked on standards acquisition" (decision 1) rather than guessing — the generic gate still enforces whatever value is
     set, and the negative row pins it.
  2. *National-result rendering path.* Mitigation: national is a UTF-16 string in the model (D-N1); render national-result
     intrinsics through the same string channel as any national item — reuse P10's national MOVE/DISPLAY paths; a golden that
     `DISPLAY`s the national result catches a mis-category at run time.
  3. *Tier-C consolidation changes a message a test pins.* Mitigation: `EditionGateDiagnosticTests`-style facts assert on stable
     substrings ("Tier-C", the leaf kind), not full prose; keep the leaf-naming.
  4. *Tier-C codec accepts a case previously rejected piecemeal (over-broad).* Mitigation: step C keeps every guard's citation in
     the one reason table; step D.4 flips only *mixed-usage puns* to valid and keeps Tier-D (object/pointer/strong/ODO) rejected
     — assert the Tier-D reject goldens survive.
  5. *Guard throughput.* If the full guard is slow, use `guard-fast.sh` between family commits and the FULL `guard.sh` at the
     step-C/D commits and phase end (`feedback_guard_speed` — no unbounded searches, kill stale background tasks).

---

## 7. ISO feature work — spec sections, editions, goldens

**Spec sections (all in `specs/ISO_COBOL.md`, ISO/IEC 1989:2023 §15):** derive every expected value from these, cite in the row
comment and the test:

| Function(s) | § | Edition (window authority) |
|---|---|---|
| BOOLEAN-OF-INTEGER / INTEGER-OF-BOOLEAN | §15.13 / §15.45 | 2002 (boolean data amendment) |
| BYTE-LENGTH | §15.14 | 2002 (byte-length ≠ LENGTH, D7) |
| CHAR-NATIONAL | §15.16 | 2002 (national) |
| DISPLAY-OF / NATIONAL-OF | §15.26 / §15.66 | 2002 (national) |
| EXCEPTION-FILE-N / EXCEPTION-LOCATION-N | §15.29 / §15.31 | 2002 (EC national twins) |
| DATE-TO-YYYYMMDD / DAY-TO-YYYYDDD / YEAR-TO-YYYY | §15.23 / §15.25 / §15.100 | 2002 (Y2K windowing) |
| SECONDS-PAST-MIDNIGHT | §15.80 | 2002 |
| TEST-DATE-YYYYMMDD / TEST-DAY-YYYYDDD | §15.90 / §15.91 | 2002 — **direct in-spec attribution @D.31.3.1** (non-provisional) |
| TEST-NUMVAL / TEST-NUMVAL-C | §15.93 / §15.94 | 2002 |
| CONCATENATE | §15.18 (name) | **[2002, 2023)** — removed in 2023 (RemovedIn already set) |
| SMALLEST-ALGEBRAIC | §15.83 | 2023 (already Fold; golden + 2014-window row) |
| LOCALE-COMPARE / -DATE / -TIME / -TIME-FROM-SECONDS / STANDARD-COMPARE | §15.51/52/53/54/85 | 2002 rows → **A.4.9 documented non-support** (decision 3) |
| LOCALE keyword variants (LOWER-CASE/UPPER-CASE/TEST-NUMVAL-C) | §15.57/97/94 | **A.4.9 documented non-support** (decision 3) |

**Legal basis for the disposition:** ISO §4.2.7 (documentation route — documented non-support of optional modules is conforming,
`COMPLETION_ROADMAP_COUNCIL.md:266`); A.4.14 optionality + the F.2 support-exception rule (`:265`); ratified decision 3 (`:119`).

**Tier-C spec basis:** ISO §13.18.44 (REDEFINES shared area), §13.18.60 USAGE GR4 (representation implementor-defined),
§12.4.6.4.4 SAME RECORD AREA GR2 ("equivalent to an implicit redefinition … aligned on the leftmost byte position") — the
citation for the Tier-C byte model (`DataBinder.cs`, `Binding/Model/RedefinesModel.cs:47-49`).

**Conformance goldens to add (one value case per function/family + one window-enforcement negative per function):**
- `tests/conformance/2002/`: `intrinsics_boolean_conv`, `intrinsics_national_conv`, `intrinsics_ec_national`,
  `intrinsics_date_window`, `intrinsics_test_validators`, `intrinsics_concatenate`, `intrinsics_byte_length`,
  `redefines_tierc_*` (if step D).
- `tests/conformance/2023/`: `intrinsics_smallest_algebraic`.
- `tests/conformance/negative/`: `*-at-85.cob` / `*-at-2014.cob` / `*-at-2023.cob` per promoted function (COBOLNET1502/1503);
  `locale-*-a49.cob` / `lower-case-locale-a49.cob` (COBOLNET1518).
- xUnit facts: extend `IntrinsicFunctionDifferentialTests` (spec-pinned `AssertSpec` values), add `LocaleDispositionTests` and
  `TierCRejectionTests` (mirroring `EditionGateDiagnosticTests`/`EditionHarness`).

**New diagnostic codes introduced:** `COBOLNET1518` (A.4.9 locale documented non-support). The Tier-C single-source verdict
reuses the existing REDEFINES-reject channel (`RedefinesClass.RejectReason`); if a distinct code is wanted, allocate
`COBOLNET1519` and register it in the diagnostics catalogue (the P2/test-build diagnostic registry) — keep it one-code-one-rule.
