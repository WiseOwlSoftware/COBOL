*>--- conformance-case
*> id: pb4765-index-name-sole-function
*> rules: [GR-8.8.4.2.13-2]
*> accept: [2002, 2014, 2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4765 control: 8.8.4.2.13 2) admits "an index-name and a numeric data item or numeric literal", and a
      *> function-identifier "references the unique data item that results from the evaluation of a function" (8.4.3.2.1)
      *> whose class and category are numeric (15.2 items 4 and 5), so IX = FUNCTION ABS (N) is defined and compares the
      *> occurrence number 5 with 5. The same function enclosed in parentheses is an arithmetic expression (8.8.1.1) and is
      *> refused (pb4765-index-name-paren-function).
      *>   cite.py --check 8.8.4.2.13 "Relation tests may be made only between" -> OK
      *> Derivation: IX = 5, ABS (-5) = 5, so EQ is displayed; ABS (-4) = 4, so the second test is false and NE is displayed.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. POS4765.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC S9(3) VALUE -5.
       01 M PIC S9(3) VALUE -4.
       01 T.
          05 E PIC X OCCURS 9 INDEXED BY IX.
       PROCEDURE DIVISION.
           SET IX TO 5
           IF IX = FUNCTION ABS (N)
               DISPLAY "EQ"
           END-IF
           IF IX = FUNCTION ABS (M)
               DISPLAY "WRONG"
           ELSE
               DISPLAY "NE"
           END-IF
           STOP RUN.
