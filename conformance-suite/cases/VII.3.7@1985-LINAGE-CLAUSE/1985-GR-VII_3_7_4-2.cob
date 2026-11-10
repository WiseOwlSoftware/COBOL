      *>--- conformance-case
      *> id: 1985-GR-VII_3_7_4-2
      *> rules: [1985-GR-VII.3.7.4-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.3.7.4 GR2 (THE LINAGE CLAUSE). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. LINTEST.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT-FILE ASSIGN TO PRTOUT
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  PRT-FILE
           LINAGE IS 2 LINES.
       01  PRT-REC PIC X(10).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT PRT-FILE.
           MOVE "LINE ONE" TO PRT-REC.
           WRITE PRT-REC BEFORE ADVANCING 1 LINE
               AT END-OF-PAGE DISPLAY "EOP-1"
               NOT AT END-OF-PAGE DISPLAY "NOEOP-1"
           END-WRITE.
           MOVE "LINE TWO" TO PRT-REC.
           WRITE PRT-REC BEFORE ADVANCING 1 LINE
               AT END-OF-PAGE DISPLAY "EOP-2"
               NOT AT END-OF-PAGE DISPLAY "NOEOP-2"
           END-WRITE.
           CLOSE PRT-FILE.
           STOP RUN.
