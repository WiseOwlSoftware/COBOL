      *>--- conformance-case
      *> id: 1985-GR-VI_6_18_4-1
      *> rules: [1985-GR-VI.6.18.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.18.4 GR1 (THE INSPECT STATEMENT). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WEAK original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. INSPLEN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  G.
           05  N   PIC 9.
           05  T   PIC X OCCURS 1 TO 5 TIMES DEPENDING ON N.
       01  C1  PIC 99 VALUE 0.
       01  C2  PIC 99 VALUE 0.
       01  C3  PIC 99 VALUE 0.
       01  C4  PIC 99 VALUE 0.
       PROCEDURE DIVISION.
       P1.
           MOVE 5 TO N.
           MOVE "A" TO T (1) T (2) T (3) T (4) T (5).
           MOVE 3 TO N.
           INSPECT G TALLYING C1 FOR CHARACTERS.
           DISPLAY C1.
           INSPECT G TALLYING C2 FOR ALL "A".
           DISPLAY C2.
           INSPECT G REPLACING ALL "A" BY "B".
           DISPLAY "[" G "]".
           INSPECT G TALLYING C3 FOR ALL "B".
           DISPLAY C3.
           INSPECT G TALLYING C4 FOR CHARACTERS
               REPLACING ALL "B" BY "C".
           DISPLAY C4.
           DISPLAY "[" G "]".
           STOP RUN.
