      *>--- conformance-case
      *> id: 1985-SR-VII_2_7_3-2
      *> rules: [1985-SR-VII.2.7.3-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.2.7.3 SR2 (THE PADDING CHARACTER CLAUSE). An output
      *> test: the program must translate and run at 85 and DISPLAY
      *> exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PADTEST.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SEQ-FILE ASSIGN TO SEQDATA
               ORGANIZATION IS SEQUENTIAL
               PADDING CHARACTER IS PAD-CHAR OF PAD-GROUP-A.
       DATA DIVISION.
       FILE SECTION.
       FD  SEQ-FILE.
       01  SEQ-REC PIC X(10).
       WORKING-STORAGE SECTION.
       01  PAD-GROUP-A.
           05  PAD-CHAR PIC X VALUE "*".
       01  PAD-GROUP-B.
           05  PAD-CHAR PIC X VALUE "#".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT SEQ-FILE.
           MOVE "HELLOWORLD" TO SEQ-REC.
           WRITE SEQ-REC.
           CLOSE SEQ-FILE.
           OPEN INPUT SEQ-FILE.
           READ SEQ-FILE AT END DISPLAY "EOF".
           DISPLAY SEQ-REC.
           CLOSE SEQ-FILE.
           STOP RUN.
