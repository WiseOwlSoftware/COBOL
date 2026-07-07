# PHASE 14 — Matrix closure + in-repo greenfield guard + one-time equivalence proof

- **Phase:** P14
- **Track:** feature-iso
- **Risk:** MEDIUM
- **Depends on:** P3 (version-gating framework — EditionValidator waves + harness-driven VCR audit) and P13 (M4/COBOL-2023 deltas + EC remnants + Table 1/5 behavior-row burn-down). Transitively consumes P0's migration safety net (`tests/nist/corpus.tsv`, the characterization harness, the cached Roslyn reference set, the oracle bake-out R1) and P2's first-class diagnostic-descriptor registry.
- **Blocks:** P15 (G8 legacy retirement). **The equivalence proof in this phase is the single irreversible ordering constraint of the whole migration: no legacy deletion (P15) may begin until Step 9 of this phase has recorded a green verdict-diff against the still-running legacy oracle.**

## Goal

Close the version-correctness program to zero open work and make the greenfield test net self-standing and provably faithful *before* the legacy oracle is severed. Concretely: (1) drive every row of `docs/VERSION_CHANGE_REFERENCE.md` (VCR) to `green`/`GATED` or a written disposition — no `TODO` survives; (2) run the full INV-1 / INV-2 / INV-3 sweeps in both strict and permissive modes, **including golden re-match at `--std 2023` (INV-1-strong at the shipping default edition — the fatal-challenge criterion)**; (3) complete the negative corpus so every registered diagnostic descriptor has ≥1 case, enforced by a registry-coverage unit test; (4) build the **in-repo greenfield census guard** that rebuilds the lost 403/459-census tooling by driving the greenfield `cobol.dll` (run-only + chain-intermediate handling ported from `scripts/guard.sh`); (5) run the **one-time verdict-diff equivalence proof** of the greenfield census guard against the legacy `guard.sh` **while the legacy engine still runs**, and record it; (6) migrate the 11 `LEGACY_DIVERGENT` ISO citations out of `guard.sh` into the greenfield guard and a durable LEDGER doc.

**OUT of scope (P15):** deleting the legacy byte engine / `tests/CobolSharp.Tests.*` / the legacy `guard*.sh` scripts, and the `CobolSharp.* → CobolNet.*` / `CobolNet.Runtime → Cobol.Net.Runtime` namespace flip. P14 *builds and proves* the replacement; P15 *removes* the original.

## Exit criteria (the phase is DONE when all hold)

1. **VCR zero-TODO:** `grep -c '| TODO |' docs/VERSION_CHANGE_REFERENCE.md` returns `0`. Every row is `GATED`/`green` or carries a written disposition (a `DISPOSITION:` note with an ISO citation and the reason it is intentionally not gated). A drift test binds VCR status to harness reality (Step 2).
2. **G7/G8 exit criteria satisfied as counts/exit codes** (the criteria P3 wrote into `docs/COMPLETION_ROADMAP_COUNCIL.md` Phase 1, line 45): continuity-sweep green permissive at all four editions with every strict failure tracing to a recognized edition-band code (0801/0802/0810/0811/0873/0875–0879/0882/0893/0900-band); the 2023-permissive golden run byte-matches; drift tests green over scrubbed metadata.
3. **INV sweeps green:** INV-1 (strict + permissive, weak *and* the strong `--std 2023` golden re-match leg), INV-2 (introduction-gating both directions), INV-3 (behavior-variant rows) all pass in `dotnet test`.
4. **Negative-corpus / registry coverage:** every diagnostic descriptor in the registry (P2) has ≥1 negative-corpus case, asserted by `DiagnosticRegistryCoverageTests`; the corpus manifest drift test is green.
5. **Greenfield guard exits 0** covering: goldens (byte-match ≥357 GREEN), the full census (all 459 census programs compile+run health, golden-less residue accepted by design), per-edition discovery (positive+negative corpora), the INV sweeps, and `dotnet test` (Unit + Conformance + Characterization). Runs cross-platform (`.sh` + `.ps1`).
6. **Equivalence proof recorded** against the still-running oracle: the per-program verdict-diff of `greenfield-guard.sh` vs `guard.sh` is empty except for the 11 documented `LEGACY_DIVERGENT` programs, and the result is committed to `docs/rearchitecture/EQUIVALENCE-PROOF.md`.
7. **`LEGACY_DIVERGENT` citations migrated** into `docs/LEGACY_DIVERGENCE_LEDGER.md` and consumed by the greenfield guard (so nothing is lost when `guard.sh` is deleted in P15).

## STATUS

`NOT STARTED`

> The executing session updates this line to `IN PROGRESS @ step N` after each step, and to `DONE` when all exit criteria hold. Also append a DEVLOG entry per commit boundary (descending, real timestamp) referencing `PHASE-14`.

---

## 2. Rationale — the problems this phase fixes

This phase is the load-bearing hinge between "the compiler works" and "we can safely delete the oracle." The survey/critique findings it closes:

- **The net evaporates at G8 unless a faithful replacement is proven first** (`DESIGN-test-build-ci.md` §1.2, §3.4, §6 risk 4; `COMPLETION_ROADMAP_COUNCIL.md` §4 risk 4). The authoritative NIST regression today is `scripts/guard.sh`, which compiles+runs ~353 programs **through the frozen legacy `cobolsharp.dll`** and diffs `tests/nist/valid/*.txt`. The greenfield NIST coverage is only the 318 golden-bearing `[InlineData]` rows in `NistDifferentialTests.cs` — it does **not** exercise the golden-less census residue (459 census programs − 364 goldens), the run-only programs, or the compile+run health of the full corpus the way `guard.sh` does. When P15 deletes `guard.sh` and the legacy engine, that census coverage is gone unless P14 rebuilds it greenfield **and proves the rebuild matches**, program-by-program, while the oracle is still runnable. Once legacy is deleted the proof is impossible forever (`COMPLETION_ROADMAP_COUNCIL.md` risk 4: "the G8 equivalence window closes unproven").

