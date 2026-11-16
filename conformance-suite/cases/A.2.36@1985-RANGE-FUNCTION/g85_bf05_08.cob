      *>--- conformance-case
      *> id: g85_bf05_08
      *> rules: [1989-FMT-A.2.36.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      * §1989:A.2.36.2 - FUNCTION RANGE ( { argument-1 } ... ) accepted
      *    with one, two and three arguments and an ALL subscript;
      *    values from
      * §1989:A.2.36.4 RV1 "equal to the greatest value of argument-1
      *    minus the least value of argument-1":
      * RAN1 (7) = 0; RAN2 (1.25 4) = 2.75; RAN3 (3 9 4) = 6;
      * RAN4 T (ALL) = 8 - 1 = 7.
      * §1989:A.2.2 AR4 - the repeated argument as T (ALL): T = 2.50
      *    8.00 1.00, "as if each table element ... were specified".
      * Results are COMPUTEd into ZZZ9.9999, so a value shows as 4
      *    integer and 4 fraction positions with leading zeros as
      *    spaces.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF0508.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  T-TBL.
           02  T           PIC 9(2)V99 OCCURS 3 TIMES.
       01  E               PIC ZZZ9.9999.
       PROCEDURE DIVISION.
       P-MAIN.
           MOVE 2.50 TO T (1).
           MOVE 8.00 TO T (2).
           MOVE 1.00 TO T (3).
           COMPUTE E = FUNCTION RANGE (7).
           DISPLAY "RAN1=" E.
           COMPUTE E = FUNCTION RANGE (1.25 4).
           DISPLAY "RAN2=" E.
           COMPUTE E = FUNCTION RANGE (3 9 4).
           DISPLAY "RAN3=" E.
           COMPUTE E = FUNCTION RANGE (T (ALL)).
           DISPLAY "RAN4=" E.
           STOP RUN.
