## PB5844: the RECORD DELIMITER clause takes BINARY-SEQUENTIAL or LINE-SEQUENTIAL, refuses every other word, and refuses a fixed-length file

**What was wrong.** `FileControlBinder.BindFileControl` read any word as the clause's implementor-name and declined
the whole clause with warning COBOL1778, at every edition: `RECORD DELIMITER IS FOOBAR` and `RECORD DELIMITER IS F1`
(the file's own name) compiled and ran, and so did the clause on a file with `RECORD CONTAINS 10 CHARACTERS`, which
§1985:VII.2.8.3 SR1 and §2023:12.4.5.11.3 SR1 forbid ("may be specified only for variable-length records"). The
re-probe of the checker's six programs on the branch showed exactly that at `--std 85` and `--std 2023`.

**The decision, completed.** The implementor defines the names (§1985:VII.2.8.4 GR3; §2023:12.4.5.11.3 SR2), so by
CLAUDE.md rule 1 they are GnuCOBOL's (`cobc/parser.y`, `record_delimiter_option`), with GnuCOBOL's meanings:
BINARY-SEQUENTIAL is the record sequential file's 4-byte length prefix, which a sequential file has anyway, and
LINE-SEQUENTIAL makes the file LINE SEQUENTIAL. GnuCOBOL's fourth arm, a warning for any other word, is a leniency
and is not taken. STANDARD-1 stays the documented decline (a reel-device element, §1985:I.1.5.2.5.1, §1985:C.3).

**The fix.** One table, `RecordDelimiterNames`; the binder matches the operand's text against it, so neither name is
a reserved word (a data item and a paragraph spelled with them compile, §1985:IV.4.2.2.1). Any other word is
COBOL7598. `FileControlBinder.ApplyRecordDelimiter` applies the method after the whole entry (the ORGANIZATION clause
may follow) and refuses BINARY-SEQUENTIAL on a written ORGANIZATION LINE SEQUENTIAL. `FileControlBinder.ScreenRecordDelimiter`
enforces SR1 post-build on both arms (COBOL7599) through `FileModel.HasVariableLengthRecords`, now the one predicate the
FORMAT clause's rule asks too. The rules whose subject is the written LINE SEQUENTIAL phrase (§2023:12.4.5.2 SR12,
§2023:13.4.5.3 SR4, the format marker of §2023:12.4.5.2 SR11) ask `FileModel.LineSequentialPhraseWritten`, so a 1985 file made line
sequential keeps its RESERVE and BLOCK CONTAINS clauses. COBOL1778 is STANDARD-1's alone.

**Siblings.** The 1985 AT END rule skipped every line sequential file (`ExceptionPhraseRequirement.Rule.TryGet`),
which at 85 now means a file made line sequential by the clause: fixed, so its READ owes AT END (COBOL6826). The
external connector identity compares the method instead of the spelling (`FileEntryIdentity.Segments`, DOC-A.1-149),
and COBOL6847 cites §1985:VII.2.8.4 GR6 at 85. NIST SQ219A writes the X-card XXXXX070 in the clause; the preprocessor
now substitutes BINARY-SEQUENTIAL, and SQ218A and SQ219A stay green. The 1985 case
`pb5311-external-reserve-and-delimiter-defaults` wrote the clause on a fixed-length file and now has RECORD CONTAINS 1 TO 4.

**Witnesses.** `wiseowl/cases/12.4.5.11-RECORD-DELIMITER/pb5844_rd_implementor_names` (85, 2023) writes AB and C D
through each name and reads them back as 6 and 6 characters (length prefix) and 2 and 3 (line delimiter, trailing
spaces not written, §2023:14.9.51.4 GR21). Refusals: `cases/12.4.5.11-RECORD-DELIMITER/pb5844_rd_on_fixed_length`
(COBOL7599, 85 and 2023), `cases/VII.2.8@1985-RECORD-DELIMITER/pb5844_rd_on_fixed_length_85` (STANDARD-1 on a format 1
file, COBOL7599), `wiseowl/cases/12.4.5.11-RECORD-DELIMITER/pb5844_rd_word_not_a_name` (COBOL7598).
`RecordDelimiterClauseTests` pins each arm, the user-word case, RESERVE and BLOCK CONTAINS, AT END and OPEN REVERSED.

**Rows.** 2023: SR-12.4.5.11.3-1, GR-12.4.5.11.4-4, GR-12.4.5.11.4-6, DOC-A.1-149 and DOC-A.1-150 from
DOCUMENTED-NON-SUPPORT to CONFORMS; FMT-12.4.5.11.2 and SR-12.4.5.11.3-2 restated CONFORMS; the deleted test
`RecordDelimiterFeatureName_NamedNonSupportWarning_PinnedToSpec` retired from each. 1985: 1985-GR-VII.2.8.4-6 to
CONFORMS; 1985-SR-VII.2.8.3-1, 1985-FMT-VII.2.8.2, 1985-GR-VII.2.8.4-3 and -5 given their witnesses. The register
(docs/conformance/2/26.md, 7/DOC-A.1-149, -150, -151, 4/index.md) and docs/FILES_DESIGN.md say one thing.
Codes COBOL7598 and COBOL7599.
