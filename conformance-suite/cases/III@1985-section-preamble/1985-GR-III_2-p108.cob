      *>--- conformance-case
      *> id: 1985-GR-III_2-p108
      *> rules: [1985-GR-III.2-p108]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:III.2 p108 ((section preamble)). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the PB5453 reader of
      *> lineage-reads-1985/g-99.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. FPIAT.
      * FILE POSITION INDICATOR (1985 III.2 P108): AFTER AT END THE
      * FPI INDICATES "THE AT END CONDITION ALREADY EXISTS", SO A
      * SECOND READ GIVES STATUS 46; AN OPTIONAL INPUT FILE NOT
      * PRESENT GIVES AT END (STATUS 10) ON THE FIRST READ.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "FPIAT1.DAT"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS ST1.
           SELECT OPTIONAL F2 ASSIGN TO "FPIATNOPE.DAT"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS ST2.
       DATA DIVISION.
       FILE SECTION.
       FD  F1.
       01  R1 PIC X(4).
       FD  F2.
       01  R2 PIC X(4).
       WORKING-STORAGE SECTION.
       01  ST1 PIC XX.
       01  ST2 PIC XX.
       01  S1 PIC XX.
       01  S2 PIC XX.
       01  S3 PIC XX.
       01  S4 PIC XX.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D1 SECTION.
           USE AFTER STANDARD EXCEPTION PROCEDURE ON F1 F2.
       D1P.
           EXIT.
       END DECLARATIVES.
       M SECTION.
       P1.
           OPEN OUTPUT F1.
           MOVE "AAAA" TO R1.
           WRITE R1.
           CLOSE F1.
           OPEN INPUT F1.
           READ F1.
           MOVE ST1 TO S1.
           READ F1.
           MOVE ST1 TO S2.
           READ F1.
           MOVE ST1 TO S3.
           CLOSE F1.
           OPEN INPUT F2.
           READ F2.
           MOVE ST2 TO S4.
           CLOSE F2.
           IF S1 = "00" AND S2 = "10" AND S3 = "46" AND S4 = "10"
               DISPLAY "PASS FPIAT " S1 " " S2 " " S3 " " S4
           ELSE
               DISPLAY "FAIL FPIAT " S1 " " S2 " " S3 " " S4.
           STOP RUN.
