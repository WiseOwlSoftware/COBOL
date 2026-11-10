      *>--- conformance-case
      *> id: 1985-FMT-VII_2_6_2
      *> rules: [1985-FMT-VII.2.6.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.2.6.2 (THE ORGANIZATION IS SEQUENTIAL CLAUSE). An
      *> output test: the program must translate and run at 85 and
      *> DISPLAY exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from written and run by the witness-drafting
      *> pass (a rule the audit never drafted) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. ORGSEQ1.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FILE-A ASSIGN TO "ORGSEQ1A.DAT"
               ORGANIZATION IS SEQUENTIAL.
           SELECT FILE-B ASSIGN TO "ORGSEQ1B.DAT"
               ORGANIZATION SEQUENTIAL.
           SELECT FILE-C ASSIGN TO "ORGSEQ1C.DAT"
               SEQUENTIAL.
           SELECT FILE-D ASSIGN TO "ORGSEQ1D.DAT"
               SEQUENTIAL
               ACCESS MODE IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  FILE-A.
       01  REC-A               PIC X(4).
       FD  FILE-B.
       01  REC-B               PIC X(4).
       FD  FILE-C.
       01  REC-C               PIC X(4).
       FD  FILE-D.
       01  REC-D               PIC X(4).
       WORKING-STORAGE SECTION.
       01  WS-EOF              PIC X VALUE "N".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT FILE-A.
           MOVE "A001" TO REC-A. WRITE REC-A.
           MOVE "A002" TO REC-A. WRITE REC-A.
           MOVE "A003" TO REC-A. WRITE REC-A.
           CLOSE FILE-A.
           OPEN INPUT FILE-A.
           MOVE "N" TO WS-EOF.
           PERFORM READ-A UNTIL WS-EOF = "Y".
           CLOSE FILE-A.
           OPEN OUTPUT FILE-B.
           MOVE "B001" TO REC-B. WRITE REC-B.
           MOVE "B002" TO REC-B. WRITE REC-B.
           CLOSE FILE-B.
           OPEN INPUT FILE-B.
           MOVE "N" TO WS-EOF.
           PERFORM READ-B UNTIL WS-EOF = "Y".
           CLOSE FILE-B.
           OPEN OUTPUT FILE-C.
           MOVE "C001" TO REC-C. WRITE REC-C.
           MOVE "C002" TO REC-C. WRITE REC-C.
           CLOSE FILE-C.
           OPEN INPUT FILE-C.
           MOVE "N" TO WS-EOF.
           PERFORM READ-C UNTIL WS-EOF = "Y".
           CLOSE FILE-C.
           OPEN OUTPUT FILE-D.
           MOVE "D001" TO REC-D. WRITE REC-D.
           MOVE "D002" TO REC-D. WRITE REC-D.
           CLOSE FILE-D.
           OPEN INPUT FILE-D.
           MOVE "N" TO WS-EOF.
           PERFORM READ-D UNTIL WS-EOF = "Y".
           CLOSE FILE-D.
           STOP RUN.
       READ-A.
           READ FILE-A
               AT END MOVE "Y" TO WS-EOF
                      DISPLAY "A END".
           IF WS-EOF = "N"
               DISPLAY REC-A.
       READ-B.
           READ FILE-B
               AT END MOVE "Y" TO WS-EOF
                      DISPLAY "B END".
           IF WS-EOF = "N"
               DISPLAY REC-B.
       READ-C.
           READ FILE-C
               AT END MOVE "Y" TO WS-EOF
                      DISPLAY "C END".
           IF WS-EOF = "N"
               DISPLAY REC-C.
       READ-D.
           READ FILE-D
               AT END MOVE "Y" TO WS-EOF
                      DISPLAY "D END".
           IF WS-EOF = "N"
               DISPLAY REC-D.
