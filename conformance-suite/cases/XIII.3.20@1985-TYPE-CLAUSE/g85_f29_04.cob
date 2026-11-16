      *>--- conformance-case
      *> id: g85_f29_04
      *> rules: [1985-GR-XIII.3.20.4-1, 1985-GR-XIII.3.20.4-5,
      *>    1985-GR-XIII.3.20.4-9, 1985-GR-XIII.3.20.4-11]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.3.20.4 GR1 - detail groups are processed as a
      *>   direct result of GENERATE; every other type is processed
      *>   automatically. Pinned by "INITIATED" being the first line
      *>   (INITIATE processes no group) and by RH, CH, CF and RF
      *>   running with no statement naming them.
      *> §1985:XIII.3.20.4 GR5 - the DETAIL group is processed when a
      *>   corresponding GENERATE is executed: one U-DE line per
      *>   GENERATE.
      *> §1985:XIII.3.20.4 GR9 - non-detail groups: a. the USE BEFORE
      *>   REPORTING procedure runs; b. after SUPPRESS nothing more
      *>   (CH FINAL's USE suppresses it: U-CHF is shown, no CHF line
      *>   is printed); c. otherwise it is presented (RH, CHK and RF
      *>   lines; the CFK and CFF lines are control footings,
      *>   presented by §1985:XIII.3.20.4 GR10e, not GR9).
      *> §1985:XIII.3.20.4 GR11 - detail processing: a. subtotalling;
      *>   b. the USE procedure; c. SUPPRESS ends it; d. a GENERATE
      *>   report-name ends it; e. otherwise presented. The second
      *>   GENERATE DE-G is suppressed in its USE (no "DE 2" line),
      *>   and GENERATE RPT (one detail group, so 11a-11e apply as
      *>   though GENERATE DE-G) runs U-DE but prints no "DE 4" line.
      *>   Both still subtotal (11a precedes 11b), so the first CFK
      *>   sum is 1 + 2 + 4 = 7.
      *> DERIVATION of the run (unpaged, every LINE PLUS 1, so the
      *> printed lines are consecutive; SUPPRESS leaves LINE-COUNTER
      *> alone, §1985:XIII.3.5.6 GR5):
      *>   GENERATE DE-G (K=1,V=1), first: U-RH, U-CHF, U-CHK, U-DE
      *>     (§1985:XIII.4.3.4 GR5) -> lines RH, CHK, DE 1
      *>   GENERATE DE-G (V=2, suppressed): U-DE
      *>   GENERATE RPT (V=4): U-DE
      *>   GENERATE DE-G (K=2,V=8): U-CFK 07, U-CHK, U-DE
      *>     -> lines CFK 07, CHK, DE 8
      *>   TERMINATE: U-CFK 08, U-RF -> lines CFK 08, CFF, RF
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F2904.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS SYM-LF SYM-CR SYM-FF
               ARE 11 14 13.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "g85f2904.txt".
           SELECT CHK ASSIGN TO "g85f2904.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS RPT.
       FD  CHK.
       01  CHK-REC         PIC X.
       WORKING-STORAGE SECTION.
       01  WS-K            PIC X     VALUE "1".
       01  WS-V            PIC 9     VALUE 0.
       01  WS-SUP          PIC X     VALUE "N".
       01  WS-N            PIC 99    VALUE 0.
       01  WS-EOF          PIC X     VALUE "N".
       01  WS-I            PIC 99    VALUE 0.
       01  WS-LN           PIC 99    VALUE 0.
       01  WS-LINE         PIC X(20) VALUE SPACES.
       REPORT SECTION.
       RD  RPT CONTROLS ARE FINAL WS-K.
       01  RH-G TYPE REPORT HEADING LINE PLUS 1.
           02  COLUMN 1 PIC X(2) VALUE "RH".
       01  CHF-G TYPE CONTROL HEADING FINAL LINE PLUS 1.
           02  COLUMN 1 PIC X(3) VALUE "CHF".
       01  CHK-G TYPE CH WS-K LINE PLUS 1.
           02  COLUMN 1 PIC X(3) VALUE "CHK".
       01  DE-G TYPE DETAIL LINE PLUS 1.
           02  COLUMN 1 PIC X(2) VALUE "DE".
           02  COLUMN 4 PIC 9 SOURCE WS-V.
       01  CFK-G TYPE CF WS-K LINE PLUS 1.
           02  COLUMN 1 PIC X(3) VALUE "CFK".
           02  TK COLUMN 5 PIC 99 SUM WS-V.
       01  CFF-G TYPE CONTROL FOOTING FINAL LINE PLUS 1.
           02  COLUMN 1 PIC X(3) VALUE "CFF".
       01  RF-G TYPE RF LINE PLUS 1.
           02  COLUMN 1 PIC X(2) VALUE "RF".
       PROCEDURE DIVISION.
       DECLARATIVES.
       U-RH SECTION.
           USE BEFORE REPORTING RH-G.
       U-RH-P.
           DISPLAY "U-RH".
       U-CHF SECTION.
           USE BEFORE REPORTING CHF-G.
       U-CHF-P.
           DISPLAY "U-CHF".
           SUPPRESS PRINTING.
       U-CHK SECTION.
           USE BEFORE REPORTING CHK-G.
       U-CHK-P.
           DISPLAY "U-CHK".
       U-DE SECTION.
           USE BEFORE REPORTING DE-G.
       U-DE-P.
           DISPLAY "U-DE " WS-V.
           IF WS-SUP = "Y"
               SUPPRESS PRINTING
           END-IF.
       U-CFK SECTION.
           USE BEFORE REPORTING CFK-G.
       U-CFK-P.
           MOVE TK TO WS-N.
           DISPLAY "U-CFK " WS-N.
       U-RF SECTION.
           USE BEFORE REPORTING RF-G.
       U-RF-P.
           DISPLAY "U-RF".
       END DECLARATIVES.
       MAIN-S SECTION.
       MAIN-P.
           OPEN OUTPUT PRT.
           INITIATE RPT.
           DISPLAY "INITIATED".
           MOVE 1 TO WS-V.
           GENERATE DE-G.
           MOVE 2 TO WS-V.
           MOVE "Y" TO WS-SUP.
           GENERATE DE-G.
           MOVE "N" TO WS-SUP.
           MOVE 4 TO WS-V.
           GENERATE RPT.
           MOVE "2" TO WS-K.
           MOVE 8 TO WS-V.
           GENERATE DE-G.
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
           IF WS-I > 0
               PERFORM SHOW-LINE
           END-IF.
           STOP RUN.
       TAKE-BYTE.
           IF CHK-REC = SYM-LF
               PERFORM SHOW-LINE
           ELSE
               IF CHK-REC NOT = SYM-CR AND CHK-REC NOT = SYM-FF
                   ADD 1 TO WS-I
                   MOVE CHK-REC TO WS-LINE(WS-I:1)
               END-IF
           END-IF.
       SHOW-LINE.
           ADD 1 TO WS-LN.
           DISPLAY "LINE " WS-LN " [" WS-LINE(1:6) "]".
           MOVE SPACES TO WS-LINE.
           MOVE 0 TO WS-I.
