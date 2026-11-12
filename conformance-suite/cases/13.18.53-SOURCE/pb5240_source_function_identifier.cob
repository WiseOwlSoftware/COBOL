      *>--- conformance-case
      *> id: pb5240_source_function_identifier
      *> rules: [FMT-13.18.53.2, GR-13.18.53.4-1]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *>---
      *> kb/Work PB5240 - a function-identifier written as the whole
      *> SOURCE operand is identifier-1, never arithmetic-expression-1.
      *> A function-identifier is an identifier (ISO 8.4.3.1.2 Format
      *> 1); ISO 15.2: a function "may be used anywhere a sending data
      *> item of that class and category may be specified"; X3.23-1985
      *> Amendment 1 (IV 4.3.8.3.3 SR3): "A function-identifier which
      *> references an alphanumeric function may be specified anywhere
      *> in the general formats that an identifier is permitted", and
      *> the 1985 SOURCE operand is identifier-1 (XIII 3.18.2). So each
      *> operand below is the sending operand of the implicit MOVE of
      *> 13.18.53.4 GR1 to an alphanumeric printable item. It was
      *> refused at every edition as an arithmetic expression whose
      *> entry is not numeric (COBOL2141, COBOL0844; at 85 COBOL0900).
      *> DERIVATION (WS-T = "hello", WS-P = "AB", WS-Q = "CD"):
      *>   col 1  UPPER-CASE(WS-T)        = HELLO
      *>   col 7  REVERSE(WS-T)           = olleh
      *>   col 13 UPPER-CASE(WS-T)(2:3)   = ELL (IV 4.3.8.3.3 SR2 lets
      *>          an alphanumeric function be reference-modified)
      *>   col 17 MAX(WS-P WS-Q)          = CD (alphanumeric arguments
      *>          make an alphanumeric result, A.2.23)
      *> One GENERATE prints `HELLO olleh ELL CD`. The read-back is
      *> byte-wise (see pb1292_source_forms_85).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5240S.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS SYM-X0A SYM-X0D
               ARE 11 14.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "pb5240s.txt".
           SELECT CHK ASSIGN TO "pb5240s.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R-FN.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-EOF PIC X VALUE "N".
       01  WS-I   PIC 99 VALUE 0.
       01  WS-LINE PIC X(30) VALUE SPACES.
       01  WS-T   PIC X(5) VALUE "hello".
       01  WS-P   PIC X(2) VALUE "AB".
       01  WS-Q   PIC X(2) VALUE "CD".
       REPORT SECTION.
       RD  R-FN PAGE LIMIT 20 LINES.
       01  DET-A TYPE DETAIL.
           02  LINE PLUS 1.
               03  COLUMN 1 PIC X(5) SOURCE FUNCTION UPPER-CASE(WS-T).
               03  COLUMN 7 PIC X(5) SOURCE FUNCTION REVERSE(WS-T).
               03  COLUMN 13 PIC X(3)
                   SOURCE FUNCTION UPPER-CASE(WS-T)(2:3).
               03  COLUMN 17 PIC X(2) SOURCE FUNCTION MAX(WS-P WS-Q).
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT PRT.
           INITIATE R-FN.
           GENERATE DET-A.
           TERMINATE R-FN.
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
