      *>--- conformance-case
      *> id: g85_bf41_04
      *> rules: [1989-AR-A.2.30.3-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.30.3 AR2 - with DECIMAL-POINT IS COMMA "the
      *> functions of the comma and decimal point in argument-1 are
      *> reversed": the comma is the decimal point and the period the
      *> optional grouping separator. Each value is the numeric value
      *> represented (§1989:A.2.30.4 RV1), times 100 into C, PIC -9(9)
      *> (an integer picture, so the PICTURE exchange of
      *> §1985:VI.4.5.4 GR12 plays no part in the display):
      *>   C1 "$1.234,56"      1234.56     ->  000123456
      *>   C2 "1.000.000,5-"   -1000000.5  -> -100000050
      *>   C3 " $ 12,75 "      12.75       ->  000001275
      *>   C4 ",5"             0.5         ->  000000050
      *> Without the reversal C1 reads 1.234 (or fails on ",56"), C2
      *> fails on its second period, C3 reads 1275, C4 fails.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF4104.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           DECIMAL-POINT IS COMMA.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  ARG             PIC X(9) VALUE " $ 12,75 ".
       01  C               PIC -9(9).
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE C = FUNCTION NUMVAL-C ("$1.234,56") * 100.
           DISPLAY "C1=" C.
           COMPUTE C = FUNCTION NUMVAL-C ("1.000.000,5-") * 100.
           DISPLAY "C2=" C.
           COMPUTE C = FUNCTION NUMVAL-C (ARG) * 100.
           DISPLAY "C3=" C.
           COMPUTE C = FUNCTION NUMVAL-C (",5") * 100.
           DISPLAY "C4=" C.
           STOP RUN.
