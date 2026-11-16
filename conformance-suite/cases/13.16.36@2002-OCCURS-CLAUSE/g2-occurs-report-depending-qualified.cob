      *>--- conformance-case
      *> id: g2-occurs-report-depending-qualified
      *> rules: [2002-SR-13.16.36.2-2]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.36.2 SR2 - "Data-name-1 and data-name-2 may be
      *> qualified." The format 3 (report-writer) OCCURS is written
      *> DEPENDING ON WS-N OF C1; WS-N is declared in C1 (value 2) and
      *> in C2 (value 4), so only the qualifier picks the item.
      *> §2002:13.16.36.3 GR13 - 2 is in the range integer-1 to
      *> (integer-2 - 1) = 1 to 3, so the clause has "the same effect as
      *> an OCCURS clause with no TO or DEPENDING phrases and with an
      *> integer-2 equal to the current value of data-name-1": two
      *> repetitions. GR12a - STEP 4 prints each at "a horizontal
      *> distance integer-3 columns to the right of the preceding
      *> occurrence": CCC at column 1 and column 5, "CCC CCC".
      *> With WS-N OF C2 (4, not in 1 to 3) the group would print all
      *> four, "CCC CCC CCC CCC". The line is read back and shown as its
      *> first 15 positions so trailing padding does not enter the
      *> expectation.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2OCCQ3.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "g2occq3.txt".
           SELECT CHK ASSIGN TO "g2occq3.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R-RE.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-EOF  PIC X VALUE "N".
       01  C1.
           05 WS-N PIC 9 VALUE 2.
       01  C2.
           05 WS-N PIC 9 VALUE 4.
       01  WS-I    PIC 999 VALUE 0.
       01  WS-LINE PIC X(200) VALUE SPACES.
       REPORT SECTION.
       RD  R-RE PAGE LIMIT 20 LINES.
       01  DET-A TYPE DE.
           02  LINE PLUS 1.
               03  COLUMN 1 PIC X(3) OCCURS 1 TO 4 TIMES
                   DEPENDING ON WS-N OF C1 STEP 4 VALUE "CCC".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT PRT
           INITIATE R-RE
           GENERATE DET-A
           TERMINATE R-RE
           CLOSE PRT
           OPEN INPUT CHK
           PERFORM UNTIL WS-EOF = "Y"
               READ CHK
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END PERFORM TAKE-BYTE
               END-READ
           END-PERFORM
           CLOSE CHK
           PERFORM SHOW-LINE
           STOP RUN.
       TAKE-BYTE.
           IF CHK-REC = X"0A"
               PERFORM SHOW-LINE
           ELSE
               IF CHK-REC NOT = X"0D" AND WS-I < 200
                   ADD 1 TO WS-I
                   MOVE CHK-REC TO WS-LINE(WS-I:1)
               END-IF
           END-IF.
       SHOW-LINE.
           IF WS-LINE NOT = SPACES
               DISPLAY "[" WS-LINE(1:15) "]"
           END-IF
           MOVE SPACES TO WS-LINE
           MOVE 0 TO WS-I.
