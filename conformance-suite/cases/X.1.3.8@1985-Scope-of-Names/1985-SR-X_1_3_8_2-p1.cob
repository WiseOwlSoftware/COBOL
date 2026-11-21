      *>--- conformance-case
      *> id: 1985-SR-X_1_3_8_2-p1
      *> rules: [1985-SR-X.1.3.8.2-p1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:X.1.3.8.2 p1 (Scope of Names). A refusal: the rule
      *> forbids line 16 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL1639 there; a compiler that accepts it fails.
      *> Placed from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-16.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. XOUT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-A PIC X VALUE "A".
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
