      *>--- conformance-case
      *> id: g85_bf41_01
      *> rules: [1989-AR-A.2.28.3-2, 1989-RV-A.2.28.4-1, 1989-RV-A.2.28.4-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.28.4 RV1 - MOD returns argument-1 - (argument-2 *
      *> FUNCTION INTEGER (argument-1 / argument-2)); INTEGER is the
      *> greatest integer not greater than its argument, so the
      *> result takes the sign of argument-2:
      *>   M1 MOD (11 5)      11 - 5 * 2             =  1
      *>   M2 MOD (-11 5)    -11 - 5 * (-3)          =  4
      *>   M3 MOD (11, -5)    11 - (-5) * (-3)       = -4
      *>   M4 MOD (-11, -5)  -11 - (-5) * 2          = -1
      *>   M5 MOD (-7 3)      -7 - 3 * (-3)          =  2
      *>   M6 MOD (25 D7)     25 - 7 * 3             =  4
      *>   M8 MOD (123456789 1000)                   = 789
      *>   M9 MOD (0 5)        0 - 5 * 0             =  0
      *> §1989:A.2.28.4 RV2 - M1-M4 are the four rows of the RV2 table
      *> (1, 4, -4, -1).
      *> §1989:A.2.28.3 AR2 - "The value of argument-2 must not be
      *> zero": every reference keeps argument-2 nonzero, including a
      *> negative literal (M3, M4) and a divisor computed at run time
      *> (M7: DV = 3 - 7 = -4, MOD (17 DV) = 17 - (-4) * (-5) = -3).
      *> A zero argument-2 is outside the permitted values and its
      *> result is undefined (§1989:A.2.2 AR4), so no line probes it.
      *> A leg fails on a truncating MOD (M2 would print -1, M3 1,
      *> M5 -1, M7 1), on a refused negative or computed divisor, or
      *> on a wrong table value.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF4101.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  N-11            PIC S99 VALUE -11.
       01  N-7             PIC S9 VALUE -7.
       01  D7              PIC 9 VALUE 7.
       01  DV              PIC S9 VALUE 0.
       01  M               PIC -9.
       01  BIG             PIC -999.
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE M = FUNCTION MOD (11 5).
           DISPLAY "M1=" M.
           COMPUTE M = FUNCTION MOD (N-11 5).
           DISPLAY "M2=" M.
           COMPUTE M = FUNCTION MOD (11, -5).
           DISPLAY "M3=" M.
           COMPUTE M = FUNCTION MOD (N-11, -5).
           DISPLAY "M4=" M.
           COMPUTE M = FUNCTION MOD (N-7 3).
           DISPLAY "M5=" M.
           COMPUTE M = FUNCTION MOD (25 D7).
           DISPLAY "M6=" M.
           COMPUTE DV = 3 - 7.
           COMPUTE M = FUNCTION MOD (17 DV).
           DISPLAY "M7=" M.
           COMPUTE BIG = FUNCTION MOD (123456789 1000).
           DISPLAY "M8=" BIG.
           COMPUTE M = FUNCTION MOD (0 5).
           DISPLAY "M9=" M.
           STOP RUN.
