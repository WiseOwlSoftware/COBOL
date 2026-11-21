      *>--- conformance-case
      *> id: 1985-GR-X_4_4_1-p1
      *> rules: [1985-GR-X.4.4.1-p1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:X.4.4.1 p1 (THE REPORT DESCRIPTION ENTRY IN THE
      *> INTER-PROGRAM COMMUNICATION MODULE). A refusal: the rule
      *> forbids line 25 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL1757 there; a compiler that accepts it fails.
      *> Placed from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-16.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. XOUT.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPF ASSIGN TO "x16r.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD RPF REPORT IS R.
       REPORT SECTION.
       RD R.
       01 DL TYPE DETAIL LINE PLUS 1.
          05 COLUMN 1 PIC X(2) VALUE "DL".
       PROCEDURE DIVISION.
       P-1.
           OPEN OUTPUT RPF.
           CALL "XIN".
           CLOSE RPF.
           DISPLAY "DONE".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. XIN.
       PROCEDURE DIVISION.
       I-1.
           INITIATE R.
           GENERATE DL.
           TERMINATE R.
           DISPLAY "INNER GENERATED".
           EXIT PROGRAM.
       END PROGRAM XIN.
       END PROGRAM XOUT.
