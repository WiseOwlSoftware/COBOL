---
heading: "### 3.7 The obsolete COBOL-85 file description clauses LABEL RECORDS, VALUE OF and DATA RECORDS (kb/Work PB5070)"
---
The three clauses are obsolete elements of X3.23-1985 and are deleted from COBOL-2002 (COBOL0902, COBOL0873). WiseOwl COBOL's label
convention is that no label is read or written, and the 1985 text makes that convention the implementor's to specify: the label
specifications STANDARD names (§1985:VII.3.6.3 GR2) and those an omitted clause names (§1985:VII.3.6.3 GR3), and the label handling of
OPEN and CLOSE (§1985:VII.4.3.4 GR7, §1985:VIII.4.4.4 GR7, §1985:IX.4.4.4 GR7, §1985:XIII.4.5.4 GR5 and GR11), are items 57, 58, 60 and 61
of the Implementor-Defined Language Element List (§1985:C.2: "STANDARD specifies that labels exist for the file or the device to which
the file is assigned and the labels conform to the implementor's label specifications"; "The labels are checked or written in
accordance with the implementor's specified conventions"). LABEL RECORDS is not a hardware-dependent element, so the hardware exemption
(§1985:I.1.5.2.5.1) is not its ground. Under this convention LABEL RECORDS and VALUE OF have no effect at run time, and DATA RECORDS is
documentation (§1985:VII.3.5.4 GR1). Their general formats and syntax rules are enforced at COBOL-85, where they are rules of the source program.

| Item | Determination |
|---|---|
| The general formats | The grammar is the printed format: LABEL takes STANDARD or OMITTED only (§1985:VII.3.6.2), and VALUE OF writes implementor-name-1 before every data-name-1 or literal-1 (§1985:VII.3.9.2; §1985:IV.2.1.4, a brace requires one of its options), so any other spelling is a syntax error |
| VALUE OF implementor-name-1 | Any word. The label fields it names are the implementor's, and no label is read or written, so no name is checked against a list. The run-unit consistency rule for an external file connector's VALUE OF clauses (§1985:VII.3.9.4 GR2: "The implementor will specify the consistency rules") compares the literal-1 each entry gives an implementor-name both write, by value (the agreement row above, COBOL6847) |
| VALUE OF data-name-1 | §1985:VII.3.9.3 SR1 and SR2: not subscripted (COBOL2024, the clause-operand capture), not described with USAGE IS INDEX (an index data item, or a group whose own entry writes the clause, kb/Work PB5417), and in the Working-Storage Section (COBOL6849); an undefined name is COBOL1639 |
| DATA RECORDS data-name-1 | §1985:VII.3.5.3 SR1: the name of an 01 record description of the same entry (COBOL6849), in an FD and in an SD |
