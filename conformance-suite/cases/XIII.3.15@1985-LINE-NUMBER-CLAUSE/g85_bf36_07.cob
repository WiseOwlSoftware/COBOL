      *>--- conformance-case
      *> id: g85_bf36_07
      *> rules: [1985-GR-XIII.3.15.4-5]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *  §1985:XIII.3.15.4 GR5 - "The NEXT PAGE phrase specifies that
      *    the report group is to be presented beginning on the
      *    indicated line number on a new page", for a body group (fit
      *    test §1985:XIII.3.10.8 GR3 c: a body group already on the
      *    page -> page advance) and for a report footing
      *    (§1985:XIII.3.10.10 GR3 c: "presented on a page on which no
      *    other report group has been presented").
      *  PAGE LIMIT 12 HEADING 1 FD 3 LD 9 FOOTING 10; PH LINE 1, PF
      *  LINE 11, DE-A LINE PLUS 1, DE-N LINE 6 ON NEXT PAGE + LINE PLUS
      *  1, RF LINE 4 ON NEXT PAGE.
      *   G1 DE-A: page 1, LC 1 < 3 -> line 3; LC 3.
      *   G2 DE-N: new page: PF1 at 11, PH2 at 1, then lines 6, 7; LC 7.
      *   G3 DE-A: 7 + 1 = 8 <= 9 -> line 8; LC 8.
      *   TERMINATE: PF2 at 11 (last group of page 2, §1985:XIII.3.20.4
      *     GR7); the RF on page 3 at line 4, with no page heading or
      *     page footing on that page (§1985:XIII.3.20.4 GR3, GR7).
      *  FAILS IF NEXT PAGE does not begin a new page, or the group is
      *  not at its integer line there.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF3607.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS SYM-LF SYM-FF SYM-CR
               ARE 11 13 14.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "g85bf3607.txt".
           SELECT CHK ASSIGN TO "g85bf3607.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R-N.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-EOF  PIC X     VALUE "N".
       01  WS-I    PIC 99    VALUE 0.
       01  WS-LN   PIC 99    VALUE 0.
       01  WS-PG   PIC 9     VALUE 1.
       01  WS-G    PIC 9     VALUE 0.
       01  WS-LC   PIC 99    VALUE 0.
       01  WS-LINE PIC X(20) VALUE SPACES.
       REPORT SECTION.
       RD  R-N PAGE LIMIT 12 LINES HEADING 1 FIRST DETAIL 3
                LAST DETAIL 9 FOOTING 10.
       01  PH-N TYPE PH LINE 1.
           02  COLUMN 1 PIC XX VALUE "PH".
           02  COLUMN 3 PIC 9 SOURCE PAGE-COUNTER.
       01  DE-A TYPE DE LINE PLUS 1.
           02  COLUMN 1 PIC X VALUE "A".
           02  COLUMN 2 PIC 9 SOURCE WS-G.
       01  DE-N TYPE DE.
           02  LINE 6 ON NEXT PAGE.
               03  COLUMN 1 PIC X VALUE "N".
               03  COLUMN 2 PIC 9 SOURCE WS-G.
           02  LINE PLUS 1.
               03  COLUMN 1 PIC X VALUE "n".
               03  COLUMN 2 PIC 9 SOURCE WS-G.
       01  PF-N TYPE PF LINE 11.
           02  COLUMN 1 PIC XX VALUE "PF".
           02  COLUMN 3 PIC 9 SOURCE PAGE-COUNTER.
       01  RF-N TYPE RF LINE 4 ON NEXT PAGE.
           02  COLUMN 1 PIC XX VALUE "RF".
           02  COLUMN 3 PIC 9 SOURCE PAGE-COUNTER.
       PROCEDURE DIVISION.
       MAIN-1.
           OPEN OUTPUT PRT.
           INITIATE R-N.
           MOVE 1 TO WS-G.
           GENERATE DE-A.
           PERFORM SHOW-LC.
           MOVE 2 TO WS-G.
           GENERATE DE-N.
           PERFORM SHOW-LC.
           MOVE 3 TO WS-G.
           GENERATE DE-A.
           PERFORM SHOW-LC.
           TERMINATE R-N.
           CLOSE PRT.
           PERFORM READ-BACK.
           STOP RUN.
       SHOW-LC.
           MOVE LINE-COUNTER TO WS-LC.
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
