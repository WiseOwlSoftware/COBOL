## PB5602 - at --std 2002 the CONTROL, TYPE CH/CF, RESET ON, FILE STATUS, RELATIVE KEY, RECORD KEY, ALTERNATE RECORD KEY and key COLLATING SEQUENCE clauses take a subscripted data-name

COBOL-2002 reads a clause's data-name-n as "a user-defined word, including qualification and subscription if needed"
(§2002:5.1.3), and the 2002 clauses print only "may be qualified" (§2002:13.16.15.2 SR3, §2002:12.3.4.13.2 SR1,
§2002:12.3.4.7.2 SR1, §2002:12.3.4.12.2 SR1, §2002:12.3.4.5.2 SR1). The prohibition is the 2014 text's ("Data-name-1 shall not be
subscripted", §2014:13.18.16.2 SR3). The compiler read every such operand at every edition as §2023:8.4.2.2.2 Format 1 and refused the
subscript (COBOL2024, COBOL1699), so `RECORD KEY IS K(2)`, `RELATIVE KEY IS RK(2)`, `FILE STATUS IS FS(1)` and `CONTROL IS T-C(1)` were
refused at --std 2002, and the 2023 "shall not be subject to any OCCURS clauses" rows also refused an UNsubscripted table element there with a
sentence the 2002 text does not have.

**Re-probe** on this build (the note's p/ctl_sub1.cob and p/reckey.cbl, refute-q-21 ctl_gr6/ctl_gr7, probes-q-30 read-relkey-sub): every one
reproduced (COBOL2024 / COBOL1699, rc 65) before the change, and runs after it (`reckey.cbl` prints `FS=00 K1=BBB`).

**The fix is one mechanism, the written occurrence, across every clause whose operand is a place:**

- `EditionInfo.SubscriptedClauseOperands` (2002 alone) is the ONE predicate. `DataBinder.ScreenDataNameShape` gained the caller's own 2002 reading,
  `ClauseDataNameSubscripted` keeps the subscripts, `RefModBinder.ReadOperandSuffixes` reads their values lexically, and `DataBinder.ScreenOperandOccurrence`
  judges them against the resolved item (§2002:8.4.1.2.2 SR2, SR3, SR5 and §2002:8.4.1.2.3 GR2: COBOL2096, COBOL2097, COBOL2270, new COBOL7355). A subscript
  that is not an integer literal is new COBOL7354. The literal requirement is the implementor's choice (docs/conformance/3/D-CLAUSE-SUBSCRIPT.md): these clauses
  name one data item whose place is fixed when the program is compiled (a key's "relative location within a record shall be the same as that used when the file
  was created", §2002:12.3.4.12.3 GR3).
- CONTROL, a TYPE CH/CF operand and a SUM RESET ON operand are one capture (`ReportSectionBinder.ControlOperandRef`), so all three carry the subscripts in
  `ReportControlRef`; `SameOperandAs` compares them (T-C(1, 2) and T-C(2, 3) are two controls), `PlaceTablePass` builds the prior control's place through
  the new `PlaceTable.OccurrenceOf`.
- FILE STATUS and RELATIVE KEY keep their subscripts on the `FileModel`; every emitter reads the place through `PlaceTable.FileStatusOf` / `RelativeKeyOf`, and the
  external-file rules and the emitted descriptor compare the occurrence (`DataItem.ExternalItemIdentity(occurrence)`).
- RECORD KEY, ALTERNATE RECORD KEY (data-name-1 and the SOURCE phrase's data-name-2) and the key-level COLLATING SEQUENCE operand carry them on `RecordKey`,
  `AlternateKeyClause`, `KeySourceOperand` and `CollatingKeyOperand`. A key's location is `RecordLayout.OffsetOf(item, occurrence)` (the first occurrence's walk,
  REDEFINES classes included, plus `OccurrenceDisplacement`, the strides `StorageExtent` already records), read by `KeyWindowOf`, the emitter's key windows,
  the SORT/MERGE GIVING screen, `FileControlKeyRules` and `FileEntryIdentity`. A READ or START KEY operand is a key by `RecordKey.Names(item,
  Place.ConstantOccurrence())`, so K(1) is not the key K(2); START's 2023 OCCURS ban (SR4) is not in force at 2002.
- The 2023 OCCURS bans of the key clauses and of FILE STATUS are not in force at 2002 (`FileControlKeyRules.Absent2002`, pinned against the 2002 text by a drift test);
  an unsubscripted table element there is §2002:8.4.1.2.2 SR5's omitted subscript (COBOL2270).

**Goldens** (every one runs at the gate): `13.16.15@2002-CONTROL-CLAUSE/pb5602-control-subscripted-2002` (FINAL plus two subscripted controls of a two-level table, expected
lines derived from §2002:13.16.15.3 GR3), `12.3.4.13@2002-RELATIVE-KEY-CLAUSE/pb5602-relative-key-file-status-subscripted-2002` (§2002:14.8.29.3 GR22, §2002:14.8.33.3 GR20, START, the
invalid-key status 23), `12.3.4.12@2002-RECORD-KEY-CLAUSE/pb5602-indexed-keys-subscripted-2002` (RECORD KEY K(2), ALTERNATE KEY A(1) WITH DUPLICATES: statuses 00/02/00/10/23, START, READ
KEY IS, a second 01 in the FD), `.../pb5602-record-key-source-subscripted-2002`, `12.3.4.6@2002-COLLATING-SEQUENCE-CLAUSE/pb5602-collating-key-subscripted-2002`, and thirteen refusals
under `conformance-suite/wiseowl/diagnostics/pb5602-*.json` (non-literal subscript, occurrence out of range, unsubscripted table element, READ and START naming another occurrence,
and the subscripted operands refused at 85, 2014 and 2023). Eleven existing refusals drop 2002 from their `refuse-at` (the 2002 text has no such rule); ten of the programs stay refused at 2002
by COBOL2270 and the START on `IX-ELEM (1)` is a legal generic key there (§2002:14.8.37.2 SR6 b), which has no OCCURS ban).

**Rows**: twelve 2002 rows recorded CONFORMS by one `record_verdicts` batch (GAP 3297 to 3285); `2002-GR-13.16.55.3-6/-7` now run but their verdicts are not about the subscript and stay open.
The first gate run (000002) was RED on the 2002 arms of seven refusal goldens and on the backend contract (a text-returning member on `FileModel`); both are fixed in this change.
The line-endings audit refused the first gate (a helper script wrote CRLF); every writer in this change states `newline=`.

**Review round 1 (R1-1).** ControlLevelOf's second step (the operand's item by reference identity plus equal ref-mod) ignored the written subscripts, so at 2002 `TYPE CONTROL FOOTING T-C(2)` under `CONTROL IS T-C(1)` bound to T-C(1)'s level instead of being refused (§2002:13.16.55.2 SR10). The step now also requires equal Subscripts (RESET ON shares the lookup); golden pb5602-control-footing-other-occurrence refuses it with COBOL0899 at 2002.
