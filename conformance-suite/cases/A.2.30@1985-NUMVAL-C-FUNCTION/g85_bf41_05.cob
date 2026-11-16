      *>--- conformance-case
      *> id: g85_bf41_05
      *> rules: [1989-AR-A.2.30.3-5]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.30.3 AR5 - "If argument-2 is not specified, the
      *> character used for cs is the currency symbol specified for
      *> the program": here CURRENCY SIGN IS "#" (§1985:VI.4.5.4 GR11),
      *> so "#" is cs and is ignored as a currency sign. Values (RV1:
      *> the numeric value represented) times 100 into C, PIC -9(9):
      *>   C1 "#1,234.50"     1234.5    ->  000123450
      *>   C2 " - # 7.25"     -7.25     -> -000000725
      *>   C3 "#12.5-"        -12.5     -> -000001250
      *>   C4 ARG "  #  99 "  99        ->  000009900
      *> An implementation that took "$" (the default currency sign)
      *> for cs would find "#" in no permitted position.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF4105.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           CURRENCY SIGN IS "#".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  ARG             PIC X(8) VALUE "  #  99 ".
       01  C               PIC -9(9).
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE C = FUNCTION NUMVAL-C ("#1,234.50") * 100.
           DISPLAY "C1=" C.
           COMPUTE C = FUNCTION NUMVAL-C (" - # 7.25") * 100.
           DISPLAY "C2=" C.
           COMPUTE C = FUNCTION NUMVAL-C ("#12.5-") * 100.
           DISPLAY "C3=" C.
           COMPUTE C = FUNCTION NUMVAL-C (ARG) * 100.
           DISPLAY "C4=" C.
           STOP RUN.
