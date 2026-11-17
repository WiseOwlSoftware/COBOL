## Entry 3818 — 2026-11-17 02:32 PST — Records of the 1985 code reads, branch 6: slices 248 and 249 go on record, kb/Work PB5892 and PB5893 are filed, the two MEAN doubts are rejected

A records branch: no compiler or runtime logic changed (one doc comment moved).

- **Imports** (`--replace`, dry run on a store copy first, none refused): slices 1985-248 and 1985-249 at pin d77571151b: 8 reads, 6 confirmed, 0 contradicted, 2 undecided (the two MEAN rows, each with a refuter doubt).
- **Rulings** (`rulings-map-b21.json`, 2 rows, both applied): `1989-RV-A.2.24.4-1` and `1989-RV-A.2.24.4-2` adjudicated CONFORMS. The doubt (a MEAN with an approximate argument is evaluated whole in binary64) is rejected: `§1985:IV.4.3.4` leaves the technique of evaluating numeric functions to the implementor, and the register entry `DOC-A.1-92b` indicates it. That entry gains one sentence naming `§1985:IV.4.3.4` as the 1985 basis of the same determination. The rulings file is copied unchanged to `kb/Work/PB5683/2026-11-17-rulings-code-reads-batch-21.md`.
- **kb/Work PB5892** (open, MINOR, rejects legal source): `FUNCTION CONCAT ("A" U)` over `U PIC 99P` is refused COBOL1627 at `--std 2023`, because the test asks the written scale rather than the value. The note carries the ruling's bullet, `concat_p.cob` in full, its run on a build of this tree and the Sites.
- **kb/Work PB5893** (open, MAJOR, wrong answer): an argument at a numeric position of a function under a COMP-1 or COMP-2 receiver is evaluated in binary64 although every operand is fixed-point (`REM ((Y * 100 - X * 10.25) * 4, 1000)` stores 672 into a COMP-2 item and 41 into a fixed-point one). The note carries the bullet, `n2.cob` in full, its run and the Sites.
- **kb/Work PB5889** (frontmatter only, for its owner): the title narrows to the forms that are integers only by a function's meaning, PB5891 joins `related`, and a dated file says what PB5891 admitted and what is left.
- **Doc comment** (the PB5891 round-6 lead R6-1): in `IntrinsicRenderer.cs` the summary and remarks of `RawArg` are back above `RawArg`, after `ReceiverFor` and its summary. Comment lines only; the solution builds with 0 warnings and 0 errors.
- **Summary before and after**: 1985 0 GAP at both. Code reads, before: 1350 confirmed, 5 contradicted, 0 undecided, 0 stale, 0 KEPT owed; after: 1354 confirmed, 1 contradicted, 0 undecided, 0 stale, 0 KEPT owed. `plan --edition 1985` after: no read owed a re-read.
- No 2002 verdict was recorded.

---

