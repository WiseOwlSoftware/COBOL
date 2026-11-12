      *>--- conformance-case
      *> id: pb5240-source-numeric-function-85
      *> rules: [FMT-13.18.53.2]
      *> source-format: fixed
      *>---
      *> kb/Work PB5240 - the sibling of pb5240_source_function_identifier
      *> that 1985 refuses. A function-identifier written as the whole
      *> SOURCE operand is identifier-1, but X3.23-1985 Amendment 1 says
      *> "A function-identifier which references an integer or numeric
      *> function may be used only in an arithmetic expression" (IV
      *> 4.3.8.3.3 SR4), and the 1985 SOURCE operand is identifier-1
      *> alone (XIII 3.18.2); ISO/IEC 1989:2002 Annex F.2 item 115:
      *> "SOURCE allows arithmetic expressions and functions". So at 85
      *> FUNCTION LENGTH(WS-T) is refused with COBOL0900 (construct
      *> report-source-expression-2002), decided by the function's bound
      *> category; from 2002 it is the implicit MOVE's sender.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5240N.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "pb5240n.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R-FN.
       WORKING-STORAGE SECTION.
       01  WS-T   PIC X(5) VALUE "hello".
       REPORT SECTION.
       RD  R-FN PAGE LIMIT 20 LINES.
       01  DET-A TYPE DETAIL.
           02  LINE PLUS 1.
               03  COLUMN 1 PIC 9(3) SOURCE FUNCTION LENGTH(WS-T).
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT PRT.
           INITIATE R-FN.
           GENERATE DET-A.
           TERMINATE R-FN.
           CLOSE PRT.
           STOP RUN.
