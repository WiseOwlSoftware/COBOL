      *>--- conformance-case
      *> id: g85_f37_06
      *> rules: [1989-RV-A.2.33.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.33.4 RV1 - ORD-MIN returns the position of the
      *> argument with the least value, compared as simple conditions do
      *> (§1985:VI.6.3.1.1.1 numeric by algebraic value;
      *> §1985:VI.6.3.1.1.2 nonnumeric by the collating sequence). (3 9
      *> 2 7) -> 3; (N2=-1.25 N1=7 0.125) -> 1, where a magnitude
      *> comparison gives 3; (M5=-5 M2=-2 M9=-9) -> 3, where a magnitude
      *> comparison gives 2; ("APPLE" "PEAR" "FIG") -> 1; one argument
      *> -> 1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F3706.
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
           COMPUTE K = FUNCTION ORD-MIN (3 9 2 7). DISPLAY K.
           COMPUTE K = FUNCTION ORD-MIN (N2 N1 0.125). DISPLAY K.
           COMPUTE K = FUNCTION ORD-MIN (M5 M2 M9). DISPLAY K.
           COMPUTE K = FUNCTION ORD-MIN ("APPLE" "PEAR" "FIG").
           DISPLAY K.
           COMPUTE K = FUNCTION ORD-MIN (5). DISPLAY K.
           STOP RUN.
