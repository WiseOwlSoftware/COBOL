- **D-CLAUSE-SUBSCRIPT — at COBOL-2002 a data-name-n clause operand may be subscripted, and each subscript is an integer
  literal (§2002:5.1.3; kb/Work PB5602)**: §2002:5.1.3 lists, as an operand, "a) User-defined word, including qualification and
  subscription if needed", and the clauses of COBOL-2002 that print data-name-n say only that it "may be qualified" — the FILE
  STATUS clause (§2002:12.3.4.7.2 SR1), the RECORD KEY clause (§2002:12.3.4.12.2 SR1), the ALTERNATE RECORD KEY clause
  (§2002:12.3.4.5.2 SR1), the RELATIVE KEY clause (§2002:12.3.4.13.2 SR1) and the CONTROL clause (§2002:13.16.15.2 SR3, with a
  reference-modified operand also named in SR4). A table element reference shall be subscripted (§2002:8.4.1.2.2 SR5), so at
  COBOL-2002 `RELATIVE KEY IS RK(2)` and `CONTROL IS T-C(1)` are the way a table element is named in those clauses, and
  §2002:8.4.1.2.2 SR2/SR3/SR5 and §2002:8.4.1.2.3 GR2 judge the subscripts (COBOL2096, COBOL2097, COBOL2270, COBOL7355). COBOL-2014
  forbids the subscript (§2014:13.18.16.2 SR3 "Data-name-1 shall not be subscripted"; likewise §2014:12.4.4.11.2 SR1) and
  COBOL-2023 forbids an operand subject to any OCCURS clause (§2023:13.18.16.3 SR3), so both refuse it (COBOL2024, COBOL1699).
  The standard leaves one point open: whether the subscript may be an expression whose value is read at run time. These
  clauses identify ONE data item whose place is fixed when the program is compiled — a record key's "relative location within a
  record shall be the same as that used when the file was created" (§2002:12.3.4.12.3 GR3), a control's prior control has "the
  same data description as the corresponding data item" (§2002:13.16.15.3 GR3), and the clause's reference modifier already takes
  integer literals (§2002:13.16.15.2 SR4). **WiseOwl COBOL requires each subscript to be an integer literal** (COBOL7354 for
  a data-name, an index-name, an arithmetic expression, ALL or the figurative ZERO). Rejected: a run-time subscript, which
  would make the key's, the status's or the control's location a run-time quantity the connector registration and the prior-control
  save have no place for. Edition scope: COBOL-2002 only; COBOL-85 keeps its own reading of data-name-n, which the register leaves
  open.
  Code sites: `DataBinder.AdmitsSubscriptedDataName` (the one edition predicate), `DataBinder.ScreenDataNameShape`,
  `DataBinder.ScreenOperandOccurrence`, `ReportSectionBinder.ControlOperandRef`, `PlaceTable.OccurrenceOf`; pinned by
  `conformance:cases/13.16.15@2002-CONTROL-CLAUSE/pb5602-control-subscripted-2002` and
  `conformance:cases/12.3.4.13@2002-RELATIVE-KEY-CLAUSE/pb5602-relative-key-file-status-subscripted-2002`.
