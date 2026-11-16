      *>--- conformance-case
      *> id: g2ctl01_hierarchy_order_cd_control
      *> rules: [2002-GR-13.16.15.3-1, 2002-GR-13.16.15.3-3]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.15.3 GR1 - the order of the CONTROL operands sets
      *> the hierarchy: "The order of appearance of the operands of the
      *> CONTROL clause establishes the levels of the control hierarchy"
      *>   cite.py: OK  §2002:13.16.15.3 GR1
      *> §2002:13.16.15.3 GR3 - the first GENERATE saves the priors;
      *> later ones test "in order major to minor, for equality with the
      *> corresponding prior control", and a change breaks that level and
      *> every lower one.
      *>   cite.py: OK  §2002:13.16.15.3 GR3
      *> The minor/major control CD-B is a CD record item: the 2002 rule
      *> reaches control data items in the communication section too (the
      *> facility is obsolete at 2002 and draws COBOL0903, a warning).
      *> Printing order (§2002:13.16.55.3):
      *>   GR6c "in order of control levels from highest to lowest" (first
      *>   GENERATE) and "in order of control levels from the level of the
      *>   control break down to the lowest" (after a break);
      *>   GR6e "in order of controls from the lowest up to the level of
      *>   the control break" (GENERATE) and "in order of controls from
      *>   lowest to highest" (TERMINATE).
      *>   cite.py: OK for each quotation  §2002:13.16.55.3 GR6c / GR6e
      *> R-1 is CONTROLS ARE WS-A CD-B (WS-A major); R-2 is CONTROLS ARE
      *> CD-B WS-A (CD-B major). Same data, both reports:
      *>   G1 A=1 B=X  first GENERATE  R-1: HA HB D1X     R-2: HB HA D1X
      *>   G2 A=1 B=Y  B changed       R-1: minor break -> FB HB D1Y
      *>                               R-2: major break -> FA FB HB HA D1Y
      *>   G3 A=2 B=Y  A changed       R-1: major break -> FB FA HA HB D2Y
      *>                               R-2: minor break -> FA HA D2Y
      *>   G4 unchanged                both: no break   -> D2Y
      *>   TERMINATE                   R-1: FB FA       R-2: FA FB
      *> Fails if the hierarchy ignored operand order (R-1 and R-2 would
      *> print alike), if a change at the minor level broke the major one
      *> (R-1 G2 or R-2 G3 would print FA), if a major change did not break
      *> the lower level (R-2 G2 or R-1 G3 would lack FB/FA), or if the CD
      *> item were not saved and compared (no break at R-1 G2 / R-2 G2).
      *> R-1 declares its CH and CF groups against the operand order
      *> (CH CD-B before CH WS-A, CF WS-A before CF CD-B) and R-2 in
      *> it, so a hierarchy taken from the order of the group entries
      *> rather than from the operands (GR1) also fails.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CTL01.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS SYM-X0A SYM-X0C SYM-X0D
               ARE 11 13 14.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT1 ASSIGN TO "G2CTL01A.TXT".
           SELECT PRT2 ASSIGN TO "G2CTL01B.TXT".
           SELECT CHK1 ASSIGN TO "G2CTL01A.TXT".
           SELECT CHK2 ASSIGN TO "G2CTL01B.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT1 REPORT IS R-1.
       FD  PRT2 REPORT IS R-2.
       FD  CHK1.
       01  CHK1-REC PIC X.
       FD  CHK2.
       01  CHK2-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-A    PIC X     VALUE SPACE.
       01  WS-EOF  PIC X     VALUE "N".
       01  WS-BYTE PIC X.
       01  WS-TAG  PIC X(2).
       01  WS-I    PIC 99    VALUE 0.
       01  WS-LN   PIC 99    VALUE 0.
       01  WS-LINE PIC X(20) VALUE SPACES.
       COMMUNICATION SECTION.
       CD  CDIN FOR INPUT.
       01  CDIN-REC.
           02  CD-Q    PIC X(12).
           02  CD-B    PIC X.
           02  FILLER  PIC X(74).
       REPORT SECTION.
       RD  R-1 CONTROLS ARE WS-A CD-B
           PAGE LIMIT IS 60 LINES.
       01  TYPE CH CD-B LINE PLUS 1.
           02  COLUMN 1 PIC X(2) VALUE "HB".
       01  TYPE CH WS-A LINE PLUS 1.
           02  COLUMN 1 PIC X(2) VALUE "HA".
       01  D1 TYPE DE LINE PLUS 1.
           02  COLUMN 1 PIC X    VALUE "D".
           02  COLUMN 2 PIC X    SOURCE WS-A.
           02  COLUMN 3 PIC X    SOURCE CD-B.
       01  TYPE CF WS-A LINE PLUS 1.
           02  COLUMN 1 PIC X(2) VALUE "FA".
       01  TYPE CF CD-B LINE PLUS 1.
           02  COLUMN 1 PIC X(2) VALUE "FB".
       RD  R-2 CONTROLS ARE CD-B WS-A
           PAGE LIMIT IS 60 LINES.
       01  TYPE CH CD-B LINE PLUS 1.
           02  COLUMN 1 PIC X(2) VALUE "HB".
       01  TYPE CH WS-A LINE PLUS 1.
           02  COLUMN 1 PIC X(2) VALUE "HA".
       01  D2 TYPE DE LINE PLUS 1.
           02  COLUMN 1 PIC X    VALUE "D".
           02  COLUMN 2 PIC X    SOURCE WS-A.
           02  COLUMN 3 PIC X    SOURCE CD-B.
       01  TYPE CF WS-A LINE PLUS 1.
           02  COLUMN 1 PIC X(2) VALUE "FA".
       01  TYPE CF CD-B LINE PLUS 1.
           02  COLUMN 1 PIC X(2) VALUE "FB".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT PRT1 PRT2.
           INITIATE R-1 R-2.
           MOVE SPACES TO CDIN-REC.
           MOVE "1" TO WS-A.
           MOVE "X" TO CD-B.
           GENERATE D1.
           GENERATE D2.
           MOVE "Y" TO CD-B.
           GENERATE D1.
           GENERATE D2.
           MOVE "2" TO WS-A.
           GENERATE D1.
           GENERATE D2.
           GENERATE D1.
           GENERATE D2.
           TERMINATE R-1 R-2.
           CLOSE PRT1 PRT2.
           MOVE "R1" TO WS-TAG.
           MOVE "N" TO WS-EOF.
           OPEN INPUT CHK1.
           PERFORM UNTIL WS-EOF = "Y"
               READ CHK1
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END
                       MOVE CHK1-REC TO WS-BYTE
                       PERFORM TAKE-BYTE
               END-READ
           END-PERFORM.
           CLOSE CHK1.
           IF WS-I > 0
               PERFORM SHOW-LINE
           END-IF.
           MOVE "R2" TO WS-TAG.
           MOVE "N" TO WS-EOF.
           MOVE 0 TO WS-LN.
           OPEN INPUT CHK2.
           PERFORM UNTIL WS-EOF = "Y"
               READ CHK2
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END
                       MOVE CHK2-REC TO WS-BYTE
                       PERFORM TAKE-BYTE
               END-READ
           END-PERFORM.
           CLOSE CHK2.
           IF WS-I > 0
               PERFORM SHOW-LINE
           END-IF.
           STOP RUN.
       TAKE-BYTE.
           EVALUATE TRUE
               WHEN WS-BYTE = SYM-X0A
                   PERFORM SHOW-LINE
               WHEN WS-BYTE = SYM-X0C
                   IF WS-I > 0
                       PERFORM SHOW-LINE
                   END-IF
               WHEN WS-BYTE = SYM-X0D
                   CONTINUE
               WHEN OTHER
                   ADD 1 TO WS-I
                   MOVE WS-BYTE TO WS-LINE(WS-I:1)
           END-EVALUATE.
       SHOW-LINE.
           ADD 1 TO WS-LN.
           DISPLAY WS-TAG " " WS-LN " [" WS-LINE(1:4) "]".
           MOVE SPACES TO WS-LINE.
           MOVE 0 TO WS-I.
