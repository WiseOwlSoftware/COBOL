      *>--- conformance-case
      *> id: pb5624-present-when-varying-counter-85
      *> rules: [FMT-13.18.64.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5624 - the program of pb5624-present-when-varying-counter
      *> names a VARYING counter, a COBOL-2002 introduction (§2023:13.18.64):
      *> COBOL-85 has no VARYING clause, so it is refused at 85 (COBOL0900).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5624PW8.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "pb5624p8.txt".
           SELECT CHK ASSIGN TO "pb5624p8.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R-VR.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-N    PIC 9 VALUE 2.
       01  WS-EOF  PIC X VALUE "N".
       01  WS-I    PIC 99 VALUE 0.
       01  WS-L    PIC 99 VALUE 1.
       01  WS-LINE PIC X(30) VALUE SPACES.
       REPORT SECTION.
       RD  R-VR PAGE LIMIT 35 LINES.
       01  DET-V TYPE DE.
           02  LINE PLUS 1 OCCURS 3 TIMES VARYING R.
               03  COLUMN 1 PIC X(1) VALUES "A" "B" "C"
                   PRESENT WHEN R NOT = 2.
               03  COLUMN 3 PIC 9 SOURCE R.
               03  COLUMN 5 PIC 99 SUM WS-N
                   PRESENT WHEN R NOT = 1.
       01  DET-W TYPE DE.
           02  LINE PLUS 1 OCCURS 4 TIMES VARYING Q
                   PRESENT WHEN Q NOT = 3.
               03  COLUMN 1 PIC 9 SOURCE Q.
               03  COLUMN 3 PIC X VALUE "x" PRESENT WHEN Q = 2.
           02  LINE PLUS 1.
               03  COLUMN 1 PIC 9 OCCURS 3 TIMES STEP 2
                   VARYING K SOURCE K
                   PRESENT WHEN K NOT = 2.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT PRT.
           INITIATE R-VR.
           GENERATE DET-V.
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
