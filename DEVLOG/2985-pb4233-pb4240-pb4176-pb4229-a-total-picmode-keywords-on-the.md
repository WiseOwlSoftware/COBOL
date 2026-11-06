## Entry 2985 — 2026-11-01 23:17 PST — PB4233, PB4240, PB4176, PB4229: a total PICMODE, keywords on the basic letters alone, the open COPY literal, and a reserved word as a procedure-name; PB4420: the fold of a user-defined word applies only an extended letter's own case pair

### PB4233, PB4240, PB4176, PB4229: a total PICMODE, keywords on the basic letters alone, the open COPY literal, and a reserved word as a procedure-name

Wave 20261788 group B, one lexer-and-front-end root, four mechanisms, each re-probed on the build of 33f0f7a6a before any
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

**PB4322 (the landing finisher, its gate's red).** The gate on 0b16801ac went RED on one Unit case,
`ProcessObservationDriftTests.ASlowProcess_UnderOneUnit_…`. The case ran a 3 s child against a 1 s budget whose clock
starts only at `WaitForExit`, and on a host 85 % busy the starved test host saw the child exit before the wait began:
a wall-clock race, not a regression in the observer. The witness now runs a child that never ends by itself (`ping -t`,
`exec sleep infinity`), which also hardens `RunsForever`'s two other users. The case is renamed
`AProcessPastOneUnit_…`, and the whole-population gate on 9f8204070 is GREEN on both hosts.

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

Review round 3 (the second finisher): the branch review found that re-keying the intrinsic catalog by the basic letters made the
user-word declaration screen (`DataBinder.DeclareUserWord`) stop folding, so under `FUNCTION ALL INTRINSIC` (or `FUNCTION INTEGER INTRINSIC`) a
data-name spelled `<U+0130>NTEGER` compiled and ran, though a user-defined word is compared in the Annex C fold and the basic spelling is refused
(COBOL1649; ISO §8.3.2.1 5), §12.3.8.3 SR12, SR13). `FunctionWord.OfUserWord` folds the user word before the directive is applied and is the declaration
funnel's one call; function names written as keywords keep the basic-letter match. Two goldens pin the ALL and the named specifier at 2002, 2014 and 2023.
The review's lead is fixed too: `CobolWordsMap.Is` no longer goes through `Resolve`'s spelled copy (`Denoted` is the one reading of GR2-GR4), so
its allocation-free comment is true again.

### PB4420: the fold of a user-defined word applies only an extended letter's own case pair

**What the owner decided (2026-11-01, "Follow gnucobol").** ISO §8.1.3.2 4) b) folds an uppercase extended letter to "its
corresponding lowercase extended letter, if any". Annex C.2 lists two pairs whose lowercase is a BASIC letter, `(0130,0069)` LATIN
CAPITAL LETTER I WITH DOT ABOVE to `i` and `(212A,006B)` KELVIN SIGN to `k`; GnuCOBOL 3.2 compares extended letters byte for
byte, so `CUSTOMER-<U+0130>D` and `CUSTOMER-ID` are two names there. Under CLAUDE.md rule 1 (ISO, then GnuCOBOL) the standard's
wording leaves that latitude, and WiseOwl now follows GnuCOBOL.

**Re-probe on a Debug build of main (19:32 PST).** `01 CUSTOMER-<U+0130>D` beside `01 CUSTOMER-ID`: COBOL1639 on both
references ("2 declarations share the name"). `CUSTOMER-<U+0130>D` alone read as `CUSTOMER-ID`: printed its value (one name,
silent wrong answer). `WORK-<U+212A>` read as `WORK-K`: printed. The four compiler sites the PB4240 finisher listed
accepted or refused wrongly the same way: a constant `F<U+0130>LLER` was FILLER (COBOL1547 + COBOL0901), `L<U+0130>NE-COUNTER`
a report subscript was the barred register (COBOL2751 + COBOL0901), `CONVERT(XM ANY NAT<U+0130>ONAL HEX)` was NATIONAL, and
`XM IS NUMER<U+0130>C` drew COBOL0901. After the change: two items (`DOTI`, `BASI`), the other spelling COBOL1639, the constant and the
subscript compile as ordinary user words, the CONVERT format word is COBOL1514 and the class condition COBOL1639 alone.

**The shape.** The exclusion is derived, not listed. `scripts/spec/gen_annex_bc.py` takes the three pair lists it transcribes (C.2,
E.2 item 14, E.3.3 item 6), moves every pair whose letters are not both basic or both extended into a new list,
`CobolNames.NotApplied`, and emits the other pairs as before, so `CobolNames.Fold`, `Same`, `UpperFold` and every fold-keyed table
needed no change. The derived set is the two pairs above and, in the 2002/2014 table, `(0131,0069)`: the dotless i is the same
pair class (extended to basic), so below 2023 it no longer folds to `i` either, and `EditionSpelling` now respells only final
sigma. The note named the first two; the third follows from the rule and is the one reading of 4) b) at every edition (the
2002 and 2014 wording is the same).

