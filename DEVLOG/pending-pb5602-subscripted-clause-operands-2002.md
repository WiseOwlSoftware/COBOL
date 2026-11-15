## PB5602 - at --std 2002 the CONTROL, TYPE CH/CF, RESET ON, FILE STATUS and RELATIVE KEY clauses take a subscripted data-name

COBOL-2002 reads a clause's data-name-n as "a user-defined word, including qualification and subscription if needed"
(§2002:5.1.3), and the 2002 clauses print only "may be qualified" (§2002:13.16.15.2 SR3, §2002:12.3.4.13.2 SR1,
§2002:12.3.4.7.2 SR1). The prohibition is the 2014 text's ("Data-name-1 shall not be subscripted", §2014:13.18.16.2 SR3). The
compiler read every such operand at every edition as §2023:8.4.2.2.2 Format 1 and refused the subscript (COBOL2024, COBOL1699), so
`RELATIVE KEY IS RK(2)`, `FILE STATUS IS FS(1)` and `CONTROL IS T-C(1)` were refused at --std 2002.

Re-probe on this build (the note's p/ctl_sub1.cob and p/reckey.cbl, refute-q-21 ctl_gr6/ctl_gr7, probes-q-30 read-relkey-sub):
every one still reproduced before the change. The fix is one mechanism across the place-based clauses:

- `DataBinder.AdmitsSubscriptedDataName` is the ONE edition predicate (2002). `ScreenDataNameShape` gained the caller's own
  2002 reading (`admitsSubscript2002`), `ClauseDataNameSubscripted` keeps the subscripts, `RefModBinder.ReadOperandSuffixes` reads their
  values lexically, and `DataBinder.ScreenOperandOccurrence` judges them against the resolved item (§2002:8.4.1.2.2 SR2, SR3, SR5 and
  §2002:8.4.1.2.3 GR2: COBOL2096, COBOL2097, COBOL2270, new COBOL7355). A subscript that is not an integer literal is new COBOL7354.
  The literal requirement is the implementor's choice (docs/conformance/3/D-CLAUSE-SUBSCRIPT.md): the clauses name one data item whose
  place is fixed when the program is compiled.
- The CONTROL clause, a TYPE CH/CF operand and a SUM RESET ON operand are one capture (`ReportSectionBinder.ControlOperandRef`),
  so all three carry the subscripts in `ReportControlRef`; `SameOperandAs` compares them, `PlaceTablePass` builds the prior control's
  place through the new `PlaceTable.OccurrenceOf`. The SR3 "subject to any OCCURS clauses" arm of `ControlOperandShapeViolation` is not asked
  at 2002.
- FILE STATUS and RELATIVE KEY keep their subscripts on the `FileModel` (`FileStatusSubscripts`, `RelativeKeySubscripts`); every emitter reads
  the place through `PlaceTable.FileStatusOf` / `RelativeKeyOf`, and the external-file connector rules and the emitted external
  descriptor compare the identity with the occurrence (`FileModel.FileStatusExternalIdentity`). The 2023 OCCURS bans of the key clauses
  and of FILE STATUS are not in force at 2002 (`FileControlKeyRules.Absent2002`, pinned against the 2002 text by a drift test).

Goldens: `13.16.15@2002-CONTROL-CLAUSE/pb5602-control-subscripted-2002` (a two-level table, FINAL plus two subscripted controls, expected
lines derived from §2002:13.16.15.3 GR3), `12.3.4.13@2002-RELATIVE-KEY-CLAUSE/pb5602-relative-key-file-status-subscripted-2002`
(§2002:14.8.29.3 GR22), and six refusals under `conformance-suite/wiseowl/diagnostics/pb5602-*.json` (a non-literal subscript, an
occurrence out of range, an unsubscripted table element, and the subscripted FILE STATUS and RELATIVE KEY refused at 85, 2014 and 2023).
`pb205-control-operand-subscripted` no longer refuses at 2002.
