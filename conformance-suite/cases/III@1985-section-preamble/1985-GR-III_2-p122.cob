      *>--- conformance-case
      *> id: 1985-GR-III_2-p122
      *> rules: [1985-GR-III.2-p122, 1985-GR-III.2-p92]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:III.2 p122; §1985:III.2 p92 ((section preamble)). An
      *> output test: the program must translate and run at 85 and
      *> DISPLAY exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from written and run by the PB5453 reader of
      *> lineage-reads-1985/g-99.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. MODES.
      * EXTEND MODE (1985 III.2 P92) AND I-O MODE (P122): IN EXTEND
      * MODE A READ FAILS WITH STATUS 47 AND A WRITE APPENDS; IN I-O
      * MODE READ AND REWRITE SUCCEED AND WRITE FAILS WITH STATUS 48.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "MODES1.DAT"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS ST1.
       DATA DIVISION.
       FILE SECTION.
       FD  F1.
       01  R1 PIC X(4).
       WORKING-STORAGE SECTION.
       01  ST1 PIC XX.
       01  S1 PIC XX.
       01  S2 PIC XX.
       01  S3 PIC XX.
       01  S4 PIC XX.
       01  S5 PIC XX.
       01  GOT PIC X(8) VALUE SPACES.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D1 SECTION.
           USE AFTER STANDARD EXCEPTION PROCEDURE ON F1.
       D1P.
           EXIT.
       END DECLARATIVES.
       M SECTION.
       P1.
           OPEN OUTPUT F1.
           MOVE "AAAA" TO R1.
           WRITE R1.
           CLOSE F1.
           OPEN EXTEND F1.
           READ F1.
           MOVE ST1 TO S1.
           MOVE "BBBB" TO R1.
           WRITE R1.
           MOVE ST1 TO S2.
           CLOSE F1.
           OPEN I-O F1.
           READ F1.
           MOVE ST1 TO S3.
           MOVE "CCCC" TO R1.
           REWRITE R1.
           MOVE ST1 TO S4.
           WRITE R1.
           MOVE ST1 TO S5.
           CLOSE F1.
           OPEN INPUT F1.
           READ F1.
           MOVE R1 TO GOT (1:4).
           READ F1.
           MOVE R1 TO GOT (5:4).
           CLOSE F1.
           IF S1 = "47" AND S2 = "00" AND S3 = "00" AND S4 = "00"
              AND S5 = "48" AND GOT = "CCCCBBBB"
               DISPLAY "PASS MODES " S1 S2 S3 S4 S5 " " GOT
           ELSE
               DISPLAY "FAIL MODES " S1 S2 S3 S4 S5 " " GOT.
           STOP RUN.
