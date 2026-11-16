      *>--- conformance-case
      *> id: g85_f38_02
      *> rules: [1989-FMT-A.2.28.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.28.2 - the format FUNCTION MOD (argument-1
      *> argument-2), two arguments given as literals, as
      *> identifiers and as an arithmetic expression, is accepted at
      *> 85. The values are §1989:A.2.28.4 RV2's own table (11 5 -> 1,
      *> -11 5 -> 4, 11 -5 -> -4, -11 -5 -> -1) and RV1's definition
      *> for (2 + 15) 4 -> 1. A compiler that refuses the format, or
      *> binds the arguments in another order, fails a line. The
      *> integer function sits in an arithmetic expression only
      *> (§1985:IV.4.3.8.3.3 SR4).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F3802.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W-P11       PIC S99 VALUE 11.
       01  W-N11       PIC S99 VALUE -11.
       01  W-P5        PIC S9  VALUE 5.
       01  W-N5        PIC S9  VALUE -5.
       01  W-R         PIC -9.
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE W-R = FUNCTION MOD (11 5).
           DISPLAY "MOD 11 5 =" W-R.
           COMPUTE W-R = FUNCTION MOD (W-N11 W-P5).
           DISPLAY "MOD -11 5 =" W-R.
           COMPUTE W-R = FUNCTION MOD (W-P11 W-N5).
           DISPLAY "MOD 11 -5 =" W-R.
           COMPUTE W-R = FUNCTION MOD (W-N11 W-N5).
           DISPLAY "MOD -11 -5 =" W-R.
           COMPUTE W-R = FUNCTION MOD ((2 + 15) 4).
           DISPLAY "MOD 17 4 =" W-R.
           STOP RUN.
