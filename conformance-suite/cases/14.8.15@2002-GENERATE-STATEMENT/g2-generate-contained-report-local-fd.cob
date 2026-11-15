      *>--- conformance-case
      *> id: g2-generate-contained-report-local-fd
      *> rules: [2002-SR-14.8.15.2-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.15.2 SR4 - "If report-name-1 is defined in a
      *>   containing program, the file description entry associated
      *>   with report-name-1 shall contain a GLOBAL clause." R-1 IS
      *>   GLOBAL but its FD RPT is not, so GENERATE R-1 in the contained
      *>   G2GEN05B violates the rule and is refused at that statement.
      *>   The accepted side is g2-generate-contained-report-global-fd.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2GEN05.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "G2GEN05.TXT".
           SELECT CHK ASSIGN TO "G2GEN05.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT REPORT IS R-1.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-EOF  PIC X     VALUE "N".
       01  WS-I    PIC 99    VALUE 0.
       01  WS-LINE PIC X(20) VALUE SPACES.
       REPORT SECTION.
       RD  R-1 IS GLOBAL CONTROL IS FINAL PAGE LIMIT IS 20 LINES.
       01  TYPE CH FINAL LINE PLUS 1.
           02 COLUMN 1 PIC X(3) VALUE "CHF".
       01  DET-1 TYPE DE LINE PLUS 1.
           02 COLUMN 1 PIC X(2) VALUE "DE".
       01  TYPE CF FINAL LINE PLUS 1.
           02 COLUMN 1 PIC X(3) VALUE "CFF".
       PROCEDURE DIVISION.
       A-MAIN.
           OPEN OUTPUT RPT.
           INITIATE R-1.
           CALL "G2GEN05B".
           TERMINATE R-1.
           CLOSE RPT.
           OPEN INPUT CHK.
           PERFORM UNTIL WS-EOF = "Y"
               READ CHK
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END PERFORM TAKE-BYTE
               END-READ
           END-PERFORM.
           CLOSE CHK.
           PERFORM SHOW-LINE.
           DISPLAY "DONE".
           STOP RUN.
       TAKE-BYTE.
           EVALUATE TRUE
               WHEN CHK-REC = X"0A" OR CHK-REC = X"0C"
                   PERFORM SHOW-LINE
               WHEN CHK-REC = X"0D"
                   CONTINUE
               WHEN OTHER
                   ADD 1 TO WS-I
                   MOVE CHK-REC TO WS-LINE(WS-I:1)
           END-EVALUATE.
       SHOW-LINE.
           IF WS-LINE NOT = SPACES
               DISPLAY "[" WS-LINE(1:WS-I) "]"
           END-IF.
           MOVE SPACES TO WS-LINE.
           MOVE 0 TO WS-I.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2GEN05B.
       PROCEDURE DIVISION.
       B-MAIN.
           DISPLAY "IN G2GEN05B".
           GENERATE R-1.
           EXIT PROGRAM.
       END PROGRAM G2GEN05B.
       END PROGRAM G2GEN05.
