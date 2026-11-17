## PB5883 and PB5888: the KEY phrase of READ and START asks the data-name shape screen, and an external data item's identity drops the group names

Wave 20262097, group B. Two mechanisms, one checkpoint each.

**PB5883: READ KEY IS a subscripted table element compiled.** Re-probe on main ed7d9b6d97: the refuter's `r1-sub-run.cob`
(`READ IXF KEY IS IX-T (1)`, IX-T PIC X(4) OCCURS 2 over the prime key's positions in a second record) exited 0 and printed
`R1 TWO` at `--std 85` and `--std 2023`. The KEY operand is a data-name that may be qualified and nothing more
(`§1985:IX.4.5.3 SR3`, `§2023:14.9.30.3 SR12`; START `§1985:IX.4.7.3 SR2`, `§2023:14.9.41.3 SR4`). `KeyedIoBinder.BindRead`
asked no shape question of data-name-1; the file-control key clauses already did (`ClauseDataNameSubscripted`). Fix:
`KeyedIoBinder.ScreenKeyOperandShape`, asked by READ and START before the operand is read as a record-key-name or data-name-1,
over `DataBinder.ScreenDataNameShape` (COBOL2024), with the statement's own rule quoted in the targeted edition's text.
COBOL-2002 keeps its integer-literal subscript (`§2002:5.1.3`, kb/Work PB5602). After: COBOL2024 at 85, 2014 and 2023 for READ
and START. Four refusals now answer COBOL2024 first (pb481-read-key-refmod, pb602-start-key-refmod-slice, pb354-start-occurs-operand-*).
Goldens: `IX.4.5@1985-READ-STATEMENT/pb5883-read-key-qualified-85` (accepted, `R1 TWO`), `pb5883-read-key-subscripted-85`,
`14.9.30-READ/pb5883-read-key-subscripted` (refused at 2014, 2023).

**PB5888: one external RELATIVE KEY item reached through differently named groups was refused.** Re-probe: `r1-group-differ.cob`
(REL-KEY at positions 1-4 of EXT-KEYS under GA in one program, GB in the other) was COBOL1575 at 85 and 2023.
`DataItem.ExternalItemIdentity` keyed on the whole dotted name path. Fix: the identity is the external record (its
externalized name, the new `DataItem.ExternalizedRecordName`, `§2023:13.18.22.4 GR5`, which the cell keying in `LinkageBinder`
now reads too), the item's own name (kept, as PB5857 decided), and its offset and width; never the groups between
(`§1985:X.4.5.4 GR2`). Every reader asks the one member: the RELATIVE KEY and FILE STATUS agreement, the PADDING segment of the
entry identity and the run-time descriptor `OoEmitter` emits. A sibling: two records with different data-names that write the same
`AS literal-1` are one external record, and are now one record here as well. After: `r1-group-differ` exit 0, `B[SECOND-REC]`, at 85
and 2023; `r1-name-differ` (another item name) and PB5857's other-positions case are still COBOL1575. Goldens:
`VIII.2.4@1985-ACCESS-MODE-CLAUSE/pb5888-external-relative-key-groups-differ-85` and
`12.4.5-File-control/pb5888-external-relative-key-record-as-literal` (2023).

**Rows.** Witnesses added to 1985-SR-IX.4.5.3-2, 1985-SR-IX.4.5.3-3, 1985-GR-VIII.2.4.4-8 (KEPT rows, their code reads'
verdicts stand), SR-14.9.30.3-12, SR-14.9.41.3-4 and GR-12.4.5.3-1; no GAP moved. Gate: the dispatching session's.
