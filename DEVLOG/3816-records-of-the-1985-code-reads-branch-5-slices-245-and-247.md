## Entry 3816 — 2026-11-16 22:43 PST — Records of the 1985 code reads, branch 5: slices 245 and 247 go on record, kb/Work PB5891 is filed, six rulings are applied

A records branch: no compiler or runtime source changed.

- **kb/Work PB5891** (open, MINOR, rejects legal source, every edition) is filed from the rulings of batch 20: an integer argument that always yields an integer by arithmetic (`N * (N + 1) / 2`) is refused COBOL1627. The note carries the rulings section unchanged, the refuter's program `fn_edge.cob` in full and its runs at `--std 85` and `--std 2023` on a build of this tree (exit 65, the same four diagnostics), and the Sites block. It claims the four rows `1989-AR-A.2.9.3-1`, `1989-AR-A.2.14.3-1`, `1989-AR-A.2.28.3-1` and `1989-AR-A.2.35.3-1`. The rulings file is copied unchanged to `kb/Work/PB5683/2026-11-16-rulings-code-reads-batch-20.md`.
- **Imports** (each `--replace`, dry run on a store copy first, none refused): slice 1985-245 at pin 6450516b5e: 6 reads, 5 confirmed, 0 contradicted, 1 undecided. Slice 1985-247 at pin cdc9601608: 5 reads, 0 confirmed, 0 contradicted, 5 undecided. Slice 1985-246 is SUPERSEDED (its one row was read again in 247) and is not imported.
- **Rulings** (`rulings-map-b19-b20.json`, 6 rows, all applied): `1985-SR-IX.4.5.3-2` and `1985-SR-XIII.4.9.3-4` adjudicated CONFORMS (the doubts are rejected); the four intrinsic rows recorded contradicted with work PB5891.
- **Test comment** (the review lead R1-1 of PB5890): in `DeclarativesStructuralRulesTests.cs` the summary of `ReportProgram` is back above `ReportProgram`.
- **Summary before and after**: 1985 0 GAP at both. Code reads, before: 1343 confirmed · 11 contradicted · 0 undecided · 0 stale · 1 KEPT owed; after: 1350 confirmed · 5 contradicted · 0 undecided · 0 stale · 0 KEPT owed. `plan --edition 1985` after: 0 KEPT owed a read, 0 owed a re-read.
- No 2002 verdict was recorded.

