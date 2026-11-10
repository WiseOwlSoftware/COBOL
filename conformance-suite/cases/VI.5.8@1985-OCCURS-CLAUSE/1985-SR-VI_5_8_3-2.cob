      *>--- conformance-case
      *> id: 1985-SR-VI_5_8_3-2
      *> rules: [1985-SR-VI.5.8.3-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.5.8.3 SR2 (THE OCCURS CLAUSE). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. OCCQUAL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  GRP-A.
           05  CNT  PIC 9 VALUE 3.
       01  GRP-B.
           05  CNT  PIC 9 VALUE 5.
       01  TBL.
           05  ITEM PIC X OCCURS 1 TO 9 TIMES
                   DEPENDING ON CNT OF GRP-A.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE "A" TO ITEM (1).
           MOVE "B" TO ITEM (2).
           MOVE "C" TO ITEM (3).
           DISPLAY CNT OF GRP-A.
           DISPLAY TBL.
           STOP RUN.
