      *>--- conformance-case
      *> id: 1985-GR-XIII_3_20_4-10
      *> rules: [1985-GR-XIII.3.20.4-10]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.3.20.4 GR10 (THE TYPE CLAUSE). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the witnesses-85m drafting pass (a
      *> first draft for a 1985 rule no pass had drafted, its
      *> expectation derived from the rule's text and its citations
      *> checked with cite.py) by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CFSTEP.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO "CFSTEP.RPT".
           SELECT IN-FILE ASSIGN TO "CFSTEP.RPT".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE
           REPORT IS RPT.
       FD  IN-FILE
           RECORD CONTAINS 1 CHARACTERS.
       01  IN-CH PIC X.
       WORKING-STORAGE SECTION.
       01  WS-K PIC X VALUE "A".
       01  WS-A PIC 99 VALUE 0.
       01  WS-B PIC 99 VALUE 0.
       01  V1 PIC 999.
       01  V2 PIC 999.
       01  V3 PIC 9999.
       01  V4 PIC 9999.
       01  WS-EOF PIC X VALUE "N".
       01  WS-LINE PIC X(40) VALUE SPACES.
       01  WS-LEN PIC 99 VALUE 0.
       REPORT SECTION.
       RD  RPT
           CONTROLS ARE FINAL WS-K.
       01  DL TYPE IS DETAIL LINE PLUS 1.
           05  COLUMN 1 PIC X SOURCE WS-K.
           05  COLUMN 3 PIC 99 SOURCE WS-A.
           05  COLUMN 6 PIC 99 SOURCE WS-B.
       01  CFK TYPE IS CONTROL FOOTING WS-K LINE PLUS 1.
           05  S1 COLUMN 1 PIC 999 SUM WS-A.
           05  S2 COLUMN 5 PIC 999 SUM WS-B.
           05  XS COLUMN 9 PIC 9999 SUM S1 S2.
       01  CFF TYPE IS CONTROL FOOTING FINAL LINE PLUS 1.
           05  FS COLUMN 1 PIC 9999 SUM S1.
       PROCEDURE DIVISION.
       DECLARATIVES.
       CFK-USE SECTION.
           USE BEFORE REPORTING CFK.
       CFK-PARA.
           MOVE S1 TO V1.
           MOVE S2 TO V2.
           MOVE XS TO V3.
           MOVE FS TO V4.
           DISPLAY "USE " V1 " " V2 " " V3 " " V4.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-PARA.
           OPEN OUTPUT RPT-FILE.
           INITIATE RPT.
           MOVE 1 TO WS-A.
           MOVE 2 TO WS-B.
           GENERATE DL.
           MOVE 3 TO WS-A.
           MOVE 4 TO WS-B.
           GENERATE DL.
           MOVE "B" TO WS-K.
           MOVE 5 TO WS-A.
           MOVE 6 TO WS-B.
           GENERATE DL.
           MOVE S1 TO V1.
           DISPLAY "AFTER " V1.
           TERMINATE RPT.
           CLOSE RPT-FILE.
           OPEN INPUT IN-FILE.
           PERFORM READ-PARA UNTIL WS-EOF = "Y".
           PERFORM SHOW-PARA.
           CLOSE IN-FILE.
           STOP RUN.
       READ-PARA.
           READ IN-FILE AT END MOVE "Y" TO WS-EOF.
           IF WS-EOF = "N"
               IF IN-CH < SPACE
                   PERFORM SHOW-PARA
               ELSE
                   ADD 1 TO WS-LEN
                   MOVE IN-CH TO WS-LINE (WS-LEN:1).
       SHOW-PARA.
           IF WS-LEN > 0
               DISPLAY WS-LINE
               MOVE SPACES TO WS-LINE
               MOVE 0 TO WS-LEN.
