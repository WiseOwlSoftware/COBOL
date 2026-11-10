      *>--- conformance-case
      *> id: 1985-GR-XIII_2_3_4-3
      *> rules: [1985-GR-XIII.2.3.4-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.2.3.4 GR3 (THE FILE CONTROL ENTRY). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WEAK original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RWASGN.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO "RPTOUT".
           SELECT CHK-FILE ASSIGN TO "RPTOUT".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE
           REPORT IS RPT.
       FD  CHK-FILE.
       01  CHK-CHAR PIC X.
       WORKING-STORAGE SECTION.
       01  WS-TEXT PIC X(5) VALUE SPACES.
       01  WS-I PIC 9 VALUE 0.
       REPORT SECTION.
       RD  RPT.
       01  DL TYPE IS DETAIL LINE PLUS 1.
           05  COLUMN 1 PIC X(5) VALUE "HELLO".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RPT-FILE.
           INITIATE RPT.
           GENERATE DL.
           TERMINATE RPT.
           CLOSE RPT-FILE.
           OPEN INPUT CHK-FILE.
           PERFORM READ-CHAR 5 TIMES.
           CLOSE CHK-FILE.
           DISPLAY WS-TEXT.
           STOP RUN.
       READ-CHAR.
           ADD 1 TO WS-I.
           READ CHK-FILE AT END MOVE "?" TO CHK-CHAR.
           MOVE CHK-CHAR TO WS-TEXT (WS-I:1).
