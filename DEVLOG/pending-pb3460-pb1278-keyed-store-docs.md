## PB3460 + PB1278: the RESERVE and record-size determinations describe the paged keyed store

Wave 1140, group A (PB3460, PB1278, PB3338). Two of the group's notes were documentation that still described the
whole-store format kb/Work PB3333-PB3335 deleted; this entry records them.

**PB3460 — DOC-A.1-164 (RESERVE, the number of input-output areas).** Re-probed: `git grep -n "_areas" -- src` prints
nothing on 62cd0d954, yet the row's keyed arm said a relative or indexed connector's areas were `KeyedConnector._areas`,
the buffer its whole store passed through at OPEN and CLOSE. The engine reads and writes one 4,096-byte page per
positional call into the run unit's page cache for the physical file (DESIGN-runtime-library 2.2.1.13), and the OPEN
that attaches a connector raises the cache's capacity to at least its areas (`KeyedConnector.Attach` →
`KeyedStore.AttachHandle` → `PageCache.Reserve`), above the cache's own 256-page floor. The row now says so: one area is
one page; a sequential connector's areas are allocated by the OPEN (its '30' when memory runs out), a keyed connector's
reserved by it and filled as pages are read; and the "when the areas are emptied" paragraph is the SEQUENTIAL connector's,
because a keyed statement writes every page it changed before it completes in every sharing mode (2.2.1.6). The same
`_areas` sentence in DESIGN-runtime-library §2.2 (the `OpenConnectorStore` paragraph) was rewritten too.

**PB1278 — the RECORD clause's Annex A.1 items 146 and 148.** Re-probed: `RECORD CONTAINS 5 TO 10` writing a 5- and a
10-byte record makes the 23-byte file `05000000 'ABCDE' 0A000000 'ABCDEFGHIJ'`, so format 3 is variable-length. The
note's premise had moved on: DOC-A.1-148 (with the `l1c23_record_format3_variable` witness), DOC-A.1-146 and DOC-A.1-147
were written by later landings and `audit_annex_a1.py --check` reports no remaining obligation. Two things were still
wrong. GR-13.18.43.4-17 stayed PARTIAL for "documentation absent"; it is re-verdicted CONFORMS. And DOC-A.1-146's arm
(d) still gave the relative and indexed sizes of the deleted whole store (a 36-byte header, an empty slot as
`0xFFFFFFFF`). It now gives paged store format 6's arithmetic: a file of whole 4,096-byte pages; a relative record one
leaf entry of 6 + 8 + (4 + size) bytes; an indexed record 6 + k + (4 + 8n + size) in the prime tree and 6 + a + 8 + k in
each alternate tree; ⌊4,064 / entry⌋ to a full leaf; overflow pages past a 1,016-byte frame. The new witness
`RelativeEngineTests.A_relative_records_size_on_the_medium_is_the_documented_leaf_entry_in_a_file_of_whole_pages` pins
the row's example (100 80-byte records: 98-byte entries, 41 to a full leaf, 11 pages, 45,056 bytes). The sibling sweep
(`grep -n "store header" docs/CONFORMANCE.md`) found the deleted header named in §2 row 40 and DOC-A.1-129 arm (4); both
now name the meta page. Batch `w1140a/batch-pb1278.json`: GAP 66 → 65.

**The Linux gate caught what leg 1 did not.** The first Windows gate was GREEN on leg 1, and the Linux unit leg was RED
on `AnnexA1RegisterDriftTests.EveryDocRow_IsFiledUnderTheItemAnnexA1Names`: the inventory's DOC-A.1-146 row now cited the
new witness, and the row's `Pinned by` column in `docs/CONFORMANCE.md` did not name it. `audit_annex_a1.py --check`
reports the same finding, but neither it nor the drift class that `drift_rules.py docs/CONFORMANCE.md` names runs in
leg 1 without an impact map; that is kb/Work PB3247's open gap, not a new one. The row names the witness now.

**PB3338 was not started.** The third note of the group, the compiler half of APPLY COMMIT, is a whole facility: the
clause's grammar and syntax rules, activation registration, COMMIT and ROLLBACK with data-item images, the EC-FLOW
conditions, §14.6.13.1.3 item 8, the decline's removal and its fault-injection cases. It does not fit the rest of this
implementer's turn budget. The group was split at the note boundary, and PB3338's note records the code sites found
while orienting.
