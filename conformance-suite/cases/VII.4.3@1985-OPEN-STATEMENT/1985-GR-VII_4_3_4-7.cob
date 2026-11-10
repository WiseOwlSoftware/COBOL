      *>--- conformance-case
      *> id: 1985-GR-VII_4_3_4-7
      *> rules: [1985-GR-VII.4.3.4-7]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.4.3.4 GR7 (THE OPEN STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. LBLTST.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT TFILE ASSIGN TO DISK1.
       DATA DIVISION.
       FILE SECTION.
       FD  TFILE
           LABEL RECORDS ARE STANDARD.
       01  TREC PIC X(10).
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT TFILE.
           MOVE "LABELTEST1" TO TREC.
           WRITE TREC.
           CLOSE TFILE.
           OPEN INPUT TFILE.
           READ TFILE AT END DISPLAY "EOF".
           DISPLAY TREC.
           CLOSE TFILE.
           STOP RUN.
