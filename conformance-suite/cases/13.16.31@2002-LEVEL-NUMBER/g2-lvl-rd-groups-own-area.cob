      *>--- conformance-case
      *> id: g2-lvl-rd-groups-own-area
      *> rules: [2002-GR-13.16.31.3-1, 2002-GR-13.16.31.3-3]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.31.3 GR3 - "Multiple level 1 entries subordinate to
      *> a report description entry do not represent implicit
      *> redefinitions of the same area." §2002:13.16.31.3 GR1 - "The
      *> level-number 1 identifies the first entry in each record
      *> description or report group": DA and DB are two report groups
      *> of one RD, each opened by its own level-1 entry.
      *> DA prints AAAAA at column 1 and DB prints BBB at column 1, each
      *> from its own VALUE. GENERATE DA, DB, DA prints three lines:
      *> AAAAA, BBB, AAAAA. Fails if DB shares DA's area (the third line
      *> would carry DB's BBB over DA's content, e.g. BBBAA) or if a
      *> group is not a group of its own.
      *> The report file is read back one byte at a time; each line is
      *> shown as its first 10 positions so the trailing padding of a
      *> printed line does not enter the expectation.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2LVL04.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "g2lvl04.txt".
           SELECT CHK ASSIGN TO "g2lvl04.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS RPT.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-EOF  PIC X VALUE "N".
       01  WS-I    PIC 999 VALUE 0.
       01  WS-LINE PIC X(200) VALUE SPACES.
       REPORT SECTION.
       RD  RPT PAGE LIMIT 20 LINES.
       01  DA TYPE DE.
           02  LINE PLUS 1.
               03  COLUMN 1 PIC X(5) VALUE "AAAAA".
       01  DB TYPE DE.
           02  LINE PLUS 1.
               03  COLUMN 1 PIC X(3) VALUE "BBB".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT PRT
           INITIATE RPT
           GENERATE DA
           GENERATE DB
           GENERATE DA
           TERMINATE RPT
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
               DISPLAY "[" WS-LINE(1:10) "]"
           END-IF
           MOVE SPACES TO WS-LINE
           MOVE 0 TO WS-I.
