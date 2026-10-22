## Entry 2340 — 2026-10-22 00:08 PDT — PB3508: a successor branch no longer takes the notes its predecessor closed

Wave 1143 planned 0 groups. PB3489's branch had one blocking review finding, so it needed a review finisher. The planner
excluded that finisher as `awaiting landing` with fix-pb3504, the same-file successor that merged PB3489's head. The
branch survey held fix-pb3504 for those same unanswered findings, so each side waited for the other.

The cause is in `plan_wave.add_closed_notes` (kb/Work PB3473). It credits each unlanded report with every note its
branch flipped to a terminal status since it left main. The successor's diff from main includes the predecessor's flip,
and the successor's newer DONE report superseded the predecessor's FINDINGS report. `add_closed_notes` now gives no
report a note that a contained unlanded report branch also closes. This is the carrier rule `plan()` already states.

Witness: `test_plan_wave.py` check 6b'''00 (PB3508), 329/329. A live dry run lists no `awaiting landing`, and PB3489
waits only on the survey's `--min-age`.

