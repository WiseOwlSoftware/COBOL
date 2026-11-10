      *>--- conformance-case
      *> id: 1985-GR-VI_5_8_4-2
      *> rules: [1985-GR-VI.5.8.4-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.5.8.4 GR2 (THE OCCURS CLAUSE). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WEAK original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. OCC1.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       77  K PIC 9 VALUE 4.
       01  G.
           05  E PIC X OCCURS 3 TIMES.
       01  H.
           05  U PIC X OCCURS 1 TO 5 TIMES DEPENDING ON K
                   INDEXED BY UX.
       PROCEDURE DIVISION.
       P1.
           MOVE "ABC" TO G.
           DISPLAY E (1) E (2) E (3).
           DISPLAY E (3).
           MOVE "VWXY" TO H.
           DISPLAY "[" H "]".
           PERFORM P-FIND.
           MOVE 2 TO K.
           DISPLAY "[" H "]".
           PERFORM P-FIND.
           STOP RUN.
       P-FIND.
           SET UX TO 1.
           SEARCH U
               AT END DISPLAY "X NOT AN OCCURRENCE"
               WHEN U (UX) = "X" DISPLAY "X IS AN OCCURRENCE".
