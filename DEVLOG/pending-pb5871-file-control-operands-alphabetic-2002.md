## PB5871, PB5735, PB6073: the file control clause operands admit a PIC A item at COBOL-2002; a retired INSPECT lead; five class-of-operand leads adjudicated

**PB5871 (the defect).** At `--std 2002`, `SELECT ... ASSIGN USING WS-ALPHA` with `WS-ALPHA PIC A(8)` was refused COBOL1810, and so
were a PIC A RECORD KEY, ALTERNATE RECORD KEY, FILE STATUS and PADDING CHARACTER item. §2002:8.5.2.1's second sentence ("Where an
alphabetic data item is not expressly permitted, the alphabetic data item is treated as though it were an alphanumeric data item") is
not limited to statement operands, and none of §2002:12.3.4.2 SR7, §2002:12.3.4.12.2 SR2, §2002:12.3.4.5.2 SR2, §2002:12.3.4.7.2 SR2
or §2002:12.3.4.10.2 SR3 expressly permits an alphabetic item, so PB6073's first lead (are the clause operands "operands"?) is answered
yes. PB5615 had put the edition fact in `EditionInfo.AlphabeticIsAlphanumeric` and read it through the Place overload; the
DataItem overloads of `ItemCategory.IsAlphanumeric` / `IsAlphanumericOrNational` had no edition. Fix at the root: they take the
`EditionInfo` and the edition-less forms are deleted, so every caller (the file control binder's ASSIGN USING, FILE STATUS, key
collating and SUPPRESS WHEN screens, PADDING CHARACTER, the report CODE operand, READ / RETURN INTO record arm b, and the
`FileControlKeyRules` rows through a new `FileKeyOperand.Edition`) states its edition. ASSIGN USING's messages cite the rule of the edition
compiled (§2002:12.3.4.2 SR7, §2014:12.4.4.2 SR7, §2023:12.4.5.2 SR7). Re-probe before: g2asg06 at 2002 COBOL1810 rc 65; after:
it runs and prints `F4 ALPHABETIC FOUR`; at 2014 and 2023 it is still COBOL1810. Goldens: `pb5871-assign-using-alphabetic-2002`,
`pb5871-keys-status-alphabetic-2002` (written, read by prime and alternate key, status 00), `pb5871-padding-alphabetic-2002`, and refused at
2014 and 2023 `pb5871-assign-using-alphabetic-refused`, `pb5871-keys-status-alphabetic-refused`. Row `2002-SR-12.3.4.2-7` is
CONFORMS; five 2002 rows gained a witness.

**PB5735 (retired).** The INSPECT refusal of a GROUP-USAGE NATIONAL group as identifier-3 does not reproduce: commit 7470049259 (2026-11-15,
the PB5646 review round) made `IsInspectElementaryCharacterItem` ask `IsElementaryOrBitNationalGroupReference`, the day the note was filed
against an older build. The sibling sweep ran a national group in every operand position (REPLACING pattern and replacement, CONVERTING pair,
BEFORE and AFTER delimiters): all accepted, with values derived from §2023:14.9.22.4. Golden `pb5735-inspect-national-group-every-operand-position`;
`SR-14.9.22.3-2` re-verdicted PARTIAL to CONFORMS, which un-gaps `2002-SR-14.8.21.2-2`.

**PB6073 (adjudicated).** Lead 2 (STRING accepts a PIC A sender) is correct: §2023:14.9.43.3 SR1 is a USAGE rule and PIC A is usage display;
UNSTRING's SR2 is a category rule. Golden `pb6073-string-alphabetic-sender`. Lead 3 (`PIC 99 COMP-5` holding 4 shows `004`) is the
recorded implementor choice of DOC-A.1-56 (a binary-capacity item shows its container's digits; GnuCOBOL 3.2.0 does the same): no change.
Lead 4: the COMP-5 container golden now cites DOC-A.1-208 instead of §2023:13.18.60.4 GR12. Lead 5: `pb6073-testnumvalf-alphabetic-2002`
witnesses `2002-AR-15.77.2-1`.

**Left open (named in the report).** The 2002 rows of `FileControlKeyRules` still print the 2023 clause in their messages.
