# PB5683 - landed: nothing of 1985 is owed (wave 20262132 group h, 2026-11-21)

Measured on branch `train85e-e3-cat` (cut from train D at 0ffdede4cc):

- **What PB5692 requires** (its adjudication file, "What the sample decides" item 1): every KEPT row of 1985 needs a
  verdict adjudicated at 1985, a code read at `--std 85` with a refuter.
- `python scripts/spec/code_reads.py plan --edition 1985`: `plan 1985: 0 KEPT row(s) owed a code read` and
  `0 row(s) owed a RE-READ (import --replace): 0 whose fix has landed since the read, 0 whose code has moved since the read`.
- `python scripts/spec/edition_status.py --edition 1985`, part 2: `1357 code read(s) stored`, `0 owed a read (a stale
  record, 9, counts as no read) · 0 owed a re-read · 0 contradicted · 0 undecided`; part 1: `0 of 3109 rule rows in state
  GAP`. The last owed read this note gained (`1989-RV-A.2.46.3-3`, the sub-note of the same day) went in on train D
  (`28e9850f85`, slice 1985-c03, CONFORMS, UPHELD).
- **The 14 rules with no record:** none is left. The 3109 rows of the 1985 inventory by record: KEPT 1365, CHANGED 556,
  REMOVED 319, an own verdict 869 (CONFORMS 868, DOCUMENTED-NON-SUPPORT 1); no row has neither a record nor a verdict.
- **What stays elsewhere:** how a read that confirms a KEPT row becomes the row's `adjudicated` verdict is the switch's
  design, PB5682 (blocked), as this note's body says; it is not 1985 work owed here. The defects the reads found are
  notes of their own (PB5731 to PB5738 and the code-read rulings' notes).

So every 1985 rule PB5692's answer requires is adjudicated in 1985's own terms, and the note lands.
