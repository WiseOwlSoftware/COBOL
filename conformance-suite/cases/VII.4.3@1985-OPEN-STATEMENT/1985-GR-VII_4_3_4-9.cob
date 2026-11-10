      *>--- conformance-case
      *> id: 1985-GR-VII_4_3_4-9
      *> rules: [1985-GR-VII.4.3.4-9]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.4.3.4 GR9 (THE OPEN STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. OPENNR.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT TFILE ASSIGN TO TAPEDEV
               ORGANIZATION IS SEQUENTIAL
               ACCESS MODE IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  TFILE
           LABEL RECORDS ARE STANDARD.
       01  TREC PIC X(10).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT TFILE WITH NO REWIND.
           MOVE "REC-A" TO TREC.
           WRITE TREC.
           CLOSE TFILE.
           OPEN INPUT TFILE REVERSED.
           READ TFILE AT END DISPLAY "NO RECORD".
           DISPLAY TREC.
           READ TFILE AT END DISPLAY "END".
           CLOSE TFILE.
           STOP RUN.
