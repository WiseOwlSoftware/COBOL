      *>--- conformance-case
      *> id: g85_f37_05
      *> rules: [1989-RV-A.2.32.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.32.4 RV1 - ORD-MAX returns the position of the
      *> argument with the greatest value, compared as simple conditions
      *> do (§1985:VI.6.3.1.1.1 numeric by algebraic value;
      *> §1985:VI.6.3.1.1.2 nonnumeric by the collating sequence). (3 9
      *> 2 7) -> 2; (N2=-1.25 N1=7 0.125) -> 2; (M5=-5 M2=-2 M9=-9) ->
      *> 2, where a magnitude comparison gives 3; ("APPLE" "PEAR" "FIG")
      *> -> 2 (P > F > A in any native letter order); one argument -> 1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F3705.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N1      PIC 9(3)    VALUE 7.
       01 N2      PIC S9V99   VALUE -1.25.
       01 M2      PIC S9      VALUE -2.
       01 M5      PIC S9      VALUE -5.
       01 M9      PIC S9      VALUE -9.
       01 K       PIC 99.
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE K = FUNCTION ORD-MAX (3 9 2 7). DISPLAY K.
           COMPUTE K = FUNCTION ORD-MAX (N2 N1 0.125). DISPLAY K.
           COMPUTE K = FUNCTION ORD-MAX (M5 M2 M9). DISPLAY K.
           COMPUTE K = FUNCTION ORD-MAX ("APPLE" "PEAR" "FIG").
           DISPLAY K.
           COMPUTE K = FUNCTION ORD-MAX (5). DISPLAY K.
           STOP RUN.
