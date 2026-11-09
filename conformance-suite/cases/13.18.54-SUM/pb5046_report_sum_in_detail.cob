      *>--- conformance-case
      *> id: pb5046_report_sum_in_detail
      *> rules: [SR-13.18.54.3-4]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5046 - the LEGAL side of a SUM entry outside a control footing. 1985:XIII.3.19.3 SR3 admits a SUM clause only in a
      *> control footing; COBOL-2002 and later name a detail as a SUM group too (2023:13.18.54.3 SR4 f)).
      *> conformance:negative/pb5046-sum-not-in-control-footing is the refused side at COBOL-85.
      *> DERIVATION. T is a SUM entry of a detail with no UPON: 2023:13.18.54.4 GR7 c) 1. adds the addend WS-A (2) whenever a GENERATE
      *> is executed for the report, GR4 moves the counter to the printable item, and GR2 resets it at the end of the processing of the
      *> report group in which it prints. Each of the two details therefore prints WS-A (2) in column 1 and the counter 02 in column 5.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5046P.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS SYM-X0A SYM-X0C SYM-X0D
               ARE 11 13 14.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "PB5046P.TXT".
           SELECT CHK ASSIGN TO "PB5046P.TXT".
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
       REPORT SECTION.
       RD  R1 PAGE LIMIT IS 10 LINES.
       01  DET TYPE DETAIL LINE PLUS 1.
           05  COLUMN 1 PIC 9 SOURCE WS-A.
           05  T COLUMN 5 PIC 99 SUM WS-A.
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
