# CobolSharp — Session Resume Prompt (2026-06-03)

Paste this to start a new session. **Mission: drive the FULL NIST CCVS85 suite to "operational."**
This file is the authoritative, current orientation; linked docs hold the detail. Current as of DEVLOG 294.

## Read first
- **CLAUDE.md** (root) → **PROMPT.md** (non-negotiable doctrine), **PROJECT_PLAN.md** (status + session log),
  **DEVLOG.md** (decision narrative — now at entry 294).
- **specs/ISO_COBOL.md** is the authoritative spec (submodule — `git submodule update --init --recursive` if
  absent). **Implement from the spec, not from assumptions.** The markdown preserves required-keyword
  underlining as `<u>…</u>` in figure-style formats but NOT inside ``` code blocks — check the figure form
  when a keyword's optionality matters.
- **docs/dialect-strictness.md** — the two-axis dialect model (version `--standard` vs strictness) and the
  registry of CCVS non-conformant constructs (leniencies L1–L5; L4 deferred). **Discipline rule: every
  grammar leniency is dialect-gated through `DialectStrictnessChecks` from the moment it is added — never an
  unconditional grammar relaxation.** `--nist` implies `--standard default` (permissive); named-strict modes
  (e.g. `--standard cobol2023`) reject the leniencies.
- Memory index loads automatically. Key entries: `project_nist_progress` (suite-by-suite), `project_fileio_remaining`
  (file-I/O history + RL208A), `project_dialect_strictness` (leniency registry), `reference_nist_xcards`
  (X-card model), `project_collating_gap`.

## What "operational" means (the finish line)
Every one of the **459 NIST programs** in `tests/nist/programs/` is in exactly one accounted-for class:
1. **Baselined** — in `NIST_TESTS` (scripts/guard.sh) with a `tests/nist/valid/<T>.txt` at **0 FAIL\***,
   and its CCVS report is **non-vacuous** (tests EXECUTED > 0, footer "TEST(S) FAILED" = 0).
2. **NO_OUTPUT producer/builder** — runs in the guard ahead of a baselined consumer to build/sort a shared
   file (e.g. ST115A/ST116A; the 10 ST producers). Not baselined, but feeds a chain.
3. **Documented exclusion** — a flagging "M" module (compile-time FLAG test, no CCVS report: IF401M, IX301M/401M,
   SQ303M/401M, RL301M/401M, ST301M, SM301M/401M, …), or a non-deterministic test (NC214M, ACCEPT FROM
   DATE/TIME), or an out-of-scope obsolete/optional module (see Phase 5).
A new test enters the guard ONLY at 0 FAIL\* AND non-vacuous. Baselines must stay 0 FAIL\* forever.

## Current coverage (branch `main`, all committed, guard GREEN: 1000 unit / 347 integration / 299 NIST)
| Suite | Present | Baselined | Survey (CLEAN/FAIL\*/CF/NO/RT) | Status |
|-------|--:|--:|--|--|
| **NC** nucleus            | 95 | 94 | — | ✅ COMPLETE (NC214M dropped: non-deterministic ACCEPT) |
| **IF** intrinsics         | 45 | 42 | — | ✅ COMPLETE (IF401M/402M/403M flagging) |
| **IX** indexed I/O        | 42 | 40 | — | ✅ COMPLETE (IX301M/401M flagging) |
| **ST** sort/merge         | 40 | 29 +10 prod | — | ✅ COMPLETE (ST301M flagging) — every program accounted for |
| **SM** COPY/REPLACE       | 17 | 12 | 15/0/0/2/0 | ◐ **3 CLEAN un-baselined: SM104A/105A/205A** (+SM301M/401M flagging) |
| **SQ** sequential I/O     | 85 | 59 | (re-survey) | ◐ FAIL\*-tail clear; ~24 un-surveyed + SQ303M/401M flagging |
| **RL** relative I/O       | 35 | 19 | (re-survey) | ◐ chains done; **RL208A FAIL\*** + ~13 un-surveyed + RL301M/401M flagging |
| **IC** inter-program CALL | 47 |  4 | 20/2/4/21/0 | ⚠ **20 CLEAN, only 4 baselined** — biggest headroom (vacuous-verify needed) |
| **DB** debug              | 15 |  0 | 0/0/15/0/0 | ✗ whole Debug module unimplemented |
| **SG** segmentation       | 13 |  0 | 0/0/13/0/0 | ✗ whole Segmentation module (OBSOLETE in COBOL-2002) |
| **CM** communication      |  9 |  0 | 0/0/9/0/0  | ✗ whole Communication module (OBSOLETE in COBOL-2002) |
| **RW** report writer      |  6 |  0 | 0/0/6/0/0  | ✗ whole Report Writer module unimplemented |
| **OBSQ** obsolete-seq     |  4 |  0 | 4/0/0/0/0  | ◐ **all 4 CLEAN un-baselined: OBSQ1A/3A/4A/5A** |
| **OBNC/OBIC** obsolete    |  5 |  0 | 0/0/4/1/0  | ✗ obsolete-feature NC/IC variants |
| **EXEC** EXEC85           |  1 |  0 | 0/0/1/0/0  | ✗ EXEC85 driver (COMPILE_FAIL) |

Survey any suite: `bash scripts/run-suite.sh <PREFIX>` → per-test `CLEAN | N FAIL* | COMPILE_FAIL | NO_OUTPUT | RUNTIME(rc)` + a footer `=== PREFIX: total=… ===`. It does NOT create baselines.

## Roadmap to full coverage (priority order)

### Phase 1 — Quick baseline wins (~22 candidates, little/no code). Verify-non-vacuous, then baseline.
The survey already shows these CLEAN but not yet in the guard. For EACH: confirm the report is non-vacuous
(tests EXECUTED > 0, footer "TEST(S) FAILED" = 0, COMPUTED vs CORRECT meaningful), confirm determinism
(run twice, diff), order any producer ahead of its consumer, then add to `NIST_TESTS` + capture
`tests/nist/valid/<T>.txt`, and run the full guard.
- **SM104A / SM105A / SM205A** — CLEAN COPY/REPLACE tests not yet baselined. Likely the easiest wins.
- **OBSQ1A / OBSQ3A / OBSQ4A / OBSQ5A** — obsolete-sequential, all 4 CLEAN. Check they don't depend on an
  obsolete feature that's only *parsed-and-ignored* (which would make them vacuous).
- **IC: 15 CLEAN candidates** — IC101A/103A/108A/112A/115A/201A/206A/207A/209A/213A/216A/222A/223A/226A/237A
  (IC116M is flagging; IC203A/224A/225A/228A already baselined). **⚠ CAUTION — IC "CLEAN" is vacuous-prone:**
  the earlier session deliberately baselined only 4. An IC caller that `CALL`s a **separately-compiled** callee
  will, under the guard's one-`.cob`→one-`.dll` build, either not link or silently no-op while the test still
  prints PASS. Before baselining any IC test, verify the inter-program transfer ACTUALLY happened (the callee's
  effect shows in COMPUTED). Self-contained IC tests (caller + callee as nested/contained programs in ONE file)
  are genuinely baselineable; caller+separate-callee pairs need the multi-program link (Phase 4).

### Phase 2 — SQ/RL un-surveyed tail (runtime-correctness, the proven file-I/O method).
~24 SQ + ~13 RL programs are neither baselined nor known-excluded. Re-survey (`run-suite.sh SQ` / `RL`), then
per result: **FAIL\*** → read the failing line's COMPUTED vs CORRECT, trace to the responsible
`FileRuntime`/`*FileHandler` path, cite the ISO §9.1.13 status rule, **fix the pattern across all 3 handlers**
(Sequential/Indexed/Relative), baseline. **NO_OUTPUT** → it's a producer; order it consecutively ahead of its
consumer in the guard (data files are NOT cleaned between tests; RELATIVE/INDEXED `XXXXX###` share `TF###` by
number). **COMPILE_FAIL** → a parse form; add the grammar leniency dialect-gated (never unconditional).

