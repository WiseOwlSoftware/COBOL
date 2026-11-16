      *>--- conformance-case
      *> id: g85_bf36_04
      *> rules: [1985-GR-XIII.3.10.9-5]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *  §1985:XIII.3.10.9 GR5 - page footing final LINE-COUNTER: a.
      *    absolute NEXT GROUP: "places the NEXT GROUP integer into
      *    LINE-COUNTER"; b. relative: "the sum of the NEXT GROUP
      *    integer and the line number on which the final print line of
      *    the page footing report group was presented"; c. none: its
      *    final print line; d. "LINE-COUNTER is unaffected by the
      *    processing of a nonprintable report group".
      *  The "BEFORE RF" line shows that setting (a USE BEFORE REPORTING
      *  procedure runs just before its group, §1985:XIII.4.9.4 GR4);
      *  the report footing then sits at that setting + its integer, or,
      *  with no page footing presented, integer + FOOTING
      *  (§1985:XIII.3.10.10
      *  GR3 b). At TERMINATE the page footing is "the second to last
      *  report group on a page" before the report footing
      *  (§1985:XIII.3.20.4 GR7).
      *  All four: PAGE LIMIT 30 HEADING 1 FD 3 LD 15 FOOTING 20; DE at
      *    3.
      *   R-P PF LINE 22 NEXT GROUP 25: LC 25 (a); RF PLUS 2 -> 27.
      *   R-Q PF LINE 22, PLUS 1, NEXT GROUP PLUS 2: 22, 23; LC 25 (b);
      *       RF PLUS 3 -> 28.
      *   R-S PF LINE 22: LC 22 (c); RF PLUS 2 -> 24.
      *   R-T PF nonprintable: LC stays 3 (d); RF PLUS 2 -> 2 + 20 = 22.
      *  FAILS IF any arm stores the wrong setting: the BEFORE RF value
      *  and the report footing's line both move.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF3604.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS SYM-LF SYM-FF SYM-CR
               ARE 11 13 14.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "g85bf3604.txt".
           SELECT CHK ASSIGN TO "g85bf3604.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORTS ARE R-P R-Q R-S R-T.
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
       REPORT SECTION.
       RD  R-P PAGE LIMIT 30 LINES HEADING 1 FIRST DETAIL 3
                LAST DETAIL 15 FOOTING 20.
       01  DE-P TYPE DE LINE PLUS 1.
           02  COLUMN 1 PIC X(3) VALUE "DEP".
       01  PF-P TYPE PF LINE 22 NEXT GROUP 25.
           02  COLUMN 1 PIC X(3) VALUE "PFP".
       01  RF-P TYPE RF LINE PLUS 2.
           02  COLUMN 1 PIC X(3) VALUE "RFP".
       RD  R-Q PAGE LIMIT 30 LINES HEADING 1 FIRST DETAIL 3
                LAST DETAIL 15 FOOTING 20.
       01  DE-Q TYPE DE LINE PLUS 1.
           02  COLUMN 1 PIC X(3) VALUE "DEQ".
       01  PF-Q TYPE PF NEXT GROUP PLUS 2.
           02  LINE 22.
               03  COLUMN 1 PIC X(3) VALUE "PQ1".
           02  LINE PLUS 1.
               03  COLUMN 1 PIC X(3) VALUE "PQ2".
       01  RF-Q TYPE RF LINE PLUS 3.
           02  COLUMN 1 PIC X(3) VALUE "RFQ".
       RD  R-S PAGE LIMIT 30 LINES HEADING 1 FIRST DETAIL 3
                LAST DETAIL 15 FOOTING 20.
       01  DE-S TYPE DE LINE PLUS 1.
           02  COLUMN 1 PIC X(3) VALUE "DES".
       01  PF-S TYPE PF LINE 22.
           02  COLUMN 1 PIC X(3) VALUE "PFS".
       01  RF-S TYPE RF LINE PLUS 2.
           02  COLUMN 1 PIC X(3) VALUE "RFS".
       RD  R-T PAGE LIMIT 30 LINES HEADING 1 FIRST DETAIL 3
                LAST DETAIL 15 FOOTING 20.
       01  DE-T TYPE DE LINE PLUS 1.
           02  COLUMN 1 PIC X(3) VALUE "DET".
       01  PF-T TYPE PF.
           02  PF-T-S PIC 9 SOURCE WS-ZERO.
       01  RF-T TYPE RF LINE PLUS 2.
           02  COLUMN 1 PIC X(3) VALUE "RFT".
       PROCEDURE DIVISION.
       DECLARATIVES.
       DP SECTION.
           USE BEFORE REPORTING RF-P.
       DP-1.
           MOVE LINE-COUNTER OF R-P TO WS-LC.
           DISPLAY "BEFORE RF-P LINE-COUNTER " WS-LC.
       DQ SECTION.
           USE BEFORE REPORTING RF-Q.
       DQ-1.
           MOVE LINE-COUNTER OF R-Q TO WS-LC.
           DISPLAY "BEFORE RF-Q LINE-COUNTER " WS-LC.
       DS SECTION.
           USE BEFORE REPORTING RF-S.
       DS-1.
           MOVE LINE-COUNTER OF R-S TO WS-LC.
           DISPLAY "BEFORE RF-S LINE-COUNTER " WS-LC.
       DT SECTION.
           USE BEFORE REPORTING RF-T.
       DT-1.
           MOVE LINE-COUNTER OF R-T TO WS-LC.
           DISPLAY "BEFORE RF-T LINE-COUNTER " WS-LC.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-1.
           DISPLAY "==== R-P".
           OPEN OUTPUT PRT.
           INITIATE R-P.
           GENERATE DE-P.
           TERMINATE R-P.
           CLOSE PRT.
           PERFORM READ-BACK.
           DISPLAY "==== R-Q".
           OPEN OUTPUT PRT.
           INITIATE R-Q.
           GENERATE DE-Q.
           TERMINATE R-Q.
           CLOSE PRT.
           PERFORM READ-BACK.
           DISPLAY "==== R-S".
           OPEN OUTPUT PRT.
           INITIATE R-S.
           GENERATE DE-S.
           TERMINATE R-S.
           CLOSE PRT.
           PERFORM READ-BACK.
           DISPLAY "==== R-T".
           OPEN OUTPUT PRT.
           INITIATE R-T.
           GENERATE DE-T.
           TERMINATE R-T.
           CLOSE PRT.
           PERFORM READ-BACK.
           STOP RUN.
       READ-BACK.
           MOVE "N" TO WS-EOF.
           MOVE 0 TO WS-LN.
           MOVE 0 TO WS-I.
           MOVE 1 TO WS-PG.
           MOVE SPACES TO WS-LINE.
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
           END-IF.
           MOVE SPACES TO WS-LINE.
           MOVE 0 TO WS-I.
