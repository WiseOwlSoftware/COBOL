      *>--- conformance-case
      *> id: g85_f07_07
      *> rules: [1985-GR-XIII.3.22.4-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.3.22.4 GR2 - "if the elementary report entry
      *> containing the VALUE clause does not contain a GROUP INDICATE
      *> clause, then the printable item will assume the specified
      *> value each time its report group is printed. However, when the
      *> GROUP INDICATE clause is also present, the specified value
      *> will be presented only when certain object time conditions
      *> exist". Detail DL has VALUE "VV" in column 1 (no GROUP
      *> INDICATE) and VALUE "GI" GROUP INDICATE in column 4. RPT has
      *> neither PAGE nor CONTROL, so by §1985:XIII.3.13.4 GR2 the
      *> indicated item is presented the first time DL is presented
      *> after each INITIATE and spaces are supplied thereafter.
      *> DERIVATION. Three GENERATEs, TERMINATE, INITIATE, one more:
      *>   "VV GI 1", "VV    2", "VV    3", "VV GI 4".
      *> Each leg can fail: a missing "VV" on any line breaks the
      *> first sentence; "GI" on line 2 or 3, or missing on line 1
      *> or 4, breaks the second. The report file is read back one
      *> character at a time and each line DISPLAYed.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F0707.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO "G85F0707.RPT".
           SELECT IN-FILE ASSIGN TO "G85F0707.RPT".
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
           05  COLUMN 1 PIC XX VALUE "VV".
           05  COLUMN 4 PIC XX VALUE "GI" GROUP INDICATE.
           05  COLUMN 7 PIC 9 SOURCE WS-N.
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
