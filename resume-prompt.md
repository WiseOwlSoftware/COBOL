# CobolSharp — Session Resume Prompt (2026-05-29)

Paste this to start a new session. It orients you fast; the linked docs have the detail.

## Read first
- **CLAUDE.md** (project root) → which points to **PROMPT.md** (non-negotiable doctrine), **PROJECT_PLAN.md** (status), **DEVLOG.md** (decision narrative).
- **claude/state/session-state-2026-05-29.md** — the most detailed resume context (sections 0–0c cover the latest work).
- Memory index is loaded automatically; key entries: `project_nist_progress`, `project_fileio_remaining`, `project_collating_gap`, `reference_nist_xcards`.
- **specs/ISO_COBOL.md** is the authoritative COBOL spec (submodule — `git submodule update --init --recursive` if absent). Implement from the spec, not from assumptions.

## Current state (branch `main`, all committed, guard GREEN)
- **Guard** (`bash scripts/guard.sh`): 1000 unit / 348 integration (347 pass + 1 unrelated skip) / **181 NIST baselines (94 NC + 42 IF + 12 SM + 4 IC + 23 SQ + 5 RL + 1 IX)**. Must stay ALL GREEN; baselines must stay 0 FAIL*. (NC214M was dropped — live ACCEPT FROM DATE/DAY/TIME, inherently non-deterministic; DEVLOG 226.)
- **Suites:** NC 100%, **IF 100%** (42 baselined), **SM COPY-feature 100%** (12 baselined). **IC: 20/47** (IC203A/224A/225A/228A; DEVLOG 233–236). **File-I/O wall broken (DEVLOG 237–240):** the FILE-CONTROL/READ/USE grammar + FILE STATUS/REWRITE checks were over-strict vs ISO — fixed. **SQ 2→75 compiling, 23 baselined; RL 5 baselined; IX 1 baselined.**
- Spec-audit follow-ups done (DEVLOG 221–223): multi-dim `table(ALL)`, AT-END-vs-I/O-error split, runtime `FUNCTION LENGTH` for ref-mod.
- **Collating subsystem COMPLETE (DEVLOG 224–227):** comparisons, SORT/MERGE/table-sort keys, and FUNCTION CHAR/ORD all honor the alphanumeric program collating sequence. The STANDARD-2 all-255-table bug (which made `"ABCD" = SPACE` vacuously true) is fixed; identity sequences normalize to null. 8 contaminated NIST baselines corrected; NC214M dropped (non-deterministic). See `project_collating_gap` + `docs/collating-baseline-finding.md`.

## Standing directive (autonomous)
Drive NIST suites group-by-group to 100% (depth-first: IF→SM→**IC**→SQ→IX→RL→ST), implementing missing COBOL-85 features per the spec. **Grammar changes are pre-authorized for the NIST effort** — log each in DEVLOG, rely on the full guard, commit (see memory `feedback_autonomous_grammar_nist`). Run `scripts/guard.sh` after meaningful changes; never change valid COBOL source to dodge a compiler bug; every commit needs a DEVLOG entry; commit messages end with the `Co-Authored-By: Claude Opus 4.8 (1M context)` trailer. Don't touch global NuGet config.

