      *>--- conformance-case
      *> id: 1985-GR-VII_4_7_4-17
      *> rules: [1985-GR-VII.4.7.4-17]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.4.7.4 GR17 (THE WRITE STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. EOPTEST.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT-FILE ASSIGN TO PRTFILE
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  PRT-FILE
           LINAGE IS 5 LINES WITH FOOTING AT 4.
       01  PRT-REC PIC X(20).
       WORKING-STORAGE SECTION.
       01  W PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT PRT-FILE.
           PERFORM WRITE-PARA 3 TIMES.
           CLOSE PRT-FILE.
           DISPLAY "DONE".
           STOP RUN.
       WRITE-PARA.
           ADD 1 TO W.
           MOVE "LINE" TO PRT-REC.
           WRITE PRT-REC BEFORE ADVANCING 1 LINE
               AT END-OF-PAGE DISPLAY "EOP ON WRITE " W.
