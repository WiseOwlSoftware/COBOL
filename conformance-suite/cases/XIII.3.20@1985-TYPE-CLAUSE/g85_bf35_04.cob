      *>--- conformance-case
      *> id: g85_bf35_04
      *> rules: [1985-GR-XIII.3.20.4-13]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.3.20.4 GR13 - prior values: a) a control footing's
      *> USE procedure and SOURCE clauses see the values that detected
      *> the break; b) at TERMINATE the control footings and the report
      *> footing see the prior values as though the highest control
      *> broke; c) every other reference (headings, details) sees the
      *> current values. Pinned by the CF lines at G2, G4, G5 (a: A+1,
      *> A+2, B+2, both in the USE DISPLAY and in the SOURCE columns of
      *> the file), by the T lines (b: C+5 while K1 K2 hold D+9) and by
      *> the CH and DE lines (c).
      *>
      *> SCENARIO. CONTROLS ARE FINAL K1 K2 (K1 PIC X major, K2 PIC S9
      *> SIGN LEADING SEPARATE minor). Every group is LINE PLUS 1 in an
      *> unpaged report (§1985:XIII.3.8.4 GR3) and has a USE BEFORE
      *> REPORTING procedure that DISPLAYs its tag and K1 K2
      *> (§1985:XIII.3.20.4 GR9 a, GR10 c, GR11 b: the USE procedure
      *> runs before the group is presented). Values per GENERATE: G1
      *> A+1, G2 A+2, G3 A+2, G4 B+2, G5 C+5; before TERMINATE D+9. The
      *> report file is read back and each line shown as R P<page>
      *> L<line> <text>; its COLUMN 5/6 SOURCE K1 K2 shows the values
      *> the SOURCE clauses saw.
      *>
      *> DERIVATION. G1, the chronologically first GENERATE: CH FINAL,
      *> CH K1, CH K2 major to minor, then the detail (§1985:XIII.4.3.4
      *> GR5), with the current values A+1, and the RWCS saves A,+1.
      *> G2: K2 changed, K1 did not, so the break is at the K2 level:
      *> CF K2 with the prior values A+1 (§1985:XIII.3.20.4 GR13a,
      *> then CH K2 and the detail with the current A+2 (GR13 c); K1's
      *> groups are not processed (§1985:XIII.4.3.4 GR6 a 2) and 3):
      *> only groups not more major than the highest break level); the
      *> saved set becomes A,+2. G3: no change, no break: the detail
      *> alone. G4: K1 changed (K2 did not): the break is associated
      *> with the HIGHEST level changed, K1, so CF K2 then CF K1 (minor
      *> to major) with the prior A+2, then CH K1, CH K2 (major to
      *> minor) with B+2. G5: both changed: the same shape at the K1
      *> level, prior B+2, current C+5. TERMINATE (values now D+9):
      *> every control footing, minor first, CF K2, CF K1, CF FINAL,
      *> then the report footing, all with the prior set C+5 as though
      *> a break in the most major control had been sensed
      *> (§1985:XIII.4.7.4 GR1, §1985:XIII.3.20.4 GR13b. The file
      *> holds the 22 lines in that order, LINE PLUS 1 each
      *> (§1985:XIII.3.10.8 GR4d.
      *>
      *> Fails if the hierarchy is misordered (a CF K1 at G2, CH groups
      *> minor first, CF groups major first), if a break is sensed at
      *> G3, if the prior values are not saved anew at each break (G4's
      *> CF would show A+1), if a control footing or the report footing
      *> sees current instead of prior values (D+9 at TERMINATE, B+2 at
      *> G4), or if a heading or detail sees prior values.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF3504.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS SYM-LF SYM-CR SYM-FF
               ARE 11 14 13.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "g85bf3504.txt".
           SELECT CHKR ASSIGN TO "g85bf3504.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R.
       FD  CHKR.
       01  CHKR-REC      PIC X.
       WORKING-STORAGE SECTION.
       01  WS-EOF          PIC X     VALUE "N".
       01  WS-B            PIC X.
       01  WS-I            PIC 99    VALUE 0.
       01  WS-E            PIC 99    VALUE 0.
       01  WS-P            PIC 9     VALUE 1.
       01  WS-L            PIC 99    VALUE 1.
       01  WS-TAG          PIC X     VALUE SPACE.
       01  WS-LINE         PIC X(20) VALUE SPACES.
       01  KEYS.
           05  K1          PIC X     VALUE "A".
           05  K2          PIC S9 SIGN LEADING SEPARATE VALUE +1.
       REPORT SECTION.
       RD  R CONTROLS ARE FINAL K1 K2.
       01  CHF TYPE CONTROL HEADING FINAL LINE PLUS 1.
           02  COLUMN 1 PIC X(3) VALUE "CHF".
           02  COLUMN 5 PIC X SOURCE K1.
           02  COLUMN 6 PIC 9 SOURCE K2.
       01  CH1 TYPE CONTROL HEADING K1 LINE PLUS 1.
           02  COLUMN 1 PIC X(3) VALUE "CH1".
           02  COLUMN 5 PIC X SOURCE K1.
           02  COLUMN 6 PIC 9 SOURCE K2.
       01  CH2 TYPE CONTROL HEADING K2 LINE PLUS 1.
           02  COLUMN 1 PIC X(3) VALUE "CH2".
           02  COLUMN 5 PIC X SOURCE K1.
           02  COLUMN 6 PIC 9 SOURCE K2.
       01  DTL TYPE DETAIL LINE PLUS 1.
           02  COLUMN 1 PIC X(3) VALUE "DE ".
           02  COLUMN 5 PIC X SOURCE K1.
           02  COLUMN 6 PIC 9 SOURCE K2.
       01  CF2 TYPE CONTROL FOOTING K2 LINE PLUS 1.
           02  COLUMN 1 PIC X(3) VALUE "CF2".
           02  COLUMN 5 PIC X SOURCE K1.
           02  COLUMN 6 PIC 9 SOURCE K2.
       01  CF1 TYPE CONTROL FOOTING K1 LINE PLUS 1.
           02  COLUMN 1 PIC X(3) VALUE "CF1".
           02  COLUMN 5 PIC X SOURCE K1.
           02  COLUMN 6 PIC 9 SOURCE K2.
       01  CFF TYPE CONTROL FOOTING FINAL LINE PLUS 1.
           02  COLUMN 1 PIC X(3) VALUE "CFF".
           02  COLUMN 5 PIC X SOURCE K1.
           02  COLUMN 6 PIC 9 SOURCE K2.
       01  RFT TYPE REPORT FOOTING LINE PLUS 1.
           02  COLUMN 1 PIC X(3) VALUE "RF ".
           02  COLUMN 5 PIC X SOURCE K1.
           02  COLUMN 6 PIC 9 SOURCE K2.
       PROCEDURE DIVISION.
       DECLARATIVES.
       U-CHF SECTION.
           USE BEFORE REPORTING CHF.
       U-CHF-P.
           DISPLAY "CHF " K1 K2.
       U-CH1 SECTION.
           USE BEFORE REPORTING CH1.
       U-CH1-P.
           DISPLAY "CH1 " K1 K2.
       U-CH2 SECTION.
           USE BEFORE REPORTING CH2.
       U-CH2-P.
           DISPLAY "CH2 " K1 K2.
       U-DTL SECTION.
           USE BEFORE REPORTING DTL.
       U-DTL-P.
           DISPLAY "DE  " K1 K2.
       U-CF2 SECTION.
           USE BEFORE REPORTING CF2.
       U-CF2-P.
           DISPLAY "CF2 " K1 K2.
       U-CF1 SECTION.
           USE BEFORE REPORTING CF1.
       U-CF1-P.
           DISPLAY "CF1 " K1 K2.
       U-CFF SECTION.
           USE BEFORE REPORTING CFF.
       U-CFF-P.
           DISPLAY "CFF " K1 K2.
       U-RFT SECTION.
           USE BEFORE REPORTING RFT.
       U-RFT-P.
           DISPLAY "RF  " K1 K2.
       END DECLARATIVES.
       MAIN-S SECTION.
       MAIN-P.
           OPEN OUTPUT PRT.
           INITIATE R.
           DISPLAY "G1".
           GENERATE DTL.
           DISPLAY "G2".
           MOVE 2 TO K2.
           GENERATE DTL.
           DISPLAY "G3".
           GENERATE DTL.
           DISPLAY "G4".
           MOVE "B" TO K1.
           GENERATE DTL.
           DISPLAY "G5".
           MOVE "C" TO K1.
           MOVE 5 TO K2.
           GENERATE DTL.
           DISPLAY "T".
           MOVE "D" TO K1.
           MOVE 9 TO K2.
           TERMINATE R.
           CLOSE PRT.
           PERFORM READ-R.
           STOP RUN.
       READ-R.
           MOVE "R" TO WS-TAG.
           PERFORM RESET-RB.
           OPEN INPUT CHKR.
           PERFORM UNTIL WS-EOF = "Y"
               READ CHKR
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END
                       MOVE CHKR-REC TO WS-B
                       PERFORM TAKE-BYTE
               END-READ
           END-PERFORM.
           CLOSE CHKR.
           PERFORM SHOW-LINE.
       RESET-RB.
           MOVE "N" TO WS-EOF.
           MOVE 1 TO WS-P.
           MOVE 1 TO WS-L.
           MOVE 0 TO WS-I.
           MOVE 0 TO WS-E.
           MOVE SPACES TO WS-LINE.
       TAKE-BYTE.
           IF WS-B = SYM-LF
               PERFORM SHOW-LINE
               ADD 1 TO WS-L
           ELSE
               IF WS-B = SYM-FF
                   PERFORM SHOW-LINE
                   ADD 1 TO WS-P
                   MOVE 1 TO WS-L
               ELSE
                   IF WS-B NOT = SYM-CR
                       ADD 1 TO WS-I
                       MOVE WS-B TO WS-LINE (WS-I:1)
                       IF WS-B NOT = SPACE
                           MOVE WS-I TO WS-E
                       END-IF
                   END-IF
               END-IF
           END-IF.
       SHOW-LINE.
           IF WS-LINE NOT = SPACES
               DISPLAY WS-TAG " P" WS-P " L" WS-L " "
                   WS-LINE (1:WS-E)
           END-IF.
           MOVE SPACES TO WS-LINE.
           MOVE 0 TO WS-I.
           MOVE 0 TO WS-E.
