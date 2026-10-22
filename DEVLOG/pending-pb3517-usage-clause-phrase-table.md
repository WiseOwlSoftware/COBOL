## PB3517: one table decides which usage each optional USAGE-clause phrase may follow

**The defect.** `PIC 9(2) USAGE COMP UNSIGNED`, `PIC X(2) USAGE DISPLAY SIGNED` and `PIC S9(3) PACKED-DECIMAL
UNSIGNED` compiled clean at every `--std`, and the program printed `07 AB -005`, the word ignored (kb/Work PB3517,
found by the second differential fuzz night: GnuCOBOL 3.2 refused the clause). The re-probe on this branch's base
(b37c551fe) reproduced it at 85, 2002, 2014 and 2023.

**What the standard says.** The rendered §13.18.60.2 figure (PDF p.533, printed folio 503) prints `{ SIGNED |
UNSIGNED }` after BINARY-CHAR, BINARY-SHORT, BINARY-LONG and BINARY-DOUBLE and after no other usage; SIGNED is NOT
underlined (an optional word, so writing neither means SIGNED) and UNSIGNED is. `figure_extract.py 533` measures it;
the transcription's figure note had listed SIGNED among the underlined words and is corrected in
`specs/ISO_COBOL.md`. §13.18.60.4 GR12 tabulates the signed and unsigned ranges of those four usages only.

**The mechanism, and why the fix is a table.** The grammar parses all four of the clause's optional phrases
(`binarySign`, `noSignPhrase`, and the endianness and encoding phrases through `floatFormatPhrase`) after every
usage word, and the binder narrowed them with one hand-written arm per phrase in `DataBinder.BindEntry`: three arms,
in two places, and the SIGNED / UNSIGNED arm was never written. The fix writes the rule once.
`UsagePhraseRules` (`Binding/Model/UsagePhrase.cs`) is the one table of which resolved usage each phrase may follow,
each row carrying its refusal: COBOLNET4733 (new) for SIGNED / UNSIGNED, COBOLNET1565 for WITH NO SIGN (it was a
bare string code and is now a `DiagnosticCatalog` descriptor), COBOLNET1716 and 1717 for the float phrases.
`DataBinder.ScreenUsagePhrases` (`DataBinder.UsagePhrases.cs`) is the one reader of the clause's phrases: it asks the
table about each written phrase, refuses the misplaced ones by name ("the SIGNED / UNSIGNED phrase is written only
with USAGE BINARY-CHAR, BINARY-SHORT, BINARY-LONG or BINARY-DOUBLE, not USAGE COMP"), screens a repeated float
phrase (COBOLNET1718), and returns only the admitted phrases, so a refused one cannot change a representation.
The grammar keeps the superset parse (a phrase inside `usageKeyword` would glue into the keyword text that
`DataBinder.UsageKeyword` reads) and its comment no longer claims a rejection that did not exist.

**The sweep.** All four phrases now go through the table. Each edition: the phrase is refused off the binary usages
at 85 as well, and it needs no gate of its own because the four usages carry their 2002 gate. The group-level
spelling (`01 G USAGE BINARY-CHAR UNSIGNED. 05 A.`) already held 200 in A. NATIVE, which the dispatch named, is no word of the
§13.18.60.2 format and the grammar does not parse it after a usage word, so it has no row. The four-member fixed-width binary usage set was spelled out at five sites (`BindEntry`,
`Sr8PicturelessVerdict`, `MoveTable16.IsBinaryWidth`, `StatementValidation`, `VersionConformancePass`) and is now
`UsageFamilies.IsFixedWidthBinary`. Three comments named the float-phrase codes as `COBOLNET1716/1706/1707`
(1706 and 1707 are unrelated declined-facility codes); they now read 1716/1717/1718.

**The drift test.** `UsageFloatFormatPhraseDriftTests` became `UsageClausePhraseDriftTests`: it reads the grammar's
usage words and the clause's phrase rules out of the `.g4`, requires a `UsagePhrase` member and a table row for each
phrase rule, checks the table against its transcription of the printed figure for every usage word through the
compiler's own `ParseUsage`, and pins the table as the only place the four refusals are named and
`ScreenUsagePhrases` (plus the edition gate's presence check) as the only reader of the phrases. It also asserts the
repro draws exactly one COBOLNET4733 per entry.

**Goldens.** `cases/13.18.60-USAGE/pb3517-sign-phrase-on-non-binary-usage` (refused at all four editions,
COBOLNET4733), `pb3517-no-sign-phrase-on-binary-usage` (COBOLNET1565, which had no witness) and the positive
`pb3517_binary_sign_phrase` at 2002 (each binary usage at its GR12 range ends with SIGNED, UNSIGNED and neither).
FMT-13.18.60.2 is re-verdicted CONFORMS with these witnesses (its verdict had stood while the format was violated);
GR-13.18.60.4-12 gains the positive witness. Codes: COBOLNET4733 used, COBOLNET4734 returned.
