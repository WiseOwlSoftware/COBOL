      *>--- conformance-case
      *> id: g85_f37_09
      *> rules: [1989-FMT-A.2.45.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.45.2 - FUNCTION VARIANCE ( { argument-1 } ... ): one
      *> argument, two, and eight (items and literals mixed) must each
      *> be accepted. Values per §1989:A.2.45.4 RV2 (the square of the
      *> standard deviation, §1989:A.2.41.4 RV2) and RV3, rounded to
      *> four places: (2 4 4 4 5 5 7 9) -> 32 / 8 = 4; (3) one value ->
      *> 0; (1 3) -> 2 / 2 = 1; (N2=-1.25 N1=7) mean 2.875, deviations
      *> +-4.125, squares 17.015625 each, / 2 = 17.015625 -> 17.0156.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F3709.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N1      PIC 9(3)    VALUE 7.
       01 N2      PIC S9V99   VALUE -1.25.
       01 D2      PIC 9       VALUE 2.
       01 R       PIC S9(3)V9(4).
       01 E       PIC -ZZ9.9999.
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE R ROUNDED = FUNCTION VARIANCE (D2 4 4 4 5 5 7 9).
           MOVE R TO E. DISPLAY "[" E "]".
           COMPUTE R ROUNDED = FUNCTION VARIANCE (3).
           MOVE R TO E. DISPLAY "[" E "]".
           COMPUTE R ROUNDED = FUNCTION VARIANCE (1 3).
           MOVE R TO E. DISPLAY "[" E "]".
           COMPUTE R ROUNDED = FUNCTION VARIANCE (N2 N1).
           MOVE R TO E. DISPLAY "[" E "]".
           STOP RUN.
