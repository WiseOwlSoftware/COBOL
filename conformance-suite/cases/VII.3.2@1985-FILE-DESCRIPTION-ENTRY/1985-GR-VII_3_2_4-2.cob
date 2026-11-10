      *>--- conformance-case
      *> id: 1985-GR-VII_3_2_4-2
      *> rules: [1985-GR-VII.3.2.4-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.3.2.4 GR2 (THE FILE DESCRIPTION ENTRY). An output
      *> test: the program must translate and run at 85 and DISPLAY
      *> exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. FDTEST.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT OUT-FILE ASSIGN TO OUTFILE.
       DATA DIVISION.
       FILE SECTION.
       FD  OUT-FILE
           BLOCK CONTAINS 1 RECORDS
           DATA RECORD IS OUT-REC
           LABEL RECORDS ARE STANDARD
           RECORD CONTAINS 10 CHARACTERS.
       01  OUT-REC PIC X(10).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT OUT-FILE.
           MOVE "HELLO" TO OUT-REC.
           WRITE OUT-REC.
           CLOSE OUT-FILE.
           DISPLAY "FD OK".
           STOP RUN.
