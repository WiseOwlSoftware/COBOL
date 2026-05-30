# CobolSharp — Session Resume Prompt (2026-05-29)

Paste this to start a new session. It orients you fast; the linked docs have the detail.

## Read first
- **CLAUDE.md** (project root) → which points to **PROMPT.md** (non-negotiable doctrine), **PROJECT_PLAN.md** (status), **DEVLOG.md** (decision narrative).
- **claude/state/session-state-2026-05-29.md** — the most detailed resume context (sections 0–0c cover the latest work).
- Memory index is loaded automatically; key entries: `project_nist_progress`, `project_fileio_remaining`, `project_collating_gap`, `reference_nist_xcards`.
- **specs/ISO_COBOL.md** is the authoritative COBOL spec (submodule — `git submodule update --init --recursive` if absent). Implement from the spec, not from assumptions.

## Current state (branch `main`, all committed, guard GREEN)
- **Guard** (`bash scripts/guard.sh`): 1000 unit / 341 integration (340 pass + 1 unrelated skip) / **149 NIST baselines (95 NC + 42 IF + 12 SM)**. Must stay ALL GREEN; baselines must stay 0 FAIL*. (Verified green at HEAD `7591dae`.)
- **Suites:** NC 100%, **IF 100%** (42 baselined), **SM COPY-feature 100%** (12 baselined). **IC in progress: 16/47 CLEAN.** SQ/IX/RL/ST surveyed but paused.
- Spec-audit follow-ups done (DEVLOG 221–223): multi-dim `table(ALL)`, AT-END-vs-I/O-error split, runtime `FUNCTION LENGTH` for ref-mod.
- **Collating (DEVLOG 224–225):** SORT/MERGE/table-sort key collating DONE (`0a7caae`) + numeric sort-key misclassification fixed (`8900437`). **Gap 2 (FUNCTION CHAR/ORD under a PCS) was attempted and reverted** — it's blocked by a deeper bug (a STANDARD-2 program collating sequence changes CCVS `… NOT EQUAL TO SPACE` report output; 8 NIST regressions). See `project_collating_gap` for the blocker + reflog recovery refs.

## Standing directive (autonomous)
Drive NIST suites group-by-group to 100% (depth-first: IF→SM→**IC**→SQ→IX→RL→ST), implementing missing COBOL-85 features per the spec. **Grammar changes are pre-authorized for the NIST effort** — log each in DEVLOG, rely on the full guard, commit (see memory `feedback_autonomous_grammar_nist`). Run `scripts/guard.sh` after meaningful changes; never change valid COBOL source to dodge a compiler bug; every commit needs a DEVLOG entry; commit messages end with the `Co-Authored-By: Claude Opus 4.8 (1M context)` trailer. Don't touch global NuGet config.

## Pick up here (any of)
1. **Finish IC** (current suite): 5 FAIL* (IC203A; IC224A's `BY CONTENT`/level-2; IC225A; IC227A; IC114A) + 5 COMPILE_FAIL (nested-program GLOBAL visibility, duplicate names across nested programs). ~21 NO_OUTPUT are callee-only files (not real tests — exclude from baselining).
2. **Collating-sequence subsystem — Gap 2 only, BLOCKED** (`project_collating_gap`): comparisons + SORT/MERGE keys already honor the program collating sequence (done; on `main`). The remaining piece, FUNCTION CHAR/ORD under a PCS, is blocked: making CHAR/ORD honor the table requires correct `STANDARD-2` alphabet handling, which then changes ordinary alphanumeric-vs-`SPACE` comparisons in CCVS boilerplate and regresses 8 NIST tests (NC114M, NC214M, IF105A, IF119A, IF123A, IF127A–129A). Fix the figurative-`SPACE`-vs-PCS comparison/field-init semantics FIRST, then re-apply CHAR/ORD (reverted work in reflog: `ae3db2d`/`fcaab53`; turn-key design in `docs/collating-gap2-turnkey.md`).
3. **File-I/O FILE-CONTROL wall** (`project_fileio_remaining`): SQ/IX/RL are ~144/162 COMPILE_FAIL on CCVS-specific FILE-CONTROL forms; producer/consumer orchestration + SORT-USING hang already fixed.

## Tooling
- Build: `dotnet build src/CobolSharp.CLI/CobolSharp.CLI.csproj`
- Survey a suite: `bash scripts/run-suite.sh <PREFIX>` (NC/IF/SM/IC/SQ/IX/RL/ST) → CLEAN | N FAIL* | COMPILE_FAIL | NO_OUTPUT | RUNTIME
- NIST run convention: `export COBOL_SWITCH_1=ON`. Capture guard output to a file + `echo "guard exit=$?"` (piping to grep masks the exit code).
- Preprocess (debug COPY/REPLACE, copylib auto-resolved): `cobolsharp preprocess <file> -o <out>`