### Phase 3 — RL208A (the ONE known open file-I/O compiler bug).
2 FAIL\*, 5-record gap in the RL207A→RL208A delete/update chain. Latent: `RelativeFileHandler.Rewrite` pads a
varying record to max length (via `ToSlot`) instead of storing the ACTUAL length — needs a variable-REWRITE
path mirroring `WriteVariable`. RL207A is baselined, so change carefully + full-guard.

### Phase 4 — IC genuine remaining (the real inter-program work).
- **Multi-program compile/link** for caller + separately-compiled callee (the 21 NO_OUTPUT callee halves + the
  4 COMPILE_FAIL + 2 FAIL\*). The grammar + multi-program compilation pipeline exist (nested programs); the gap
  is letting a guard test bundle a caller with its callee file(s) so the CALL resolves at runtime.
- **IC233A/234A** — `USE GLOBAL AFTER ERROR` declaratives on a file error + `SELECT OPTIONAL`/`OPEN INPUT`/`READ`
  (note IC233A omits the spec-required `STANDARD` — accept only as a documented leniency, ISO §14.9.49.2).
- **IC235A/227A/114A** — EXTERNAL data + sequential file I/O.
- Deferred GLOBAL follow-ups (not blocking): subscripted/ref-modded inherited globals (extend `OwnerProgramId`
  through element/ref-mod address paths), level-88 under a global group, FILE SECTION globals.

