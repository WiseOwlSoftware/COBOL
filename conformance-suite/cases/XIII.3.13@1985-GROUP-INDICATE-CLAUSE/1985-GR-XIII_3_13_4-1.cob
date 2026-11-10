      *>--- conformance-case
      *> id: 1985-GR-XIII_3_13_4-1
      *> rules: [1985-GR-XIII.3.13.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.3.13.4 GR1 (THE GROUP INDICATE CLAUSE). An output
      *> test: the program must translate and run at 85 and DISPLAY
      *> exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from written and run by the triage of the
      *> drafts that did not match (a suspect draft corrected) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. GRPIND.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO RPTFILE.
           SELECT IN-FILE ASSIGN TO RPTFILE.
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE
           REPORT IS RPT.
       FD  IN-FILE
           RECORD CONTAINS 1 CHARACTERS.
       01  IN-CH PIC X.
       WORKING-STORAGE SECTION.
       01  WS-K PIC X VALUE "A".
       01  WS-N PIC 9 VALUE 0.
       01  WS-EOF PIC X VALUE "N".
       01  WS-LINE PIC X(20) VALUE SPACES.
       01  WS-LEN PIC 99 VALUE 0.
       REPORT SECTION.
       RD  RPT
           CONTROL IS WS-K
           PAGE LIMIT IS 3 LINES.
       01  DL TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           05  COLUMN NUMBER IS 1 PIC XX VALUE "AA" GROUP INDICATE.
           05  COLUMN NUMBER IS 4 PIC 9 SOURCE WS-N.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RPT-FILE.
           INITIATE RPT.
           MOVE 1 TO WS-N.
           GENERATE DL.
           MOVE 2 TO WS-N.
           GENERATE DL.
           MOVE "B" TO WS-K.
           MOVE 3 TO WS-N.
           GENERATE DL.
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
