## Entry 2634 — 2026-10-26 16:33 PDT — #664's two review leads: PB3489's Landing section restored, PB3853 filed; PB3852's deadline becomes a requirement

The branch review of w20261707b (PB3831, PB3832; #664) left two LOW leads for the operator. R4-1: the merge with main
kept main's stale `#ResidueWalk` and `ResidueWalkNestingTests.cs` bullets inside kb/Work/PB3489.md's Landing section,
between the branch's own bullets, undoing the earlier R1-4 fix (the work-note union merge PB3850 describes). The section
now equals the reviewed branch's (b2abdd01c) with the product's paths; neither retired name exists in `src/` or
`tests/`. R4-2: GeneralFormatViolationTests' class comment lists PBGF15 among rows that draw the bare parse code, while
its row pins COBOL2269 since PB3830. It is filed as PB3853, blocked by PB906 and PB3569, because both change that test
class in the fuzz-deadline campaign and an edit beside them would only make a conflict.

PB3852 records the owner's ~16:20 word that the deadline is a requirement: every member lands before the night's
fuzz run, and the run starts only then.

