      *>--- conformance-case
      *> id: 1985-GR-XIII_3_10_7-p4
      *> rules: [1985-GR-XIII.3.10.7-p3, 1985-GR-XIII.3.10.7-p4, 1985-GR-XIII.3.10.9-p3, 1985-GR-XIII.3.10.9-p4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.3.10.7 p4; §1985:XIII.3.10.7 p5; §1985:XIII.3.10.9
      *> p4; §1985:XIII.3.10.9 p5 (PRESENTATION RULES TABLES). An output
      *> test: the program must translate and run at 85 and DISPLAY
      *> exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-99.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      *  g3-blank-row: §1985:XIII.3.10.7 and XIII.3.10.9, Tables 2 and
      *  4 footnotes ** and ***: a page heading and a page footing with
      *  NO LINE NUMBER and NO NEXT GROUP clause take the blank/blank
      *  row: only rules 3c/4b (PH) and 3b/5d (PF) apply, no limit or
      *  next group rule. Expected: neither group is presented, and
      *  LINE-COUNTER is unaffected: 00 before DE, 05 before RF.
      *  PAGE LIMIT 30 HEADING 1 FD 3 LD 15 FOOTING 20; DE LINE 5;
      *  RF LINE PLUS 2 with no page footing presented -> 2 + 20 = 22.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G3BLANK.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS SYM-LF SYM-FF SYM-CR
               ARE 11 13 14.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "g3blank.txt".
           SELECT CHK ASSIGN TO "g3blank.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R-T.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-EOF  PIC X     VALUE "N".
       01  WS-I    PIC 99    VALUE 0.
       01  WS-LN   PIC 99    VALUE 0.
       01  WS-PG   PIC 9     VALUE 1.
       01  WS-LC   PIC 99    VALUE 0.
       01  WS-ZERO PIC 9     VALUE 0.
       01  WS-LINE PIC X(20) VALUE SPACES.
       01  WS-OK   PIC 9     VALUE 0.
       REPORT SECTION.
       RD  R-T PAGE LIMIT 30 LINES HEADING 1 FIRST DETAIL 3
                LAST DETAIL 15 FOOTING 20.
       01  PH-T TYPE PH.
           02  PH-T-S PIC 9 SOURCE WS-ZERO.
       01  DE-T TYPE DE LINE 5.
           02  COLUMN 1 PIC X(3) VALUE "DET".
       01  PF-T TYPE PF.
           02  PF-T-S PIC 9 SOURCE WS-ZERO.
       01  RF-T TYPE RF LINE PLUS 2.
           02  COLUMN 1 PIC X(3) VALUE "RFT".
       PROCEDURE DIVISION.
       DECLARATIVES.
       DD SECTION.
           USE BEFORE REPORTING DE-T.
       DD-1.
           MOVE LINE-COUNTER OF R-T TO WS-LC.
           DISPLAY "BEFORE DE LINE-COUNTER " WS-LC.
           IF WS-LC = 0 ADD 1 TO WS-OK.
       DT SECTION.
           USE BEFORE REPORTING RF-T.
       DT-1.
           MOVE LINE-COUNTER OF R-T TO WS-LC.
           DISPLAY "BEFORE RF LINE-COUNTER " WS-LC.
           IF WS-LC = 5 ADD 1 TO WS-OK.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-1.
           OPEN OUTPUT PRT.
           INITIATE R-T.
           GENERATE DE-T.
           TERMINATE R-T.
           CLOSE PRT.
           PERFORM READ-BACK.
           IF WS-OK = 4
               DISPLAY "BLANK-ROW OK"
           ELSE
               DISPLAY "BLANK-ROW FAIL " WS-OK
           END-IF.
           STOP RUN.
       READ-BACK.
           OPEN INPUT CHK.
           PERFORM UNTIL WS-EOF = "Y"
               READ CHK
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END PERFORM TAKE-BYTE
               END-READ
           END-PERFORM.
           CLOSE CHK.
           IF WS-I > 0
               PERFORM SHOW-LINE
           END-IF.
       TAKE-BYTE.
           IF CHK-REC = SYM-LF
               PERFORM SHOW-LINE
           ELSE
               IF CHK-REC = SYM-FF
                   IF WS-I > 0
                       PERFORM SHOW-LINE
                   END-IF
                   ADD 1 TO WS-PG
                   MOVE 0 TO WS-LN
               ELSE
                   IF CHK-REC NOT = SYM-CR
                       ADD 1 TO WS-I
                       MOVE CHK-REC TO WS-LINE(WS-I:1)
                   END-IF
               END-IF
           END-IF.
       SHOW-LINE.
           ADD 1 TO WS-LN.
           IF WS-LINE NOT = SPACES
               DISPLAY "PAGE " WS-PG " LINE " WS-LN " [" WS-LINE(1:4)
                   "]"
               IF WS-LN = 5 AND WS-LINE(1:3) = "DET" ADD 1 TO WS-OK
               END-IF
               IF WS-LN = 22 AND WS-LINE(1:3) = "RFT" ADD 1 TO WS-OK
               END-IF
           END-IF.
           MOVE SPACES TO WS-LINE.
           MOVE 0 TO WS-I.
