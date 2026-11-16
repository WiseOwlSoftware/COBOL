      *>--- conformance-case
      *> id: g85_f37_02
      *> rules: [1989-AR-A.2.24.3-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.24.3 AR1 - argument-1 of MEAN is class numeric: every
      *> argument here is class numeric (an unsigned integer item, a
      *> signed noninteger item, numeric literals) and the program must
      *> be accepted and run. The values pin §1989:A.2.24.4 RV2 (sum /
      *> number of arguments) so a mis-bound argument shows: (7 - 1.25 +
      *> 3 + N3=-0.75) / 4 = 2; MEAN (7) = 7; MEAN (1 2) = 1.5.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F3702.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N1      PIC 9(3)    VALUE 7.
       01 N2      PIC S9V99   VALUE -1.25.
       01 N3      PIC S9V99   VALUE -0.75.
       01 R       PIC S9(3)V9(4).
       01 E       PIC -ZZ9.9999.
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE R = FUNCTION MEAN (N1 N2 3 N3).
           MOVE R TO E. DISPLAY "[" E "]".
           COMPUTE R = FUNCTION MEAN (N1).
           MOVE R TO E. DISPLAY "[" E "]".
           COMPUTE R = FUNCTION MEAN (1 2).
           MOVE R TO E. DISPLAY "[" E "]".
           STOP RUN.