- **Version-gating is unaudited and the VCR ledger is stale** (`DESIGN-edition-framework.md` P7/P8; `VERSION_CHANGE_REFERENCE.md` — `grep -c TODO` = 117 rows still open, 6 done/GATED as of this writing). The "four compilers in one" mission (`docs/VERSION_TEST_MATRIX_DESIGN.md`) is only validated at its deltas/boundaries; a `TODO` row is an un-proven claim. P3 made the audit harness-driven (`scripts/gen-vcr.ps1` + `--emit-status`); P14 is where the burn-down actually reaches zero and the ledger becomes structurally incapable of drifting.

- **The default shipping edition (2023) is never behaviorally executed before G8** — the critics' one *fatal* challenge (`COMPLETION_ROADMAP_COUNCIL.md` §2 Phase 1, decision #10). `NistDifferentialTests` hard-compiles at `DialectLevel 85` (`CompilerUnderTest.cs`: `CobolNetCompiler(int dialectLevel = 85)`); the INV-1 sweep only asks "does it *compile*" via `check-batch` (`scripts/version-continuity-sweep.sh`), never runs. The `COBOLNET_NIST_STD` / `COBOLNET_NIST_PERMISSIVE` env override (`NistDifferentialTests.cs:533-536`) was seeded at the P2.7 flip precisely so the whole golden run can be re-targeted to `--std 2023 --permissive` and asserted byte-identical; P14 promotes that seeded leg to a hard, always-on G7 exit criterion.

- **Diagnostics are unaddressable without a coverage floor** (`DESIGN-test-build-ci.md` §1.6, §3.5). P2 built the first-class descriptor registry; P14 is where "every rule has a test" becomes an enforced invariant (`DiagnosticRegistryCoverageTests`), so a registered code with no negative-corpus case is a red — closing the false-green class the 0899 catch-all and the `1533`-style code reuse used to hide.

