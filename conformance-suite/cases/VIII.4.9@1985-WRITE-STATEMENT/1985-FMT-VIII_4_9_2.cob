      *>--- conformance-case
      *> id: 1985-FMT-VIII_4_9_2
      *> rules: [1985-FMT-VIII.4.9.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VIII.4.9.2 (THE WRITE STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RELWR.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT REL-FILE ASSIGN TO RELDATA
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  REL-FILE.
       01  REL-REC PIC X(10).
       WORKING-STORAGE SECTION.
       01  WS-DATA PIC X(10) VALUE "ABCDEFGHIJ".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT REL-FILE.
           WRITE REL-REC FROM WS-DATA
               INVALID KEY DISPLAY "INVALID"
               NOT INVALID KEY DISPLAY "WRITTEN"
           END-WRITE.
           CLOSE REL-FILE.
           DISPLAY "DONE".
           STOP RUN.
