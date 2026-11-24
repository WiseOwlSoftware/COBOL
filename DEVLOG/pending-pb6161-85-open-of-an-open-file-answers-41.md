## PB6161, PB6130: at --std 85 a second SELECT's OPEN of an open physical file answers 41; the A.1 exemption bands follow the text

**PB6161 (owner decision 2026-11-24, "Yes, return 41").** PB670's option (i) let a second connector open a physical file another
connector of the run unit held open at `--std 85`, answering 00 in every pair of modes, on the premise that 1985 is silent. It is
not. 1985 keys I-O status 41 to the physical file: "file" means "the physical file regardless of the file-name used" (§1985:II.2), a
second OPEN of it must follow a CLOSE (§1985:VII.4.3.4 GR5), and 41 is "An OPEN statement is attempted for a file in the open mode"
(§1985:VII.1.3.5). The owner then asked that every edition be derived from its own text. 2002, 2014 and 2023 key 41 to the file
connector (§2002:9.1.12.6, read on the rendered page; §2014:9.1.12.6; §2023:9.1.13.7 GR1) and arbitrate the second connector's OPEN
by the sharing facility (00 for two readers, 61 otherwise under the INPUT READ ONLY / NO OTHER default). Their behaviour already
matched the text; only 85 changed.

`FileRegistry.SharedOpenAttempt` now walks the connectors open on the physical file at every edition and answers 41 at 85, Table 19's
61 from 2002. What option (i) needed only to admit an 85 sibling is deleted: the release-before-sibling loop,
`ReleaseBufferedRecords` and the sequential connector's deferred release fault, the open-sibling arm of
`OtherConnectorsMayShare`, `StoreTable.AttachCreated`'s `arbitrated` parameter (PB754's guard is loud at every edition again), and
the OUTPUT arm's truncation `NoteRelease`. Toward other run units 85 is unchanged (no file lock; an outsider's refusal answers 30).

Witnesses: `pb6161_second_select_opens_an_open_file_85` and its 2002-2023 twin (three SELECTs on one path; each edition's lines
derived from its text, and each matched on a probe at 85, 2002, 2014 and 2023 before the gate). The header refused one test
accepted at all four editions because GR-14.9.27.4-23 and the 1985 rows do not exist at every one, so the program is two tests.
`SharingFacilityEditionTests` was rewritten: every organization and pair of modes at 85 (41, holder undisturbed, the refused
connector's CLOSE 42, OPEN after CLOSE 00) and at 2002, 2014 and 2023 (expected computed from §2002:9.1.12.8's items, not from
`Table19.Conflicts`), a third connector, and the buffered 85 writer. The 85 tests of admitted siblings were deleted with the
behaviour. `code_read_landing.py` refused the branch: the 1985 reads of 1985-GR-VII.4.7.4-7 and -12 named
`ReleaseBufferedRecords`. Both were re-read by a reader and a refuter on the branch and imported with `import --replace`.

**PB6130.** The adjudication and its refuter settled one reading: an A.1 item takes the band of the rule whose own edition's text
leaves the latitude, and a determination of behaviour at an edition never widens a band. DOC-A.1-43 re-bands to 2014,2023,
DOC-A.1-77 and -131 to 2002,2014,2023; `DocItemBandDriftTests.Exempt` loses -43 and -131 and rewords -64 (the refuter's
amendment: GR2 carries 2002's default-convention half) and -65 (a stale cross-reference). `record_verdicts`: 9 rows changed in
2023 (the brief expected 3; the other 6 are the currency golden's move to `cases/13.18.40-PICTURE/`, which the A.1 folder refused
once its header named no A.1 rule, and the PB6161 goldens added to the OPEN rows), 7 witnesses retired, GAP 1 → 1; 2 rows in 1985,
GAP 0 → 0. `l1c06_currency_non_cobol_equivalence_85` keeps only its 1985-legal `é` line. The first gate was RED before any
host ran: `generate.py` refused `l1c06_currency_non_cobol_symbols`, accepted at 2002 with DOC-A.1-43, a sibling the adjudication had
not listed. It is now accepted at 2014 and 2023, a 2002 twin writes each symbol as its clause gives it (3 more witness-only rows), and
the equivalent-rebound refusal names 2014 and 2023 only (its 2002 refusal is PB6162's). The 85 coverage points were re-recorded
(`coverage.py --edition 85 --record`); the record was already stale on main (catalog digest and tests this change does not touch),
so every points file moved. P2.10 now names 1985's own
Implementor-Defined Language Element List as the 85 check. Leads filed: PB6162 (`PIC É9.99` accepted at 85), PB6163
(SR-12.3.7.3-20's 2002 band), PB6164 (the standard's stale GOBACK rule numbers).

The gate's Conformance population then found five 85 goldens that opened a second connector on an open file. `pb5210_close_lock_exception_paths_85`
was re-derived (S2's OPEN 41, its CLOSE 42, the lock's 38 legs unchanged). The three PB5480 goldens existed only for the zero-byte route
to a keyed '04', which needs that second OPEN; with every other OPEN validating the attributes (the writer-first probe answers 39), a
keyed record outside the reader's size cannot reach a READ at 85. They were deleted and retired from their 1985 rows (each keeps another
witness), and `g85_bf18_04` lost its 04 leg. A unit test that tried the route through a second run unit was dropped: at 85 a run unit
is not promised another's records, and the reader saw none (23).

**Review round 1.** The branch review found the keyed '04' arm unwitnessed: it is still live from 2002, where a SHARING WITH ALL
OTHER reader opened on a zero-byte file meets a writer's records. The three PB5480 goldens return as 2002-2023 tests under that
clause, and D-IO-STATUS-04 pins them. It also found 2002 rejecting legal source: the currency fold made 'é' and 'É' one symbol at
2002, where only 2014's SR20 grants that equivalence, so two clauses binding them to EUR and USD were refused COBOL0891.
`CurrencySymbolEquivalence.Fold` now folds a character outside the repertoire only from 2014, which also refuses `PIC É9.99` under
`CURRENCY SIGN IS "é"` at 85 and 2002 (PB6162, landed). The 2023 sharing quotation is now 2023's own wording. The 1985 code reads of
1985-GR-VIII.1.3.4-L2.1 and 1985-GR-VII.4.3.4-5 were re-read and refuted, and the change record of the second became CHANGED (PB6165).
PB6166 records the refuter's unadjudicated ADVANCING overprint.

