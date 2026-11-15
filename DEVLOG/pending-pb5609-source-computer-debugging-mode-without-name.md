## PB5609: at 2002, SOURCE-COMPUTER takes WITH DEBUGGING MODE without a computer-name

kb/Work PB5609 (wave 20262045, group H). The grammar nested the DEBUGGING MODE clause inside the computer-name, the 1985
shape (`§1985:VI.4.3.2`: `[ computer-name [ WITH DEBUGGING MODE ] . ]`), at every edition. The 2002 format, rendered from
the printed page (PDF p212 / folio 190), has two independent brackets: `§2002:12.2.4.1`
`SOURCE-COMPUTER. [ computer-name-1 ] [ WITH DEBUGGING MODE ] .`. The re-probe on the base showed
`SOURCE-COMPUTER. WITH DEBUGGING MODE.` refused with COBOL1971 at 85, 2002, 2014 and 2023, and in a 2002 method the parse
error stood in for COBOL2644.

**Fix.** `sourceComputerParagraph` parses the clause after the name or without one. The name-less clause is the same 2002
relaxation as the name-less OBJECT-COMPUTER clauses, so `VisitSourceComputerParagraph` asks the one row
`computer-name-optional-2002` (COBOL0900 at 85), whose display, description and citation now name both paragraphs; the
clause's own row still makes it obsolete at 2002 (COBOL0903) and removed at 2014 (COBOL0902). `DebuggingLineRewriter`
already read the clause off the tokens whether or not a name preceded it, so the clause's GR4 effect needed no change.
The ParagraphPeriods text for SOURCE-COMPUTER now names the clause (`§2002:12.2.4.2 SR1`). After the fix: 85 COBOL0900;
2002 compiles with COBOL0903 and prints NO-NAME, DEBUG-ON; 2014 and 2023 COBOL0902; a missing period is COBOL3408 and a
trailing word COBOL1970 at every edition; a 2002 method is COBOL2644.

**Goldens.** `cases/12.2.4@2002-SOURCE-COMPUTER-PARAGRAPH/pb5609_debugging_mode_without_name` (accepted at 2002, with a
contained program that inherits the clause, `§2002:12.2.4.3 GR1`), `pb5609-debugging-mode-without-name-at-85` (COBOL0900),
and `cases/12.2.2@2002-CONFIGURATION-SECTION/pb5609-method-debugging-mode-without-name` (COBOL2644, `§2002:12.2.2 SR2`).

**Rows.** `2002-GR-12.2.4.3-3` and `2002-SR-12.2.2-2` recorded CONFORMS (GAP 2169 to 2167). The design doc
(DESIGN-frontend-grammar §3.3e) states the new rule shape.

**Sibling sweep.** OBJECT-COMPUTER already takes clauses without a name (PB78); SPECIAL-NAMES has no name slot; no other
rule hangs a clause off `computerName`.
