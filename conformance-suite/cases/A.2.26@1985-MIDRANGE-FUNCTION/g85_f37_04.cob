      *>--- conformance-case
      *> id: g85_f37_04
      *> rules: [1989-AR-A.2.26.3-1, 1989-RV-A.2.26.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.26.3 AR1 - every argument is class numeric (integer
      *> and signed noninteger items, numeric literals): accepted and
      *> run. §1989:A.2.26.4 RV1 - the mean of the greatest and least
      *> argument, compared as simple conditions do (§1985:VI.6.3.1.1.1:
      *> by algebraic value). Line 1: (3 M7=-7 10 1) -> (10 + -7) / 2 =
      *> 1.5 (a magnitude comparison would give (10 + 1) / 2 = 5.5).
      *> Line 2: (N2=-1.25 N1=7 0.125) -> (7 + -1.25) / 2 = 2.875. Line
      *> 3: (M4=-4 M8=-8) -> -6. Line 4: one argument (42) -> 42.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F3704.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N1      PIC 9(3)    VALUE 7.
       01 N2      PIC S9V99   VALUE -1.25.
       01 M4      PIC S9      VALUE -4.
       01 M7      PIC S9      VALUE -7.
       01 M8      PIC S9      VALUE -8.
       01 R       PIC S9(3)V9(4).
       01 E       PIC -ZZ9.9999.
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE R = FUNCTION MIDRANGE (3 M7 10 1).
           MOVE R TO E. DISPLAY "[" E "]".
           COMPUTE R = FUNCTION MIDRANGE (N2 N1 0.125).
           MOVE R TO E. DISPLAY "[" E "]".
           COMPUTE R = FUNCTION MIDRANGE (M4 M8).
           MOVE R TO E. DISPLAY "[" E "]".
           COMPUTE R = FUNCTION MIDRANGE (42).
           MOVE R TO E. DISPLAY "[" E "]".
           STOP RUN.
