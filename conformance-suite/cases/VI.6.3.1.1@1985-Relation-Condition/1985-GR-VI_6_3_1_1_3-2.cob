      *>--- conformance-case
      *> id: 1985-GR-VI_6_3_1_1_3-2
      *> rules: [1985-GR-VI.6.3.1.1.3-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.3.1.1.3 GR2 (Relation Condition). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RELIDX.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  TBL.
           05  ELEM PIC X OCCURS 5 TIMES INDEXED BY IX.
       01  NUM PIC 9 VALUE 3.
       PROCEDURE DIVISION.
       P1.
           SET IX TO 3.
           IF IX = 3
               DISPLAY "LIT EQUAL"
           ELSE
               DISPLAY "LIT NOT EQUAL".
           IF IX = NUM
               DISPLAY "ITEM EQUAL"
           ELSE
               DISPLAY "ITEM NOT EQUAL".
           IF IX < 4
               DISPLAY "LESS 4"
           ELSE
               DISPLAY "NOT LESS 4".
           IF IX > 2
               DISPLAY "GREATER 2"
           ELSE
               DISPLAY "NOT GREATER 2".
           STOP RUN.
