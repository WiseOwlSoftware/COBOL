## PB4233, PB4240, PB4176, PB4229: a total PICMODE, keywords on the basic letters alone, the open COPY literal, and a reserved word as a procedure-name

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

**PB4240 (wrong answer), to the owner's direction.** `>>IF X DEF<U+0130>NED` was COBOL1619 beside an accepted `>><U+0130>F` and
`>>END-<U+0130>F`. The owner ruled on 2026-10-31 (GAP-REVIEW question 2; kb/Work PB4240's addendum, PB4312) that §8.1.3.2 4)
folds extended letters in user-defined words only and a keyword matches on the basic letters alone (3) a)), so the
outlier was the accepted directive, not the refused DEFINED. The branch's first fix went the other way: it made
`D<U+0130>SPLAY "HI"` the DISPLAY verb by retyping a folded word as its keyword. Review round 1 blocked it (R1-1), and the
finisher deleted that mechanism whole. The root of `>><U+0130>F` was `CompilerDirectiveLine`, which wrote a directive word
as its Annex C `UpperFold`, so every stage after it read IF. One class, `Editions.CobolKeywords`, now holds the keyword
reading: basic letters of either case, every other character only itself. The directive word, the directive catalog and
every directive-word table use it, and so do the operand keywords of seven directives, the division cursor, the
comment-entry paragraphs, the COPY phrase words, the lexer probe's map and the lexer's DEFINED (which had compared by the
host's ordinal-ignore-case, which raises the dotless i and the long s to I and S). Four goldens are refused at 2002, 2014
and 2023: the verb (COBOL0001), DEFINED (COBOL1619), the directive (COBOL0001), and the user-word half, where
`01 D<U+0130>SPLAY` folds to the reserved `display` (COBOL0901). `pb1965-kelvin-spelled-reserved-word` again expects COBOL0901.
`CobolNameFoldDriftTests` gains the rule that no Frontend or Editions line compares a keyword literal by the fold. PB4312,
the diagnostics printing the folded spelling, stays open.
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

**PB4322 (the landing finisher, its gate's red).** The gate on c4aac6fe3 went RED on one Unit case,
`ProcessObservationDriftTests.ASlowProcess_UnderOneUnit_…`. The case ran a 3 s child against a 1 s budget whose clock
starts only at `WaitForExit`, and on a host 85 % busy the starved test host saw the child exit before the wait began:
a wall-clock race, not a regression in the observer. The witness now runs a child that never ends by itself (`ping -t`,
`exec sleep infinity`), which also hardens `RunsForever`'s two other users. The case is renamed
`AProcessPastOneUnit_…`, and the whole-population gate on 94b05879c is GREEN on both hosts.

**PB4240, branch review round 2 (R2-1).** The first fix's sweep looked for a keyword LITERAL beside a `CobolNames` comparison and
missed the by-name classifier, which is handed the keyword as a parameter: `CobolParserCoreBase.Word` -> `CobolWordsRewriter.TokenIs` ->
`CobolWordsMap.Is`. So `OBJECT-COMPUTER. X CLASS<U+0130>F<U+0130>CAT<U+0130>ON IS USER-DEFAULT.` still compiled as CLASSIFICATION and
`SEGMENT-L<U+0130>M<U+0130>T IS 50` as SEGMENT-LIMIT (both re-probed on the merged build). `Is`, `TokenIs` and `CanonicalWordOf` now compare
through `CobolKeywords`, and `CobolWordsMap.Resolve` returns the word in its `CobolKeywords.Spelling`, so none of its callers folds the
word before asking (they did, which made the extended letter a basic one before the map could tell). The map's UNDEFINE and
SUBSTITUTE tables are keyed by `CobolKeywords.Comparer`; the synonym table, keyed by a fresh user-defined word, stays on the fold, and
`ReservedWordSet.Compose` re-keys the de-reserved words by the fold because it answers the user-word question. The same mechanism
read by other callers is fixed with it: `CompilationUnitStart.IsAt` (its "IDENTIFICATION DIVISION" literal holds a space, which the
drift rule's pattern could not see), the `LC_` test of the locale category, `EmbeddedBlockReading`, the glued-keyword test of
`CopyProcessor`, `ConditionalCompilationProcessor.EndsWithWord`, the existing word of a `>>COBOL-WORDS` directive (round 1's lead (b):
`UNDEFINE "D<U+0130>SPLAY"` undefined DISPLAY; it is now written in its keyword spelling and SR3 refuses it), and the intrinsic function
catalog (`FUNCTION <U+0130>NTEGER(1.5)` compiled; the catalog is keyed by `CobolKeywords.Comparer`). Goldens, refused: `pb4240-context-word-spelled-with-dotted-capital-i`
(COBOL1970), `pb4240-function-name-spelled-with-dotted-capital-i` (COBOL1501), `pb4240-cobol-words-existing-word-spelled-with-dotted-capital-i`
(COBOL1623). The drift rule `NoKeywordComparedByTheFold_InTheTextStages` now also sees a keyword passed as a parameter named
`keyword` or `word` and a literal with an underscore or a space, and `HostUpperKey` refuses `Resolve(CobolNames.UpperFold(`. Left open
and named in PB4240: the Compiler project's direct comparisons of a reserved word, system-name or implementor-name with a literal
(about thirty sites).
