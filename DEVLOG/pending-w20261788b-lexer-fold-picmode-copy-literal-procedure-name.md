## PB4233, PB4240, PB4176, PB4229: a total PICMODE, the keyword a folded word spells, the open COPY literal, and a reserved word as a procedure-name

Wave 20261788 group B, one lexer-and-front-end root, four mechanisms, each re-probed on the build of 11e8949be before any
change.

**PB4233 (MEDIUM, under-rejection).** `01 X PIC.` then `X(3) VALUE 'ABC'.` compiled exit 0 and displayed ABC: PICMODE had no
rule for a separator period (a period followed by a space, a line end or the end of the text, ISO §8.3.5 3)), ANTLR's
default console listener printed "token recognition error" outside the diagnostics, dropped the period and stayed in the
mode, so the next line's `X(3)` became the picture. PICMODE gains `PIC_PERIOD : '.' -> type(DOT), popMode`, so every lexer
mode is total and the parser sees the §13.18.40.2 clause without its required character-string-1: COBOL5506 "expected a
picture character-string" (AdmittedKinds names PIC_STRING, §8.3.1). The main lexer now carries `LexerErrorListener` in
place of the console one; `Frontend.LexAndParseOnce` reports only the last lexer of the PB1913 debugging-line loop, as the
new COBOL5704 (a compiler defect: every mode is total). Goldens `pb4233-picture-clause-without-character-string` (refused
at every edition) and `pb4233_picture_string_then_separator_period` (accepted at 85); the warm-up lex of
`LexerDfaCacheDriftTests` now runs through `LexerErrorListener` and holds the lexer to reporting nothing on the suite's
sources (branch review R1-2: a separate test had lexed the whole corpus a third time for that question alone). DESIGN-frontend-grammar 3.3c also lost a stale SUBSCRIPT mode.

**PB4240 (wrong answer).** `RetypeDirectiveKeyword` compared a directive word with DEFINED by the host's OrdinalIgnoreCase,
so `>>IF X DEF<U+0130>NED` was COBOL1619 beside an accepted `>><U+0130>F`. The sibling the note suspected was real and wider:
the keyword rules match only the basic letters (`caseInsensitive`), so a keyword written with a letter Annex C.2 folds to a
basic one (U+0130, the Kelvin sign U+212A, the dotless i before 2023) lexed as an IDENTIFIER: `D<U+0130>SPLAY "HI"` was
COBOL0001 while `01 D<U+0130>SPLAY PIC X.` drew COBOL0901, the funnel folding by Annex C. `CobolLexer.RetypeFoldedKeyword`
(in `NextToken`, after the edition spelling, before `>>COBOL-WORDS` and every keyword decision) gives such a word the type
of the keyword its `UpperFold` spells; DEFINED compares through `CobolNames.Same`. Siblings the keyword token reached:
`WordCharacterScreen.IsCandidate` now screens it (else it compiled at COBOL-85), and three ASCII-first-letter guards
(`CobolErrorStrategy.IsReservedWord`, `IsClosedFormatResidueAhead`, `AdmittedKinds.Written`) take any letter.
`CobolNameFoldDriftTests` scans the grammars' actions too. Goldens `pb4240_keyword_spelled_with_dotted_capital_i` (2002)
and `pb4240-keyword-spelled-with-dotted-capital-i-85` (COBOL0900); `pb1965-kelvin-spelled-reserved-word` now expects
COBOL1941, what `01 KEY PIC X` draws in basic letters.

**PB4176 (diagnosis).** `COPY "ABC` drew COBOL2450 "not an alphanumeric literal" and COBOL2449 at the next line's STOP. The
COPY / REPLACE statement cursor is where every text-word of those statements is consumed, so it asks
`LiteralScreen.ReportUnterminated` (the lexer's own COBOL5563 question, §6.3.5 1), §6.4.2) of each literal there, as the
statement's first and only error. Golden `pb4176-copy-literal-not-closed`; five unit rows, one diagnostic each.

**PB4229 (diagnosis).** `PERFORM READ.` was COBOL0001 at the period at every edition: a word reserved at every edition has no
procedure-name arm, and cannot have one (a keyword in `procedureName` would compete with every statement and header
reading; `EXIT.` is both). The slot now answers on an error: `CobolParserCoreBase.ReservedProcedureName` re-reads the
sentence with one candidate word as an IDENTIFIER and, when the reading is complete with the word under `procedureName`,
`CobolErrorStrategy.Report` names it with COBOL0901, the sentence being the one fault. Every PERFORM format, both THRU ends,
qualifiers, both GO TO formats, ALTER and the section headers `DISPLAY SECTION.` / `READ SECTION.` now draw exactly one
COBOL0901; a fault the re-read cannot repair keeps its own report. Branch review R1-1 found the header reading also named the
verb of a bare incomplete statement (`STOP.`, `DISPLAY.`, `MOVE.`, `PERFORM.` inside a paragraph drew COBOL0901): that text is
also a paragraph header (§14.4.3), nothing in it shows a use as a user-defined word, so `IsIncompleteStatement` keeps the
header reading off a verb and its period alone, and the statement keeps its COBOL0001 at the period (7 new rows). Golden `pb4229-procedure-name-reserved-at-every-edition`; 24 conformance
rows.

No inventory row was claimed or re-verdicted (each row these rules map to already CONFORMS); every note writes
`closes_rows: []` with its reason. Codes: COBOL5704 used, COBOL5705 and COBOL5706 returned.
