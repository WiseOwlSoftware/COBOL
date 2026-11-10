      *>--- conformance-case
      *> id: 1985-SR-VI_5_9_3-2
      *> rules: [1985-SR-VI.5.9.3-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.5.9.3 SR2 (THE PICTURE CLAUSE). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PICCAT.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  NUM-ITEM   PIC 9(3).
       01  ALPH-ITEM  PIC X(3).
       01  EDIT-ITEM  PIC ZZ9.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE 7 TO NUM-ITEM.
           MOVE "7" TO ALPH-ITEM.
           MOVE 7 TO EDIT-ITEM.
           DISPLAY "N=" NUM-ITEM "|".
           DISPLAY "A=" ALPH-ITEM "|".
           DISPLAY "E=" EDIT-ITEM "|".
           STOP RUN.
