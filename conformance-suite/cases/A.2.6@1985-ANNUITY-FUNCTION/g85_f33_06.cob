      *>--- conformance-case
      *> id: g85_f33_06
      *> rules: [1989-AR-A.2.6.3-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.6.3 AR3 - argument-2 a positive integer, given as a
      *> literal (lines 1, 3, 5), an integer expression (line 2) and an
      *> integer data item (line 4); values by §1989:A.2.6.4 RV1 (rate zero:
      *> 1 / argument-2) and RV2, rounded to 4 places. A refusal of any
      *> integer form, or a wrong ratio, fails the case.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F3306.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-N PIC 9 VALUE 1.
       01 W-P PIC 9(2) VALUE 12.
       01 W-R PIC 9V9(4).
       01 W-E PIC 9.9999.
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE W-R ROUNDED = FUNCTION ANNUITY (0 4).
           MOVE W-R TO W-E.
           DISPLAY W-E.
           COMPUTE W-R ROUNDED = FUNCTION ANNUITY (0.1, W-N + 1).
           MOVE W-R TO W-E.
           DISPLAY W-E.
           COMPUTE W-R ROUNDED = FUNCTION ANNUITY (0 3).
           MOVE W-R TO W-E.
           DISPLAY W-E.
           COMPUTE W-R ROUNDED = FUNCTION ANNUITY (0 W-P).
           MOVE W-R TO W-E.
           DISPLAY W-E.
           COMPUTE W-R ROUNDED = FUNCTION ANNUITY (0.05 1).
           MOVE W-R TO W-E.
           DISPLAY W-E.
           STOP RUN.
