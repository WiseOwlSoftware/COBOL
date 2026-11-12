## Entry 3528 — 2026-11-12 10:49 PST — PB4996: the 139 untouched 1985 rows accounted for; 37 non-conforming verdicts recorded (1985 GAP 443, unchanged)

On origin/main 447ae77637, 139 of the 443 open 1985 rows had a test and no verdict. Two new scripts in `E:/COBOL-coord/scratch/coderead-85` (`records_85e_gather.py`, `records_85e_table.py`) gather every read, refute and held-85 adjudication of those rows and build one table, `records-85e-table.json` and `.md`, with the latest verdict, its refuter, the held decision and the kb/Work notes that name each row.

The 32 CONFORMS reads that no held file names were all OVERTURNED by their refuter. The merge scripts send those to the register, not to a held file, which is why they were never recorded. In 26 of the other 27 CONFORMS reads, the held-85 adjudication decided a non-conforming verdict. The 27th, `1985-SR-VI.6.12.3-3`, stays DOUBTFUL until PB5308 lands. No CONFORMS met the recording rule.

`record_verdicts.py --edition 1985` recorded 37 non-conforming verdicts, 32 PARTIAL and 5 DIVERGES, each with its code-location and an open defect note. It reported "rows changed : 37" and "GAP : 443 → 443". The 1985 store now holds 598 adjudicated rows. The other 102 rows are listed in `kb/Work/PB4996/2026-11-12-records-85e.md`:
- 51 had a fix land after their read.
- 32 are the overturned reads.
- 15 were never read: slices 43 to 49 skipped them.
- 2 wait on a decision.
- 1 is DOUBTFUL.
- `1985-GR-IX.4.5.4-18` is adjudicated DIVERGES, but its note PB5238 is retired.

