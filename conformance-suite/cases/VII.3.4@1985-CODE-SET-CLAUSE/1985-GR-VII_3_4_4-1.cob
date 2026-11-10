      *>--- conformance-case
      *> id: 1985-GR-VII_3_4_4-1
      *> rules: [1985-GR-VII.3.4.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.3.4.4 GR1 (THE CODE-SET CLAUSE). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CSTEST.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET ASCII-SET IS STANDARD-1.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT CS-FILE ASSIGN TO CSDATA
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  CS-FILE
           CODE-SET IS ASCII-SET.
       01  CS-REC PIC X(5).
       WORKING-STORAGE SECTION.
       01  WS-EOF PIC X VALUE "N".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT CS-FILE.
           MOVE "HELLO" TO CS-REC.
           WRITE CS-REC.
           CLOSE CS-FILE.
           OPEN INPUT CS-FILE.
           READ CS-FILE AT END MOVE "Y" TO WS-EOF.
           IF WS-EOF = "N" DISPLAY CS-REC.
           CLOSE CS-FILE.
           STOP RUN.
