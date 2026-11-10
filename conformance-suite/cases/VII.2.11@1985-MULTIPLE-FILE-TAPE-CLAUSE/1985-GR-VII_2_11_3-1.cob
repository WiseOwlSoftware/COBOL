      *>--- conformance-case
      *> id: 1985-GR-VII_2_11_3-1
      *> rules: [1985-GR-VII.2.11.3-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.2.11.3 GR1 (THE MULTIPLE FILE TAPE CLAUSE). An output
      *> test: the program must translate and run at 85 and DISPLAY
      *> exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. MFTAPE.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT TFILE-A ASSIGN TO TAPE-UNIT-1
               ORGANIZATION IS SEQUENTIAL.
           SELECT TFILE-B ASSIGN TO TAPE-UNIT-1
               ORGANIZATION IS SEQUENTIAL.
       I-O-CONTROL.
           MULTIPLE FILE TAPE CONTAINS TFILE-A TFILE-B.
       DATA DIVISION.
       FILE SECTION.
       FD  TFILE-A
           LABEL RECORDS ARE STANDARD.
       01  REC-A PIC X(10).
       FD  TFILE-B
           LABEL RECORDS ARE STANDARD.
       01  REC-B PIC X(10).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT TFILE-A.
           MOVE "FIRST FILE" TO REC-A.
           WRITE REC-A.
           CLOSE TFILE-A.
           OPEN INPUT TFILE-A.
           READ TFILE-A AT END MOVE "NO RECORD" TO REC-A.
           DISPLAY REC-A.
           CLOSE TFILE-A.
           DISPLAY "DONE".
           STOP RUN.
