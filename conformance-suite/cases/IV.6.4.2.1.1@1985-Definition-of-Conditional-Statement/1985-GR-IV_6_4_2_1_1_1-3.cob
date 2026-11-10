      *>--- conformance-case
      *> id: 1985-GR-IV_6_4_2_1_1_1-3
      *> rules: [1985-GR-IV.6.4.2.1.1.1-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.6.4.2.1.1.1 GR3 (Definition of Conditional Statement).
      *> An output test: the program must translate and run at 85 and
      *> DISPLAY exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. EOPTEST.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT-FILE ASSIGN TO "PRTOUT"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  PRT-FILE
           LINAGE IS 10 LINES.
       01  PRT-REC PIC X(20).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT PRT-FILE.
           MOVE "LINE ONE" TO PRT-REC.
           WRITE PRT-REC BEFORE ADVANCING 1 LINE
               AT END-OF-PAGE
                   DISPLAY "EOP"
               NOT AT END-OF-PAGE
                   DISPLAY "NOT EOP"
           END-WRITE.
           DISPLAY "DONE".
           CLOSE PRT-FILE.
           STOP RUN.
