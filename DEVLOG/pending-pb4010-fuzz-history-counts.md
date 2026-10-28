## PB4010: the fuzz ledger tells a compiler bug by where it lives, and counts a run that stopped early

The round-2 review of fuzz-summary (PB4006) left two low leads. This change files them as PB4010 and fixes both, at the owner's
request, in the same landing.

R2-1. `finding_kind` called a defect a harness bug only when its area started with `tooling` or `docs`. The register also files
harness defects under `tests/`, `process/`, `orchestrator/`, `scripts/` and `ci/`. The review suggested `process_only` as the
test, but an audit of the 29 fuzz notes showed that is wrong too: PB3569, PB3831 and PB3832 are compiler defects (a misleading
diagnostic changes no program's result) with `process_only: true`. Where the defect lives is what decides it, so `finding_kind`
now reads the note's declared sites through `work.note_sites`: a site under `src/` is a compiler bug. 16 of the landed fuzz notes
predate site blocks, and their sites cannot be backfilled honestly (their landings predate the rename and other merges name
them too). Those are read by area against every non-product area the register uses, and `fuzz_triage.py history` now refuses a
new defect note with no site block, so that fallback covers a closed set. All 16 classify as before.

R2-2. `defects_of` counted only notes naming a recorded night, so PB3538 (a wrong-answer chained-division COMPUTE from the run
of 2026-10-22 that stopped early) was missing. `unrecorded_runs` now counts such notes and lists them as "Run of <date>". The
ledger moves from 23 to 24 compiler bugs found and from 21 to 22 fixed. The per-night chart still holds nights only.

The self-test gains five cases. DESIGN-differential-fuzz.md §6 states the rule, FUZZ-LEDGER.md is regenerated, and the summary
Artifact is republished.
