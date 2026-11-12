      *>--- conformance-case
      *> id: 1985-FMT-X_4_4_2
      *> rules: [1985-FMT-X.4.4.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:X.4.4.2 (THE REPORT DESCRIPTION ENTRY IN THE
      *> INTER-PROGRAM COMMUNICATION MODULE). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the confirmation pass (the redraft of a
      *> WEAK or WRONG record, whose filed defect was fixed; re-run to
      *> MATCH with its expectation unchanged, witnesses-85g) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RDGLOB.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO "RDGLOB".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE
           REPORT IS RPT-1.
       REPORT SECTION.
       RD  RPT-1 IS GLOBAL
           CONTROL IS FINAL
           PAGE LIMIT IS 20 LINES
           HEADING 1
           FIRST DETAIL 3
           LAST DETAIL 15
           FOOTING 18.
       01  DET-LINE TYPE IS DETAIL LINE PLUS 1.
           05 COLUMN 1 PIC X(5) VALUE "HELLO".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RPT-FILE.
           CALL "RDSUB1".
           GENERATE DET-LINE.
           CALL "RDSUB2".
           CLOSE RPT-FILE.
           DISPLAY "RD GLOBAL OK".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RDSUB1.
       PROCEDURE DIVISION.
       SUB1-PARA.
           INITIATE RPT-1.
           IF PAGE-COUNTER = 1 AND LINE-COUNTER = 0
               DISPLAY "INITIATED"
           ELSE
               DISPLAY "NOT INITIATED".
           EXIT PROGRAM.
       END PROGRAM RDSUB1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RDSUB2.
       PROCEDURE DIVISION.
       SUB2-PARA.
           IF LINE-COUNTER = 3
               DISPLAY "DETAIL ON LINE 3"
           ELSE
               DISPLAY "DETAIL NOT ON LINE 3".
           TERMINATE RPT-1.
           EXIT PROGRAM.
       END PROGRAM RDSUB2.
       END PROGRAM RDGLOB.
