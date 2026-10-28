## PB4010: the fuzz ledger tells a compiler bug by where it lives, counts a run that stopped early, and stays in sync with its Artifact

The round-2 review of fuzz-summary (PB4006) left two low leads. This change files them as PB4010 and fixes both, at the owner's
request, in the same landing; the owner added a third requirement during the fix.

R2-1. `finding_kind` called a defect a harness bug only when its area started with `tooling` or `docs`, but the register also
files harness defects under `tests/`, `process/`, `orchestrator/`, `scripts/` and `ci/`. The review suggested `process_only`.
An audit of the 29 fuzz notes showed that is wrong too: PB3569, PB3831 and PB3832 are compiler defects (a misleading diagnostic
changes no program's result) with `process_only: true`. The location of the defect decides it, so `site_kind` reads the
note's declared sites through `work.note_sites`, and a path or glob under `src/` is a compiler bug. The round-1 review of this
branch found two holes: a block naming only members declared no path yet passed, and the area fallback for site-less notes was
an incomplete list. Both are closed. The 16 site-less fuzz notes, whose landings predate the src/ rename, are now a frozen record
(`LEGACY_KINDS`). Any other defect that nothing classifies stops the render and is refused by `fuzz_triage.py history`, by name.
`fuzz_triage` lost its copy of the register import.

R2-2. `defects_of` counted only notes naming a recorded night, so PB3538 (a wrong-answer chained-division COMPUTE from the run
of 2026-10-22 that stopped early) was missing. `unrecorded_runs` now counts such notes and lists them as "Run of <date>". The
ledger moves from 23 to 24 compiler bugs found and from 21 to 22 fixed.

Sync. The owner: "The fuzz ledger repo file must be kept in sync with the artifact." One writer now draws the HTML page and
FUZZ-LEDGER.md together, and both carry one ledger version, a digest of the content alone. The Artifact is published from main's
file only: `fuzz_history.py mark-published` refuses a page whose version is not FUZZ-LEDGER.md's on origin/main and records the
version it published. `fuzz_history.py status` is a new session-probe line that reports current, PUBLISH OWED or REGENERATE OWED
(a fuzz note's status moved since main's file was drawn). `render --markdown` is gone, so one rendering cannot be written
without the other. The round-2 review closed the gaps: the stamp no longer carries a build time, so a render that changes
nothing leaves the tracked file byte-identical and `status` compares main's file with a fresh render byte for byte (a hand edit
shows). `status` also requires the record to name the history's Artifact, and checks drift only in a checkout at origin/main
(one behind main would report drift main does not have). `history` now draws the whole page before writing anything, so a note
it cannot show writes nothing.

The self-test gains the classification, refusal and SYNC cases. DESIGN-differential-fuzz.md section 6 and the fuzz-triage workflow's
step list state the new order: render, land, publish, mark.
