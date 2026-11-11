**Addendum (2026-11-11): 1 more row from the 1985 code read, slice 46, at the same site.** `FileControlBinder.ScreenSameClauses` enforces SR5 c) only for a file-area clause (the 2023 SR10 wording), but the 1985 SAME clause it names is `SAME [ RECORD ] AREA` (§1985:VII.2.13.2). kb/Work PB5094 still refuses a 1985 rule id in `inventory_rows`, so the row is listed here. This note claims it, and it belongs among the 1985 rows of `SameOccurrenceRule` that this note asks for. Citations checked with `scripts/spec/cite.py --check`.

- `1985-SR-XI.2.5.3-5` - `§1985:XI.2.5.3 SR5` (read: PARTIAL, harm: under-rejects; `E:/COBOL-coord/scratch/coderead-85/out-pipe/slice-46.json`): c) "all of the files named in that SAME clause must be named in that SAME SORT AREA or SAME SORT-MERGE AREA clause(s)".

**Rerun on the build of origin/main f2cf36fd15, 2026-11-11.** `out-pipe/work-slice-46/c10/c_recarea.cob` (`SAME RECORD AREA FOR F1 F2` with `SAME SORT AREA FOR S1 F1`) compiles and prints OK at --std 85. Expected: refused, because F2 is not named in the sort clause.

**From slice 48.** The refuter of `1985-FMT-XIII.2.4.2` found this note's report-file case (SAME SORT AREA naming a report file is refused COBOL2738 at 85). It held that verdict DOUBTFUL for that reason (`coderead-85/held-4.json`); the verdict is recorded after this note lands.