**Proof that no keyword equals an extended spelling through the fold.** `AnnexBCDriftTests` (a) re-reads the three lists from the
spec and requires the applied lists to be the standard's less the crossing pairs and `NotApplied` to be those pairs, derived in
the test independently of the generator; (b) pins the measured set; (c) requires that no character at or above U+0080 folds to
an ASCII character at 2002, 2014 or 2023 (`NoCharacterAboveAscii_FoldsToAscii`); (d) requires every keyword population (the
§8.9 reserved words, §8.10 context-sensitive words, the intrinsic function names, the implementor names and the §8.12 directive
words) to be basic characters only (`EveryKeywordPopulation_IsBasicCharactersOnly`). With (c) and (d) the roughly thirty
compiler sites that ask `CobolNames.Same` / `UpperFold` about a keyword literal (the PB4240 lead) and the fold-keyed word tables
answer exactly as the basic-letter comparison does, so they stay.

**Dead PB4240-era code removed.** `FunctionWord.OfUserWord` (the third factory, which folded the user word and then asked the
function catalog; the declaration funnel `DeclareUserWord` now asks `OfWrittenWord`), the fold-then-keyword guards in
`VersionConformancePass.ValidateCobolWords` and `IsReservedContextOrIntrinsic`, and `ReservedWordSet.Compose`'s re-keying of the
de-reserved words by the fold. `CobolKeywords` stays as the keyword question's home (no table, allocation-free, the directive
words' spelling as written); its comment and `CobolWordsMap`'s no longer claim that the fold turns U+0130 into `i`.

**Goldens** (`cases/8.1.3-COBOL-character-repertoire/`, each quoted from the gate's `Conformance.trx` below):
`pb4420-dotted-capital-i-name-is-a-word-of-its-own` (three editions), `pb4420-kelvin-sign-name-is-a-word-of-its-own`,
`pb4420-dotted-capital-i-name-other-spelling-undefined` and `pb4420-kelvin-sign-name-other-spelling-undefined` (COBOL1639),
four site goldens (`pb4420-filler-...-constant-name`, `pb4420-line-counter-...-user-word`, `pb4420-convert-format-word-...`,
`pb4420-class-condition-keyword-...`), and the four former refusals now acceptances (`pb4240-reserved-word-...`,
`pb4240-all-intrinsic-name-...`, `pb4240-named-intrinsic-name-...` and `pb1965-kelvin-spelled-reserved-word`, deleted with their
refusal files and re-written as `pb4420-...-is-a-user-word`). `pb1402_extended_letters` (the dotless i and `ITEM-A` are two
items), `pb1402_extended_letters_2023` (the Kelvin sign is its own word) and `pb1965_annex_c_word_keys` (the EQUATE synonym is
`K<U+00DC>P`/`K<U+00FC>P`) are re-derived; `ParameterVariableNameTests`, `PointerImageTests` and `CobolNameFoldDriftTests` follow.

**Docs.** The `CobolNames` class comment (it stated the opposite), DESIGN-frontend-grammar 3.13, DESIGN-cobol-words-directive,
and docs/CONFORMANCE.md DOC-A.1-20 (the 4 b) determination, GnuCOBOL by the precedence). Inventory rows GR-8.1.3.2-4 and
DOC-A.1-20 stay CONFORMS, restated by a `record_verdicts` batch (GAP 19 to 19; the Kelvin refusal golden retired).

**Gate.** Run `000002-20261102T065957Z` on the code head (run 000001 was RED on one finding of my own, `AnnexA1RegisterDriftTests`: a prose
mention of the row key `DOC-A.1-20` in CONFORMANCE.md; reworded):
`=== BUILD-LOCAL GATE: GREEN — Conformance 20,725/20,725 · Unit 39,210/39,210 · Characterization 37/37 cases ran (skipped 0) ===`.
`Conformance.trx` holds 28 `UnitTestResult`s for the new ids, all Passed (18 accepted-edition runs, 10 refusals). `semgrep/verify.py`: PASS,
no rule's count rose. Left open and registered: PB4312's U+0130/U+212A examples no longer reproduce (the diagnostics print the word as
written because the fold no longer maps them), and `CobolWordsDirectiveProcessor.Spelled`'s keyword/user-word split now differs only in
how an extended letter is spelled in a diagnostic.

