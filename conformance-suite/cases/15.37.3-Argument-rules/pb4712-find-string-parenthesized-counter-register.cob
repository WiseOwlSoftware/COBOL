*>--- conformance-case
*> id: pb4712-find-string-parenthesized-counter-register
*> rules: [AR-15.37.3-3]
*> source-format: free
*> portable: unverified
*>---
      *> PB4712 review R1-1 - the counter-register twin of the parenthesized function-identifier refusals. LINAGE-COUNTER written
      *> bare "references a temporary unsigned integer data item" (8.4.3.14.4 r1), so FUNCTION FIND-STRING argument-3 (15.37.3 r3, "an integer data item or integer literal") admits it (pb1153_counter_register_*).
      *> Enclosed in parentheses it is "an arithmetic expression enclosed in parentheses" (8.8.1.1), the same as (I2), which is
      *> refused: the expression binder drops the parentheses, and the argument screens read the bound tree, which is the register's.
      *> Expected: COBOL1627.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGFINDPARENCTR.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb4712-counter.dat".
       DATA DIVISION.
       FILE SECTION.
       FD F LINAGE IS 20 LINES.
       01 FR    PIC X(10).
       WORKING-STORAGE SECTION.
       01 I2    PIC S9(4) VALUE -7.
       01 X9    PIC X(9) VALUE "ABC".
       01 N     PIC 9(4).
       PROCEDURE DIVISION.
           COMPUTE N = FUNCTION FIND-STRING(X9 "A" (LINAGE-COUNTER)).
           STOP RUN.