### Phase 5 — Whole unimplemented modules (LARGE; several OBSOLETE — needs a scope decision from the user).
Each is an entire COBOL module with no current support (all COMPILE_FAIL). **Recommend asking the user which
to implement vs formally exclude as out-of-scope**, then document the decision (a one-line exclusion class in
the guard comment + memory, like the flagging modules):
- **DB** (Debug, 15) — `USE FOR DEBUGGING`, `DEBUG-ITEM`, debugging lines (COBOL-85 optional module).
- **RW** (Report Writer, 6) — `RD` report descriptions, `GENERATE`/`INITIATE`/`TERMINATE` (sizable optional module).
- **SG** (Segmentation, 13) — `SECTION` segment-numbers / overlay (OBSOLETE, removed in COBOL-2002).
- **CM** (Communication, 9) — `CD` entries, `SEND`/`RECEIVE` (OBSOLETE, removed in COBOL-2002).
- **OBNC/OBIC** (5) + **EXEC85** (1) — obsolete-feature variants / EXEC driver.
The defensible default for full *modern*-COBOL conformance: **exclude CM, SG, OBNC, OBIC, EXEC as out-of-scope
obsolete/non-standard**, and treat DB + RW as optional stretch features. Confirm with the user before either
sinking weeks into an obsolete module OR declaring it excluded.

## Process rules (non-negotiable — from PROMPT.md + memory feedback_*)
- **Run `bash scripts/guard.sh` after meaningful changes; it must stay ALL GREEN, baselines 0 FAIL\*.**
- **NEVER edit a NIST `.cob` source to dodge a compiler bug — fix the compiler.** (Verified clean: no
  `tests/nist/programs/*.cob` has ever been modified.) X-card placeholder substitution happens in
  `NistPreprocessor` at compile time (source untouched) — that is required, not a workaround.
- **Grammar/semantic changes are pre-authorized for the NIST effort** — log in DEVLOG, rely on the full guard,
  commit. But every leniency must be **dialect-gated** (strict modes still error).
- **Every commit needs ≥1 DEVLOG entry**; write it as you go, not batched. Commit messages end with the
  `Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>` trailer.
