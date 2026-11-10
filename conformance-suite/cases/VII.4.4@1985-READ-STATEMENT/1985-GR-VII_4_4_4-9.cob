      *>--- conformance-case
      *> id: 1985-GR-VII_4_4_4-9
      *> rules: [1985-GR-VII.4.4.4-9]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.4.4.4 GR9 (THE READ STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WEAK original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. REELRD.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT TFILE ASSIGN TO TAPE01.
       DATA DIVISION.
       FILE SECTION.
       FD  TFILE
           LABEL RECORDS ARE STANDARD.
       01  TREC PIC X(5).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT TFILE.
           MOVE "REC01" TO TREC.
           WRITE TREC.
           CLOSE TFILE REEL.
           CLOSE TFILE REEL.
           MOVE "REC02" TO TREC.
           WRITE TREC.
           CLOSE TFILE.
           OPEN INPUT TFILE.
       READ-PARA.
           READ TFILE AT END GO TO END-PARA.
           DISPLAY TREC.
           GO TO READ-PARA.
       END-PARA.
           CLOSE TFILE.
           DISPLAY "END".
           STOP RUN.