- **The three-way "which programs are green" triplication** (`DESIGN-test-build-ci.md` §1.2, smell #3): `guard.sh` `NIST_TESTS`, `NistDifferentialTests` `[InlineData]`, and `tests/nist/chains.tsv`. P0 introduces `tests/nist/corpus.tsv` as the single source; P14's greenfield guard consumes it (not a fourth copy), and the equivalence proof confirms the manifest-driven run reproduces the legacy verdict list before the old sources are deleted (`DESIGN-test-build-ci.md` §6 risk 4 mitigation).

---

## 3. Target end-state (what exists when this phase is DONE)

Files created / changed by P14 (real paths):

**Scripts / tooling**
- `scripts/greenfield-guard.sh` — the in-repo greenfield census guard (bash). Drives `src/Cobol.Net.Cli/bin/Debug/net10.0/cobol.dll` over the full NIST census (from `tests/nist/corpus.tsv`), compiles+runs every program, diffs golden-bearing ones against `tests/nist/valid/`, accepts golden-less run-only programs (census health), resolves producer→consumer chains, and carries the `LEGACY_DIVERGENT` set as expected-diff sourced from the LEDGER. Exit 0 iff no regression.
- `scripts/greenfield-guard.ps1` — Windows-parity wrapper (thin; may shell the same census-runner logic or a shared `dotnet` entry). Behavior-equivalent output vocabulary.
- `scripts/equivalence-proof.sh` — the one-time verdict-diff harness: runs `guard.sh` and `greenfield-guard.sh`, captures each per-program verdict list, diffs them, and asserts the delta ⊆ `LEGACY_DIVERGENT`. Writes the recorded proof.
- `scripts/gen-vcr.ps1` — (from P3; extended here) regenerates VCR status from `constructs.json` + harness results so `TODO` can never be hand-ticked back in.

**Compiler / CLI**
- `src/Cobol.Net.Cli` — a census *run* mode. Prefer extending the existing `check-batch` command (`Program.cs:103` — parse+bind only, no Roslyn) with a sibling `run-batch` (or `census`) subcommand that compiles **and runs** each manifest entry and reports `MATCH`/`DIFF`/`COMPILE-FAIL`/`RUN-FAIL`/`NO-GOLDEN` per program, so the census guard is one warm process instead of ~459 cold `dotnet` invocations. If a run-batch command is judged too heavy, the guard script may fall back to per-program `cobol` + `dotnet <dll>` (the `guard.sh` shape), but the single-process form is the target (matches P0's Roslyn-cache throughput goal).

**Tests**
- `tests/Cobol.Net.Tests.Conformance/VersionMatrixTests.cs` — (from P0/P3) extended: INV-1 strict+permissive rows over `corpus.tsv`; the `expectDiagnostic` assertions cover every folded-in VCR row.
- `tests/Cobol.Net.Tests.Conformance/VersionBehaviorMatrixTests.cs` — (from P3) INV-3: every `variant`-tagged construct row runs under each `--std` and its stdout is diffed against the per-edition expectation.
- `tests/Cobol.Net.Tests.Conformance/Inv1StrongGoldenTests.cs` — NEW: the always-on INV-1-strong leg — compiles AND runs the full golden set at `--std 2023 --permissive` and asserts byte-identical output (the seeded `COBOLNET_NIST_STD`/`COBOLNET_NIST_PERMISSIVE` path promoted from an env-gated leg to a first-class `[Theory]`).
- `tests/Cobol.Net.Tests.Unit/DiagnosticRegistryCoverageTests.cs` — NEW: asserts every `DiagnosticDescriptor` in the P2 registry is (a) unique-coded and (b) exercised by ≥1 negative-corpus `.err` case (or an inline diagnostic-snapshot case). A registered-but-untested code is a red.
- `tests/conformance/**/manifest.json` + `tests/conformance/negative/*.{cob,err}` — completed so every registered descriptor and every folded-in VCR gate has a witness; `CorpusRunnerTests.Manifest_CoversEveryProgram_NoOverlap` stays green.

**Docs**
- `docs/VERSION_CHANGE_REFERENCE.md` — zero `TODO`; each row `GATED`/`green` or `DISPOSITION:`-noted with citation.
- `docs/LEGACY_DIVERGENCE_LEDGER.md` — NEW (type LEDGER): the 11 `LEGACY_DIVERGENT` programs, each with its ISO §-citation and the reason the golden diverges from the legacy engine's output (migrated verbatim from `guard.sh:122-145`). The greenfield guard reads its divergent-set from here.
- `docs/rearchitecture/EQUIVALENCE-PROOF.md` — NEW: the recorded one-time proof (date, commits of both engines, the verdict-diff output, the ⊆-`LEGACY_DIVERGENT` conclusion). This is the artifact P15 Cut-1 cites as its precondition.
- `docs/DOC_INDEX.md` — rows added for the LEDGER, the equivalence proof, and this phase doc; the VCR row's maintenance note updated ("status generated, never hand-ticked").

**CI (`.github/workflows/build-and-test.yml`)**
- A new `greenfield-guard` job (per-OS or ubuntu) running `scripts/greenfield-guard.sh` (or `.ps1`), now an **authoritative** gate alongside the existing `greenfield-tests` and `inv1-sweep`.
- The existing `guard` (legacy `guard-fast.sh`) job is **kept** and renamed in intent to `legacy-oracle` — the temporary cross-check that the greenfield guard still matches a live legacy run — retained through P14 and deleted in P15.

---

## 4. STEP-BY-STEP

> Ordering rationale: the burn-down and sweeps (Steps 1–5) make the *greenfield truth* complete and green; the guard (Steps 6–8) packages that truth into a self-standing regression; the equivalence proof (Step 9) validates the package against the oracle; the LEDGER migration (Step 10) and CI wiring (Step 11) make it durable. The equivalence proof MUST run while the legacy engine is present — it is the last step that structurally requires the oracle.

### Precheck (no commit) — reproduce the baseline green

Before touching anything, prove the battery is green from a clean build (the DEVLOG-577 incremental-build-masking lesson):

```
dotnet build CobolSharp.sln -c Debug
dotnet build src/Cobol.Net.Cli/Cobol.Net.Cli.csproj -c Debug
dotnet test tests/Cobol.Net.Tests.Conformance/Cobol.Net.Tests.Conformance.csproj
dotnet test tests/Cobol.Net.Tests.Unit/Cobol.Net.Tests.Unit.csproj
bash scripts/guard-fast.sh                 # legacy oracle: ALL GREEN
bash scripts/version-continuity-sweep.sh | tee /tmp/sweep.log ; ! grep -q BREAKS /tmp/sweep.log
```

Expected: conformance + unit green; `guard-fast.sh` prints `=== ALL GREEN ===`; the sweep prints no `BREAKS`. Record the counts in the DEVLOG opener. If any is red, STOP — a precondition phase (P3/P13) is not actually complete.

Confirm the P14 preconditions exist:
- `tests/nist/corpus.tsv` exists (P0). If not present, P0's manifest step was skipped — create it first (fold `NistDifferentialTests` `[InlineData]` ∪ `guard.sh` `NIST_TESTS` ∪ `chains.tsv` into the schema in `DESIGN-test-build-ci.md` §3.2, guarded by `CorpusManifestTests`). This is a P0 deliverable; do it here only if missing.
- The P2 diagnostic-descriptor registry exists (`src/Cobol.Net.Compiler/Diagnostics/` with `DiagnosticDescriptor`/`Diag`). If absent, Step 4's coverage test cannot be authored — escalate; it is a P2 deliverable.

---

### Step 1 — Drive the VCR to zero-TODO (green/GATED or written disposition)

**Files:** `docs/VERSION_CHANGE_REFERENCE.md`; per-row, whichever of `constructs.json` (`tests/version-matrix/constructs.json`), the `EditionValidator`/`ConstructRegistry` gates, or a binder gate the row needs; `tests/conformance/negative/<row>.{cob,err}` for each newly-gated row.

**Change:** enumerate the ~117 `TODO` rows (`grep -n '| TODO |' docs/VERSION_CHANGE_REFERENCE.md`). For each, ONE of:
- **Gate it** (the row names a real, in-scope gating obligation whose feature is implemented by P13): add/confirm the `constructs.json` row + registry gate + a negative witness, run the row's matrix cell, then flip the status cell to `GATED (…, DEVLOG NNN)` with the code and site — mirroring the existing `GATED (W2: move-alphanumeric-figurative-removed-2023, 0902 …)` shape already in the file.
- **Disposition it** (the row is intentionally not gated — e.g. an Annex A.4 documented-non-support module per ratified decision #3, a behavior with no observable edition delta, or a spec-undefined choice): replace `TODO` with `DISPOSITION: <one line + ISO § + reason>`. Non-support dispositions must trace to a `COMPLETION_ROADMAP_COUNCIL.md` §5 decision (screen/MCS/commit-rollback/locale/extended-letters/A.4.8/A.4.13/VALIDATE) or the §4.2 conformance document plan.

Work in row-family batches (do NOT try all 117 at once). Natural batches: Table 1 (2014→2023 E.2 substantive), Table 2/3 (new directives / reserved words), Table 5 (behavior rows), the archaic/obsolete flags (VCR 89/90/126/127), the 85→2002 interim rows. For each batch, cite the ISO § and the edition. This is a spec-first burn-down: derive the expected outcome from `specs/ISO_COBOL.md` and cite the § in the row and (for a gate) in the code (`feedback_use_the_spec`, `project_spec_to_code_traceability`).

**Why:** a `TODO` row is an unproven version-correctness claim; the exit criterion is zero. Making status *derived* (Step 2) is what stops it drifting back.

**Verify (per batch):**
```
dotnet test tests/Cobol.Net.Tests.Conformance/Cobol.Net.Tests.Conformance.csproj --filter "VersionMatrixTests|EditionGateDiagnosticTests|CorpusRunnerTests"
```
Expected: green, with the new matrix reject/accept cells and negative witnesses passing. At the end of the last batch: `grep -c '| TODO |' docs/VERSION_CHANGE_REFERENCE.md` → `0`.

**COMMIT BOUNDARY** (one per batch): `docs(cobolnet): VCR burn-down batch <name> — <k> rows GATED/dispositioned to zero-TODO (PHASE-14)`. Keep the battery green at each commit.

---

### Step 2 — Make VCR status harness-derived + add the drift guard

**Files:** `scripts/gen-vcr.ps1` (from P3 — extend); `tests/Cobol.Net.Tests.Unit/VcrStatusDriftTests.cs` (NEW or extend a P3 drift test).

**Change:** ensure `scripts/gen-vcr.ps1` regenerates every row's status column from `constructs.json` + a `VersionMatrixTests --emit-status` run (a row is `GATED` iff its `(construct × edition)` fixture passes; `DISPOSITION` rows are pass-through from a `disposition` field in `constructs.json`). Add `VcrStatusDriftTests` asserting the committed `VERSION_CHANGE_REFERENCE.md` status column equals `gen-vcr.ps1`'s output (mirroring `ConstructRegistryDriftTests`). A future hand-edit that re-introduces a `TODO`, or a gate that silently regresses, now fails the drift test.

**Why:** `DESIGN-edition-framework.md` §2.10 / P8 — "a stale ledger becomes structurally impossible." Without this, Step 1's zero-TODO is a snapshot, not an invariant.

**Verify:**
```
pwsh scripts/gen-vcr.ps1 -Check          # exits non-zero on any drift
dotnet test tests/Cobol.Net.Tests.Unit/Cobol.Net.Tests.Unit.csproj --filter VcrStatusDriftTests
```
Expected: no drift; test green.

**COMMIT BOUNDARY:** `feat(cobolnet): VCR status is generated from the harness + drift-guarded — no hand-ticked rows (PHASE-14)`.

---

### Step 3 — Promote the INV-1-strong golden re-match at `--std 2023` to an always-on test

**Files:** `tests/Cobol.Net.Tests.Conformance/Inv1StrongGoldenTests.cs` (NEW); reuses `NistDifferentialTests.RunNist` / `Normalize` / `Chains` (make them `internal static` shared helpers or lift into a `NistRunner` helper class so both test classes call one implementation — `feedback_singular_pattern`).

**Change:** author a `[Theory]` that, for every golden-bearing program in `corpus.tsv` (status `green`), compiles AND runs it at `DialectLevel: 2023, Permissive: true` and asserts byte-identical normalized output vs `tests/nist/valid/<name>.txt`. This is the seeded `COBOLNET_NIST_STD=2023 COBOLNET_NIST_PERMISSIVE=1` path (`NistDifferentialTests.cs:528-536`) promoted from an env-gated manual leg to a first-class always-run test. Keep the env override too (for ad-hoc runs at 2002/2014), but 2023-permissive now runs unconditionally in `dotnet test`.

If any program diffs at 2023-permissive, it is a real bug in a version-gating behavior (not the test): triage against a VCR behavior row; fix the gate; re-run. This is exactly the fatal-challenge triage the P2.7 flip attached (`COMPLETION_ROADMAP_COUNCIL.md` §2 Phase 1: "re-run the 318 goldens at --std 2023 permissive and triage every diff against VCR behavior rows").

**Why:** decision #10 (RATIFIED) — INV-1-strong at the default edition is a G7 exit criterion; the shipping default must be behaviorally executed before G8.

**Verify:**
```
dotnet test tests/Cobol.Net.Tests.Conformance/Cobol.Net.Tests.Conformance.csproj --filter Inv1StrongGoldenTests
# Sanity that the env path still agrees (optional):
COBOLNET_NIST_STD=2023 COBOLNET_NIST_PERMISSIVE=1 dotnet test ... --filter NistDifferentialTests
```
Expected: all golden programs byte-match at 2023-permissive.

**COMMIT BOUNDARY:** `feat(cobolnet): INV-1-strong — full golden run byte-matches at --std 2023 --permissive, always-on (PHASE-14)`.

---

### Step 4 — Complete the negative corpus + the registry-coverage floor

**Files:** `tests/Cobol.Net.Tests.Unit/DiagnosticRegistryCoverageTests.cs` (NEW); `tests/conformance/negative/*.{cob,err}` (add missing witnesses); `tests/conformance/negative/manifest.json` (list them); `tests/conformance/{2002,2014,2023}/manifest.json` (enable per-edition positives that P13 landed).

**Change:**
1. Author `DiagnosticRegistryCoverageTests`: enumerate every `DiagnosticDescriptor` in the P2 registry; assert each `Code` is unique; assert each code appears in ≥1 negative-corpus `.err` file (or an inline diagnostic-snapshot case for codes that cannot be triggered by a whole-program `.cob`, e.g. internal ICE guards — those are allowlisted with a reason). The set of `.err`-referenced codes is computed by scanning `tests/conformance/negative/*.err` for the `COBOLNET####` token.
2. For each uncovered code, add a minimal `<name>.cob` that triggers it + a `<name>.err` naming the code, list it in `tests/conformance/negative/manifest.json` with the editions it should reject at, and verify via the CLI before enabling (the discipline `COMPLETION_ROADMAP_COUNCIL.md` W2(c) used: "every (case × edition) rejection AND every pre-removal-edition clean compile verified against the CLI before enablement").

**Why:** exit criterion #4 — "every diagnostic descriptor ≥1 case." Closes the false-green class where a registered code has no test and can silently rot.

**Verify:**
```
dotnet test tests/Cobol.Net.Tests.Unit/Cobol.Net.Tests.Unit.csproj --filter DiagnosticRegistryCoverageTests
dotnet test tests/Cobol.Net.Tests.Conformance/Cobol.Net.Tests.Conformance.csproj --filter CorpusRunnerTests
# spot-check a new negative case:
dotnet src/Cobol.Net.Cli/bin/Debug/net10.0/cobol.dll tests/conformance/negative/<new>.cob --std 2023 --check   # expect the .err code
```
Expected: coverage test green (no uncovered descriptor); corpus manifest drift green.

**COMMIT BOUNDARY:** `test(cobolnet): negative-corpus completion + registry-coverage floor (every descriptor has a witness) (PHASE-14)`.

---

### Step 5 — Run the full INV-1 / INV-2 / INV-3 sweeps (strict + permissive) and close any gap

**Files:** none new necessarily — this is a *run + fix* step over `VersionMatrixTests` (INV-1 weak + INV-2), `Inv1StrongGoldenTests` (INV-1 strong, Step 3), `VersionBehaviorMatrixTests` (INV-3), and `scripts/version-continuity-sweep.sh` (INV-1 permissive, check-batch).

**Change:** run all four; every failure is a version-gating bug to fix (`feedback_diff_is_a_bug`). Confirm the continuity-sweep exit condition of the G7 criteria: every *strict* later-edition failure traces to a recognized edition-band code (0801/0802/0810/0811/0873/0875–0879/0882/0893/0900-band) — add a sweep post-check that, for each strict `BREAKS`, greps the compile diagnostics for one of those codes and fails otherwise (this makes "traces to a removal/reserved row" machine-checked, not asserted).

**Why:** exit criterion #3 + #2. INV-3 (behavior-variant) is "the currently-weakest leg of four-compilers-in-one" (`DESIGN-edition-framework.md` §2.10) — running every `variant` row under all four `--std` and diffing stdout is the discovery tool that catches un-gated semantic deltas.

**Verify:**
```
dotnet test tests/Cobol.Net.Tests.Conformance/Cobol.Net.Tests.Conformance.csproj \
  --filter "VersionMatrixTests|VersionBehaviorMatrixTests|Inv1StrongGoldenTests"
bash scripts/version-continuity-sweep.sh | tee /tmp/sweep.log ; ! grep -q BREAKS /tmp/sweep.log
pwsh scripts/version-continuity-sweep.ps1   # add a .ps1 sibling if P3 didn't (DESIGN-test-build-ci §3.7)
```
Expected: all green; sweep no `BREAKS`; strict breaks (if any) all edition-band-coded.

**COMMIT BOUNDARY:** `test(cobolnet): full INV-1/2/3 sweeps green strict+permissive; strict breaks are edition-band-coded (PHASE-14)`.

---

### Step 6 — Add the census `run-batch` CLI mode (single warm process)

**Files:** `src/Cobol.Net.Cli/Program.cs` (add a `run-batch` command next to `check-batch` at `:103`); `src/Cobol.Net.Cli/CliOptions.cs` if a DTO is needed.

**Change:** add a `run-batch <manifest>` subcommand. Manifest line format (TSV): `<source>\t<std>\t<name>\t<permissive 0|1>\t<golden|-|run-only>\t<chain-preds space-joined|->`. For each entry it compiles (with NIST X-card preprocessing when the source is under `tests/nist/programs`), runs chain predecessors first (in an isolated dir), runs the program, locates the print file (`<name>.txt`) or stdout (the `guard.sh` discovery order), normalizes (drop CR, strip trailing, mask `COMPUTED=`), and reports one line per program:
`<name>\tMATCH | DIFF | COMPILE-FAIL | RUN-FAIL | NO-GOLDEN | DIVERGENT`. It parallelizes across cores (like `check-batch`) but each program's chain runs serially in its own scratch dir (port the isolation model from `NistDifferentialTests.RunNist` and `guard-fast.sh`). It reuses the cached Roslyn reference set (P0) so 459 compiles are one process.

**Why:** the census guard must exercise compile+**run** health of all 459 programs, not just the 318 golden-bearing ones (`guard.sh` does this today through legacy; the greenfield had no equivalent). A single warm process is the throughput target (P0's Roslyn-cache rationale). Reusing the exact `RunNist`/`guard.sh` normalization and chain logic is what makes the Step-9 equivalence proof clean.

**Verify:**
```
# build a tiny 5-program manifest and run it:
printf 'tests/nist/programs/NC101A.cob\t85\tNC101A\t0\tgolden\t-\n' > /tmp/m.tsv
dotnet src/Cobol.Net.Cli/bin/Debug/net10.0/cobol.dll run-batch /tmp/m.tsv
```
Expected: `NC101A\tMATCH`. Add a run-only (golden-less) program and a chained consumer (e.g. `ST103A` with preds `ST101A ST102A`) and confirm `NO-GOLDEN` and `MATCH` respectively.

**COMMIT BOUNDARY:** `feat(cobolnet): cobol run-batch — warm-process census compile+run over a manifest (PHASE-14)`.

---

### Step 7 — Build the in-repo greenfield census guard

**Files:** `scripts/greenfield-guard.sh` (NEW); `scripts/greenfield-guard.ps1` (NEW).

**Change:** the guard, driving `cobol.dll` (greenfield) — a faithful greenfield analog of `guard.sh` (Read `scripts/guard.sh` in full; port its census loop, chain handling, print-file/stdout discovery, `normalize()`, the FAIL*/footer `NNN TEST(S) FAILED` pass-fail signal, and the golden-cleanliness sweep). It:
1. Builds `cobol.dll` + copies `Cobol.Net.Runtime.dll` into the run dir.
2. Emits the census manifest from `tests/nist/corpus.tsv` (the single source — `status`, `golden`, `chain-preds` columns), NOT a hand list.
3. Runs `cobol run-batch <manifest>` (Step 6).
4. Aggregates: golden `green` rows must be `MATCH`; `run-only`/golden-less rows must be `COMPILE-FAIL`-free and `RUN-FAIL`-free (census health — output not compared, by design; NIST leaves ~102 programs golden-less); a `DIFF`/`COMPILE-FAIL`/`RUN-FAIL` on a `green` row is a regression → exit 1.
5. Reads the divergent set from `docs/LEGACY_DIVERGENCE_LEDGER.md` (Step 10) — those goldens are ISO-conforming and are reported `DIVERGENT (expected)`, never a regression.
6. Runs the golden-cleanliness sweep (port `guard.sh:232-253`: no empty golden, no `FAIL*` in a golden, no nonzero footer).
7. Prints a census summary `<GREEN>/<census> GREEN` and `=== ALL GREEN ===` / `=== N REGRESSION(S) ===`.

`greenfield-guard.ps1` mirrors it (may call the same `run-batch` + a PowerShell aggregation, or invoke a shared aggregator).

**Why:** exit criterion #5 — the self-standing regression that replaces the legacy `guard.sh` NIST loop and survives P15. Sourcing the census from `corpus.tsv` kills the triplication (smell #3).

**Verify:**
```
bash scripts/greenfield-guard.sh
```
Expected: `=== ALL GREEN ===`, census summary `≥357 GREEN`, exit 0. On Windows: `pwsh scripts/greenfield-guard.ps1` → same verdict. Cross-check the GREEN count equals or exceeds the census criterion (≥357).

**COMMIT BOUNDARY:** `feat(cobolnet): in-repo greenfield census guard (cobol.dll over the full 459-census; run-only + chains) (PHASE-14)`.

---

### Step 8 — Reconcile the greenfield guard's GREEN set with the golden set

**Files:** possibly `tests/nist/corpus.tsv` (status fixes), `tests/Cobol.Net.Tests.Conformance/NistDifferentialTests.cs` (repoint at `corpus.tsv` `[MemberData]` if P0 didn't).

**Change:** confirm the census guard's `green` set == the `NistDifferentialTests` golden set == the `corpus.tsv` `green` rows == `guard.sh` `NIST_TESTS` minus `LEGACY_DIVERGENT`. Any mismatch is a triplication artifact — fix it in `corpus.tsv` (the single source) and re-point the in-process test's `[MemberData]` at it (if P0 left `[InlineData]` in place, do the fold here). Add/confirm `CorpusManifestTests` asserting: every `tests/nist/programs/*.cob` is listed; every `green` row has a `valid/<name>.txt`; every `divergent` row has a LEDGER citation.

**Why:** the equivalence proof (Step 9) diffs verdict lists; both sides must enumerate the *same universe* from the *same manifest* or the diff is noise (`DESIGN-test-build-ci.md` §6 risk 4).

**Verify:**
```
dotnet test tests/Cobol.Net.Tests.Conformance/Cobol.Net.Tests.Conformance.csproj --filter "NistDifferentialTests|CorpusManifestTests"
bash scripts/greenfield-guard.sh   # GREEN count unchanged
```
Expected: green; the three green-sets agree.

**COMMIT BOUNDARY:** `refactor(cobolnet): single-source the green NIST set in corpus.tsv; census guard == goldens == matrix (PHASE-14)`.

---

### Step 9 — The one-time equivalence proof (IRREVERSIBLE ORDERING GATE)

**Files:** `scripts/equivalence-proof.sh` (NEW); `docs/rearchitecture/EQUIVALENCE-PROOF.md` (NEW, the recorded artifact).

**Change:** author `equivalence-proof.sh`:
1. Runs `scripts/guard.sh` (or `guard-fast.sh`) capturing its per-program verdict lines (`NAME: MATCH` / `DIFF` / `LEGACY DIVERGENT` / …) → `legacy-verdicts.txt`.
2. Runs `scripts/greenfield-guard.sh` capturing its per-program verdicts → `greenfield-verdicts.txt`.
3. Normalizes both to `NAME PASS|FAIL|DIVERGENT` (collapse the vocabularies) and diffs them by program name.
4. Asserts the delta ⊆ the 11 `LEGACY_DIVERGENT` programs (where the two engines *legitimately* differ — greenfield is ISO-correct, legacy has the documented hole). Any other disagreement is a proof failure → exit 1.
5. Emits `EQUIVALENCE-PROOF.md` recording: the date, `git rev-parse HEAD`, the legacy engine build id, both full verdict lists, the diff, and the conclusion "greenfield census guard reproduces the legacy verdicts modulo the 11 documented ISO re-baselines — the greenfield guard is a faithful replacement; P15 may proceed."

Run it. Fix any non-divergent disagreement (it is a real bug in either the greenfield guard's discovery/normalization or a genuine census regression) until the delta is exactly `LEGACY_DIVERGENT`.

**Why:** exit criterion #6 and the phase's whole reason for MEDIUM-risk sequencing — this is the **single irreversible ordering constraint**. It can only run while the legacy engine exists (P15 deletes it). `COMPLETION_ROADMAP_COUNCIL.md` §4 risk 4: "deleting the oracle first makes the greenfield guard unverifiable forever." P15 Cut-1 cites `EQUIVALENCE-PROOF.md` as its precondition.

**Verify:**
```
bash scripts/equivalence-proof.sh    # exit 0; writes docs/rearchitecture/EQUIVALENCE-PROOF.md
grep -c '^' docs/rearchitecture/EQUIVALENCE-PROOF.md   # non-empty, recorded
```
Expected: exit 0; the diff is empty modulo the 11 divergent programs; the artifact is written.

**COMMIT BOUNDARY:** `docs(cobolnet): record the one-time greenfield↔legacy equivalence proof — P15 unblock gate (PHASE-14)`. **Do NOT let any P15 legacy-deletion work land before this commit exists and is green.**

---

### Step 10 — Migrate the LEGACY_DIVERGENT citations into a durable LEDGER

**Files:** `docs/LEGACY_DIVERGENCE_LEDGER.md` (NEW); `scripts/greenfield-guard.sh` / `run-batch` (read the divergent set from the LEDGER, not a hard-coded list); `scripts/guard.sh` (leave as-is — deleted in P15).

**Change:** move the 11-program `LEGACY_DIVERGENT` block and its per-program ISO citations (`guard.sh:122-145` — IX111A §14.9.49.4 GR3a; IX210A/214A/215A §14.9.17/§14.9.41 GR9/§9.1.13.5; NC235A/NC236A §14.9.37.4 GR8b; SQ207M §14.9.46 GR1; ST146A §14.9.30 GR18; SQ101M §14.9.51 GR25a; SQ208M/SQ210M §13.18.34 GR6b) into `LEGACY_DIVERGENCE_LEDGER.md` as a table: `program | ISO § | legacy behavior | ISO-conforming greenfield behavior | golden re-baseline DEVLOG`. Point the greenfield guard's divergent-set loader at this file (a parseable list of program names). Add a `DOC_INDEX.md` row.

**Why:** exit criterion #7 — when P15 deletes `guard.sh`, these 11 citations (the provenance of 11 re-baselined goldens) must not vanish. `COMPLETION_ROADMAP_COUNCIL.md` Phase 8: "migrate the 11 LEGACY_DIVERGENT ISO citations into the new guard / a LEDGER doc."

**Verify:**
```
bash scripts/greenfield-guard.sh    # still ALL GREEN; the 11 reported DIVERGENT (expected) sourced from the LEDGER
```
Expected: identical verdict; the divergent set now flows from the LEDGER.

**COMMIT BOUNDARY:** `docs(cobolnet): LEGACY_DIVERGENCE_LEDGER — migrate the 11 ISO re-baseline citations out of guard.sh (PHASE-14)`.

---

### Step 11 — Wire the greenfield guard into CI as an authoritative gate

**Files:** `.github/workflows/build-and-test.yml`.

**Change:** add a `greenfield-guard` job (ubuntu; run `bash scripts/greenfield-guard.sh`) as a first-class gate. Add a Windows leg (or a matrix) running `pwsh scripts/greenfield-guard.ps1` to close the OS gap (`DESIGN-test-build-ci.md` §1.5 smell #5 — the NIST regression was Linux-only). **Keep** the existing `guard` (legacy `guard-fast.sh`) job, re-commented as `legacy-oracle` — the temporary cross-check retained through P14/P15-Cut-1 and deleted in P15. Optionally add an `equivalence-proof` job that runs `scripts/equivalence-proof.sh` on a schedule/manually (it needs both engines; it is the pre-P15 insurance).

**Why:** the authoritative regression must run cross-platform and gate every PR before P15; keeping the legacy job is the "cheap insurance through the rearch" the owner chose (decision #3 of `DESIGN-test-build-ci.md` §7, `COMPLETION_ROADMAP_COUNCIL.md` decision #8).

**Verify:** push the branch; confirm the new `greenfield-guard` job (both OSes) is green and `legacy-oracle` still green. Locally: `bash scripts/greenfield-guard.sh` and `pwsh scripts/greenfield-guard.ps1` both exit 0.

**COMMIT BOUNDARY:** `ci(cobolnet): greenfield census guard is authoritative (cross-OS); legacy oracle kept as temporary cross-check (PHASE-14)`.

---

## 5. Verification — the full battery at phase end

Run all of the following from a clean build; every one must be green before declaring DONE:

```
# 1. Clean build (no incremental masking)
dotnet build CobolSharp.sln -c Release          # warnings-as-errors on Release
dotnet build src/Cobol.Net.Cli/Cobol.Net.Cli.csproj -c Debug

# 2. In-process greenfield battery
dotnet test tests/Cobol.Net.Tests.Conformance/Cobol.Net.Tests.Conformance.csproj
dotnet test tests/Cobol.Net.Tests.Unit/Cobol.Net.Tests.Unit.csproj
dotnet test tests/Cobol.Net.Tests.Characterization/Cobol.Net.Tests.Characterization.csproj   # gate 2 & 3 (P0)

# 3. INV sweeps
bash scripts/version-continuity-sweep.sh | tee /tmp/sweep.log ; ! grep -q BREAKS /tmp/sweep.log
pwsh scripts/version-continuity-sweep.ps1

# 4. Greenfield census guard (the P15-survival regression) — both OSes
bash  scripts/greenfield-guard.sh          # === ALL GREEN ===, ≥357 GREEN, exit 0
pwsh  scripts/greenfield-guard.ps1

# 5. Legacy oracle still green (needed for the proof)
bash scripts/guard-fast.sh                 # === ALL GREEN ===

# 6. The one-time equivalence proof (records the artifact)
bash scripts/equivalence-proof.sh          # exit 0; delta ⊆ LEGACY_DIVERGENT

# 7. VCR / drift invariants
test "$(grep -c '| TODO |' docs/VERSION_CHANGE_REFERENCE.md)" -eq 0
pwsh scripts/gen-vcr.ps1 -Check
```

**Behavior-neutrality / byte-exact checks:**
- The greenfield census guard's GREEN-golden set is byte-identical to `tests/nist/valid/*.txt` on the NIST acceptance basis (drop CR, strip trailing, mask `COMPUTED=`) — proven per-program by Step 7 and cross-proven against legacy by Step 9.
- INV-1-strong: the full golden set byte-matches at `--std 2023 --permissive` (Step 3), i.e. the shipping default edition produces conforming output on every golden program.
- The equivalence-proof delta is *exactly* the 11 `LEGACY_DIVERGENT` programs — no more, no fewer (a smaller delta means a golden silently regressed to the legacy value; a larger delta means an un-proven census divergence).
- Characterization snapshots (P0 gate 3) are unchanged — P14 adds no emitter behavior, so `Snapshots/` must not move. If a snapshot diffs, a Step-1 gate accidentally changed emission; investigate before re-baselining.

---

## 6. Rollback / resumability

- **Every step is its own commit boundary and leaves the battery green.** To resume after an interruption, read the `STATUS` line, run the Precheck battery, then continue at the next unstarted step. Steps 1–5 are independently valuable and independently revertible (a bad VCR batch or matrix fix reverts without touching the guard).
- **Step 1 (VCR burn-down) is resumable mid-family:** the `grep -c '| TODO |'` count is the progress meter; work in row-family commits so a partial burn-down is still green and mergeable.
- **The guard (Steps 6–8) is additive** — it introduces new scripts/tests and a new CLI subcommand; it changes no existing behavior, so it cannot regress the battery. If `run-batch` proves too complex, the guard may fall back to the per-program `cobol` + `dotnet <dll>` shape (the `guard.sh` idiom) — slower but equivalent; note the fallback in the script header.
- **Step 9 is the hard gate and is idempotent** — re-runnable any number of times while legacy exists; it writes/overwrites `EQUIVALENCE-PROOF.md`. If it fails, DO NOT proceed to P15; the failure is a real census divergence or a guard discovery/normalization bug (most likely: print-file-vs-stdout discovery, chain ordering, or a normalization mismatch — diff a single failing program's two verdict paths by hand).

**Risks + mitigations:**
1. **A VCR row is genuinely ambiguous (2002/2014 edge with no in-repo authority).** Mitigation: per ratified decision #1 (no standards acquisition), disposition it with a provisional-confidence marker and an Annex-E/legacy-inventory citation; a provisional edge is a written disposition, not a `TODO`. Never gate on a guess (`feedback_use_the_spec`; a wrong gate can reject a valid program).
2. **The equivalence proof reveals a census program the greenfield gets wrong that legacy got right** (a real regression the 318-golden subset never covered). Mitigation: this is the entire point of running the proof *before* deletion — fix the greenfield compiler (`feedback_no_workarounds_root_cause`), never weaken the guard to pass. This may pull in a small feature fix; that is in-scope for P14 (it is a correctness gap the net had been blind to).
3. **`corpus.tsv` chain semantics don't reproduce `chains.tsv` + `guard.sh` ordering** (`DESIGN-test-build-ci.md` §6 risk 4). Mitigation: Step 8 reconciles the three sources before Step 9; the equivalence proof is itself the guard-verify-style diff that confirms fidelity.
4. **Snapshot / warnings-as-errors churn from the new test/CLI code.** Mitigation: the new code is test/tooling, not emitter; keep it warning-clean (Release build gate) and it cannot move a characterization snapshot.

---

## 7. ISO feature work in this phase

P14 is primarily verification/tooling, but the VCR burn-down (Step 1) closes the last version-gating gaps and the INV sweeps (Step 5) may surface un-gated behavior deltas. All work is spec-first; cite `specs/ISO_COBOL.md` §s in every row and gate.

**Spec sections / editions in play (Step 1 batches + Step 5 discoveries):**
- **2014→2023, Annex E.2 substantive changes** (VCR Table 1, ~50 rows): the remaining `TODO` removal/behavior rows — CALL … ON OVERFLOW removal (E.2 item 1c, §14.9.11), ALIGN/strong-typing bit alignment (E.2 item 2), boolean shift operators B-SHIFT-* (E.2 item 3, §8.8.x — new-feature-gate ≥2023), UCS user-defined-word character changes (E.2 item 4), the nine new directive words (E.2 item 5). Each: positive at ≥ intro, negative (reject with the edition-band code) at intro−1; behavior rows diff stdout per `--std` (INV-3).
- **2023 new reserved words** (VCR Table row 32; ISO §8.9): the interval-encoded witnesses already exist — confirm each has a matrix row + negative case (Step 4).
- **Archaic / obsolete flags** (VCR 89/90/126/127; ISO §4.2.12 archaic / §4.2.13 obsolete): EXIT PROGRAM, NEXT SENTENCE — 0903 warnings at ≥2023, their own sub-code in the band (`COMPLETION_ROADMAP_COUNCIL.md` D12).
- **85→2002 / 2002→2014 interim rows** (VCR Table 7): where the 2023 spec cannot adjudicate, disposition with a provisional edge + legacy-inventory citation (ratified decision #1).
- **Annex A.4 documented-non-support dispositions** (screen A.4.2, MCS A.3, commit/rollback A.4.3, locale A.4.9, extended-letters A.4.6, A.4.8, A.4.13, VALIDATE A.4.14 — per ratified decision #3): these VCR rows are `DISPOSITION:` (documented non-support with a uniform diagnostic + the §4.2 conformance-document plan), never `TODO`.

**Conformance tests / goldens to add:**
- One negative-corpus `.cob`/`.err` per newly-gated VCR row (Step 1) + per uncovered diagnostic descriptor (Step 4), listed in `tests/conformance/negative/manifest.json`, verified against the CLI at each named edition before enablement.
- INV-3 behavior-variant `.out` fixtures for every `variant`-tagged row (Step 5), one per edition where the behavior differs.
- The INV-1-strong leg (Step 3) adds no new goldens — it re-runs the existing `tests/nist/valid/*.txt` at `--std 2023 --permissive`.

No new *positive* NIST goldens are minted in P14 (the corpus is COBOL-85; new positive per-edition corpora are P9–P13 work). P14 proves what exists is correct and complete, and packages it into a self-standing, equivalence-proven guard.
