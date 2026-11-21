## 1985 fixes: an ADVANCING write keeps a line-framed record's size, a literal computer-name is refused as one, a failed compilation lists into a new directory, and a SEARCH ALL lead is retired

Wave 20262114, group A (kb/Work PB5917, PB5920, PB5940); the group's source files were four unrelated sites, so each note was fixed at its own root and one commit holds them.

**PB5917 (retired, no code change).** The lead said SEARCH ALL's "shall not specify a variable-length group" rule (§2023:14.9.37.3 SR12, §2014:14.9.34.2 SR10) is refused at `--std 85` and 2002, where neither text has it. The re-probe on this build reproduces the extra COBOL1965 line at 85, but the group it refuses needs a DYNAMIC LENGTH item or a dynamic-capacity table (§2023:8.5.1.12.1), declarable from 2014 only; at 85 the same program already draws COBOL0900 on the DYNAMIC LENGTH clause, COBOL2787 on the key and COBOL2492 on the comparison. No program the edition accepts can reach the rule, so it rejects no legal source. The note is retired with that evidence (kb/Work/PB5917/2026-11-20-retired-no-legal-source.md); 2002 stays frozen.

**PB5920 (landed).** `SOURCE-COMPUTER. "XYZ".` drew COBOL1971 ("not a paragraph of the CONFIGURATION SECTION", citing the section's format) and `OBJECT-COMPUTER. "XYZ".` drew COBOL1970 ("not a clause of the OBJECT-COMPUTER paragraph"), though the user had written a computer-name. §1985:VI.4.3.3 SR1 and §1985:VI.4.4.3 SR1 say "Computer-name is a system-name."; §2002:8.3.1.1.2, §2014:8.3.1.1.2 and §2023:8.3.2.3.1 give a system-name's formation as a COBOL word's (each `cite.py --check` OK). New COBOL7654 (`ComputerNameNotASystemName`); `ClosedFormats.ComputerNameSlotOf` is the one reading of the slot (the first clause of a nameless OBJECT-COMPUTER paragraph, or the configuration paragraph right after a nameless, mode-less SOURCE-COMPUTER paragraph), and `ClosedFormatPass.Refuse` reports it with the edition's own citation. Every edition, since the old wording was wrong at every edition; the verdict (refused) is unchanged. A reserved word in the slot is met by the parse layer before any tree exists and keeps its row's wording (lead in the report). Goldens: the two 1985 witnesses (`1985-SR-VI_4_3_3-1`, `1985-SR-VI_4_4_3-1`) now expect COBOL7654, and `pb5920-source-computer-literal-name` / `pb5920-object-computer-literal-name` refuse at all four editions with the message text; unit rows in `ClosedFormatReservedWordTests` include the named-SOURCE-COMPUTER control that keeps COBOL1971.

**PB5940 (landed).** A listing's default place is beside the output assembly; the backend makes that directory, which a compilation that fails earlier never reaches, so `cobol prog.cob -o out/prog.dll` with an error reported "cannot write the listing" instead of listing the source. `ListingWriter.Write` now creates the directory when the request names no `--listing-output` (the note's direction put this in `CompilerDriver.Compile`; the default place is decided in `ListingWriter.Write`, so the creation sits there and the driver keeps no second copy of the rule). An explicit `--listing-output` into a missing directory stays a refused write. Witness `ListingOutcomeTests.AFailedCompilation_ListsBesideAnAssemblyInADirectoryItNeverMade`, which fails on the base tree with the note's message and passes with the fix. DESIGN-listing.md section 4.6 and DESIGN-frontend-grammar.md (the closed-format table) say so.

Self-review: architecture (one slot reader, one message site, no second default-place rule), full code (the message cites the rule each edition prints, checked), performance (the slot reader runs only on a refused run), duplication (the system-name quotation is one constant).

### PB5914
kb/Work PB5914 (a lead of PB5903): at `--std 85` a file with RECORD DELIMITER IS LINE-SEQUENTIAL took the plain WRITE at its
size (PB5903) but dropped the record's trailing spaces on a WRITE with ADVANCING, because the print-control arm
(`SequentialConnector.WriteAdvancingRecord`) always ran `TrimRecordEnd` on a non-record stream. A file that has taken one
ADVANCING write routes its plain WRITEs through that arm too, so the rule PB5903 set held only until the first one.

**Re-probe.** A writer with `RECORD CONTAINS 1 TO 10` and `WRITE OREC AFTER ADVANCING 1 LINE` of `AB` followed by eight
spaces left `AB` in the file (bytes `\r\n A B \r ...`); a reader's DEPENDING item would have shown 2.

**Ruling.** DOC-A.1-146 (c') is about a RECORD SEQUENTIAL print stream. A file the delimiter clause frames is a line-framed
file, and before 2023 no line sequential file exists, so §1985:VII.2.8.4 GR1 ("Any method used will not be reflected in the
record area or the record size used within the program", checked with `cite.py`) binds it on both write arms.

**Fix.** `WriteAdvancingRecord` keeps the record at its `FitRecord` size when `_lineSequential && !LineSequentialFileRules`,
the same decider the plain arm and the READ arm ask. From 2023, and for LINAGE or report streams without the clause, the
text form is unchanged.

**Golden.** `conformance:cases/VII.4.7@1985-WRITE-STATEMENT/pb5914_line_delimited_advancing_keeps_size_85` (AFTER, plain
and BEFORE writes read back as 10-character records, spaces kept). No refusal golden: the rule is a runtime form, not a
diagnostic. No row closed; `docs/FILES_DESIGN.md` (the RECORD DELIMITER paragraph) names the print arm.
