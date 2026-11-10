      *>--- conformance-case
      *> id: 1985-FMT-VII_2_7_2
      *> rules: [1985-FMT-VII.2.7.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.2.7.2 (THE PADDING CHARACTER CLAUSE). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PADTEST.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PAD-FILE ASSIGN TO "PADFILE"
               ORGANIZATION IS SEQUENTIAL
               PADDING CHARACTER IS "*".
       DATA DIVISION.
       FILE SECTION.
       FD  PAD-FILE.
       01  PAD-REC PIC X(10).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT PAD-FILE.
           MOVE "HELLOWORLD" TO PAD-REC.
           WRITE PAD-REC.
           CLOSE PAD-FILE.
           OPEN INPUT PAD-FILE.
           READ PAD-FILE AT END DISPLAY "EOF".
           DISPLAY PAD-REC.
           CLOSE PAD-FILE.
           STOP RUN.
