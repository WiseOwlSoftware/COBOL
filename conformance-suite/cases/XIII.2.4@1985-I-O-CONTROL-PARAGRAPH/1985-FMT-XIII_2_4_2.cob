      *>--- conformance-case
      *> id: 1985-FMT-XIII_2_4_2
      *> rules: [1985-FMT-XIII.2.4.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.2.4.2 (THE I-O-CONTROL PARAGRAPH). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WEAK original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. IOCTL.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT WORK-FILE ASSIGN TO "WORKF".
           SELECT RPT-FILE ASSIGN TO "RPTONE".
           SELECT RPT-TWO ASSIGN TO "RPTTWO".
       I-O-CONTROL.
           SAME AREA FOR WORK-FILE RPT-FILE
           MULTIPLE FILE TAPE CONTAINS RPT-FILE POSITION 1
                                       RPT-TWO POSITION 2.
       DATA DIVISION.
       FILE SECTION.
       FD  WORK-FILE.
       01  WORK-REC PIC X(11).
       FD  RPT-FILE
           REPORT IS RPT-A.
       FD  RPT-TWO
           REPORT IS RPT-B.
       REPORT SECTION.
       RD  RPT-A.
       01  LINE-A TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           05  COLUMN 1 PIC X(5) VALUE "HELLO".
       RD  RPT-B.
       01  LINE-B TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           05  COLUMN 1 PIC X(5) VALUE "AGAIN".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RPT-FILE.
           INITIATE RPT-A.
           GENERATE LINE-A.
           TERMINATE RPT-A.
           CLOSE RPT-FILE.
           OPEN OUTPUT RPT-TWO.
           INITIATE RPT-B.
           GENERATE LINE-B.
           TERMINATE RPT-B.
           CLOSE RPT-TWO.
           OPEN OUTPUT WORK-FILE.
           MOVE "SHARED AREA" TO WORK-REC.
           WRITE WORK-REC.
           CLOSE WORK-FILE.
           OPEN INPUT WORK-FILE.
           READ WORK-FILE
               AT END DISPLAY "NO RECORD"
               NOT AT END DISPLAY WORK-REC
           END-READ.
           CLOSE WORK-FILE.
           DISPLAY "DONE".
           STOP RUN.
