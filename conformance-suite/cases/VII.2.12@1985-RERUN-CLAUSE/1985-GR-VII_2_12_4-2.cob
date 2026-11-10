      *>--- conformance-case
      *> id: 1985-GR-VII_2_12_4-2
      *> rules: [1985-GR-VII.2.12.4-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.2.12.4 GR2 (THE RERUN CLAUSE). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RERUNA.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT OUTF ASSIGN TO OUTDATA
               ORGANIZATION IS SEQUENTIAL.
       I-O-CONTROL.
           RERUN EVERY END OF REEL OF OUTF.
       DATA DIVISION.
       FILE SECTION.
       FD  OUTF.
       01  OUT-REC PIC X(10).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT OUTF.
           MOVE "RECORD-ONE" TO OUT-REC.
           WRITE OUT-REC.
           CLOSE OUTF.
           DISPLAY "RERUN DONE".
           STOP RUN.
