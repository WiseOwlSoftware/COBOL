      *>--- conformance-case
      *> id: g85_f33_07
      *> rules: [1989-AR-A.2.28.3-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.28.3 AR1 - argument-1 and argument-2 integers: signed
      *> integer data items (lines 1-4, the four rows of the §1989:A.2.28.4
      *> RV2 table), an integer expression (line 5) and an integer function
      *> (line 6, FUNCTION INTEGER). Values by RV1: a1 - a2 * INTEGER(a1/a2).
      *> A refusal of any integer form, or a wrong value, fails the case.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F3307.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-P11 PIC S99 VALUE 11.
       01 W-M11 PIC S99 VALUE -11.
       01 W-P5 PIC S9 VALUE 5.
       01 W-M5 PIC S9 VALUE -5.
       01 W-A PIC 9 VALUE 9.
       01 W-R PIC S9.
       01 W-E PIC -9.
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE W-R = FUNCTION MOD (W-P11 W-P5).
           MOVE W-R TO W-E.
           DISPLAY W-E.
           COMPUTE W-R = FUNCTION MOD (W-M11 W-P5).
           MOVE W-R TO W-E.
           DISPLAY W-E.
           COMPUTE W-R = FUNCTION MOD (W-P11 W-M5).
           MOVE W-R TO W-E.
           DISPLAY W-E.
           COMPUTE W-R = FUNCTION MOD (W-M11 W-M5).
           MOVE W-R TO W-E.
           DISPLAY W-E.
           COMPUTE W-R = FUNCTION MOD (W-A + 1, 4).
           MOVE W-R TO W-E.
           DISPLAY W-E.
           COMPUTE W-R = FUNCTION MOD (FUNCTION INTEGER (7.9) 3).
           MOVE W-R TO W-E.
           DISPLAY W-E.
           STOP RUN.
