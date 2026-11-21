      *>--- conformance-case
      *> id: 1985-GR-X_2_1-p1
      *> rules: [1985-GR-X.2.1-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:X.2.1 p1 (NESTED SOURCE PROGRAMS). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-16.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. XOUT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-A PIC X VALUE "A" GLOBAL.
          88 W-A-ON VALUE "A".
       PROCEDURE DIVISION.
       P-1.
           CALL "XIN".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. XIN.
       DATA DIVISION.
       PROCEDURE DIVISION.
       I-1.
           DISPLAY "W-A=" W-A.
           IF W-A-ON DISPLAY "ON".
           EXIT PROGRAM.
       END PROGRAM XIN.
       END PROGRAM XOUT.
