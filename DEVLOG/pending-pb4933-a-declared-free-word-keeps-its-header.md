## PB4933: a declared free word no longer hides the edition refusal of the header it begins

**The defect.** At COBOL-85, `01 SCREEN LINE 1.` in a SCREEN SECTION made the section header a syntax error, `COBOL0001:
unexpected 'SCREEN'`, where the same section without the item draws the named COBOL1560. ISO §2023:8.3.2.1 1) refuses only a
reserved word as a user-defined word, and SCREEN is not reserved at 85, so the item is a user word; the reservation gate
(`ReservationGateRewriter`) then retypes every SCREEN token to `IDENTIFIER`, the header's included, and `screenSection` spelled
the bare token.

**Re-probe and sweep.** Re-probed on f19719cfaf: the note's `c22.cob --std 85` still drew the COBOL0001. The mechanism is every
reservation-gated word that heads a section or paragraph header of a construct its free editions lack. A probe pair (with and
without the declaration) per gated header word found nine more: LOCAL-STORAGE at 85 (COBOL0001 in place of COBOL0900), REPOSITORY
and OPTIONS at 85 (COBOL1971 "not a paragraph" in place of COBOL0900 and COBOL0804), COMMUNICATION and its CD entry at 2014 and 2023,
and AUTHOR, INSTALLATION, DATE-WRITTEN, DATE-COMPILED and SECURITY from 2002 (COBOL0001 in place of COBOL0902).

**The fix.** The design already had the shape: the generated `<word>Keyword : TOKEN | {wordAhead("WORD")}? IDENTIFIER` rule that
`keywordWhereFree` rows get (PB2623, PB3794). That determination is "a format of a free edition prints the word", and it also puts
the word into `CobolLexer.IsKeywordWhereFree`, which lets a syntax diagnostic name it as expected, which would be wrong at 85 for
SCREEN. So the rows carry a second determination, `refusalWhereFree`: the union grammar spells the word as the keyword of a construct
the free editions lack, so its edition refusal can answer. `gen-cobol-words.ps1` emits the same rule for either, keeps
`IsKeywordWhereFree` to the first, and refuses a row that carries both. The eleven words' grammar spellings go through their rules,
the SCREEN of BLANK SCREEN, ERASE SCREEN and OPTIONS INITIALIZE included (the spelling obligation), and `OptionsBinder` reads the
INITIALIZE target through the rule accessors. The six source-unit words (CLASS-ID, FUNCTION-ID, INTERFACE-ID, METHOD-ID, FACTORY,
OBJECT) carry no determination: the union grammar lets a unit begin without its IDENTIFICATION DIVISION header, so
`FUNCTION-ID. X.` after a COBOL-85 program's paragraphs is two paragraphs, and a keyword rule would read it as a unit header. The
design doc (`DESIGN-frontend-grammar.md`) records the rule and the exemption.

**Drift test.** `CobolWordsDriftTests.A_Gated_Header_Word_Reads_Through_Its_Keyword_Rule` scans every header-shaped alternative of
the hand grammar (one word, an optional SECTION, the period, after an optional IDENTIFICATION DIVISION header) and requires each
gated header word to read through its keyword rule or to be one of the six, both ways, so the next gated header fails a test; a
self-test proves the scan can fail. The existing keyword-rule tests now cover both determinations.

**Goldens.** `cases/8.3.2.1-General/pb4933_free_header_words_are_user_words_85` (accepted at 85, `AAAA`: the four words as data
items, every occurrence a reference, so no keyword rule misreads one), and one refusal per construct with its word declared:
`cases/13.9-Screen-section/pb4933-screen-section-with-an-item-named-screen` (85, COBOL1560),
`cases/13.6-Local-storage-section/pb4933-local-storage-section-with-an-item-named-local-storage` (85, COBOL0900),
`cases/12.3.8-REPOSITORY/pb4933-repository-paragraph-with-an-item-named-repository` (85, COBOL0900),
`cases/11.9-OPTIONS/pb4933-options-paragraph-with-an-item-named-options` (85, COBOL0804), and in `cases/_unassigned/` the
communication section with COMMUNICATION and CD declared (2014, 2023, COBOL0902) and the five comment paragraphs with their names
declared (2002, 2014, 2023, COBOL0902).

**Rows.** None re-verdicted: FMT-13.9.2 stays DOCUMENTED-NON-SUPPORT and the other rows keep their verdicts; the note writes
`closes_rows: []` with its reason.

**Self-review.** One mechanism (the generated keyword rule) for both determinations, no second copy of the rule; the predicate
runs only on an `IDENTIFIER` spelled the word, so a program that declares none of these words parses as before. A
`>>COBOL-WORDS UNDEFINE` of one of the words still withdraws its header (§2023:7.3.10.4 GR3): `wordAhead` reads through the
directive map, where an undefined word denotes nothing.
