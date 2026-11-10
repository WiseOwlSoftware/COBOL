      *>--- conformance-case
      *> id: 1985-GR-VII_3_5_4-2
      *> rules: [1985-GR-VII.3.5.4-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.3.5.4 GR2 (THE DATA RECORDS CLAUSE). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DRTEST.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT OUT-FILE ASSIGN TO "OUTFILE"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  OUT-FILE
           LABEL RECORDS ARE STANDARD
           DATA RECORDS ARE REC-A REC-B.
       01  REC-A           PIC X(6).
       01  REC-B.
           05  REC-B1      PIC X(3).
           05  REC-B2      PIC X(3).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT OUT-FILE.
           MOVE "ABCDEF" TO REC-A.
           DISPLAY REC-B.
           DISPLAY REC-B2.
           MOVE "XYZ" TO REC-B1.
           DISPLAY REC-A.
           WRITE REC-A.
           CLOSE OUT-FILE.
           STOP RUN.
