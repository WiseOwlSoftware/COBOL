      *>--- conformance-case
      *> id: 1985-GR-XIII_3_19_4-4
      *> rules: [1985-GR-XIII.3.19.4-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.3.19.4 GR4 (THE SUM CLAUSE). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the triage of the drafts that did not
      *> match (a suspect draft corrected) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SUMMOVE.
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
       01  W-AMT PIC 999.
       01  W-EOF PIC X VALUE "N".
       01  W-LINE PIC X(20) VALUE SPACES.
       01  W-LEN PIC 99 VALUE 0.
       REPORT SECTION.
       RD  RPT
           CONTROL IS FINAL
           PAGE LIMIT IS 60 LINES
           HEADING 1
           FIRST DETAIL 1.
       01  DET-LINE TYPE IS DETAIL LINE PLUS 1.
           05  COLUMN 1 PIC ZZ9.99 SOURCE W-AMT.
       01  TYPE IS CONTROL FOOTING FINAL LINE PLUS 1.
           05  COLUMN 1 PIC ZZ9.99 SUM W-AMT.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RPT-FILE.
           INITIATE RPT.
           MOVE 12 TO W-AMT.
           GENERATE DET-LINE.
           MOVE 30 TO W-AMT.
           GENERATE DET-LINE.
           TERMINATE RPT.
           CLOSE RPT-FILE.
           OPEN INPUT IN-FILE.
           PERFORM READ-PARA UNTIL W-EOF = "Y".
           PERFORM SHOW-PARA.
           CLOSE IN-FILE.
           STOP RUN.
       READ-PARA.
           READ IN-FILE AT END MOVE "Y" TO W-EOF.
           IF W-EOF = "N"
               IF IN-CH < SPACE
                   PERFORM SHOW-PARA
               ELSE
                   ADD 1 TO W-LEN
                   MOVE IN-CH TO W-LINE (W-LEN:1).
       SHOW-PARA.
           IF W-LEN > 0
               DISPLAY W-LINE
               MOVE SPACES TO W-LINE
               MOVE 0 TO W-LEN.
