      *>--- conformance-case
      *> id: g2src04_source_operands_file_local_linkage
      *> rules: [2002-SR-13.16.51.2-4]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.51.2 SR4 - "Identifier-1 specifies a data item
      *>   defined in any section of the data division ... This same
      *>   syntax rule applies to any identifier appearing in
      *>   arithmetic-expression-1" (cite.py --check OK). This case takes
      *>   the sections the other 2002 witnesses do not: the file
      *>   section (DF-NAME, DF-QTY of the record of DATA-F), the
      *>   local-storage section (LS-NAME, LS-QTY) and the linkage
      *>   section (LK-NAME, LK-QTY, an argument of the caller). The
      *>   report lives in the contained program G2SRC04R, which has
      *>   all three sections. Each item appears once as identifier-1
      *>   (§2002:13.16.51.3 GR1, an implicit MOVE) and once inside an
      *>   arithmetic expression (GR2, an implicit COMPUTE) that is
      *>   evaluated at each print (GR3):
      *>   DATA-F holds one record "FIL010", read before the GENERATEs;
      *>   LS-NAME "LOC", LS-QTY 20; the caller passes "LNK040".
      *>   GENERATE 1: FIL LOC LNK, DF-QTY + 1 = 0011,
      *>               LS-QTY * 2 = 0040, LK-QTY - 1 = 0039
      *>   then LS-QTY := 21 and LK-QTY := 41 (the caller's item)
      *>   GENERATE 2: FIL LOC LNK, 0011, 0042, 0040
      *>   The caller then shows its argument            => "A LNK041"
      *> Why it can fail: a SOURCE operand outside working-storage,
      *> communication or the report counters refused or bound to the
      *> wrong storage, or an expression evaluated once only.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2SRC06.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS SYM-X0A SYM-X0D
               ARE 11 14.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-ARG.
           05  WS-NAME PIC X(3) VALUE "LNK".
           05  WS-QTY  PIC 9(3) VALUE 40.
       PROCEDURE DIVISION.
       MAIN-P.
           CALL "G2SRC04R" USING WS-ARG
           DISPLAY "A " WS-ARG
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2SRC04R.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT DATA-F ASSIGN TO "g2src04d.dat"
               ORGANIZATION IS SEQUENTIAL.
           SELECT PRT ASSIGN TO "g2src04.txt".
           SELECT CHK ASSIGN TO "g2src04.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  DATA-F.
       01  DF-REC.
           05  DF-NAME PIC X(3).
           05  DF-QTY  PIC 9(3).
       FD  PRT REPORT IS R1.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-EOF  PIC X     VALUE "N".
       01  WS-I    PIC 99    VALUE 0.
       01  WS-LINE PIC X(40) VALUE SPACES.
       LOCAL-STORAGE SECTION.
       01  LS-NAME PIC X(3).
       01  LS-QTY  PIC 9(3).
       LINKAGE SECTION.
       01  LK-ARG.
           05  LK-NAME PIC X(3).
           05  LK-QTY  PIC 9(3).
       REPORT SECTION.
       RD  R1.
       01  DET TYPE DE LINE PLUS 1.
           03  COLUMN 1  PIC X(3) SOURCE DF-NAME.
           03  COLUMN 5  PIC X(3) SOURCE LS-NAME.
           03  COLUMN 9  PIC X(3) SOURCE LK-NAME.
           03  COLUMN 13 PIC 9(4) SOURCE DF-QTY + 1.
           03  COLUMN 18 PIC 9(4) SOURCE LS-QTY * 2.
           03  COLUMN 23 PIC 9(4) SOURCE LK-QTY - 1.
       PROCEDURE DIVISION USING LK-ARG.
       SUB-P.
           OPEN OUTPUT DATA-F
           MOVE "FIL" TO DF-NAME
           MOVE 10 TO DF-QTY
           WRITE DF-REC
           CLOSE DATA-F
           OPEN INPUT DATA-F
           READ DATA-F END-READ
           MOVE "LOC" TO LS-NAME
           MOVE 20 TO LS-QTY
           OPEN OUTPUT PRT
           INITIATE R1
           GENERATE DET
           MOVE 21 TO LS-QTY
           MOVE 41 TO LK-QTY
           GENERATE DET
           TERMINATE R1
           CLOSE PRT
           CLOSE DATA-F
           OPEN INPUT CHK
           PERFORM UNTIL WS-EOF = "Y"
               READ CHK
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END PERFORM TAKE-BYTE
               END-READ
           END-PERFORM
           CLOSE CHK
           PERFORM SHOW-LINE
           GOBACK.
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
       END PROGRAM G2SRC04R.
       END PROGRAM G2SRC06.