- **Verify output is CORRECT, not just that it ran** — the vacuous-pass trap (a test self-certifying PASS with
  degenerate data, e.g. ST147A's blank-vs-blank collating checks before XXXXX063 was substituted).
- Keep PROJECT_PLAN.md, this file, and memory synced. Don't touch global NuGet config.
- **Guard soundness note:** the guard asserts 0 `FAIL\*` *detail* lines but does NOT check the footer
  `NNN TEST(S) FAILED` total — a suppressed-detail failure can slip through (NC208A did once). A footer-total
  sweep of existing baselines is a worthwhile hardening task.

## Architecture quick-reference (the pipeline + the files you'll touch most)
Grammar (`src/CobolSharp.Compiler/Grammar/Core/*.g4`) → `SemanticBuilder` (builds symbols; FD/RECORD clauses) →
`StorageLayoutComputer` (per-FD byte layout) → `Binder`/`FileIoBinder` (`BindingContext`, `BoundTreeValidator`,
`DialectStrictnessChecks`) → IR lowering (`FileIoLowerer`, `LocationResolver`, `ControlFlowLowerer`) → CIL
emission (`CilEmitter`, `CilFileIoEmitter`, `CilLocationEmitter`) → `CobolSharp.Runtime` (`FileRuntime`,
`IO/{Sequential,Indexed,Relative}FileHandler`, `SortRuntime`, `StorageHelpers`).
- **Single source of truth for variable-length records:** `SemanticModel.IsVariableLengthSequential`
  (= `IsRecordVarying || HasMultipleRecordSizes`); `FileSymbol.IsRecordVarying` is set in
  `SemanticBuilder.VisitRecordClause` for BOTH `RECORD IS VARYING` (Format 3) AND `RECORD CONTAINS m TO n`
  (Format 2, m≠n, ISO §13.18.43). Binder + FileIoLowerer both derive from it, so they can't disagree.
- **Key resolution (qualified / position-based):** `SemanticModel.ResolveQualifiedData` (base + OF/IN quals —
  NEVER `dataReference().GetText()`, which concatenates "A OF B"→"AOFB"), `ResolveKeyData`, position-based
  `ResolveKeyOfReference` (ISO §14.9.41).
- **Paragraph dispatch:** symbol-based control transfer (dup names OK) + return-address `Dispatch(startPc,exitPc)`
  helper (`CilEmitter.EmitDispatchHelper`) for PERFORM…THRU; declaratives are in `ParagraphDispatchOrder`, main
  loop starts at `EntryParagraphIndex`.
- **X-cards** (`NistPreprocessor` + `ReferenceFormatProcessor`): `XXXXX###`/`XXXXP###`(produce)/`XXXXD###`(consume)
  placeholders → substituted at compile time; column-7 indicator letters select TPF/X-card line variants. See
  `reference_nist_xcards`. **Boundary-anchor** any new substitution (`(?<![A-Za-z0-9])XXXXX0NN(?![A-Za-z0-9])`)
  so it can't corrupt an embedded test-data literal (cf. IX106A's `…XXXXXXXX065A…`); use a `MatchEvaluator` if
  the replacement contains regex-special chars like `$`.
- **GOTCHA (costs an afternoon):** a NEW emitted `IrRuntimeCall`/IR node needs an explicit `CilEmitter` dispatch
  case, or it falls through to the `// NOP` tail with its args left on the stack → `InvalidProgramException` at
  Main. A stale `tests/nist/output/<t>.txt` can mask it — check the exit code and `rm` the output first.

## Tooling
- Build: `dotnet build src/CobolSharp.CLI/CobolSharp.CLI.csproj`  (CLI dll: `src/CobolSharp.CLI/bin/Debug/net9.0/cobolsharp.dll`)
- Compile one test: `dotnet <cli.dll> --nist tests/nist/programs/<T>.cob -o tests/nist/output/<T>.dll`
- Run it: `(cd tests/nist/output && timeout 30 dotnet <T>.dll)` — always copy `CobolSharp.Runtime.dll` into
  `tests/nist/output/` first; use a `timeout` + a file-size guard for SORT/build tests (an unsubstituted
  record-count X-card once grew an 8.2 GB report).
- Survey a suite: `bash scripts/run-suite.sh <PREFIX>`. NIST run convention: `export COBOL_SWITCH_1=ON`.
  Capture guard output to a file + `echo "guard exit=$?"` (piping to grep masks the exit code).
- Preprocess (debug COPY/REPLACE + X-cards): `cobolsharp preprocess <file> -o <out>`.

## How we got here (one line)
NC/IF complete early; collating subsystem (DEVLOG 224–227); spec-audit follow-ups (228–232); IC to its
non-file-I/O ceiling (233–236); the file-I/O FILE-CONTROL "wall" broken as over-strict-grammar-vs-spec
(237–268); paragraph-dispatch engine reworked (259–260); IX suite complete (269–282); ST suite complete
(283–294). Full detail in DEVLOG.md + `project_nist_progress` / `project_fileio_remaining`.
