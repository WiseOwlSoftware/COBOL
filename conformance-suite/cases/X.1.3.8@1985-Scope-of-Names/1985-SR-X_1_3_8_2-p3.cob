      *>--- conformance-case
      *> id: 1985-SR-X_1_3_8_2-p3
      *> rules: [1985-SR-X.1.3.8.2-p3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:X.1.3.8.2 p3 (Scope of Names). A refusal: the rule
      *> forbids line 6 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL1639 there; a compiler that accepts it fails.
      *> Placed from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-16.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. XOUT.
       DATA DIVISION.
       PROCEDURE DIVISION.
       P-1.
           OPEN OUTPUT F1.
           CLOSE F1.
           CALL "XIN".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. XIN.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "x16f1.dat".
       DATA DIVISION.
       FILE SECTION.
       FD F1 GLOBAL.
       01 R1 PIC X(4).
       PROCEDURE DIVISION.
       I-1.
           EXIT PROGRAM.
       END PROGRAM XIN.
       END PROGRAM XOUT.
