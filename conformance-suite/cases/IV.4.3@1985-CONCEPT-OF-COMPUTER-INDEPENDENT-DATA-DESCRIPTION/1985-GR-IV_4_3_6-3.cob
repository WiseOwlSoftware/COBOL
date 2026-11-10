      *>--- conformance-case
      *> id: 1985-GR-IV_4_3_6-3
      *> rules: [1985-GR-IV.4.3.6-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.3.6 GR3 (CONCEPT OF COMPUTER INDEPENDENT DATA
      *> DESCRIPTION). An output test: the program must translate and
      *> run at 85 and DISPLAY exactly the lines its .out holds, the
      *> outcome the rule requires. Placed from the audit's draft,
      *> confirmed by scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. ALIGNT.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  SHORT-ITEM PIC X(3).
       01  LONG-ITEM  PIC X(8).
       01  ALPHA-ITEM PIC A(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE "ABCDE" TO SHORT-ITEM.
           MOVE "XY" TO LONG-ITEM.
           MOVE "PQ" TO ALPHA-ITEM.
           DISPLAY "[" SHORT-ITEM "]".
           DISPLAY "[" LONG-ITEM "]".
           DISPLAY "[" ALPHA-ITEM "]".
           STOP RUN.
