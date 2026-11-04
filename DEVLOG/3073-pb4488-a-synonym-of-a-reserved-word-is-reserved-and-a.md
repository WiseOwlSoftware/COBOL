## Entry 3073 — 2026-11-03 23:04 PST — PB4488: a synonym of a reserved word is reserved, and a synonym of COPY or REPLACE begins no statement

Wave 20261858, group B. The wave 20261857 determination had already shown that the note's own question (should the COPY and
REPLACE finders read the word map?) had a plain answer: no. ISO 7.3.10.4 GR6 puts the compiler directing statements outside
the directive, and the stage order of 7.2.1 agrees (COPY in Step 1, COPY REPLACING in Step 2, REPLACE in Step 3, the
directive last, in Step 4). `CPY BOOK.` after `EQUATE "COPY" WITH "CPY"` is therefore not legal source, and refusing it was
right. Two defects with one root remained, and this entry fixes both.

**Re-probe.** Built the tree at 385231e27 and ran the determination's probes: p01, p16, p23 (the COBOL0307 "a period may be
missing" hint after a synonym of COPY, in source text, in library text and after SUBSTITUTE), p26 (a bare COBOL0001 at `RPL OFF.`),
and p21, p27, p30, p35, p40 (`01 CPY`, `01 LOC`, `01 RPL` compiled and ran and printed `C`). All reproduced.

**Root.** `CobolWordsRewriter.Plan.For` installs a synonym only where the original word has a lexer token, and
`ReservedWordSet.Compose` reserved only RESERVE's literal-6. A synonym of one of the ten 8.9 words the lexer has no token
for (COPY, REPLACE, LOCALE, NESTED, ANYCASE, SYSTEM-DEFAULT, USER-DEFAULT, EXCEPTION-OBJECT, COMMA, MINUS) stayed a plain
IDENTIFIER, so under GR2 and GR4 (a synonym is "treated as a synonym for" the original) and 8.3.2.1 rule 1 (reserved words are
no user-defined words) the declaration should have been refused and was not.

**Fix.**
- `CobolWordsMap.ReservedSynonyms` maps every EQUATE literal-2 and SUBSTITUTE literal-5 whose original 8.9 reserves (at the
  directive's edition) to that original. `ReservedWordSet.Compose` reserves them as it reserves RESERVE's words (verdict
  `Removed`: the program's own directive made the word reserved), and `ReservedWordSet.UserWordViolation` words the COBOL0901
  for a synonym ("'CPY' is a synonym for the reserved word COPY ..."), the one sentence both reporters build. A synonym of a
  context-sensitive word or an intrinsic function name stays what it was; a synonym of a word the lexer retypes is already its
  keyword token and the set never decides it.
- `CobolWordsMap.CompilerDirectingStatementWords` is the one owner of the pair COPY and REPLACE (7.2.2.2).
  `CobolErrorListener.NoteDirectingStatementSynonym` reports the new warning COBOL6085 beside a syntax error whose offending
  token, or the one before it, is a synonym of one of them: it names the directive and GR6, once per directive, at the
  directive. `CobolErrorStrategy` (hint 14) no longer guesses a missing period when the word before the faulting token is such a
  synonym.
- `DESIGN-cobol-words-directive.md` states both rules (the GR6 bullet and the reserve overlay), and corrects the PB4281 note's
  parenthesis that listed COPY and REPLACE among the words a directive can make the comment-entry cursor respell.

**Witnesses.** `CobolWordsReachDriftTests.EveryReservedWordsSynonym_RejectsAsAUserWord_OnBothSidesOfTheLexerPartition` takes every
8.9 word of the COBOL-2023 list, by EQUATE and by SUBSTITUTE, and requires the overlay to refuse its synonym, so the next reserved
word needs no edit; its inverse covers a synonym of an 8.10 word and of an intrinsic. `CobolWordsDirectiveTests` runs the
thirteen declarations through the compiler, the paragraph-name slot, the controls (`01 LOCALE` stays legal after SUBSTITUTE
"LOCALE" BY "LOC"), the COBOL6085 note with no period hint, and the case with no such synonym. Conformance suite, under
`cases/7.3.10-COBOL-WORDS/`: one positive (`pb4488_cobol_words_directing_statements_precede_the_directive`: COPY and a RESERVEd
text-name survive the directive, REPLACE matches DISPLAY and not its synonym SHOW, Step 4 reaches library text and replaced
text, the INSPECT phrase words follow their synonyms; expected output derived from 7.2.1 and 7.2.4.4 8) b) before it was run)
and six negatives (`pb4488-*`). Inventory rows GR-7.3.10.4-2, -4 and -6 were already CONFORMS; they gained these witnesses
(`record_verdicts`, GAP 1 to 1).

**Found on the way.** `01 DISPLAY PIC X.` compiles because DISPLAY there is the optional USAGE keyword of an unnamed item, not a
data-name; the determination's control p29 is therefore refused only at the use, and the first draft of a unit test that
expected a declaration refusal was wrong, not the compiler. The anonymous COBOL0001 for `>>SRC FORMAT` after an EQUATE of
SOURCE stays kb/Work PB1575's.

