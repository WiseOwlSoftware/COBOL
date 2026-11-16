      *>--- conformance-case
      *> id: g2src02_source_cd_move_rounded
      *> rules: [2002-SR-13.16.51.2-2, 2002-SR-13.16.51.2-5, 2002-GR-13.16.51.3-1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> SOURCE identifier-1 naming items of a record under a CD.
      *> §2002:13.16.51.2 SR4 "Identifier-1 specifies a data item defined
      *> in any section of the data division", and in 2002 the data
      *> division has a communication section (§2002:A.1.1.2 "A record
      *> description consists of a set of data description entries").
      *> The communication facility is obsolete at 2002 (COBOL0903 is a
      *> warning and does not fail the case).
      *> §2002:13.16.51.2 SR2 - without ROUNDED, "identifier-1 shall be
      *> described such that a MOVE statement is valid with identifier-1
      *> as the sending operand and the printable item as the receiving
      *> operand": every MOVE below is "Yes" in Table 17
      *> (§2002:14.8.24.2 SR8): numeric CN into numeric 9(3) and
      *> numeric-edited ZZ9, numeric CFR into numeric 9(3) and
      *> numeric-edited 9.9, alphanumeric CA into alphanumeric X(5) and
      *> X(3). Compiling is the accepted leg; the refused leg is
      *> g2src03_source_cd_move_invalid.
      *> §2002:13.16.51.3 GR1 - the implicit MOVE "in which the data item
      *> referenced by identifier-1 is moved to the printable item".
      *> §2002:13.16.51.2 SR5 - "If identifier-1 is specified with the
      *> ROUNDED phrase, it is considered to be an arithmetic-expression",
      *> so the implicit COMPUTE with a ROUNDED phrase of
      *> §2002:13.16.51.3 GR2 applies, and rounding (§2002:14.6.3)
      *> increases the result by one in the low-order position "whenever
      *> the most significant digit of the excess is greater than or
      *> equal to five"; a MOVE truncates (§2002:14.8.24.3 GR4b).
      *> DERIVATION.  First GENERATE: CN = 7, CFR = 1.55, CA = "ABC".
      *>  col 1  9(3)  SOURCE CN            MOVE                   => 007
      *>  col 5  ZZ9   SOURCE CN            MOVE, edited           =>   7
      *>  col 9  9(3)  SOURCE CFR ROUNDED   COMPUTE ROUNDED 1.55   => 002
      *>  col 13 9(3)  SOURCE CFR           MOVE, fraction cut     => 001
      *>  col 17 9.9   SOURCE CFR ROUNDED   excess 5 >= 5: 1.5+0.1 => 1.6
      *>  col 21 9.9   SOURCE CFR           MOVE truncates         => 1.5
      *>  col 25 X(5)  SOURCE CA            MOVE, space filled     => ABC__
      *>  col 31 X(3)  SOURCE CA                                   => ABC
      *> Second GENERATE: CN = 8, CA = "XYZ": the MOVE is executed again
      *> before the line is printed (§2002:13.16.51.3 GR3), so the CN and
      *> CA legs show the new values.
      *> SR5 fails if ROUNDED is ignored (col 9 shows 001, col 17 1.5);
      *> GR1 fails if the CD item is not moved, is rounded, or is moved
      *> once only.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2SRC04.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS SYM-X0A SYM-X0D
               ARE 11 14.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "g2src02.txt".
           SELECT CHK ASSIGN TO "g2src02.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R1.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-EOF  PIC X     VALUE "N".
       01  WS-I    PIC 99    VALUE 0.
       01  WS-LINE PIC X(40) VALUE SPACES.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       01  CDREC.
           05 CN   PIC 9(3).
           05 CFR  PIC 9V99.
           05 CA   PIC X(3).
       REPORT SECTION.
       RD  R1 PAGE LIMIT 20 LINES.
       01  DET TYPE DE LINE PLUS 1.
           03  COLUMN 1  PIC 9(3) SOURCE IS CN.
           03  COLUMN 5  PIC ZZ9  SOURCE IS CN.
           03  COLUMN 9  PIC 9(3) SOURCE IS CFR ROUNDED.
           03  COLUMN 13 PIC 9(3) SOURCE IS CFR.
           03  COLUMN 17 PIC 9.9  SOURCE IS CFR ROUNDED.
           03  COLUMN 21 PIC 9.9  SOURCE IS CFR.
           03  COLUMN 25 PIC X(5) SOURCE IS CA.
           03  COLUMN 31 PIC X(3) SOURCE IS CA.
       PROCEDURE DIVISION.
       MAIN.
           MOVE 7 TO CN
           MOVE 1.55 TO CFR
           MOVE "ABC" TO CA
           OPEN OUTPUT PRT.
           INITIATE R1.
           GENERATE DET.
           MOVE 8 TO CN
           MOVE "XYZ" TO CA
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
