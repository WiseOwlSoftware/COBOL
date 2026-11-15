## Entry 3671 — 2026-11-15 02:52 PST — The third 2002 read report files 10 defect notes and extends the adjudication

**What was read.** The third pass of the 2002 read found 41 defect cases (the files are under `E:/COBOL-coord/scratch/op-20261114/read-2002/out`): 10 new groups, 6 extensions of open notes, 1 owner-decision facility, 3 conditional items and 3 dropped groups. Only `kb/Work/` and this entry change.

**New notes (every citation re-checked with `scripts/spec/cite.py --check`).**

- PB5639: an indexed READ PREVIOUS immediately after OPEN returns the last record with status 00 at 2002 and 2014, where 2002 requires at end with status 10. The goldens `pb344_read_previous_indexed_2002.out` and `_2014.out` pin the wrong answer and change with the fix; PB344 rested on §2023 Annex E.2 item 22 alone.
- PB5640: INVOKE and CALL accept a bit argument reference-modified with a data-item length
- PB5641: a table SORT of a nested table with a subscript omitted is accepted
- PB5642: a floating-point literal is left out of the composite of operands of ADD and the like
- PB5643: an overflowing report SUM addition raises EC-REPORT-SUM-SIZE at the addition
- PB5644: SUM ROUNDED in an entry with no COLUMN clause is refused COBOL2143
- PB5645: REWRITE and WRITE FROM a user-defined function is refused COBOL1914
- PB5646: INSPECT of a national group with an alphanumeric literal operand
- PB5647: a PICTURE SYMBOL character that 2002 permits is refused COBOL0891
- PB5648: ARITHMETIC IS STANDARD on BINARY-LONG and FLOAT-LONG operands

**Extended (rows and a dated file beside each).** PB5602 (3 rows), PB5598 (3), PB5578 (2, including the owner-decision row), PB5612 (1; the same fatal disposition stops `>>PROPAGATE ON` for a SORT), PB5575 (8), PB4950 (1; further 2002 diagnostics that cite 2023 rules).

**Adjudication PB5625 extended.** Three more open readings, each in `kb/Work/PB5625/2026-11-15-read-2002-c.md`: SET index-name outside the table range (§2002:14.8.35.3 against the informative §2002:F.2), the class of a USAGE NATIONAL numeric item (six more rows), and LOWER-CASE and UPPER-CASE with no locale (Annex D). Ten rows were added to its `inventory_rows`.

**Dropped.** Read cases matched only because they carry a defect key with an empty field, refuter cases upheld with the compiler outcome right, and the 25 communication-facility rows (all different because 2023 has no communication facility, none compiler-wrong).

