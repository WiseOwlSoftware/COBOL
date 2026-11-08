      *>--- conformance-case
      *> id: pb4980_a_function_over_a_nested_selection_is_the_same_for_every_receiver_85
      *> rules: [RV-15.88.4-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4980: the refused twin of
      *> pb4980_a_function_over_a_nested_selection_is_the_same_for_every_receiver. The program is witnessed
      *> against an item of 31 digits, which COBOL-2002 introduced (ISO 8.3.3.3.2), so under --std 85 it is
      *> refused at the item and the lane is never reached.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4980NS85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 Q1 PIC 9V9(25).
       01 Q2 PIC 9V9(25).
       01 TB.
          05 T PIC 9(31) OCCURS 3 VALUE 0.
       PROCEDURE DIVISION.
           MOVE FUNCTION ANNUITY (FUNCTION MEDIAN (T (ALL)) 3)
               TO Q1 Q2
           DISPLAY "MEDT " Q1 " " Q2
           STOP RUN.
       END PROGRAM PB4980NS85.
