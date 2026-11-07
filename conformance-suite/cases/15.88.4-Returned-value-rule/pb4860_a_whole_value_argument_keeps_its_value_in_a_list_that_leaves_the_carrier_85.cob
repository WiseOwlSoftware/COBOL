      *>--- conformance-case
      *> id: pb4860_a_whole_value_argument_keeps_its_value_in_a_list_that_leaves_the_carrier_85
      *> rules: [RV-15.88.4-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4860: the refused twin of pb4860_a_whole_value_argument_keeps_its_value_in_a_list_that_leaves_the_carrier. The program
      *> is witnessed against items of 26 to 31 digits, which COBOL-2002 introduced (ISO 8.3.3.3.2), so under --std 85 it is refused at
      *> the item and the lane is never reached.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4860NS85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC 9(31) VALUE 12345678901234567890123456.
       01 B PIC V9(9) VALUE 0.004999999.
       01 Z PIC V9 VALUE 0.
       01 R PIC 9(26)V99.
       PROCEDURE DIVISION.
           COMPUTE R ROUNDED = FUNCTION SUM (FUNCTION SUM (A B) Z)
           DISPLAY "S1 " R
           STOP RUN.
       END PROGRAM PB4860NS85.
