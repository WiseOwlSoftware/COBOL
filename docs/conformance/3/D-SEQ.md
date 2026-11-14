- **D-SEQ — the statement-sequence placement rules, and what "a discreet block of code" is** (kb/Work PB397).
  Three syntax rules are stated over a SEQUENCE of statements and are refused at compile time at every edition
  (none carries an edition qualifier): §2023:14.9.14.3 SR1 — a Format 1 (bare) EXIT "shall appear in a sentence by itself
  that shall be the only sentence in the paragraph or in a section without paragraphs" (COBOL2966); §2023:14.9.17.3 SR2
  — a Format 1 GO TO appearing "in a consecutive sequence of imperative statements within a sentence … shall appear as
  the last statement in that sequence" (COBOL2967); §2023:14.9.42.3 SR1 — "The STOP statement shall be specified only as
  the last statement in any discreet block of code" (COBOL2968). **(a) The sequence** is a consecutive run of
  IMPERATIVE statements inside one sentence, or inside one phrase of another statement (an IF THEN or ELSE phrase, an
  inline PERFORM body, an ON SIZE ERROR phrase) (kb/Work PB2610). A CONDITIONAL statement ends the run: §2023:14.5.1 says "Any
  statement with a conditional phrase that is not terminated by its explicit scope terminator is a conditional
  statement", and the rule binds only "a consecutive sequence of imperative statements". So `GO TO P IF A DISPLAY "Y".`
  and `GO TO P ADD 1 TO A ON SIZE ERROR DISPLAY "Z".` are legal. `GO TO P IF A DISPLAY "Y" END-IF.` is refused,
  because the delimited IF is imperative (§2023:14.5.3.2). `IF A GO TO P END-IF DISPLAY "X"` is legal, because the GO TO
  ends its own phrase, and `IF A GO TO P DISPLAY "X" END-IF` is refused. **(b) "A discreet block of code"**
  is undefined by the standard (the spelling is the printed one); the implementor latitude is settled by CLAUDE.md
  rule 1's order: the standard does not decide, GnuCOBOL 3.2 compiles all four violating shapes without a diagnostic (measured), and IBM Enterprise COBOL and Micro Focus
  document the STOP rule as a statement last in "a sequence of imperative statements within a sentence" — so the block
  is read as the SAME sequence §2023:14.9.17.3 SR2 names, for STOP RUN, including (a)'s end at a conditional statement:
  `STOP RUN IF A DISPLAY "Y".` is accepted, as IBM's and Micro Focus's wording ("a sequence of imperative statements")
  admits. "The STOP statement" of §2023:14.9.42 is STOP RUN alone;
  the X3.23-1985 STOP literal (deleted 2002) communicates to the operator and then CONTINUES with the next statement,
  so a statement after it executes and it is not governed (`STOP "HALT" DISPLAY "A".` compiles at 85). A narrower reading (only sentence-level
  sequences) would admit `IF A STOP RUN DISPLAY "X" END-IF`, whose DISPLAY can never execute. **(c) Format 1 GO TO**
  includes the 1985 target-less `GO TO.` (that edition's Format 1; its procedure-name is optional there); the
  Format 2 DEPENDING GO TO is not constrained, since control falls through when no name is selected (§2023:14.9.17.4 GR2).
  **(d) An EXIT inside another statement** (`IF A EXIT END-IF`) is not "in a sentence by itself": the sentence is the
  IF. A paragraph is §2023:14.4.3's, so the sentences after a section header or the procedure division header form one
  (unnamed) paragraph and a section without paragraphs is one such paragraph. Witnesses:
  `conformance:cases/14.9.14-EXIT/pb397-exit-not-alone`, `conformance:cases/14.9.17-GO-TO/pb397-go-to-not-last`,
  `conformance:cases/14.9.42-STOP/pb397-stop-not-last`, `conformance:cases/14.9.14-EXIT/pb397_sequence_placement_legal`,
  `conformance:cases/14.9.17-GO-TO/pb2610_go_to_before_conditional`,
  `conformance-test:StatementSequencePlacementTests`.
