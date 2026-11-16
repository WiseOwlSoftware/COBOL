      *>--- conformance-case
      *> id: g85_bf05_09
      *> rules: [1989-FMT-A.2.42.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      * §1989:A.2.42.2 - FUNCTION SUM ( { argument-1 } ... ) accepted
      *    with one, two and three arguments, an ALL subscript, and an
      *    ALL subscript beside a written argument; values from
      *    §1989:A.2.42.4 RV1 "The returned value is the sum of the
      *    arguments":
      * SUM1 (7) = 7; SUM2 (1.5 2.25) = 3.75; SUM3 (1 2 3) = 6;
      * SUM4 T (ALL) = 2.5 + 8 + 1 = 11.5; SUM5 (T (ALL) 10) = 21.5.
      * §1989:A.2.2 AR4 - the repeated argument as T (ALL): T = 2.50
      *    8.00 1.00, "as if each table element ... were specified".
      * Results are COMPUTEd into ZZZ9.9999, so a value shows as 4
      *    integer and 4 fraction positions with leading zeros as
      *    spaces.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF0509.
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
           COMPUTE E = FUNCTION SUM (7).
           DISPLAY "SUM1=" E.
           COMPUTE E = FUNCTION SUM (1.5 2.25).
           DISPLAY "SUM2=" E.
           COMPUTE E = FUNCTION SUM (1 2 3).
           DISPLAY "SUM3=" E.
           COMPUTE E = FUNCTION SUM (T (ALL)).
           DISPLAY "SUM4=" E.
           COMPUTE E = FUNCTION SUM (T (ALL) 10).
           DISPLAY "SUM5=" E.
           STOP RUN.
