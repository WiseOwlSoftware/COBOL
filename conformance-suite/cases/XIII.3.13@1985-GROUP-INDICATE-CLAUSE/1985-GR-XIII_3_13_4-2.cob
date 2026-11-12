      *>--- conformance-case
      *> id: 1985-GR-XIII_3_13_4-2
      *> rules: [1985-GR-XIII.3.13.4-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.3.13.4 GR2 (THE GROUP INDICATE CLAUSE). An output
      *> test: the program must translate and run at 85 and DISPLAY
      *> exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from written and run by the witnesses-85m
      *> drafting pass (a first draft for a 1985 rule no pass had
      *> drafted, its expectation derived from the rule's text and its
      *> citations checked with cite.py) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. GRPIN2.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO "GRPIN2.RPT".
           SELECT IN-FILE ASSIGN TO "GRPIN2.RPT".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE
           REPORT IS RPT.
       FD  IN-FILE
           RECORD CONTAINS 1 CHARACTERS.
       01  IN-CH PIC X.
       WORKING-STORAGE SECTION.
       01  WS-N PIC 9 VALUE 0.
       01  WS-EOF PIC X VALUE "N".
       01  WS-LINE PIC X(40) VALUE SPACES.
       01  WS-LEN PIC 99 VALUE 0.
       REPORT SECTION.
       RD  RPT.
       01  DL TYPE IS DETAIL LINE PLUS 1.
           05  COLUMN 1 PIC X(3) VALUE "HDR" GROUP INDICATE.
           05  COLUMN 5 PIC 9 SOURCE WS-N.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RPT-FILE.
           INITIATE RPT.
           MOVE 1 TO WS-N.
           GENERATE DL.
           MOVE 2 TO WS-N.
           GENERATE DL.
           MOVE 3 TO WS-N.
           GENERATE DL.
           TERMINATE RPT.
           INITIATE RPT.
           MOVE 4 TO WS-N.
           GENERATE DL.
           MOVE 5 TO WS-N.
           GENERATE DL.
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
