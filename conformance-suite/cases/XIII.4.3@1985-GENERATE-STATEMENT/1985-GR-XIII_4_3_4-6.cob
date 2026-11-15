      *>--- conformance-case
      *> id: 1985-GR-XIII_4_3_4-6
      *> rules: [1985-GR-XIII.4.3.4-6]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.4.3.4 GR6 (THE GENERATE STATEMENT). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the witnesses-85m drafting pass (a
      *> first draft for a 1985 rule no pass had drafted, its
      *> expectation derived from the rule's text and its citations
      *> checked with cite.py) by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. GENSTP.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO "GENSTP.RPT".
           SELECT IN-FILE ASSIGN TO "GENSTP.RPT".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE
           REPORT IS RPT.
       FD  IN-FILE
           RECORD CONTAINS 1 CHARACTERS.
       01  IN-CH PIC X.
       WORKING-STORAGE SECTION.
       01  WS-MAJ PIC 9 VALUE 1.
       01  WS-MIN PIC 9 VALUE 1.
       01  WS-EOF PIC X VALUE "N".
       01  WS-LINE PIC X(40) VALUE SPACES.
       01  WS-LEN PIC 99 VALUE 0.
       REPORT SECTION.
       RD  RPT
           CONTROLS ARE WS-MAJ WS-MIN.
       01  CHA TYPE IS CONTROL HEADING WS-MAJ LINE PLUS 1.
           05  COLUMN 1 PIC X(6) VALUE "CH-MAJ".
           05  COLUMN 8 PIC 9 SOURCE WS-MAJ.
       01  CHI TYPE IS CONTROL HEADING WS-MIN LINE PLUS 1.
           05  COLUMN 1 PIC X(6) VALUE "CH-MIN".
           05  COLUMN 8 PIC 9 SOURCE WS-MIN.
       01  DL TYPE IS DETAIL LINE PLUS 1.
           05  COLUMN 1 PIC XX VALUE "DE".
           05  COLUMN 4 PIC 9 SOURCE WS-MAJ.
           05  COLUMN 6 PIC 9 SOURCE WS-MIN.
       01  CFI TYPE IS CONTROL FOOTING WS-MIN LINE PLUS 1.
           05  COLUMN 1 PIC X(6) VALUE "CF-MIN".
           05  COLUMN 8 PIC 9 SOURCE WS-MIN.
       01  CFA TYPE IS CONTROL FOOTING WS-MAJ LINE PLUS 1.
           05  COLUMN 1 PIC X(6) VALUE "CF-MAJ".
           05  COLUMN 8 PIC 9 SOURCE WS-MAJ.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RPT-FILE.
           INITIATE RPT.
           GENERATE DL.
           MOVE 2 TO WS-MIN.
           GENERATE DL.
           MOVE 2 TO WS-MAJ.
           MOVE 1 TO WS-MIN.
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
