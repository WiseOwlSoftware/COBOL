      *>--- conformance-case
      *> id: 1985-SR-IV_4_3_8_1-p2
      *> rules: [1985-SR-IV.4.3.8.1-p2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.3.8.1 p2 (Uniqueness of Reference). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-05.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. UNIQREFC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  DUP-NAME PIC X VALUE "A" GLOBAL.
       01  GRP.
           02  OTHER-NAME PIC X VALUE "B".
       PROCEDURE DIVISION.
       P1.
           DISPLAY DUP-NAME GRP.
           STOP RUN.
