*>--- conformance-case
*> id: pb4712-highest-algebraic-parenthesized-counter-register
*> rules: [AR-15.43.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> PB4712 review R1-1 - the counter-register twin of the parenthesized function-identifier refusals. LINAGE-COUNTER written
      *> bare "references a temporary unsigned integer data item" (8.4.3.14.4 r1), so FUNCTION HIGHEST-ALGEBRAIC (15.43.3 r1, "a data item of category numeric") admits it (pb1153_counter_register_*).
      *> Enclosed in parentheses it is "an arithmetic expression enclosed in parentheses" (8.8.1.1), the same as (I2), which is
      *> refused: the expression binder drops the parentheses, and the argument screens read the bound tree, which is the register's.
      *> Expected: COBOL1516.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGHIGHALGPARENCTR.
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
           COMPUTE N = FUNCTION HIGHEST-ALGEBRAIC((LINAGE-COUNTER)).
           STOP RUN.
