      *>--- conformance-case
      *> id: pb5022_report_sum_crossfoot_same_group
      *> rules: [SR-13.18.54.3-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5022 - the LEGAL side of the COBOL-85 crossfoot: 1985:XIII.3.19.3 SR1 admits, in a SUM clause written without UPON, a
      *> sum counter "defined ... in the same report group that contains this SUM clause", and 1985:XIII.3.19.4 GR9 a) names the
      *> accumulation of such an addend "crossfooting". COBOL-2002 and later refuse it (2002:13.16.52.2 SR3 b), 2023:13.18.54.3 SR4 b)):
      *> conformance:negative/pb5022-sum-crossfoot-same-group is the refused side.
      *> DERIVATION. Two details each add WS-A (2) and WS-B (3) into the sum counters SA and SB (XIII.3.19.4 GR2), so at the report's
      *> end SA = 4 and SB = 6, and the control footing crossfoots TOT = SA + SB = 10: the footing line is 04, 06 and 010 in columns
      *> 1, 5 and 9. The details print on lines 1 and 2 (the SOURCE item WS-A, 2), the footing on line 3 (LINE PLUS 1).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5022P.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS SYM-X0A SYM-X0C SYM-X0D
               ARE 11 13 14.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "PB5022P.TXT".
           SELECT CHK ASSIGN TO "PB5022P.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R1.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-EOF  PIC X     VALUE "N".
       01  WS-BYTE PIC X.
       01  WS-I    PIC 99    VALUE 0.
       01  WS-LN   PIC 99    VALUE 0.
       01  WS-LINE PIC X(40) VALUE SPACES.
       01  WS-A PIC 9 VALUE 2.
       01  WS-B PIC 9 VALUE 3.
       REPORT SECTION.
       RD  R1 CONTROLS ARE FINAL PAGE LIMIT IS 10 LINES.
       01  DET TYPE DETAIL LINE PLUS 1.
           05  COLUMN 1 PIC 9 SOURCE WS-A.
       01  CFG TYPE CONTROL FOOTING FINAL LINE PLUS 1.
           05  SA COLUMN 1 PIC 99 SUM WS-A.
           05  SB COLUMN 5 PIC 99 SUM WS-B.
           05  TOT COLUMN 9 PIC 999 SUM SA SB.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT PRT.
           INITIATE R1.
           GENERATE DET.
           GENERATE DET.
           TERMINATE R1.
           CLOSE PRT.
           OPEN INPUT CHK.
           PERFORM UNTIL WS-EOF = "Y"
               READ CHK
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END
                       MOVE CHK-REC TO WS-BYTE
                       PERFORM TAKE-BYTE
               END-READ
           END-PERFORM.
           CLOSE CHK.
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
                   MOVE 0 TO WS-LN
               WHEN WS-BYTE = SYM-X0D
                   CONTINUE
               WHEN OTHER
                   ADD 1 TO WS-I
                   MOVE WS-BYTE TO WS-LINE(WS-I:1)
           END-EVALUATE.
       SHOW-LINE.
           ADD 1 TO WS-LN.
           IF WS-LINE NOT = SPACES
               DISPLAY "LINE " WS-LN " [" WS-LINE(1:11) "]"
           END-IF.
           MOVE SPACES TO WS-LINE.
           MOVE 0 TO WS-I.
