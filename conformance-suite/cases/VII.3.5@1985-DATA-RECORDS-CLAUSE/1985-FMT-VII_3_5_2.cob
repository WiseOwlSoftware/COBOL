      *>--- conformance-case
      *> id: 1985-FMT-VII_3_5_2
      *> rules: [1985-FMT-VII.3.5.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.3.5.2 (THE DATA RECORDS CLAUSE). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DRTEST.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT OUT-FILE ASSIGN TO OUTFILE.
       DATA DIVISION.
       FILE SECTION.
       FD  OUT-FILE
           LABEL RECORDS ARE STANDARD
           DATA RECORDS ARE REC-A REC-B.
       01  REC-A PIC X(10).
       01  REC-B PIC X(10).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT OUT-FILE.
           MOVE "HELLO" TO REC-A.
           WRITE REC-A.
           CLOSE OUT-FILE.
           DISPLAY "DATA RECORDS OK".
           STOP RUN.
