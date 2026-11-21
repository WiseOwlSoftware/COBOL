      *>--- conformance-case
      *> id: 1985-GR-X_4_3_1-p1
      *> rules: [1985-GR-X.4.3.1-p1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:X.4.3.1 p1 (THE DATA DESCRIPTION ENTRY IN THE
      *> INTER-PROGRAM COMMUNICATION MODULE). A refusal: the rule
      *> forbids line 16 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL1639 there; a compiler that accepts it fails.
      *> Placed from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-16.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. XOUT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WL.
          05 WL1 PIC X(4) VALUE "LOCA".
       PROCEDURE DIVISION.
       P-1.
           CALL "XIN".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. XIN.
       DATA DIVISION.
       PROCEDURE DIVISION.
       I-1.
           DISPLAY WL1.
           EXIT PROGRAM.
       END PROGRAM XIN.
       END PROGRAM XOUT.
