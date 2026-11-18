**2026-11-17: the integration of the eight sweep slices (branch pb4950-sweep); the 1985 direction is done and the net is a plain rule.**

- **Merged.** Slices 2 to 8 (`origin/pb4950-s2` .. `-s8`) merged into slice 1's branch one at a time. The ratchet json was never
  text-merged: it took this branch's side at each merge and was then measured from the net itself, by emptying it and running the
  whole Conformance assembly. After all eight slices, 13 tests failed, 8 of them the net: 5 codes, 8 lines (COBOL0897 1,
  COBOL1747 2, COBOL2199 1, COBOL2688 1, COBOL2912 3). The ratchet started the sweep at 298 codes and 1638 lines.
- **The parallel slices had each built their own parser edition read** (`EditionCitation`, `CiteByEdition`, `DiagnosticCitation`
  with a second `CiteAt`, `ClosedFormatRefusal` twice, `ExtensionReservationRule`) beside slice 4's `EditionInForce`. They are one
  member now, `CobolParserCoreBase.EditionInForce`, recorded in `EditionReadDriftTests` at each strategy method that reads it.
  `StringUnstringBinder` keeps slice 5's `Sr(unstring, rule2023)` as the one numbering map (slice 2's `StringAllRule` went).
- **The last five codes, each from the 1985 text:**
  - COBOL1747 (a level-77 entry with no data-name): `§1985:VI.5.2.1` and `§1985:X.4.1.1`, "The following data clauses are
    required in each data description entry: 1. level-number 77 2. data-name ...".
  - COBOL2199, the OR PAGE heading arm: OR PAGE is a COBOL-2002 phrase (COBOL0900 gates it at 85); the arm now says so with the
    edition gate's words (`NotInCobol85("the OR PAGE phrase")`).
  - COBOL2688 (a pseudo-text delimiter split across a continuation): `§1985:IV.7.2.2`, "Both characters composing the separator
    '==' must be on the same line."
  - COBOL2912 (PADDING CHARACTER or RECORD DELIMITER on a relative or indexed file): the 1985 row `1985-FMT-VII.2.3.2-SEQUENTIAL-CLAUSE`,
    the sequential entry's general format, which alone prints those clauses. A 1985 row now replaces only the 2023 rows of its id
    that report under the SAME diagnostic (`FileControlKeyRule.Replaces`), so SR11's sort-merge arm (COBOL1900) keeps its row; this
    also stops slice 2's RECORD KEY / RELATIVE KEY format rows from dropping the SR8 and SR9 sort-merge arms at 85.
  - COBOL0897, the USE-names-a-sort-file arm (slice 1's left line): the 1985 USE statements state no rule, and the ground is the
    sort-merge concept, "The sort file itself is referred to and accessed only by the SORT statement" (`§1985:II.2.2.2.1.1`; the
    merge file `§1985:II.2.2.2.1.2`). One `ReportUseNamesSortMergeFile` replaces the two copies of the refusal.
- **The net.** `conformance-suite/wiseowl/edition-citation-pending-1985.json` is deleted; `EditionCitationNet` fails any
  later-edition-only citation at `--std 85` (an edition gate's words and a descriptor's `No1985Rule` excepted), and
  `EditionCitationNetDriftTests` plants an offence and each way out.
- **Slice 4's two unexamined failures** were `EditionReadDriftTests` (slice 4 added `EditionInForce` unrecorded) and
  `WrittenKindRefusalTests` (eight rows at 85 pinned `§2023:5.2.1` and `§2023:14.5.3.1` where the messages now cite
  `§1985:IV.2.1` and `§1985:IV.6.4.1.2`). Three Conformance tests pinned a 2023 rule number at 85 (`ValueClauseScreenTests`,
  `LinageConformanceTests.Sr18`, `SequentialFileIoSpecTests`'s WRITE INVALID KEY row) and now ask the edition's own.
- **Left:** the 2002 and 2014 directions, and the 2002 rows this note claimed, are kb/Work PB5922.
