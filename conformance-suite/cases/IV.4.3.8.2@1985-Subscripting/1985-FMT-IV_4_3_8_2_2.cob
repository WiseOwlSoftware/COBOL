      *>--- conformance-case
      *> id: 1985-FMT-IV_4_3_8_2_2
      *> rules: [1985-FMT-IV.4.3.8.2.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.3.8.2.2 (Subscripting). An output test: the program
      *> must translate and run at 85 and DISPLAY exactly the lines its
      *> .out holds, the outcome the rule requires. Placed from the
      *> audit's draft, confirmed by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SUBTST.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  TBL.
           05  ELEM PIC X OCCURS 5 TIMES INDEXED BY IX.
       01  N PIC 9 VALUE 2.
       PROCEDURE DIVISION.
       P1.
           MOVE "ABCDE" TO TBL.
           SET IX TO 2.
           DISPLAY ELEM (1).
           DISPLAY ELEM (N + 1).
           DISPLAY ELEM (IX + 3).
           DISPLAY ELEM (N - 1).
           STOP RUN.
