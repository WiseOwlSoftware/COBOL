      *>--- conformance-case
      *> id: g85_bf36_02
      *> rules: [1985-GR-XIII.3.10.8-4, 1985-GR-XIII.3.10.8-6,
      *>    1985-GR-XIII.3.10.8-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *  §1985:XIII.3.10.8 GR4 - body group first print line: a.
      *    absolute integer; b. LC >= FIRST DETAIL and no body group yet
      *    on the page: "the line immediately following the line
      *    indicated by the value contained in LINE-COUNTER"; LC >=
      *    FIRST DETAIL after a body group: "adding the content of
      *    LINE-COUNTER and the integer of the first LINE NUMBER
      *    clause"; LC < FIRST DETAIL: the FIRST DETAIL line; c. "The
      *    body group is not presented"; d. unpaged: LINE-COUNTER +
      *    integer.
      *  §1985:XIII.3.10.8 GR6 - body group final LINE-COUNTER: a.
      *    absolute NEXT GROUP: last line < integer -> the integer;
      *    otherwise the FOOTING line, "in addition the RWCS places the
      *    NEXT GROUP integer into the saved next group integer
      *    location"; b. relative: sum < FOOTING -> the sum, else
      *    FOOTING; c. NEXT PAGE: FOOTING; d. none: the last line; e.
      *    "LINE-COUNTER is unaffected by the processing of a
      *    nonprintable body group"; f. unpaged: last line + integer.
      *  Fit tests §1985:XIII.3.10.8 GR3 (3a absolute, 3b relative trial
      *    sum against LAST DETAIL, 3c NEXT PAGE, the saved next group
      *    integer moved into LINE-COUNTER after the advance); page
      *    heading LINE 1, final LC 1 (§1985:XIII.3.10.7 GR4).
      *  R-1: PAGE LIMIT 30 HEADING 1 FD 5 LD 20 FOOTING 25. Gnn = the
      *  nth GENERATE, printed in the line; LC = LINE-COUNTER after it.
      *   G01 R  first body, LC 1 < 5 -> line 5 (4b); LC 5 (6d)
      *   G02 R  5 >= 5, body before -> 5 + 1 = 6; LC 6
      *   G03 AR 6 < 9 (3a) -> 9, 10 (4a); 10 < NG 14 -> LC 14 (6a)
      *   G04 Q  15 <= 20 -> 15; 15 + 2 = 17 < 25 -> LC 17 (6b)
      *   G05 W  18; 18 + 8 = 26 >= 25 -> LC 25 (6b)
      *   G06 R  25 + 1 > 20 -> advance, PH2 at 1, line 5; LC 5
      *   G07 NP body on page (3c) -> advance, PH3; 7, 8 (4a); LC 8 (6d)
      *   G08 AS 8 < 9 -> 9, 10; 10 >= NG 6 -> LC 25, saved 6 (6a)
      *   G09 R  26 > 20 -> advance, PH4; saved 6 -> LC 6, trial 7 <=
      *          20; 6 >= 5, no body yet -> line 7 (4b); LC 7
      *   G10 Z  nonprintable: not presented (4c), LC stays 7 (6e)
      *   G11 X  line 8; NEXT GROUP NEXT PAGE -> LC 25 (6c)
      *   G12 R  26 > 20 -> advance, PH5; line 5; LC 5
      *  R-2 unpaged: U LINE PLUS 1 NEXT GROUP PLUS 2, V LINE PLUS 1:
      *   G01 U line 1, LC 3 (6f); G02 V 4 (4d, 6d); G03 U 5, LC 7;
      *   G04 V 8.
      *  FAILS IF any arm of GR4 or GR6 is taken wrongly: each changes a
      *  LINE-COUNTER value or a page/line of the read-back.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF3602.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS SYM-LF SYM-FF SYM-CR
               ARE 11 13 14.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "g85bf3602.txt".
           SELECT CHK ASSIGN TO "g85bf3602.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORTS ARE R-1 R-2.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-EOF  PIC X     VALUE "N".
       01  WS-I    PIC 99    VALUE 0.
       01  WS-LN   PIC 99    VALUE 0.
       01  WS-PG   PIC 9     VALUE 1.
       01  WS-G    PIC 99    VALUE 0.
       01  WS-LC   PIC 99    VALUE 0.
       01  WS-ZERO PIC 9     VALUE 0.
       01  WS-LINE PIC X(20) VALUE SPACES.
       REPORT SECTION.
       RD  R-1 PAGE LIMIT 30 LINES HEADING 1 FIRST DETAIL 5
                LAST DETAIL 20 FOOTING 25.
       01  PH-1 TYPE PH LINE 1.
           02  COLUMN 1 PIC XX VALUE "PH".
           02  COLUMN 3 PIC 9 SOURCE PAGE-COUNTER.
       01  DE-R TYPE DE LINE PLUS 1.
           02  COLUMN 1 PIC X VALUE "R".
           02  COLUMN 2 PIC 99 SOURCE WS-G.
       01  DE-AR TYPE DE NEXT GROUP 14.
           02  LINE 9.
               03  COLUMN 1 PIC X VALUE "A".
               03  COLUMN 2 PIC 99 SOURCE WS-G.
           02  LINE PLUS 1.
               03  COLUMN 1 PIC X VALUE "a".
               03  COLUMN 2 PIC 99 SOURCE WS-G.
       01  DE-AS TYPE DE NEXT GROUP 6.
           02  LINE 9.
               03  COLUMN 1 PIC X VALUE "S".
               03  COLUMN 2 PIC 99 SOURCE WS-G.
           02  LINE PLUS 1.
               03  COLUMN 1 PIC X VALUE "s".
               03  COLUMN 2 PIC 99 SOURCE WS-G.
       01  DE-P2 TYPE DE LINE PLUS 1 NEXT GROUP PLUS 2.
           02  COLUMN 1 PIC X VALUE "Q".
           02  COLUMN 2 PIC 99 SOURCE WS-G.
       01  DE-P8 TYPE DE LINE PLUS 1 NEXT GROUP PLUS 8.
           02  COLUMN 1 PIC X VALUE "W".
           02  COLUMN 2 PIC 99 SOURCE WS-G.
       01  DE-NP TYPE DE.
           02  LINE 7 ON NEXT PAGE.
               03  COLUMN 1 PIC X VALUE "N".
               03  COLUMN 2 PIC 99 SOURCE WS-G.
           02  LINE PLUS 1.
               03  COLUMN 1 PIC X VALUE "n".
               03  COLUMN 2 PIC 99 SOURCE WS-G.
       01  DE-NX TYPE DE LINE PLUS 1 NEXT GROUP NEXT PAGE.
           02  COLUMN 1 PIC X VALUE "X".
           02  COLUMN 2 PIC 99 SOURCE WS-G.
       01  DE-Z TYPE DE.
           02  Z-S PIC 9 SOURCE WS-ZERO.
       RD  R-2.
       01  DE-U TYPE DE LINE PLUS 1 NEXT GROUP PLUS 2.
           02  COLUMN 1 PIC X VALUE "U".
           02  COLUMN 2 PIC 99 SOURCE WS-G.
       01  DE-V TYPE DE LINE PLUS 1.
           02  COLUMN 1 PIC X VALUE "V".
           02  COLUMN 2 PIC 99 SOURCE WS-G.
       PROCEDURE DIVISION.
       MAIN-1.
           DISPLAY "==== R-1".
           OPEN OUTPUT PRT.
           INITIATE R-1.
           ADD 1 TO WS-G.
           GENERATE DE-R.
           PERFORM SHOW-LC-1.
           ADD 1 TO WS-G.
           GENERATE DE-R.
           PERFORM SHOW-LC-1.
           ADD 1 TO WS-G.
           GENERATE DE-AR.
           PERFORM SHOW-LC-1.
           ADD 1 TO WS-G.
           GENERATE DE-P2.
           PERFORM SHOW-LC-1.
           ADD 1 TO WS-G.
           GENERATE DE-P8.
           PERFORM SHOW-LC-1.
           ADD 1 TO WS-G.
           GENERATE DE-R.
           PERFORM SHOW-LC-1.
           ADD 1 TO WS-G.
           GENERATE DE-NP.
           PERFORM SHOW-LC-1.
           ADD 1 TO WS-G.
           GENERATE DE-AS.
           PERFORM SHOW-LC-1.
           ADD 1 TO WS-G.
           GENERATE DE-R.
           PERFORM SHOW-LC-1.
           ADD 1 TO WS-G.
           GENERATE DE-Z.
           PERFORM SHOW-LC-1.
           ADD 1 TO WS-G.
           GENERATE DE-NX.
           PERFORM SHOW-LC-1.
           ADD 1 TO WS-G.
           GENERATE DE-R.
           PERFORM SHOW-LC-1.
           TERMINATE R-1.
           CLOSE PRT.
           PERFORM READ-BACK.
           DISPLAY "==== R-2".
           MOVE 0 TO WS-G.
           OPEN OUTPUT PRT.
           INITIATE R-2.
           ADD 1 TO WS-G.
           GENERATE DE-U.
           PERFORM SHOW-LC-2.
           ADD 1 TO WS-G.
           GENERATE DE-V.
           PERFORM SHOW-LC-2.
           ADD 1 TO WS-G.
           GENERATE DE-U.
           PERFORM SHOW-LC-2.
           ADD 1 TO WS-G.
           GENERATE DE-V.
           PERFORM SHOW-LC-2.
           TERMINATE R-2.
           CLOSE PRT.
           PERFORM READ-BACK.
           STOP RUN.
       SHOW-LC-1.
           MOVE LINE-COUNTER OF R-1 TO WS-LC.
           DISPLAY "G" WS-G " LINE-COUNTER " WS-LC.
       SHOW-LC-2.
           MOVE LINE-COUNTER OF R-2 TO WS-LC.
           DISPLAY "G" WS-G " LINE-COUNTER " WS-LC.
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
