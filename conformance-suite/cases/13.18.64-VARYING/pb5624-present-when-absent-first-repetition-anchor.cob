      *>--- conformance-case
      *> id: pb5624-present-when-absent-first-repetition-anchor
      *> rules: [GR-13.18.38.4-12, GR-13.18.64.4-4, GR-13.18.41.4-2]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5624 review finding R1-1: a STEPped entry with a
      *> relative COLUMN whose first repetition is absent (PRESENT WHEN
      *> K NOT = 1) still fixes the step anchor, because §2023:13.18.38.4
      *> GR12 measures each later repetition from the column repetition 0
      *> would occupy. HELLO fills columns 1-5; the anchor is 5 + 2 = 7;
      *> K = 2 lands at 9 and K = 3 at 11: "HELLO   2 3".
      *>
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5624AN.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "pb5624an.txt".
           SELECT CHK ASSIGN TO "pb5624an.txt".
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
               03  COLUMN PLUS 2 PIC 9 OCCURS 3 TIMES STEP 2
                   VARYING K SOURCE K
                   PRESENT WHEN K NOT = 1.
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
