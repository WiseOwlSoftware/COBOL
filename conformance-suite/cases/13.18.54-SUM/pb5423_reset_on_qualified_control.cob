      *>--- conformance-case
      *> id: pb5423_reset_on_qualified_control
      *> rules: [SR-13.18.54.3-8, SR-13.18.57.3-10, SR-8.4.2.2.3-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5423 - the CONTROL clause names WS-A qualified (WS-A OF G1); the TYPE CF operand and the
      *> RESET ON operand name it unqualified. Both were refused COBOL0899, the operands compared as written.
      *>
      *> RULES (cite.py --check, all OK):
      *> 13.18.54.3 SR8: "Data-name-3 or FINAL shall be an operand of the CONTROL clause of the current report
      *> description." 13.18.57.3 SR10: each data-name-1 "shall be the same as one of the operands of the CONTROL
      *> clause". 8.4.2.2.3 SR2: "if there is more than one combination of qualifiers that ensures uniqueness, then
      *> any such set may be used": WS-A is unique, so WS-A and WS-A OF G1 name one operand.
      *> 1985:XIII.3.19.3 SR4: "Data-name-2 must be one of the data-names specified in the CONTROL clause for this
      *> report."
      *>
      *> DERIVATION. WS-AMT is 5 at every GENERATE. A GENERATE first produces the control footings of a break, minor
      *> first, then adds to the sum counters. B-SUM (the WS-B footing) resets only on a WS-A break; A-SUM resets
      *> on its own. G1 A,B: [A B]; B=5 A=5. G2 A,C: WS-B break [B:   005] (no reset), [A C]; B=10 A=10. G3 X,C:
      *> WS-A break [B:   010] [A:   010], both reset, [X C]; B=5 A=5. G4 X,D: [B:   005], [X D]; B=10 A=10.
      *> TERMINATE: [B:   010] [A:   010]. All-space lines are skipped.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5423RQ.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS SYM-X0A SYM-X0D
               ARE 11 14.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "pb5423rq.txt".
           SELECT CHK ASSIGN TO "pb5423rq.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT REPORT IS R-Q.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  G1.
           05  WS-A    PIC X     VALUE "A".
           05  WS-B    PIC X     VALUE "B".
       01  WS-AMT  PIC 9(3)  VALUE 5.
       01  WS-EOF  PIC X     VALUE "N".
       01  WS-I    PIC 99    VALUE 0.
       01  WS-LINE PIC X(12) VALUE SPACES.
       REPORT SECTION.
       RD  R-Q
           CONTROLS ARE FINAL WS-A OF G1 WS-B.
       01  DL TYPE DETAIL LINE PLUS 1.
           05  COLUMN 1 PIC X SOURCE WS-A.
           05  COLUMN 3 PIC X SOURCE WS-B.
       01  TYPE CONTROL FOOTING WS-B LINE PLUS 1.
           05  COLUMN 1 PIC X(2) VALUE "B:".
           05  COLUMN 6 PIC 9(3) SUM WS-AMT RESET ON WS-A.
       01  TYPE CONTROL FOOTING WS-A LINE PLUS 1.
           05  COLUMN 1 PIC X(2) VALUE "A:".
           05  COLUMN 6 PIC 9(3) SUM WS-AMT.
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT RPT.
           INITIATE R-Q.
           GENERATE DL.
           MOVE "C" TO WS-B.
           GENERATE DL.
           MOVE "X" TO WS-A.
           GENERATE DL.
           MOVE "D" TO WS-B.
           GENERATE DL.
           TERMINATE R-Q.
           CLOSE RPT.
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
           IF CHK-REC = SYM-X0A
               PERFORM SHOW-LINE
           ELSE
               IF CHK-REC NOT = SYM-X0D
                   ADD 1 TO WS-I
                   MOVE CHK-REC TO WS-LINE(WS-I:1)
               END-IF
           END-IF.
       SHOW-LINE.
           IF WS-LINE NOT = SPACES
               DISPLAY "[" WS-LINE "]"
           END-IF.
           MOVE SPACES TO WS-LINE.
           MOVE 0 TO WS-I.
