      *>--- conformance-case
      *> id: 1985-GR-VII_2_3_4-4
      *> rules: [1985-GR-VII.2.3.4-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.2.3.4 GR4 (THE FILE CONTROL ENTRY). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. ALPHORD.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT TFILE ASSIGN TO "TFILE.DAT"
               FILE STATUS IS WS-STAT
               ORGANIZATION IS SEQUENTIAL
               ACCESS MODE IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  TFILE
           LABEL RECORDS ARE STANDARD.
       01  TREC PIC X(5).
       WORKING-STORAGE SECTION.
       01  WS-STAT PIC XX.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT TFILE.
           MOVE "ABCDE" TO TREC.
           WRITE TREC.
           CLOSE TFILE.
           OPEN INPUT TFILE.
           READ TFILE AT END DISPLAY "EOF".
           DISPLAY TREC.
           DISPLAY WS-STAT.
           CLOSE TFILE.
           STOP RUN.
