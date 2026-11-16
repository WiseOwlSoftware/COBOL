      *>--- conformance-case
      *> id: g2-occurs-fmt3-report
      *> rules: [2002-FMT-13.16.36.1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> OCCURS format 3 (report-writer), §2002:13.16.36.1:
      *>   OCCURS [integer-1 TO] integer-2 TIMES [DEPENDING ON data-name-1]
      *>   [STEP integer-3], each phrase written at least once.
      *> L1 OCCURS 3 TIMES STEP 4 on an absolute COLUMN 1 (STEP required,
      *>   §2002:13.16.36.2 SR22c): §2002:13.16.36.3 GR12a puts each
      *>   successive occurrence 4 columns right of the preceding one, so
      *>   columns 1, 5, 9; GR11 gives VALUE "AAA" to each repetition:
      *>   [AAA AAA AAA].
      *> L2 OCCURS 3 TIMES with no optional phrase on COLUMN PLUS 2:
      *>   GR10a, each repetition behaves as though it had COLUMN PLUS 2;
      *>   the interval comes from the relative COLUMN (GR12, last
      *>   sentence). Horizontal counter (§2002:13.16.14.3 GR7-GR9):
      *>   0+2=2 (counter 3), 3+2=5 (counter 6), 6+2=8: [ BB BB BB].
      *> L3/L4 OCCURS 2 TIMES on a relative LINE PLUS 1 entry, no STEP:
      *>   GR10c, each repetition behaves as though it had LINE PLUS 1,
      *>   so two consecutive lines [DD] [DD].
      *> L5 OCCURS 1 TO 4 TIMES DEPENDING ON WS-N STEP 4, WS-N = 2: in the
      *>   range 1 to (4 - 1), so GR13 acts as OCCURS 2 TIMES: [CCC CCC].
      *> L6 the same group generated again with WS-N = 0: not in the range
      *>   1 to 3, so GR13 processes it as though TO and DEPENDING were
      *>   absent, integer-2 = 4 occurrences: [CCC CCC CCC CCC].
      *> The DEPENDING entry is the last in its group (SR24).
      *> The report file is read back one character at a time, newline
      *> ending a line, and blank lines are skipped.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2OCC3.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "g2occ3.txt".
           SELECT CHK ASSIGN TO "g2occ3.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R-O3.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-EOF  PIC X VALUE "N".
       01  WS-N    PIC 9 VALUE 2.
       01  WS-I    PIC 99 VALUE 0.
       01  WS-LINE PIC X(30) VALUE SPACES.
       REPORT SECTION.
       RD  R-O3 PAGE LIMIT 20 LINES.
       01  DET-A TYPE DE.
           02  LINE PLUS 1.
               03  COLUMN 1 PIC X(3) OCCURS 3 TIMES STEP 4
                   VALUE "AAA".
           02  LINE PLUS 1.
               03  COLUMN PLUS 2 PIC X(2) OCCURS 3 TIMES VALUE "BB".
           02  LINE PLUS 1 OCCURS 2 TIMES.
               03  COLUMN 1 PIC X(2) VALUE "DD".
       01  DET-B TYPE DE.
           02  LINE PLUS 1.
               03  COLUMN 1 PIC X(3) OCCURS 1 TO 4 TIMES
                   DEPENDING ON WS-N STEP 4 VALUE "CCC".
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT PRT.
           INITIATE R-O3.
           GENERATE DET-A.
           GENERATE DET-B.
           MOVE 0 TO WS-N.
           GENERATE DET-B.
           TERMINATE R-O3.
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
           ELSE
               IF CHK-REC NOT = X"0D"
                   ADD 1 TO WS-I
                   MOVE CHK-REC TO WS-LINE(WS-I:1)
               END-IF
           END-IF.
       SHOW-LINE.
           IF WS-I > 0
               DISPLAY "[" WS-LINE(1:WS-I) "]"
               MOVE SPACES TO WS-LINE
               MOVE 0 TO WS-I
           END-IF.
