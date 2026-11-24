      *>--- conformance-case
      *> id: pb5624-present-when-absent-operand-anchor
      *> rules: [GR-13.18.38.4-12, GR-13.18.64.4-4, GR-13.18.41.4-2]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5624 review finding R2-1: the multi-COLUMN shape of
      *> R1-1. Every relative operand of a multiple COLUMN clause under a
      *> STEPped OCCURS has its own step anchor, written for repetition 0
      *> whether or not that operand is present (§2023:13.18.38.4 GR12).
      *> HELLO fills 1-5; operand 1 of occurrence 1 is at 7, operand 2
      *> (absent: J = 2, G = 1) would be at 9. Occurrence 2 (STEP 10)
      *> places J = 1 at 17 and J = 2 at 19: "HELLO 1         1 2".
      *>
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5624AO.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "pb5624ao.txt".
           SELECT CHK ASSIGN TO "pb5624ao.txt".
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
                       PRESENT WHEN J NOT = 2 OR G = 2.
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
