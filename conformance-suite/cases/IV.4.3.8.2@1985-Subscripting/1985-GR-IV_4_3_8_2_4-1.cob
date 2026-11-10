      *>--- conformance-case
      *> id: 1985-GR-IV_4_3_8_2_4-1
      *> rules: [1985-GR-IV.4.3.8.2.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.3.8.2.4 GR1 (Subscripting). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SUBTST.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  TBL.
           05  ELT PIC X OCCURS 3 TIMES.
       01  I PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       P1.
           MOVE "ABC" TO TBL.
           DISPLAY ELT (1).
           MOVE 3 TO I.
           DISPLAY ELT (I).
           MOVE 2 TO I.
           DISPLAY ELT (I).
           STOP RUN.