## Pick up here (any of)
0. **Spec-conformance follow-ups** (`docs/spec-gaps.md`, DEVLOG 228–231): empirical audit done. All three silent-correctness bugs resolved — `FUNCTION WHEN-COMPILED` fixed (228/230), ODO-group `FUNCTION LENGTH` fixed (231), ON SIZE ERROR was never broken. Remaining = (a) low-risk CLEANUP: delete stale "not supported" diagnostics for features that actually work (PERFORM VARYING AFTER, INSPECT CONVERTING, abbreviated conditions, multi-target SET, OCCURS DEPENDING ON, DIVIDE REMAINDER) + dead code (`CobolProgram.cs` arithmetic, `CilEmitter` DISPLAY stub, COBOL0467); (b) genuine gaps: NATIONAL/PIC N (ASCII-backed stub), in-memory SORT/MERGE, Screen I/O placeholders.
1. **Finish IC** (current suite). DEVLOG 233 fixed transitive BY CONTENT → IC224A+IC225A; 234 CANCEL return-to-initial-state (+ dynamic CANCEL) → IC203A; 235 arithmetic-binder crash hardening (COBOL0415); 236 nested-program GLOBAL data visibility with shared storage → **IC228A** (guard now 152, IC 20/47). Remaining:
   - **IC is at its non-file-I/O ceiling (20/47).** Every remaining actionable IC test is **file-I/O-blocked** (the paused FILE-CONTROL wall, item 3): IC233A/IC234A (`SELECT OPTIONAL TEST-FILE` + `OPEN INPUT`/`READ`, with a `USE…ERROR` declarative that must execute on a file error — note its USE form also omits the spec-required `STANDARD` keyword, ISO §14.9.49.2, so accepting it would be a documented non-ISO leniency, NOT a spec fix), IC235A/IC227A/IC114A (EXTERNAL + sequential file I/O). IC401M is a flagging module (excluded). So advancing IC further requires the file-I/O subsystem — which would also unblock SQ/IX/RL/ST. **Next big effort = the file-I/O FILE-CONTROL wall (item 3).**
   - **GLOBAL follow-ups** (DEVLOG 236, deferred — not blocking any current IC test): subscripted/ref-modded inherited globals (extend `OwnerProgramId` through the element/ref-mod address paths), level-88 condition names under a global group, GLOBAL items in the FILE SECTION.
   - **IC233A/IC234A**: `USE GLOBAL AFTER ERROR PROCEDURE ON INPUT` declaratives — USE/file-error grammar gap ("missing token before 'ERROR'").
   - **IC235A / IC227A / IC114A**: EXTERNAL + sequential file I/O — blocked by the file-I/O FILE-CONTROL wall (item 3).
   - **IC401M**: flagging-conformance module (no CCVS report by design) — **exclude** from baselining, like IF401M/402M/403M.
   - ~21 NO_OUTPUT (rc=139) are callee-only subprogram halves — not real tests, exclude.
2. **Collating-sequence subsystem — COMPLETE** (`project_collating_gap`; DEVLOG 224–227). Comparisons, SORT/MERGE/table-sort keys, and FUNCTION CHAR/ORD all honor the program collating sequence. Nothing left except (optional) national CHAR-NATIONAL collating. Continue with IC or the file-I/O wall instead.
3. **File-I/O FILE-CONTROL wall — IN PROGRESS** (`project_fileio_remaining`; DEVLOG 237–240). The CCVS FILE-CONTROL forms turned out to be spec-CONFORMANT (the grammar was over-strict): clauses order-free (§12.4.5.2), `[ORGANIZATION IS]`/`[FILE]`/`[IS]`/`[AT]`/`[STANDARD]`/`[ON]` optional, 2-char-group FILE STATUS valid (§12.4.5.8.3), qualified FILE STATUS name, REWRITE valid on sequential (§14.9.35). **SQ 2→75 compiling, 23 baselined; RL 5; IX 1.** Remaining:
   - **SQ:** 10 COMPILE_FAIL (parse forms — LINAGE-COUNTER special register, FD `RECORD … CHARACTERS`, `RECORD DELIMITER` clause, +2); 18 FAIL* + 3 RUNTIME (sequential runtime correctness tail); 31 NO_OUTPUT.
   - **IX/RL:** 37 COMPILE_FAIL on indexed/relative-specific PARSE forms — dominant: INVALID KEY phrase placement (×8), `START … KEY IS EQUAL` relational (×6), RECORD/ALTERNATE KEY data-name forms. Then the indexed/relative runtime FAIL* tail.
   - **ST (sort/merge):** not re-surveyed under the new grammar; 8 SORT/MERGE COMPILE_FAIL + ST132A hang were noted earlier.

## Tooling
- Build: `dotnet build src/CobolSharp.CLI/CobolSharp.CLI.csproj`
- Survey a suite: `bash scripts/run-suite.sh <PREFIX>` (NC/IF/SM/IC/SQ/IX/RL/ST) → CLEAN | N FAIL* | COMPILE_FAIL | NO_OUTPUT | RUNTIME
- NIST run convention: `export COBOL_SWITCH_1=ON`. Capture guard output to a file + `echo "guard exit=$?"` (piping to grep masks the exit code).
- Preprocess (debug COPY/REPLACE, copylib auto-resolved): `cobolsharp preprocess <file> -o <out>`
