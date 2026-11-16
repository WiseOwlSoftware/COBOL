      *>--- conformance-case
      *> id: g85_f37_03
      *> rules: [1989-AR-A.2.25.3-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.25.3 AR1 - argument-1 of MEDIAN is class numeric:
      *> every argument is class numeric (unsigned integer item, signed
      *> noninteger items, numeric literals) and the program must be
      *> accepted and run. Values per §1989:A.2.25.4 RV2: (7 -1.25 3)
      *> sorted -1.25 3 7, odd count, middle 3; (7 -1.25 3 -0.75) sorted
      *> -1.25 -0.75 3 7, even count, mean of -0.75 and 3 = 1.125;
      *> MEDIAN (5) = 5.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F3703.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N1      PIC 9(3)    VALUE 7.
       01 N2      PIC S9V99   VALUE -1.25.
       01 N3      PIC S9V99   VALUE -0.75.
       01 R       PIC S9(3)V9(4).
       01 E       PIC -ZZ9.9999.
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE R = FUNCTION MEDIAN (N1 N2 3).
           MOVE R TO E. DISPLAY "[" E "]".
           COMPUTE R = FUNCTION MEDIAN (N1 N2 3 N3).
           MOVE R TO E. DISPLAY "[" E "]".
           COMPUTE R = FUNCTION MEDIAN (5).
           MOVE R TO E. DISPLAY "[" E "]".
           STOP RUN.
