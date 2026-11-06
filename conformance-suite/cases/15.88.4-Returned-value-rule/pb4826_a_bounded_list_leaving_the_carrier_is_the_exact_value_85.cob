      *>--- conformance-case
      *> id: pb4826_a_bounded_list_leaving_the_carrier_is_the_exact_value_85
      *> rules: [RV-15.88.4-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4826: the refused twin of pb4826_a_bounded_list_leaving_the_carrier_is_the_exact_value. The program is witnessed
      *> against items of 26 to 31 digits, which COBOL-2002 introduced (ISO 8.3.3.3.2), so under --std 85 it is refused at the item and
      *> the lane is never reached.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4826BN85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC 9(31) VALUE 12345678901234567890123456.
       01 B PIC V9(9) VALUE 0.004999999.
       01 R PIC 9(26)V99.
       PROCEDURE DIVISION.
           COMPUTE R ROUNDED = FUNCTION SUM (A B)
           DISPLAY "S1 " R
           STOP RUN.
       END PROGRAM PB4826BN85.
