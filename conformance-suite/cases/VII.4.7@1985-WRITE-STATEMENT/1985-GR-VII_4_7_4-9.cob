      *>--- conformance-case
      *> id: 1985-GR-VII_4_7_4-9
      *> rules: [1985-GR-VII.4.7.4-9]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.4.7.4 GR9 (THE WRITE STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. WRTNEOP.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT-FILE ASSIGN TO "PRTOUT"
               FILE STATUS IS WS-STAT.
       DATA DIVISION.
       FILE SECTION.
       FD  PRT-FILE
           LINAGE IS 10 LINES.
       01  PRT-REC PIC X(20).
       WORKING-STORAGE SECTION.
       01  WS-STAT PIC XX VALUE "99".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT PRT-FILE.
           MOVE "HELLO" TO PRT-REC.
           WRITE PRT-REC AFTER ADVANCING 1 LINE
               AT END-OF-PAGE DISPLAY "EOP"
               NOT AT END-OF-PAGE DISPLAY "NOT EOP " WS-STAT
           END-WRITE.
           CLOSE PRT-FILE.
           DISPLAY "DONE".
           STOP RUN.
