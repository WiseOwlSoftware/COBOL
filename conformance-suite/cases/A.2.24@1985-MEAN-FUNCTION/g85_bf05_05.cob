      *>--- conformance-case
      *> id: g85_bf05_05
      *> rules: [1989-FMT-A.2.24.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      * §1989:A.2.24.2 - FUNCTION MEAN ( { argument-1 } ... ): one, two
      *    and three arguments and an ALL subscript are each accepted at
      *    85; values from §1989:A.2.24.4 RV2 "the sum of the argument-1
      *    series divided by the number of occurrences referenced by
      *    argument-1":
      * MEA1 (7) = 7; MEA2 (1 2) = 1.5; MEA3 (3 9 6) = 6;
      * MEA4 (T (ALL) 0.50) = (2.5 + 8 + 1 + 0.5) / 4 = 3.
      * §1989:A.2.2 AR4 - the repeated argument as T (ALL): T = 2.50
      *    8.00 1.00, "as if each table element ... were specified".
      * Results are COMPUTEd into ZZZ9.9999, so a value shows as 4
      *    integer and 4 fraction positions with leading zeros as
      *    spaces.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF0505.
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
           COMPUTE E = FUNCTION MEAN (7).
           DISPLAY "MEA1=" E.
           COMPUTE E = FUNCTION MEAN (1 2).
           DISPLAY "MEA2=" E.
           COMPUTE E = FUNCTION MEAN (3 9 6).
           DISPLAY "MEA3=" E.
           COMPUTE E = FUNCTION MEAN (T (ALL) 0.50).
           DISPLAY "MEA4=" E.
           STOP RUN.
