      *>--- conformance-case
      *> id: pb6146-absent-earlier-operand-live-column
      *> rules: [GR-13.18.38.4-12, GR-13.18.64.4-4, GR-13.18.41.4-2]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB6146: operand 1 of a multiple COLUMN clause is absent
      *> in occurrence 1 (J = 1 fails PRESENT WHEN), operand 2 is present.
      *> An absent item leaves the horizontal counter where it was
      *> (§2023:13.18.41.4 GR2b), so operand 2 lands at counter 5 + 2 = 7
      *> (§2023:13.18.14.4 GR8), not at the column operand 1 would leave
      *> it on (9). Occurrence 2 (STEP 10) shows J = 2 at 17, 10 columns
      *> right of repetition 0's column 7: "HELLO 2         2".
      *>
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB6146LC.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "pb6146lc.txt".
           SELECT CHK ASSIGN TO "pb6146lc.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R-VR.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-EOF  PIC X VALUE "N".
       01  WS-I    PIC 99 VALUE 0.
       01  WS-L    PIC 99 VALUE 1.
       01  WS-LINE PIC X(30) VALUE SPACES.
       REPORT SECTION.
       RD  R-VR PAGE LIMIT 35 LINES.
       01  DET-W TYPE DE.
           02  LINE PLUS 1.
               03  COLUMN 1 PIC X(5) VALUE "HELLO".
               03  OCCURS 2 TIMES STEP 10 VARYING G.
                   04  COLUMN PLUS 2 PLUS 2 PIC 9
                       VARYING J SOURCE J
                       PRESENT WHEN J NOT = 1.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT PRT.
           INITIATE R-VR.
           GENERATE DET-W.
           TERMINATE R-VR.
           CLOSE PRT.
           OPEN INPUT CHK.
           PERFORM UNTIL WS-EOF = "Y"
               READ CHK
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END PERFORM TAKE-BYTE
               END-READ
           END-PERFORM.
           CLOSE CHK.
           PERFORM SHOW-LINE.
           STOP RUN.
       TAKE-BYTE.
           IF CHK-REC = X"0A"
               PERFORM SHOW-LINE
               ADD 1 TO WS-L
           ELSE
               IF CHK-REC NOT = X"0D"
                   ADD 1 TO WS-I
                   MOVE CHK-REC TO WS-LINE(WS-I:1)
               END-IF
           END-IF.
       SHOW-LINE.
           IF WS-I > 0
               DISPLAY WS-L " [" WS-LINE(1:WS-I) "]"
               MOVE SPACES TO WS-LINE
               MOVE 0 TO WS-I
           END-IF.
