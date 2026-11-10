      *>--- conformance-case
      *> id: 1985-GR-VII_4_3_4-20
      *> rules: [1985-GR-VII.4.3.4-20]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.4.3.4 GR20 (THE OPEN STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the triage of the drafts that did not
      *> match (a suspect draft corrected) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. MFREEL.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FILE-A ASSIGN TO TAPE1
               ORGANIZATION IS SEQUENTIAL.
           SELECT FILE-B ASSIGN TO TAPE1
               ORGANIZATION IS SEQUENTIAL.
       I-O-CONTROL.
           MULTIPLE FILE TAPE CONTAINS FILE-A POSITION 1
               FILE-B POSITION 2.
       DATA DIVISION.
       FILE SECTION.
       FD  FILE-A LABEL RECORDS ARE STANDARD.
       01  REC-A PIC X(10).
       FD  FILE-B LABEL RECORDS ARE STANDARD.
       01  REC-B PIC X(10).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT FILE-A.
           MOVE "FILEA-DATA" TO REC-A.
           WRITE REC-A.
           CLOSE FILE-A.
           OPEN OUTPUT FILE-B.
           MOVE "FILEB-DATA" TO REC-B.
           WRITE REC-B.
           CLOSE FILE-B.
           OPEN INPUT FILE-B.
           READ FILE-B AT END DISPLAY "EOF-B".
           DISPLAY "B=" REC-B.
           CLOSE FILE-B.
           STOP RUN.
