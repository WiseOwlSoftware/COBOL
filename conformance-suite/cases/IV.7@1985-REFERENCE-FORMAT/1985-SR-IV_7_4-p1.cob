      *>--- conformance-case
      *> id: 1985-SR-IV_7_4-p1
      *> rules: [1985-SR-IV.7.4-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.7.4 p1 (REFERENCE FORMAT). An output test: the program
      *> must translate and run at 85 and DISPLAY exactly the lines its
      *> .out holds, the outcome the rule requires. Placed from written
      *> and run by the PB5453 reader of lineage-reads-1985/s-08.json by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
      * 1985-SR-IV.7.4-p1: level-number, space, subject name if any,
      * independent clauses, separator period. Expect: AB12
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DENFRM.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R.
          02 A PIC X VALUE "A".
          02 VALUE "B" PIC X.
          02 N VALUE 12 PIC 99
             USAGE DISPLAY.
       PROCEDURE DIVISION.
       P1.
           DISPLAY R.
           STOP RUN.
