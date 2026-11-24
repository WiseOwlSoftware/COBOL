      *>--- conformance-case
      *> id: pb5624-present-when-varying-counter
      *> rules: [SR-13.18.64.3-2, GR-13.18.64.4-4, GR-13.18.41.4-2,
      *>     GR-13.18.38.4-11]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5624 - a report VARYING counter named in the PRESENT
      *> WHEN condition of its own entry or of a subordinate entry.
      *>
      *> "This definition of data-name-1 may be referenced only within
      *> the current entry or a subordinate entry." (§2023:13.18.64.3
      *> SR2; cite.py OK)
      *>
      *> "Each value of data-name-1, established in this way, persists
      *> throughout the processing of the associated occurrence of the
      *> data item or report item." (§2023:13.18.64.4 GR4; cite.py OK)
      *>
      *> "condition-1 of each PRESENT WHEN clause is evaluated before
      *> the processing of any LINE clauses for the report group."
      *> (§2023:13.18.41.4 GR2; cite.py OK)
      *>
      *> "If a VARYING clause is present, the action of these clauses
      *> may vary from one repetition to another." (§2023:13.18.38.4
      *> GR11; cite.py OK)
      *>
      *> Expected, from those rules: the condition is evaluated for
      *> each occurrence with the counter at that occurrence's value.
      *> DET-V: R = 1, 2, 3; the COLUMN 1 item is present unless R = 2:
      *> "A 1", "  2 02", "C 3 02" (the SUM entry of WS-N = 2 is
      *> absent at R = 1; VALUES go to successive repetitions, so
      *> the third operand stays with R = 3 although the second item is
      *> absent). DET-W: the LINE entry itself says PRESENT WHEN Q NOT
      *> = 3, so the third of four lines is absent: "1", "2 x" (the
      *> item present only when Q = 2), "4"; the STEP 2 row has K = 1,
      *> 2, 3 at columns 1, 3, 5, the item for K = 2 absent: "1   3".
      *>
      *> Each leg fails if the counter reads a stale or the first
      *> occurrence's value, or if the line entry's own condition is
      *> evaluated without its counter (the program is then refused:
      *> COBOL1639).
      *>
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5624PW.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "pb5624pw.txt".
           SELECT CHK ASSIGN TO "pb5624pw.txt".
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
