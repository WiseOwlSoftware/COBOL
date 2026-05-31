# CobolSharp — Session Resume Prompt (2026-05-29)

Paste this to start a new session. It orients you fast; the linked docs have the detail.

## Read first
- **CLAUDE.md** (project root) → which points to **PROMPT.md** (non-negotiable doctrine), **PROJECT_PLAN.md** (status), **DEVLOG.md** (decision narrative).
- **claude/state/session-state-2026-05-29.md** — the most detailed resume context (sections 0–0c cover the latest work).
- Memory index is loaded automatically; key entries: `project_nist_progress`, `project_fileio_remaining`, `project_collating_gap`, `reference_nist_xcards`.
- **specs/ISO_COBOL.md** is the authoritative COBOL spec (submodule — `git submodule update --init --recursive` if absent). Implement from the spec, not from assumptions.

## Current state (branch `main`, all committed, guard GREEN)
- **Guard** (`bash scripts/guard.sh`): 1000 unit / 347 integration (346 pass + 1 unrelated skip) / **151 NIST baselines (94 NC + 42 IF + 12 SM + 3 IC)**. Must stay ALL GREEN; baselines must stay 0 FAIL*. (NC214M was dropped — live ACCEPT FROM DATE/DAY/TIME, inherently non-deterministic; DEVLOG 226.)
- **Suites:** NC 100%, **IF 100%** (42 baselined), **SM COPY-feature 100%** (12 baselined). **IC in progress: 19/47 CLEAN (IC203A + IC224A + IC225A baselined, DEVLOG 233–234).** SQ/IX/RL/ST surveyed but paused.
- Spec-audit follow-ups done (DEVLOG 221–223): multi-dim `table(ALL)`, AT-END-vs-I/O-error split, runtime `FUNCTION LENGTH` for ref-mod.
- **Collating subsystem COMPLETE (DEVLOG 224–227):** comparisons, SORT/MERGE/table-sort keys, and FUNCTION CHAR/ORD all honor the alphanumeric program collating sequence. The STANDARD-2 all-255-table bug (which made `"ABCD" = SPACE` vacuously true) is fixed; identity sequences normalize to null. 8 contaminated NIST baselines corrected; NC214M dropped (non-deterministic). See `project_collating_gap` + `docs/collating-baseline-finding.md`.

## Standing directive (autonomous)
Drive NIST suites group-by-group to 100% (depth-first: IF→SM→**IC**→SQ→IX→RL→ST), implementing missing COBOL-85 features per the spec. **Grammar changes are pre-authorized for the NIST effort** — log each in DEVLOG, rely on the full guard, commit (see memory `feedback_autonomous_grammar_nist`). Run `scripts/guard.sh` after meaningful changes; never change valid COBOL source to dodge a compiler bug; every commit needs a DEVLOG entry; commit messages end with the `Co-Authored-By: Claude Opus 4.8 (1M context)` trailer. Don't touch global NuGet config.

## Pick up here (any of)
0. **Spec-conformance follow-ups** (`docs/spec-gaps.md`, DEVLOG 228–231): empirical audit done. All three silent-correctness bugs resolved — `FUNCTION WHEN-COMPILED` fixed (228/230), ODO-group `FUNCTION LENGTH` fixed (231), ON SIZE ERROR was never broken. Remaining = (a) low-risk CLEANUP: delete stale "not supported" diagnostics for features that actually work (PERFORM VARYING AFTER, INSPECT CONVERTING, abbreviated conditions, multi-target SET, OCCURS DEPENDING ON, DIVIDE REMAINDER) + dead code (`CobolProgram.cs` arithmetic, `CilEmitter` DISPLAY stub, COBOL0467); (b) genuine gaps: NATIONAL/PIC N (ASCII-backed stub), in-memory SORT/MERGE, Screen I/O placeholders.
1. **Finish IC** (current suite). DEVLOG 233 fixed transitive BY CONTENT → IC224A + IC225A; DEVLOG 234 implemented CANCEL return-to-initial-state (+ dynamic CANCEL) → **IC203A** (guard now 151, IC 19/47). Remaining, all substantial features (no quick wins):
   - **IC228A**: nested-program GLOBAL data visibility. **Crash-robustness DONE (DEVLOG 235)** — the arithmetic binders now report COBOL0415 and skip instead of throwing on an undefined receiving item, so IC228A fails gracefully. **The feature itself is NOT done.** A contained program (IC228A-1) references its container's `01 … IS GLOBAL` item (`GLO-DATA-4`) and the storage must be SHARED at runtime (IC228A-1 `ADD 10 TO GLO-DATA-4`; IC228A then checks =11). Substantial architectural change — the pipeline flattens nested programs and compiles each with an isolated symbol table + its own static `State`. **Validated design** (cross-program `State` access): all program types share one module and the container is fully emitted (incl. its `public static State`) before the nested program, so the contained program can read the container's storage directly. Implement: (a) parent/containment map in `Compilation` (CollectProgramContexts loses it today); (b) register ancestors' GLOBAL symbols (01-record + all subordinates, with their parent-model StorageLocations) into each contained program's data scope + an `InheritedGlobalOwners` map so `ResolveData` finds them; (c) add an optional `OwnerProgramId` to `IrLocation`, set by `ResolveLocation` for inherited globals; (d) in `CilLocationEmitter.EmitLoadBackingArrayOrExternal`, when `OwnerProgramId` is set, load `<owner>::State.WorkingStorage` (look the type up in the shared module) instead of the current program's. Minimal repro: `/e/tmp/probe/GLOB.cob` (expect `GCOUNT=0011`). Watch: the area/offset-based emission threads location info piecemeal, so `OwnerProgramId` must reach `EmitLoadBackingArrayOrExternal`'s callers.
   - **IC233A/IC234A**: `USE GLOBAL AFTER ERROR PROCEDURE ON INPUT` declaratives — USE/file-error grammar gap ("missing token before 'ERROR'").
   - **IC235A / IC227A / IC114A**: EXTERNAL + sequential file I/O — blocked by the file-I/O FILE-CONTROL wall (item 3).
   - **IC401M**: flagging-conformance module (no CCVS report by design) — **exclude** from baselining, like IF401M/402M/403M.
   - ~21 NO_OUTPUT (rc=139) are callee-only subprogram halves — not real tests, exclude.
2. **Collating-sequence subsystem — COMPLETE** (`project_collating_gap`; DEVLOG 224–227). Comparisons, SORT/MERGE/table-sort keys, and FUNCTION CHAR/ORD all honor the program collating sequence. Nothing left except (optional) national CHAR-NATIONAL collating. Continue with IC or the file-I/O wall instead.
3. **File-I/O FILE-CONTROL wall** (`project_fileio_remaining`): SQ/IX/RL are ~144/162 COMPILE_FAIL on CCVS-specific FILE-CONTROL forms; producer/consumer orchestration + SORT-USING hang already fixed.

## Tooling
- Build: `dotnet build src/CobolSharp.CLI/CobolSharp.CLI.csproj`
- Survey a suite: `bash scripts/run-suite.sh <PREFIX>` (NC/IF/SM/IC/SQ/IX/RL/ST) → CLEAN | N FAIL* | COMPILE_FAIL | NO_OUTPUT | RUNTIME
- NIST run convention: `export COBOL_SWITCH_1=ON`. Capture guard output to a file + `echo "guard exit=$?"` (piping to grep masks the exit code).
- Preprocess (debug COPY/REPLACE, copylib auto-resolved): `cobolsharp preprocess <file> -o <out>`
