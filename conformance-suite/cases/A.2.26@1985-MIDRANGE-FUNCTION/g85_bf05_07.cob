      *>--- conformance-case
      *> id: g85_bf05_07
      *> rules: [1989-FMT-A.2.26.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      * §1989:A.2.26.2 - FUNCTION MIDRANGE ( { argument-1 } ... )
      *    accepted with one, two and three arguments and an ALL
      *    subscript; values from
      * §1989:A.2.26.4 RV1 "the arithmetic mean of the greatest
      *    argument-1 value and the least argument-1 value":
      * MID1 (7) = 7; MID2 (2 5) = 3.5; MID3 (3 9 4) = (3 + 9) / 2 = 6;
      * MID4 T (ALL) = (1 + 8) / 2 = 4.5.
      * §1989:A.2.2 AR4 - the repeated argument as T (ALL): T = 2.50
      *    8.00 1.00, "as if each table element ... were specified".
      * Results are COMPUTEd into ZZZ9.9999, so a value shows as 4
      *    integer and 4 fraction positions with leading zeros as
      *    spaces.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF0507.
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
           COMPUTE E = FUNCTION MIDRANGE (7).
           DISPLAY "MID1=" E.
           COMPUTE E = FUNCTION MIDRANGE (2 5).
           DISPLAY "MID2=" E.
           COMPUTE E = FUNCTION MIDRANGE (3 9 4).
           DISPLAY "MID3=" E.
           COMPUTE E = FUNCTION MIDRANGE (T (ALL)).
           DISPLAY "MID4=" E.
           STOP RUN.
