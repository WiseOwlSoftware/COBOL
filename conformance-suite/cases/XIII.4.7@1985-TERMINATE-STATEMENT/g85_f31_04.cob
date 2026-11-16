      *>--- conformance-case
      *> id: g85_f31_04
      *> rules: [1985-SR-XIII.4.7.3-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.4.7.3 SR1 - TERMINATE names report-names each defined by a
      *> report description entry (RD) in the Report Section; two in one
      *> statement. cite.py --check: OK §1985:XIII.4.7.3 SR1 "Report-name-1 must
      *> be defined by a report description entry in the Report Section of the
      *> Data Division."
      *> §1985:XIII.4.7.4 GR1 (cite.py: OK): "Then the report footing report
      *> group is produced."; GR5 (cite.py: OK): the result is the same as if a
      *> separate TERMINATE statement had been written for each report-name in
      *> the same order.
      *> DERIVATION. One detail of each report, then TERMINATE R-A R-B: R-A's
      *> report footing then R-B's. File read back: [A] [B] [END A] [END B].
      *> Each leg can fail: a refusal of TERMINATE R-A R-B, a report it did
      *> not terminate (no footing), or the footings in the wrong order.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F3104.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS SYM-X0A SYM-X0D ARE 11 14.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "G85F3104.TXT".
           SELECT CHK ASSIGN TO "G85F3104.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORTS ARE R-A R-B.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-EOF  PIC X     VALUE "N".
       01  WS-I    PIC 99    VALUE 0.
       01  WS-LINE PIC X(30) VALUE SPACES.
       REPORT SECTION.
       RD  R-A.
       01  DTL-A TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           05  COLUMN NUMBER IS 1 PIC X VALUE "A".
       01  TYPE IS REPORT FOOTING LINE NUMBER IS PLUS 1.
           05  COLUMN NUMBER IS 1 PIC X(5) VALUE "END A".
       RD  R-B.
       01  DTL-B TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           05  COLUMN NUMBER IS 1 PIC X VALUE "B".
       01  TYPE IS REPORT FOOTING LINE NUMBER IS PLUS 1.
           05  COLUMN NUMBER IS 1 PIC X(5) VALUE "END B".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT PRT.
           INITIATE R-A R-B.
           GENERATE DTL-A.
           GENERATE DTL-B.
           TERMINATE R-A R-B.
           CLOSE PRT.
           PERFORM READ-BACK.
           STOP RUN.
       READ-BACK.
           MOVE "N" TO WS-EOF.
           OPEN INPUT CHK.
           PERFORM UNTIL WS-EOF = "Y"
               READ CHK
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END PERFORM TAKE-BYTE
               END-READ
           END-PERFORM.
           CLOSE CHK.
           PERFORM SHOW-LINE.
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
