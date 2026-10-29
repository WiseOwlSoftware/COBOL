## Entry 2726 — 2026-10-29 11:44 PDT — PB3838 tier 6 review leads: notes quote compiler output verbatim, and a duplicate GnuCOBOL record still counts

The branch review of the tier-6 triage (Entry 2725) was CLEAN with five low leads, all fixed here.

- **R1-4 and R1-5, the notes.** The script that wrote each note's `## Upstream state` cut the matchers' summaries at
  420 characters and collapsed whitespace in every quotation. Four tracker bullets lost their last clause, which was what
  our record adds to the ticket. The quoted compiler output lost its spacing, so PB3899's fixed 3.3-dev output read
  `[ 10004]` where the program prints `[  10004]`. Two notes also repeated their labels (`3.2.0: 3.2.0:`). All 78 notes
  were regenerated from main: the summaries are whole, and the 3.2.0 and 3.3-dev outputs are their own list items,
  quoted exactly as run.
- **R1-2.** PB3913 still said "no ticket is known" after the triage found feature-requests #303.
- **R1-3, a count.** Entry 2725 gave the tracker split as 21 reported, 28 related only and 28 none, which sums to 77.
  The notes give 21 reported, 29 related only (PB3899, since fixed upstream, among them) and 28 none. PB3838 now says so.
- **R1-1, the ledger rule.** `work.py` retires an upstream record only when the matter has ended upstream: fixed,
  duplicate or declined. Entry 2725's rule counted only the fixed ones, so a duplicate (a real bug, already reported)
  would also have vanished from "bugs recorded in GnuCOBOL". Duplicates now count, and declined is the one ending that
  does not. The self-test pins all three, each a state `work.py check` accepts: fixed-upstream (PB9013) and duplicate
  (PB9015) count, declined (PB9014) does not. The ledger renders unchanged (786bf648b722).

