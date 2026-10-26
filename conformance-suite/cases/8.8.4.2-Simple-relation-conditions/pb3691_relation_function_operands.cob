      *>--- conformance-case
      *> id: pb3691_relation_function_operands
      *> rules: [GR-8.8.4.2.1-6]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3691 - THE POSITIVE CONTROL for the pb3691-* refusals: a
      *> refusal's failure mode is reaching source the rule ADMITS, so each
      *> function relation 8.8.4.2.1 DOES define is written here.
      *>
      *> ISO 8.8.4.2.5: "The numeric integer operand shall be an integer
      *> literal or an integer numeric data item of usage display or
      *> national. The other operand may be a literal or data item of class
      *> alphanumeric or national." A function-identifier is a data item
      *> (8.4.3.2.1), so an ALPHANUMERIC function (15.2 1): "an implicit
      *> usage display") is the "other operand", and a numeric or integer
      *> function against a NUMERIC operand is item 1 (8.8.4.2.4: numeric
      *> operands compare by algebraic value), which 8.8.4.2.5 never reaches.
      *>
      *> 01 FUNCTION INTEGER(N) = 7     item 1: 7 = 7                    EQ
      *> 02 FUNCTION INTEGER(N) = N     item 1                           EQ
      *> 03 FUNCTION LENGTH(N) = 4      item 1: N PIC 9(4) is 4 positions EQ
      *> 04 FUNCTION MAX(N 3) < 8       item 1: MAX(7 3) = 7, 7 < 8      LT
      *> 05 FUNCTION UPPER-CASE(S) = "ABCD"  both alphanumeric           EQ
      *> 06 X = FUNCTION UPPER-CASE(X)  "0007" has no letters to change  EQ
      *> 07 N = FUNCTION UPPER-CASE(X)  item 6: the integer display item N
      *>    is moved to an alphanumeric item of 4 positions, "0007"      EQ
      *> 08 N = "0007"                  item 6, the contrast the refusals
      *>    break: the usage display item answers                        EQ
      *> 09 FUNCTION INTEGER(N) NOT = 8 item 1: 7 is not 8               NOT-EQ
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3691-FN-OPERANDS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9(4) VALUE 7.
       01 X PIC X(4) VALUE "0007".
       01 S PIC X(4) VALUE "abcd".
       PROCEDURE DIVISION.
       MAIN-PARA.
           IF FUNCTION INTEGER(N) = 7
               DISPLAY "01 EQ" ELSE DISPLAY "01 NE" END-IF
           IF FUNCTION INTEGER(N) = N
               DISPLAY "02 EQ" ELSE DISPLAY "02 NE" END-IF
           IF FUNCTION LENGTH(N) = 4
               DISPLAY "03 EQ" ELSE DISPLAY "03 NE" END-IF
           IF FUNCTION MAX(N 3) < 8
               DISPLAY "04 LT" ELSE DISPLAY "04 GE" END-IF
           IF FUNCTION UPPER-CASE(S) = "ABCD"
               DISPLAY "05 EQ" ELSE DISPLAY "05 NE" END-IF
           IF X = FUNCTION UPPER-CASE(X)
               DISPLAY "06 EQ" ELSE DISPLAY "06 NE" END-IF
           IF N = FUNCTION UPPER-CASE(X)
               DISPLAY "07 EQ" ELSE DISPLAY "07 NE" END-IF
           IF N = "0007"
               DISPLAY "08 EQ" ELSE DISPLAY "08 NE" END-IF
           IF FUNCTION INTEGER(N) NOT = 8
               DISPLAY "09 NOT-EQ" ELSE DISPLAY "09 EQ" END-IF
           STOP RUN.
