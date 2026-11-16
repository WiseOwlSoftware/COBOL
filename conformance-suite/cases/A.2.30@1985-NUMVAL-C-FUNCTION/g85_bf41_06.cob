      *>--- conformance-case
      *> id: g85_bf41_06
      *> rules: [1989-AR-A.2.30.3-5]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.30.3 AR5 - "If argument-2 is not specified, the
      *> character used for cs is the currency symbol specified for
      *> the program": with no CURRENCY SIGN clause the currency
      *> symbol is identical to the currency sign "$" (the 1985
      *> glossary entry Currency Symbol), so "$" is cs. Values (RV1)
      *> times 100 into C, PIC -9(9):
      *>   C1 "$5,000.01"   5000.01   ->  000500001
      *>   C2 "-$.99"       -0.99     -> -000000099
      *> A leg fails when "$" is not taken as cs without argument-2.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF4106.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  C               PIC -9(9).
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE C = FUNCTION NUMVAL-C ("$5,000.01") * 100.
           DISPLAY "C1=" C.
           COMPUTE C = FUNCTION NUMVAL-C ("-$.99") * 100.
           DISPLAY "C2=" C.
           STOP RUN.
