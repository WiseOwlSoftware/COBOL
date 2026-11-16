      *>--- conformance-case
      *> id: g2sum04_sum_cd_identifier_expression
      *> rules: [2002-SR-13.16.52.2-4, 2002-SR-13.16.52.2-5]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> SUM addends naming items of a record under a CD.  In 2002 the
      *> data division has a communication section (§2002:A.1.1.2 "A
      *> record description consists of a set of data description
      *> entries"), which is a section other than the report section.
      *> The communication facility is obsolete at 2002 (COBOL0903 is a
      *> warning and does not fail the case).
      *> §2002:13.16.52.2 SR4 - "If the addend is identifier-1, it shall
      *> specify a numeric data item not defined in the report section":
      *> CN (PIC 9(3) in the CD record) and WK (PIC 99 in working
      *> storage) are accepted identifier-1 addends (A and D below).  The
      *> refused non-numeric CD addend is g2sum05_sum_cd_addend_not_numeric.
      *> §2002:13.16.52.2 SR5 - "If the addend is arithmetic-expression-1,
      *> any identifiers it contains may reference entries in any section
      *> of the data division other than the report section": CN + 1 (a
      *> CD item) and CN + WK (a CD item and a working-storage item) are
      *> accepted expression addends (B and C below).
      *> DERIVATION.  No UPON phrase, so each addend is added "whenever
      *> any GENERATE statement is executed for the current report or any
      *> detail defined for the current report" (§2002:13.16.52.3 GR7c);
      *> the counters are in the CONTROL FOOTING FINAL, reset only at the
      *> end of its processing (§2002:13.16.52.3 GR2), which TERMINATE
      *> prints (§2002:14.8.42.3 GR3b).  WK = 10; GENERATE DET with CN = 7,
      *> then with CN = 8; DET prints CN (SOURCE, implicit MOVE).
      *>  A  SUM OF CN       7 + 8              = 15  => 0015
      *>  B  SUM OF CN + 1   (7+1) + (8+1)      = 17  => 0017
      *>  C  SUM OF CN + WK  (7+10) + (8+10)    = 35  => 0035
      *>  D  SUM OF WK       10 + 10            = 20  => 0020
      *> The counter moves to the printable item by MOVE
      *> (§2002:13.16.52.3 GR4).  A leg fails if the CD addend is refused,
      *> not added at each GENERATE, or read from the wrong storage.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2SUM04.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS SYM-X0A SYM-X0D
               ARE 11 14.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "g2sum04.txt".
           SELECT CHK ASSIGN TO "g2sum04.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R1.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-EOF  PIC X     VALUE "N".
       01  WS-I    PIC 99    VALUE 0.
       01  WS-LINE PIC X(40) VALUE SPACES.
       01  WK      PIC 99    VALUE 10.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       01  CDREC.
           05 CN   PIC 9(3).
       REPORT SECTION.
       RD  R1 CONTROL IS FINAL PAGE LIMIT 20 LINES.
       01  DET TYPE DE LINE PLUS 1.
           03  COLUMN 1  PIC 9(3) SOURCE IS CN.
       01  CFT TYPE CONTROL FOOTING FINAL LINE PLUS 1.
           03  COLUMN 1  PIC 9(4) SUM OF CN.
           03  COLUMN 6  PIC 9(4) SUM OF CN + 1.
           03  COLUMN 11 PIC 9(4) SUM OF CN + WK.
           03  COLUMN 16 PIC 9(4) SUM OF WK.
       PROCEDURE DIVISION.
       MAIN.
           MOVE 7 TO CN
           OPEN OUTPUT PRT.
           INITIATE R1.
           GENERATE DET.
           MOVE 8 TO CN
           GENERATE DET.
           TERMINATE R1.
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
