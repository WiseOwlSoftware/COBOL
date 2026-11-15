**2026-11-15, the golden-2002-3 integrator: the same disposition for EC-SORT-MERGE-ACTIVE with no declarative. Rows added to this note's `inventory_rows`.**

Rows added: `2002-GR-14.8.36.3-9`.

The writer of g2002-14-8-36-sort found it, and the integrator re-ran it on a build of main 63ab0c4ece. The repro
(`E:/COBOL-coord/scratch/gl2002/integrate-3/repro/sort/g9.cob`, copied from `gl2002/lane1/run/g2002-14-8-36-sort/gr9/`)
starts with `>>TURN EC-SORT-MERGE-ACTIVE CHECKING ON`. Its outer SORT's input procedure RELEASEs a record, displays
BEFORE-INNER and then runs a second SORT. No USE procedure applies.

- Command: `cobol.exe g9.cob --std 2002 --run`.
- Observed: `BEFORE-INNER`, `AFTER-INNER`, `O=B`, `AFTER-OUTER`, rc 0. The inner SORT is skipped and the run unit continues.
- Expected: `BEFORE-INNER`, then abnormal termination at the inner SORT.

The rules (each checked with `python scripts/spec/cite.py --check`, verdict OK):

- `§2002:14.8.36.3 GR9`: "If the range of the input procedure causes the execution of any MERGE, RETURN, or format 1 SORT statements". The rest of GR9 sets EC-SORT-MERGE-ACTIVE and makes "the results of the execution of the SORT statement" undefined.
- EC-SORT-MERGE-ACTIVE is `Fatal` in the 2002 exception-name table.
- `§2002:14.5.12.1.2`: "If checking for the exception condition is enabled, execution of the run unit is terminated abnormally".

The mechanism is this note's: `CobolSort.RaisedByTheStatement` marks the raise `RaisedBySortMerge`, and the EC guard disposes of it under the
2023-only `§2023:14.6.13.1.3 GR2` at every edition. A doubt for the fixer: GR9 makes the SORT's results undefined.
That leaves the sorted records free, but not the fatal procedure, which 2002 states for every fatal condition. The row
is recorded DIVERGES naming this note.
