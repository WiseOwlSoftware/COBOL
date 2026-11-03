## Entry 3025 — 2026-11-03 02:05 PST — Reserved-word tables, F.2 edition edges and front-end diagnostics that name what the user wrote; A bare verb is a header only where a procedure reference names it, a stray word belongs to the statement that wrote it, and diagnostics quote what the user wrote

### Reserved-word tables, F.2 edition edges and front-end diagnostics that name what the user wrote

### PB4297: the 2002 and 2014 flags follow the ISO texts, not GnuCOBOL's implementor words

The compiler refused 19 words as data-names at `--std 2002` and 12 at `--std 2014` with COBOL0901 citing §8.9, though
the printed 2002 §8.9 list and the 2009 draft's §8.9 hold none of them. Among them were POSITION, PROCEED, WORDS,
COBOL, the DEBUG-n registers, SEGMENT-LIMIT, COMMUNICATION at 2014 and the draft's context-sensitive
NEGATIVE-INFINITY and POSITIVE-INFINITY. The re-probe of `G5/rw-position.cob` reproduced it at 2002 and 2014. The
cause was in the generator: GnuCOBOL's per-standard word lists decided both flags, and those lists also carry
implementor words. PB656 had made that choice while the repository held no 2002 text.

Two transcribed word lists now enter the repository (words only, sources in each file's `_comment`):
`tests/version-matrix/reserved-words-2002.json`, the 2002 standard's §8.9, and `words-2009-cd.json`, the 2009
draft's §8.9 and §8.10. `scripts/gen-reserved-words.ps1` takes the 2002 flag from the 2002 list. It adds ALIGNED,
which the same text's Annex F.1 item 6 adds and its §8.9 omits. The 2014 flag stays GnuCOBOL's, less every word the
draft knows that neither its §8.9 nor 2023's reserves. A 2014-final word the draft cannot speak to (PHYSICAL) keeps
its flag. The generator asserts both lists' shape, the 2002 list against GnuCOBOL's, and the premise of its older
85/2023 interpolation. The regenerated table changed exactly the note's 19 and 12 words.

Goldens: `pb4297_1985_words_user_defined_from_2002`, `pb4297_communication_user_defined_from_2014`,
`pb4297_infinity_words_user_defined`, and the negatives `pb4297-position-reserved-at-85` and
`pb4297-communication-reserved-85-2002`. The negative `pb656-cobol-reserved-through-2014` is deleted and its witness
retired from SR-8.3.2.1-2 (batch `w20261825a-pb4297`; GAP 19 → 19). D-RW3, VCR row 7.8, two construct rows and two
design docs describe the new sources.

### PB4307: a MULTIPLE FILE clause that names no file is refused

At `--std 85`, `MULTIPLE FILE TAPE.` and `MULTIPLE FILE.` compiled clean (re-probed), while `TAPE CONTAINS.` drew
COBOL0001. GnuCOBOL refuses both under its cobol85, ibm and mf dialects. `DataBinder.BindIoControl` now asks
`ScreenMultipleFileClause` beside SAME's syntax rules, and an empty file list is the new COBOL5944 at every edition.
The grammar keeps the list optional, because a required list would fall to the I-O-CONTROL error production's false
COBOL1970. The descriptor states that the 85 format is derived, not quoted. Negatives:
`_unassigned/pb4307-multiple-file-tape-names-no-file` and `pb4307-multiple-file-names-no-file`.

### PB4279: COPY's text-name and library-name obey the COBOL-word rule

`COPY PB4279U_.` and `COPY PB4279H-.` were accepted silently, with their members present (re-probed). A text-name is a
system-name (§8.3.2.3.1) and so a COBOL word (§8.3.2.1), but the lexer never sees the COPY statement.
`WordCharacterScreen.Check`'s per-word body became `WordCharacterScreen.CheckWord`, one rule for both readers.
`CopyProcessor.ScreenCopyWord` asks it, and the length rule, of text-name-1 and library-name-1 when they are written as
words. Negatives: `8.1.3-COBOL-character-repertoire/pb4279-copy-text-name-edge-underscore` and `…-edge-hyphen` give
COBOL2773 at every edition.

The first gate (`000001-20261102T152138Z`) was RED on four `Ansi85AcceptanceTests.Word_FreesAsUserWord_AtItsTableEdition`
rows. They pinned EVERY, CLOCK-UNITS, REFERENCES and PROCEDURES as reserved through 2014, which was PB4297's old
GnuCOBOL reading, so the rows now free the words at 2002.

### PB4305: fourteen 2002 Annex F.2 relaxations are refused at COBOL-85

All fourteen compiled at `--std 85` with no edition diagnostic (re-probed). Following the owner's decision of
2026-10-31, the 2002 Annex F.2 "formerly" statements decide the 85 edge, as D-SIGN2 and PB3683 already did. Each item
is now a `constructs.json` row (introducedIn 2002, COBOL0900), and VCR row 7.47 and determination D-F2-85 record them.
Each row is gated in one place:
- `VersionConformancePass`'s parse arm handles the eleven syntactic items: a signed DISPLAY literal, EXIT PROGRAM not
  last, GLOBAL in linkage, CONVERTING TO ALL, sentences before the first paragraph, an inline PERFORM AFTER, a RELATIVE
  KEY outside ACCESS MODE, an expression subscript, a reference-modified UNSTRING sender, USE without PROCEDURE, and
  VALUE in linkage, file or EXTERNAL.
- Its bound arm handles CALL USING of a group item at levels 02-49, adjudicated in `EditionGateArmDriftTests`.
- The binder handles the alphabet-name class condition and the qualified index-name, at their resolutions.

One correction to the evidence: probe p03 passes an elementary level-05 item, which item 10 allows at 85, so the
golden passes a level-05 group. Each item has a positive at 2002 and a negative at 85, and the negative expects its
own row's text.

Gate `000002-20261102T160241Z` was RED on 166 cases. Each is a program that the repository's own tests accept at 85
and that writes one of the relaxations; CCVS-85 stayed green. In 103 suite tests the program begins its procedure
division or a section with a sentence. Each of those now gets `PB4305-LEAD.` where the compiler names the sentence, so
it keeps its 85 coverage. The two pb3505 tests drop 85, because the signed DISPLAY literal is their subject.

The next link (wave 20261826, cherry-picked onto main 1046b4cff) finished the fallout, each test kept at the edition
its subject is about. Where the relaxation IS the subject the test runs from 2002: `pb436_varying_after_from_outer`,
`pb1523_global_linkage_redefiner`, `pb2087_by_reference_group_aliasing`, `pb1643_display_literal_decimal_comma`,
`pb332_use_optional_words` and `pb919_index_name_qualified`; `pb394_evaluate_function_subscript_subject_85` is deleted
(its 2002+ twins keep the function subscript). Where it is incidental the test is rewritten into its 85 form:
`pb1390` and `pb413` nest inline PERFORM VARYINGs, `pb440_perform_empty_range_85` keeps the out-of-line VARYING alone,
`pb1913` subscripts with `W-I + 2`, and `pb571` drops its alphabet-name line. The three `pb4432` tests main gained
since get `PB4305-LEAD.`, and the C# sources at 85 write `MAIN-P.`. `IndexNameUniquenessTests` compiles every case
that writes a qualified index-name at 2002, and `OptionalWordSubsetDriftTests` sweeps USE Format 1 twice: from 2002
with PROCEDURE optional, and at 85 (`use-format-1-85`) with PROCEDURE written, the format item 141 describes. The
items 46, 75 and 133 diagnostics quote source through `WrittenSource.Of` (they printed `FUNCTIONINTEGER(1)`).
Gate `000001-20261102T170457Z` was GREEN at 68d8361e5.

### PB4435: a word where a format specifies a literal is a constant-name, which COBOL-85 lacks

`OCCURS 0 TO TIMES` at `--std 85` drew "expected a user-defined word or an integer literal" (re-probed). That word is a
constant-name (§13.10.3 SR2: "constant-name-1 may be used anywhere that a format specifies a literal of the class and
category of constant-name-1"), and the constant entry is COBOL-2002's. The walk now decides it from the ATN:
`ResimulationWalk.EveryWordStandsForALiteral` asks whether every configuration taking IDENTIFIER takes it in
`integerOperand` or `valueTableSubscript`, and `CobolParserCoreBase.PrintsConstantName` is the edition read, recorded
in `EditionReadDriftTests` under a new verdict, `DiagnosisOnly`, which `PrintsKeyword` now shares. At 85 the sentence
names "an integer literal" alone. The sibling sweep found the binder's COBOL1547 for `OCCURS K TIMES` and the PICTURE
repetition count offering a constant-name at 85 too (`DataBinder.ConstantNamesExist`), and the repetition message's
citation was §13.18.40.2 where the rule is §13.18.40.3 SR6. Goldens `pb4435-occurs-bound-omitted-at-85` and
`pb4435-occurs-bound-data-name-at-85`; `pb4301-occurs-to-without-integer-2` now refuses at 2002 and later.

### PB4284: a reserved word as a paragraph header after the division's last sentence is COBOL0901

`ZERO.`, `FILLER.`, `TALLYING.` and `SPACE.` written as a header after `STOP RUN.` drew COBOL0001 or COBOL5506, never the
COBOL0901 every other reserved header draws (re-probed). The word ends the division's paragraph list, so the parser has
left `procedureDivision` when `ReservedProcedureName` reads the error context. It now also reads the division the parse
completed last (`EndsWithAProcedureDivision`). Golden `pb4284-reserved-header-after-the-last-sentence`, and four rows
in `AWordReservedAtEveryEdition_DefinedAsAProcedure_IsNamedByCobol0901`. The first gate of this link (`000002-20261102T173130Z`) was
RED on one row: `WrittenKindRefusalTests` pinned `SPACES.` after `P1.` as the PB4224 literal-at-a-statement refusal;
written alone with its period it is now the reserved header it reads as, so the row writes `SPACES DISPLAY "A".`.

### PB4312: a diagnostic names the word the user wrote, never its fold

`>>COBOL-WORDS RESERVE "CAFÉ-"` was refused as 'CAFé-', a second `RESERVE "ÉCOLE"` as 'éCOLE', and `01 CAFÉ` after
`RESERVE "café"` as "'CAFé' is a reserved word" (re-probed): the Annex C fold lowercases an upper-case extended letter.
The fold now decides identity only. The directive keeps both words in `CobolKeywords.Spelling` (the fresh word is
compared through the fold comparers it already met), and `FlagReservedUserWord` looks the word up by its fold and
names its spelling. Goldens `pb4312-reserved-fresh-word-named-as-written` and
`pb4312-fresh-word-diagnostic-spelled-as-written`; DESIGN-frontend-grammar.md §3.13 records the determination.

### PB4283: re-verdicted, left open

The note's fix (a verb between periods is an incomplete statement, asked by the header predicate, by
`ReservedProcedureName` and by `ReservedWordUse`) was implemented and reverted. It broke PB4206's pinned case: a header
`RECEIVE.` that `PERFORM RECEIVE.` references draws one COBOL0901. From 2002 no token tells that header from a bare
`FREE.` sentence; only a procedure reference does, which the binder knows and the parse does not. The note now carries
that direction.

### A bare verb is a header only where a procedure reference names it, a stray word belongs to the statement that wrote it, and diagnostics quote what the user wrote

The operator lane ran out of quota, so the Mythos lane took over its idle branch (`worktree-wf_a45e0bcf-285-1`, finished
and gated, unreviewed) and finished its NEXT list. The merge with main had one conflict: main had split
`ReferenceResolver`, so the branch's qualified-index-name edition gate (PB4305) moved to
`NameResolution.ResolveIndexName`, where the index lookup now lives. PB2103's part 2 was dropped (main landed it).

### PB4283: a verb and its period alone is a header only where the program names the word

`FREE.`, `VALIDATE.`, `ALLOCATE.`, `RAISE.` and `INVOKE.` alone in a paragraph drew COBOL0901 ("reserved word used as a
user-defined word") at `--std 2023`, where the sentence is a statement lacking its operand. The branch's earlier fix
(`STOP.`, `DISPLAY.`, `READ.`, the verbs reserved at every edition) had decided it with no evidence, and PB4206 pins the
opposite for a header that a `PERFORM RECEIVE.` references. The two disagreed because the only evidence that the source
USES the word as a name (§8.3.2.1 1) is a procedure reference elsewhere, which the parse does not know and the bound tree does.

The decision is one now, asked in three places that were three copies of the question. `IsBareVerbSentence` (a statement
opener read off the ATN, a period on each side) and `NamedAsAProcedureElsewhere` (every other token of the word's type is
re-read in its own sentence with the word retyped `IDENTIFIER`; a reading that puts it under `procedureName` is a reference,
so `FREE ptr.` is not) answer `procedureHeaderHere` (the grammar's predicate for a gated word), `ReservedWordUse`
(no violation, so the failed prediction reports as the statement's own) and `ReservedProcedureName` (the readings of a verb
reserved everywhere). With the evidence, a word is named once, at its first use: `CobolErrorStrategy` keeps the messages it
gave (`_procedureWordsNamed`), the bound-tree funnel's rule for a gated word. `PERFORM DISPLAY.` with the header `DISPLAY.` had
drawn COBOL0901 at the reference and a COBOL0001 at the header; it draws the one COBOL0901. Without it `FREE.` draws
`cannot parse construct near 'FREE'` and `DISPLAY.` `unexpected '.'`. Goldens: two cases under `8.3.2.1-General` (the refusal at
2002, 2014 and 2023, and `VALIDATE.` as a legal header at COBOL-85) and twelve rows of
`ReservedWordPositionConformanceTests.ABareVerbSentence_IsTheHeaderOnlyWhereAProcedureReferenceNamesIt`.
`DESIGN-frontend-grammar.md`'s reserved-procedure-name paragraph is rewritten.

### PB4285 item 3: the stray word of the next statement

`MOVE W-X TO` then `ADD 1 FROM W-Y.` drew one COBOL0901 at the ADD and lost the ADD's own fault: `ADD 1` parses as a prefix
and FROM follows no statement, so the statement reading was None. The weighing the note proposed (every refusal of two tokens
or more, as `NameReach` weighs one) made `MOVE FREE W-X TO W-Y.` a statement too, which PB3248's R2-1 rows pin as the operand.
What tells them apart is whether the sentence needs the token the prefix stopped at: `MOVE X TO W-Y.` reads whole with the prefix
as one operand, `MOVE W-X TO X FROM W-Y.` does not. `ReadingParses` reports where a reading stopped and whether the stop is a
token the rule is followed by nowhere; `StatementReading.CutShort` is `Faulted` and covers every stop; a stray stop asks
`SentenceReadsWithOneOperandFor`. After: the missing receiver (COBOL5506) and the ADD's stray FROM (COBOL2269), no COBOL0901.
Item 2 is discharged: a statement reading that ends at another verb's phrase word (`... FREE W-Y ELSE`) is complete exactly as
at the period, and no wrong report exists to build a repro from.

### PB4310 and PB4431 and PB4434

`CompilerDirectiveLine.TryParse` no longer trims a trailing CR (owner decision 2026-10-31, DOC-A.1-156): `>>SOURCE FORMAT
FIXED` + CR + CR + LF switched the format and now draws COBOL1911, as the same CR on a source line is refused. The two dead tab
rules are deleted, and `PhysicalLinesDriftTests` enumerates the `WiseOwl.Cobol.Editions` assembly, which had been outside its
population. CallBinder quoted five operand nodes by `GetText()` (`AOFG`, a glued qualified name) in its BY VALUE and bare-argument
messages, and looked a qualified SCREEN SECTION name up by its glued text, so it never matched; all quote `WrittenText` now. The
other binders still hold about ninety such messages (PB4252's analysis). PB4434's stale `GroupRepository.Find` prose in
`DESIGN-external-repository.md` names `GroupRepositoryProvider`; the `SearchAllFormat2Rules` comment was already current.

### Branch review round 1 (R1-1, R1-2)

R1-1: the bare-verb question was quadratic on conforming source. A legal `GOBACK.` (a reservation-gated verb alone in its
sentence) reaches `procedureHeaderHere(DOT)` at every such sentence, and `NamedAsAProcedureElsewhere` scanned the whole stream
and ran a speculative sentence read of every OTHER GOBACK for each: 800 `Pn. GOBACK.` paragraphs took 36 s (main 3 s). The
evidence is now gathered once per word type (`ProcedureReferencesToTheWord`), and a token that is itself a bare verb sentence
is no reference without a read (`VERB.` is no sentence when the word is a user-defined one), so the 800 paragraphs compile in
2 s with no read at all. The guard is a count, never a clock: `CobolParserCoreBase.ProcedureNameReadsMade`, asserted 0 for
60 GOBACK paragraphs and 1 for a program with one `PERFORM GOBACK.` (`BareVerbSentenceEvidenceTests`).

R1-2: F.2 item 90's gate skipped declarative sections on the ground that they begin with the USE statement, but ISO §14.2.1
Format 1 prints `use-statement. [ sentence ] … [ paragraph-name-1. …]`: the sentences AFTER the USE statement and before the
first paragraph are the same COBOL-85 restriction. `VisitDeclarativeSection` now asks the gate of them (`--std 85` draws
COBOL0900 at the second sentence). Goldens: `pb4305_declarative_sentence_before_first_paragraph_2002` (2002/2014/2023, "D1 35"
then "DONE") and `pb4305-declarative-sentence-before-first-paragraph-at-85`. The whole-population gate found the one corpus
program that wrote the form at 85, `pb990_user_word_types_one_per_word` (its H section has a sentence after the USE sentence,
`accept: [85]`; it also failed `RepositoryRoundTripCorpusTests` and `HostEntryShapeDriftTests`): the form is the 2002 relaxation,
so the golden is now accepted from 2002 on. R1-3 (a lead: the item-108 gate reports at the
SEARCH statement, not at the qualified index-name of VARYING) is left for the clerk, `NameResolution.cs` not being a file this
round changes.

