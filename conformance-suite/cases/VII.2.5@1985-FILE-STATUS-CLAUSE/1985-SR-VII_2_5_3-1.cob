      *>--- conformance-case
      *> id: 1985-SR-VII_2_5_3-1
      *> rules: [1985-SR-VII.2.5.3-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.2.5.3 SR1 (THE FILE STATUS CLAUSE). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. FSQUAL.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT OUT-FILE ASSIGN TO OUTFILE
               FILE STATUS IS FS-CODE OF GRP-A.
       DATA DIVISION.
       FILE SECTION.
       FD  OUT-FILE
           LABEL RECORDS ARE STANDARD.
       01  OUT-REC PIC X(10).
       WORKING-STORAGE SECTION.
       01  GRP-A.
           05  FS-CODE PIC XX VALUE "ZZ".
       01  GRP-B.
           05  FS-CODE PIC XX VALUE "XX".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT OUT-FILE.
           MOVE "HELLO" TO OUT-REC.
           WRITE OUT-REC.
           CLOSE OUT-FILE.
           DISPLAY FS-CODE OF GRP-A.
           DISPLAY FS-CODE OF GRP-B.
           STOP RUN.
