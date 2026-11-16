      *>--- conformance-case
      *> id: g85_bf09_08
      *> rules: [1985-GR-XIII.3.18.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.3.18.4 GR1 - the implicit MOVEs of SOURCE clauses
      *> are executed when the RWCS formats the print lines, just before
      *> the report group is presented.
      *>   cite.py: OK  §1985:XIII.3.18.4 GR1 "It is at this time that
      *>     the implicit MOVE statements specified by SOURCE clauses
      *>     are executed"
      *>   cite.py: OK  §1985:XIII.3.20.4 GR11 "If there is a USE BEFORE
      *>     REPORTING procedure that refers to the data-name of the
      *>     report group, the USE procedure is executed"
      *> DERIVATION. For a detail, the USE BEFORE REPORTING procedure
      *> (11b) runs before the lines are formatted and presented (11e);
      *> it adds 10 to WS-X, so the GENERATEs with WS-X 1 and 2 print
      *> D=11 and D=12. The final control footing is formatted at
      *> TERMINATE (§1985:XIII.3.20.4 GR6), after WS-X became 50: F=50.
      *> Fails with D=01 / D=02 if SOURCE is moved when GENERATE starts,
      *> or F=12 if the footing's SOURCE is captured at the last
      *> GENERATE.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF0908.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS SYM-LF SYM-FF SYM-CR ARE 11 13 14.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "g85bf0908.txt".
           SELECT CHK ASSIGN TO "g85bf0908.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R-1.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-X    PIC 99    VALUE 0.
       01  WS-EOF  PIC X     VALUE "N".
       01  WS-I    PIC 999   VALUE 0.
       01  WS-LINE PIC X(20) VALUE SPACES.
       REPORT SECTION.
       RD  R-1 CONTROL IS FINAL.
       01  DE-1 TYPE DE LINE PLUS 1.
           05  COLUMN 1 PIC X(2) VALUE "D=".
           05  COLUMN 3 PIC 99 SOURCE WS-X.
       01  CF-F TYPE CF FINAL LINE PLUS 1.
           05  COLUMN 1 PIC X(2) VALUE "F=".
           05  COLUMN 3 PIC 99 SOURCE WS-X.
       PROCEDURE DIVISION.
       DECLARATIVES.
       DE-SEC SECTION.
           USE BEFORE REPORTING DE-1.
       DE-PARA.
           ADD 10 TO WS-X.
       END DECLARATIVES.
       MAIN-SEC SECTION.
       MAIN-P.
           OPEN OUTPUT PRT.
           INITIATE R-1.
           MOVE 1 TO WS-X.
           GENERATE DE-1.
           MOVE 2 TO WS-X.
           GENERATE DE-1.
           MOVE 50 TO WS-X.
           TERMINATE R-1.
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
                   IF WS-I NOT > 20
                       MOVE CHK-REC TO WS-LINE(WS-I:1)
                   END-IF
               END-IF
           END-IF.
       SHOW-LINE.
           IF WS-LINE NOT = SPACES
               DISPLAY "[" WS-LINE "]"
           END-IF.
           MOVE SPACES TO WS-LINE.
           MOVE 0 TO WS-I.
