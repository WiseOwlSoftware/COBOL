      *>--- conformance-case
      *> id: g85_f37_08
      *> rules: [1989-FMT-A.2.41.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.41.2 - FUNCTION STANDARD-DEVIATION ( { argument-1 }
      *> ... ): one argument, two, and eight (items and literals mixed)
      *> must each be accepted. Values per §1989:A.2.41.4 RV2 and RV3,
      *> rounded to four places since the value is an approximation: (2
      *> 4 4 4 5 5 7 9) mean 5, squares 9 1 1 1 0 0 4 16 = 32, / 8 = 4,
      *> root 2; (5) one value -> 0; (1 3) mean 2, squares 1 1, / 2 = 1,
      *> root 1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F3708.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N2      PIC 9       VALUE 2.
       01 N9      PIC 9       VALUE 9.
       01 R       PIC S9(3)V9(4).
       01 E       PIC -ZZ9.9999.
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE R ROUNDED = FUNCTION STANDARD-DEVIATION
               (N2 4 4 4 5 5 7 N9).
           MOVE R TO E. DISPLAY "[" E "]".
           COMPUTE R ROUNDED = FUNCTION STANDARD-DEVIATION (5).
           MOVE R TO E. DISPLAY "[" E "]".
           COMPUTE R ROUNDED = FUNCTION STANDARD-DEVIATION (1 3).
           MOVE R TO E. DISPLAY "[" E "]".
           STOP RUN.
