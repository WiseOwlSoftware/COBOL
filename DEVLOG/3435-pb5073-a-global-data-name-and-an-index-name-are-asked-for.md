## Entry 3435 — 2026-11-10 07:06 PST — PB5073: a GLOBAL data-name and an index-name are asked for uniqueness at their declaration

Re-probe on the Debug build: a GLOBAL data-name shared with another item of its element (`01 DUP-NAME GLOBAL` beside `02 DUP-NAME`),
two `01 DUP-ITEM GLOBAL` entries, two tables `INDEXED BY IX`, and one entry `INDEXED BY IX IX` all compiled and ran at `--std 85`
and at 2023; only a REFERENCE to a duplicated index-name drew COBOL1639.

Mechanism: nothing asked the declarations, only references. New `DataBinder.NameUniqueness.cs` (`ScreenNameUniqueness`), run once in
`Bind` after the unit's own names are all declared:

- COBOL6862, every edition. §2023:8.4.2.1 (the same sentence as §1985:IV.4.3.8.1; §1985:X.4.6.3 SR2 states it for two GLOBAL
  items): a name the GLOBAL clause makes global is no other data item's name in the element. The global entry is the record that
  carries the clause (or whose FD does); the subordinates of two global records stay a qualification matter, as
  `pb1047-two-globals-one-element-ambiguous` pins (§2023:8.4.6.2.1 3) b) 1.). Reported at the later entry of the pair. A TYPE
  or SAME AS copy IS a data item of the element ("as though ... coded in place", §2023:13.18.57.4 GR1, §2023:13.18.49.4 GR1;
  branch review R1-1 caught a first version that exempted copies), so the GLOBAL screen is `CheckGlobalNameUniquenessPass`,
  run right after ExpandTypes on the composed forest; a GLOBAL name inherited from a container stays out. Golden
  `pb5073-global-data-name-duplicated-by-type-copy` (refused at 2023). The index-name screen stays in `Bind`.
- COBOL6863, `--std 85` only. §1985:VI.5.8.3 SR14: an index-name is a unique word within the program. 2002 and later qualify an
  index-name (Annex F item 108), so two tables may each declare one spelling and only an unqualified reference is ambiguous.

The first gate (two reds) found one existing test written against an illegal program: `ApplyCommitEnclosingFileTests.
TwoGlobalFilesRecordsOfOneName_AreToldApartByTheFileQualifier` declared records named R in two GLOBAL files, a record of a GLOBAL
file possessing the global attribute. It now names F1's record R1, and asserts the same-name pair draws COBOL6862. The facts file
`tests/doc-facts/compiler-facts.json` was regenerated for the two codes.

Goldens: `pb5073_global_name_unique_beside_qualified_locals` (85 to 2023) and refused `pb5073-global-data-name-duplicated` (all
four editions); `pb5073_index_name_twice_qualified` (2002 and later) and refused `pb5073-index-name-declared-twice-85`.
No inventory row is closed (the 1985 rows do not exist yet; the 2023 catalog has no row for the §2023:8.4.2.1 sentence, so the
GLOBAL goldens witness SR-13.18.27.3-1, the nearest row).


Finisher (merge-group bounce of PR #1465, run 42841304077): the one red, CCVS `CM102M_ReportsNoFailure` timing out at 120 s on a
GitHub Windows runner, was not this change. CM102M waits 90 s on its own clock by design (three 30-second ACCEPT FROM TIME
delays), which left 30 s of the one-unit harness budget for load. The budget rule now adds a stated own-clock wait to the
units (`ObservationBudget.OneProcessWaiting`, kb/Work PB5154), and CM102M states its 90 s.
