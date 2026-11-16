      *>--- conformance-case
      *> id: g85_f37_07
      *> rules: [1989-RV-A.2.37.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.37.4 RV1 - REM is argument-1 - (argument-2 * FUNCTION
      *> INTEGER-PART (argument-1 / argument-2)); INTEGER-PART truncates
      *> toward zero (§1989:A.2.18.4 RV2, RV3), so the remainder takes
      *> the sign of argument-1. (11 4) -> 3; (-11 4) -> -11 - 4 * -2 =
      *> -3 (a floor-based remainder would give 1); (11 -4) -> 11 - (-4
      *> * -2) = 3; (-11 -4) -> -11 - (-4 * 2) = -3; (7.5 2) -> 7.5 - 2
      *> * 3 = 1.5; (-1.25 0.5) -> -1.25 - 0.5 * -2 = -0.25.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F3707.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 P11     PIC S99     VALUE 11.
       01 M11     PIC S99     VALUE -11.
       01 M4      PIC S9      VALUE -4.
       01 N2      PIC S9V99   VALUE -1.25.
       01 R       PIC S9(3)V9(4).
       01 E       PIC -ZZ9.9999.
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE R = FUNCTION REM (P11 4).
           MOVE R TO E. DISPLAY "[" E "]".
           COMPUTE R = FUNCTION REM (M11 4).
           MOVE R TO E. DISPLAY "[" E "]".
           COMPUTE R = FUNCTION REM (P11 M4).
           MOVE R TO E. DISPLAY "[" E "]".
           COMPUTE R = FUNCTION REM (M11 M4).
           MOVE R TO E. DISPLAY "[" E "]".
           COMPUTE R = FUNCTION REM (7.5 2).
           MOVE R TO E. DISPLAY "[" E "]".
           COMPUTE R = FUNCTION REM (N2 0.5).
           MOVE R TO E. DISPLAY "[" E "]".
           STOP RUN.
