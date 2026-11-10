      *>--- conformance-case
      *> id: 1985-SR-XIII_2_4_3-3
      *> rules: [1985-SR-XIII.2.4.3-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.2.4.3 SR3 (THE I-O-CONTROL PARAGRAPH). An output
      *> test: the program must translate and run at 85 and DISPLAY
      *> exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from the confirmation pass's redraft (WEAK
      *> original) by scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RWIOC.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO RPTOUT.
           SELECT WRK-FILE ASSIGN TO WRKOUT.
       I-O-CONTROL.
           SAME AREA FOR RPT-FILE WRK-FILE
           MULTIPLE FILE TAPE CONTAINS RPT-FILE WRK-FILE.
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE
           REPORT IS RPT-1.
       FD  WRK-FILE.
       01  WRK-REC PIC X(10).
       REPORT SECTION.
       RD  RPT-1.
       01  DTL TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           05  COLUMN NUMBER IS 1 PIC X(5) VALUE "HELLO".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RPT-FILE.
           INITIATE RPT-1.
           GENERATE DTL.
           TERMINATE RPT-1.
           CLOSE RPT-FILE.
           OPEN OUTPUT WRK-FILE.
           MOVE "DATA" TO WRK-REC.
           WRITE WRK-REC.
           CLOSE WRK-FILE.
           DISPLAY "I-O-CONTROL OK".
           STOP RUN.
