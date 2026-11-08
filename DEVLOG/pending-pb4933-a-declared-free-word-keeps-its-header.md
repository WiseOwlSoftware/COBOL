## PB4933: a declared free word no longer hides the edition refusal of the header it begins

**The defect.** At COBOL-85, `01 SCREEN LINE 1.` in a SCREEN SECTION made the section header a syntax error, `COBOL0001:
unexpected 'SCREEN'`, where the same section without the item draws the named COBOL1560. ISO §2023:8.3.2.1 1) refuses only a
reserved word as a user-defined word, and SCREEN is not reserved at 85, so the item is a user word. The reservation gate
(`ReservationGateRewriter`) then retypes every SCREEN token to `IDENTIFIER`, the header's included, and `screenSection` spells
the bare token.

**Re-probe and sweep.** Re-probed on f19719cfaf: the note's `c22.cob --std 85` still drew the COBOL0001. The mechanism covers
every reservation-gated word that heads a section or paragraph header of a construct its free editions lack. A probe pair (with
and without the declaration) per gated header word found ten more words: LOCAL-STORAGE at 85 (COBOL0001 in place of COBOL0900),
REPOSITORY and OPTIONS at 85 (COBOL1971 "not a paragraph" in place of COBOL0900 and COBOL0804), COMMUNICATION and its CD entry
at 2014 and 2023, and AUTHOR, INSTALLATION, DATE-WRITTEN, DATE-COMPILED and SECURITY from 2002 (COBOL0001 in place of COBOL0902).

**First shape, withdrawn.** The first fix reused the generated `<word>Keyword : TOKEN | {wordAhead("WORD")}? IDENTIFIER` rule
that `keywordWhereFree` rows get (PB2623, PB3794), with a second determination keeping the words out of
`CobolLexer.IsKeywordWhereFree`. The gate measured it RED: 14 Unit and 13 Conformance diagnoses moved. The error strategy reads
the ATN without predicates in many places (recovery sets, the missing-period walk, the closed-format residue, header words), so
the IDENTIFIER arm at a header made every user-defined word there read as the header: `MAIN SECTION.` after a missing period
became a missing period before a LOCAL-STORAGE SECTION, and `WIBBLE GLOBAL.` in the identification division named GLOBAL, since
the arm kept a second alternative viable past the unknown word. Patching each reading would have meant a second, partial
predicate model inside the error strategy.

**The fix.** The rows carry the determination `refusalWhereFree`: every keyword position of the word heads or sits in a construct
the free editions lack, and no reading of the free word competes there. `gen-cobol-words.ps1` step 5d checks the rows (gated, free
at some edition, never also `keywordWhereFree`) and step 6 emits `CobolLexer.IsRefusalWhereFree`. When a pass frees such a word
that it DECLARED in a user-defined-word slot, `ReservationGateRewriter.ParseToFixpoint` keeps every keyword token of the word that
the pass's tree holds outside `reservedGatedWord` (`TokenRetypes.KeptKeywords`, by source offset) and retypes the rest. A re-lex
of the same text keeps them; a fragment, a slice of the text, applies none (`TokenRetypes.RewriteSlice`). A word freed only by the
second witness (a syntax error at it, `CobolParserCoreBase.DeclaredGatedWords` separates the two) keeps nothing, and a terminal counts only in a rule that itself spells the token (`RuleSpells`): the second gate found `DISPLAY SCREEN.` at 2002 under `--permissive` keeping a SCREEN the recovery had read as a name and then given back its keyword type (kb/Work PB4218). So `01
LOCAL-STORAGE SECTION.` at 85 stays an entry named LOCAL-STORAGE. No grammar rule changes. The six source-unit words (CLASS-ID,
FUNCTION-ID, INTERFACE-ID, METHOD-ID, FACTORY, OBJECT) carry no determination: a unit can begin without its IDENTIFICATION DIVISION
header in the union grammar, so `FUNCTION-ID. X.` after a COBOL-85 program's paragraphs is two paragraphs, and FACTORY and OBJECT
are keywords of phrases a name competes in. The design doc (`DESIGN-frontend-grammar.md`) records the rule, the exemption and why
the keyword rule was rejected.

**Drift tests.** `CobolWordsDriftTests.A_Gated_Header_Word_Carries_The_RefusalWhereFree_Determination` scans every header-shaped
alternative of the hand grammar (one word, an optional SECTION, the period, after an optional IDENTIFICATION DIVISION header) and
requires each gated header word to carry the determination or to be one of the six, both ways, so the next gated header fails a
test. A self-test proves the scan can fail. `LexerRefusalWhereFreeSet_Matches_TheRowsDeterminations` binds the generated set to
the rows and checks each row is one the gate can need.

**Goldens.** `cases/8.3.2.1-General/pb4933_free_header_words_are_user_words_85` is accepted at 85 and prints `AAAA`: the four
words are data items and every occurrence is a reference, retyped. Each construct has one refusal with its word declared:
`cases/13.9-Screen-section/pb4933-screen-section-with-an-item-named-screen` (85, COBOL1560),
`cases/13.6-Local-storage-section/pb4933-local-storage-section-with-an-item-named-local-storage` (85, COBOL0900),
`cases/12.3.8-REPOSITORY/pb4933-repository-paragraph-with-an-item-named-repository` (85, COBOL0900),
`cases/11.9-OPTIONS/pb4933-options-paragraph-with-an-item-named-options` (85, COBOL0804). In `cases/_unassigned/` are the
communication section with COMMUNICATION and CD declared (2014, 2023, COBOL0902) and the five comment paragraphs with their names
declared (2002, 2014, 2023, COBOL0902).

**Rows.** None re-verdicted. FMT-13.9.2 stays DOCUMENTED-NON-SUPPORT and the other rows keep their verdicts; the note writes
`closes_rows: []` with its reason.

**Self-review.** One mechanism, the gate's own retype, now takes a kept set. The tree walk runs only in a pass that frees a
determined word the program declares, so a program that declares none of these words parses exactly as before. A
`>>COBOL-WORDS UNDEFINE` of such a word still withdraws its header (§2023:7.3.10.4 GR3): the lexer makes the word an IDENTIFIER
before any parse, so no keyword token exists to keep.
