      *>--- conformance-case
      *> id: g85_bf05_06
      *> rules: [1989-FMT-A.2.25.2, 1989-RV-A.2.25.4-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      * §1989:A.2.25.2 - FUNCTION MEDIAN ( { argument-1 } ... ) accepted
      *    with one to four arguments and an ALL subscript (every line
      *    pins it).
      * §1989:A.2.25.4 RV2 - odd count: "at least half of the
      *    occurrences referenced by argument-1 are greater than or
      *    equal to the returned value" and at least half less or equal;
      *    even count: "the arithmetic mean of the values referenced by
      *    the two middle occurrences":
      * MED1 (7) = 7 (odd, one); MED2 (3 1 2) = 2 (odd, unsorted input);
      * MED3 (5 1 5) = 5 (odd, duplicates); MED4 (2 9) = 5.5 (even);
      * MED5 (4 1 3 10) sorted 1 3 4 10 -> (3 + 4) / 2 = 3.5 (even; the
      *    unsorted middle pair 1 3 would give 2); MED6 T (ALL) = 2.50
      *    8.00 1.00 sorted 1 2.5 8 -> 2.5 (odd);
      * MED7 (T (ALL) 4) sorted 1 2.5 4 8 -> (2.5 + 4) / 2 = 3.25
      *    (even).
      * §1989:A.2.2 AR4 - the repeated argument as T (ALL): T = 2.50
      *    8.00 1.00, "as if each table element ... were specified".
      * Results are COMPUTEd into ZZZ9.9999, so a value shows as 4
      *    integer and 4 fraction positions with leading zeros as
      *    spaces.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF0506.
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
           COMPUTE E = FUNCTION MEDIAN (7).
           DISPLAY "MED1=" E.
           COMPUTE E = FUNCTION MEDIAN (3 1 2).
           DISPLAY "MED2=" E.
           COMPUTE E = FUNCTION MEDIAN (5 1 5).
           DISPLAY "MED3=" E.
           COMPUTE E = FUNCTION MEDIAN (2 9).
           DISPLAY "MED4=" E.
           COMPUTE E = FUNCTION MEDIAN (4 1 3 10).
           DISPLAY "MED5=" E.
           COMPUTE E = FUNCTION MEDIAN (T (ALL)).
           DISPLAY "MED6=" E.
           COMPUTE E = FUNCTION MEDIAN (T (ALL) 4).
           DISPLAY "MED7=" E.
           STOP RUN.
