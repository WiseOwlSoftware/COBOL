      *>--- conformance-case
      *> id: pb5316_level_49_format_3
      *> rules: [SR-13.15.3-3]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *>---
      *> kb/Work PB5316 - 1985:XIII.3.9.3 SR3: "In format 2 the level-
      *> number may be any integer from 02 to 48 inclusive. In format 3
      *> the level-number may be any integer from 02 to 49 inclusive."
      *> (cite.py: OK). The group below is a format 1 entry, a format 2
      *> entry at level 48 (its LINE clause) and a format 3 entry at level
      *> 49 (PICTURE, COLUMN, SOURCE), each at a level SR3 admits, so it
      *> compiles at every edition (2023: 13.15.3 SR3, 1 through 49).
      *> DERIVATION: WS-A = -3 under PIC S99 SIGN LEADING SEPARATE at
      *> column 1 prints `-03`. The read-back is byte-wise (see
      *> pb1292_source_forms_85). The refusals are
      *> pb5316-level-01-format-3-clauses and pb5316-level-49-format-2.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5316D.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS SYM-X0A SYM-X0D
               ARE 11 14.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "pb5316d.txt".
           SELECT CHK ASSIGN TO "pb5316d.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS RPT.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-EOF PIC X VALUE "N".
       01  WS-I   PIC 99 VALUE 0.
       01  WS-LINE PIC X(30) VALUE SPACES.
       01  WS-A PIC S99 VALUE -3.
       REPORT SECTION.
       RD  RPT PAGE LIMIT 20 LINES.
       01  DL TYPE IS DETAIL.
           48  LINE PLUS 1.
               49  COLUMN 1 PIC S99 SIGN LEADING SEPARATE
                   SOURCE WS-A.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT PRT.
           INITIATE RPT.
           GENERATE DL.
           TERMINATE RPT.
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
           IF CHK-REC = SYM-X0A
               PERFORM SHOW-LINE
           ELSE
               IF CHK-REC NOT = SYM-X0D
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
