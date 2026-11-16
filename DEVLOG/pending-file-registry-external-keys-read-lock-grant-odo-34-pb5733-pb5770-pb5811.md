## FileRegistry: an external file's alternate keys registered once, a READ's refused lock grant is its status, and ODO status 34 is 2023 only (PB5733, PB5770, PB5811)

Wave 20262068 group B replayed the three unlanded commits of its predecessor (w20262063a, stopped by the loop's STOP-loop
before its gate) onto main and gated them. Each note was re-probed on this build.

**PB5733.** Two programs of one run unit that describe one EXTERNAL indexed file with the same alternate record keys shared
one `IndexedConnector` (`RegisterIndexed` returns early for an existing `::EXT::` name), but each describer's emitted
`AddAlternateKey` calls still ran, so the connector's key table held every alternate key twice and the second program's
OPEN got I-O status 39 (§2023:12.4.5.3 GR1: one connector, the same number of alternate keys). Fix:
`FileRegistry.RegisterIndexed` records the name of an existing external connector it skipped (`_alternateKeysDescribed`)
and clears it when it creates one; `AddAlternateKey` for that name returns. The emitter writes each registration then its
keys contiguously (`KeyedIoEmitter`), so the field is exact. Siblings swept: `Register`, `RegisterReport` and
`RegisterRelative` already return early and have no follow-on registration calls. Golden
`conformance:cases/12.4.5-File-control/pb5733_external_alternate_keys_agree` (two alternate keys, one WITH DUPLICATES,
READ by the second key), accepted at 85, 2002, 2014 and 2023; re-probe on this build `A DONE` / `02AAYY` at 85 and 2023.
No refused-below program: the rule exists in every edition. Row `GR-12.4.5.3-1` re-verdicted PARTIAL to CONFORMS.

**PB5770.** `FileRegistry.ApplyPostReadLockActions` discarded `PhysicalFileTable.LockRecord`'s result. A sequential
connector has no store statement (`FileConnector.BeginStatement` is inert there), so another run unit could take the
record between A's conflict test and A's lock, and A's READ still answered '00' holding no lock (§2023:14.9.30.4 GR11c,
§2023:9.1.16; §2002:12.3.4.8.3 GR8, "atomic operation"). The grant is now the last conflict test: a refused grant ('51')
is the READ's status, no record is made available and the position is invalidated (GR18), on both governed READ entries
(`ReadSharedInStatement`, `ReadKeyedSharedInStatement`). A test seam, `FileRegistry.ReadLockWindow`, runs a second run
unit's statement at the exact point; `CrossRunUnitSequentialLockDriftTests.ALockGrantAnotherRunUnitTookFirst_IsTheReadsStatus`
failed at '00' with the fix switched off and passes with it. Row 2002-GR-12.3.4.8.3-8 recorded CONFORMS. The WRITE's and
REWRITE's completion-time grants on a sequential file have the same window, but the record is already written there, so
their root is a cross-run-unit statement hold for the sequential connector (the shape PB2660 gave the keyed store); it is
reported as a lead, not fixed here.

**PB5811.** `SequentialIoEmitter.OdoRecordExceedsMaximum` (shared by the sequential READ, the keyed READ and SORT USING) set
status 34 and a permanent error with no edition switch. The condition is in §2023:9.1.13.6 4) b) only (`cite.py --find`
over 1985, 2002 and 2014 finds none), and §1985:IX.4.5.4 GR18 makes the READ successful. The predicate now yields no test
below `--std 2023`. Golden `pb5811_read_odo_past_maximum_by_edition` (accepted 85, 2002, 2014, 2023; `.out` all 00 and
`.2023.out` all 34, sequential and relative arms); re-probe on this build matches both. D-ODO2 and FILES_DESIGN D30 carry
the edition.

Self-review: architecture (one predicate, one edition gate; one grant path), full code (the cited rules, both READ entries
agree), performance (one null test per governed READ), duplication (no second copy of the grant rule).
