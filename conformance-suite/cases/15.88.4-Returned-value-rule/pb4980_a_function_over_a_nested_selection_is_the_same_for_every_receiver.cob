      *>--- conformance-case
      *> id: pb4980_a_function_over_a_nested_selection_is_the_same_for_every_receiver
      *> rules: [RV-15.88.4-1, RV-15.9.4-1]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4980: ISO 14.9.25.4 GR1 writes MOVE a TO Q1 Q2 as MOVE a TO temp / MOVE temp TO Q1 /
      *> MOVE temp TO Q2, and 15.4.1 adds "the returned value is the same for all instances of a given
      *> function", so a function's value cannot depend on how many receivers the MOVE names. ANNUITY over
      *> a nested MAX, MIN or MEDIAN took the float lane in the two-receiver MOVE (the temporary was a
      *> binary64, 0.3333333333333333 into 9V9(25)) and the SDIDI in the one-receiver MOVE (exact),
      *> because the lane prediction took the selection's exact value for a scale-0 operand where the
      *> renderer hands it on as a run-time-scaled exact value that the function above reads as the SDIDI.
      *> The values are 15.9.4 1) a) 1 / argument-2 for an argument-1 of zero (1/3 = 0.333...) and
      *> 15.9.4 1) b) argument-1 / (1 - (1 + argument-1) ** -argument-2) otherwise: 1.5 / (1 - 2.5 ** -3)
      *> = 1.5 / 0.936 = 1.602564102564102564102564..., each truncated to 25 places by the receiver.
      *> MEDT  ANNUITY (MEDIAN (T (ALL)) 3) over three PIC 9(31) zeros: 15.9.4 1) a) 0.3333333333333333333333333
      *> MEDL  ANNUITY (MEDIAN (U V 1.5E0) 3): the middle of 0 0 1.5 is 0, so 1/3
      *> MAXL  ANNUITY (MAX (U 1.5E0) 3) = 1.6025641025641025641025641
      *> MINL  ANNUITY (MIN (U 1.5E0) 3) = 0.3333333333333333333333333
      *> BAD   the count of cells whose two-receiver value differs from the one-receiver value, which is 0
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4980A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 Q1 PIC 9V9(25).
       01 Q2 PIC 9V9(25).
       01 Q3 PIC 9V9(25).
       01 TB.
          05 T PIC 9(31) OCCURS 3 VALUE 0.
       01 U PIC 9 VALUE 0.
       01 V PIC 9 VALUE 0.
       01 BAD PIC 9 VALUE 0.
       PROCEDURE DIVISION.
           MOVE FUNCTION ANNUITY (FUNCTION MEDIAN (T (ALL)) 3)
               TO Q1 Q2
           MOVE FUNCTION ANNUITY (FUNCTION MEDIAN (T (ALL)) 3) TO Q3
           DISPLAY "MEDT " Q1 " " Q2 " " Q3
           IF Q1 NOT = Q3 OR Q2 NOT = Q3 ADD 1 TO BAD END-IF
           MOVE FUNCTION ANNUITY (FUNCTION MEDIAN (U V 1.5E0) 3)
               TO Q1 Q2
           MOVE FUNCTION ANNUITY (FUNCTION MEDIAN (U V 1.5E0) 3) TO Q3
           DISPLAY "MEDL " Q1 " " Q2 " " Q3
           IF Q1 NOT = Q3 OR Q2 NOT = Q3 ADD 1 TO BAD END-IF
           MOVE FUNCTION ANNUITY (FUNCTION MAX (U 1.5E0) 3)
               TO Q1 Q2
           MOVE FUNCTION ANNUITY (FUNCTION MAX (U 1.5E0) 3) TO Q3
           DISPLAY "MAXL " Q1 " " Q2 " " Q3
           IF Q1 NOT = Q3 OR Q2 NOT = Q3 ADD 1 TO BAD END-IF
           MOVE FUNCTION ANNUITY (FUNCTION MIN (U 1.5E0) 3)
               TO Q1 Q2
           MOVE FUNCTION ANNUITY (FUNCTION MIN (U 1.5E0) 3) TO Q3
           DISPLAY "MINL " Q1 " " Q2 " " Q3
           IF Q1 NOT = Q3 OR Q2 NOT = Q3 ADD 1 TO BAD END-IF
           DISPLAY "BAD " BAD
           STOP RUN.
       END PROGRAM PB4980A.
