*>--- conformance-case
*> id: pb6232_evaluate_later_subject_evaluated_first
*> rules: [GR-14.9.13.4-3, GR-14.9.13.4-4]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB6232 - EVERY SELECTION SUBJECT IS EVALUATED AT THE BEGINNING OF THE EVALUATE STATEMENT.
      *>
      *> §2023:14.9.13.4 GR3: "At the beginning of the execution of the EVALUATE statement, each selection
      *> subject is evaluated and assigned a value, a range of values, or a truth value." GR4 c): the pairs of a
      *> WHEN phrase are tested left to right and "no more pairs in that WHEN phrase are evaluated" after a false
      *> one, so a subject that is read only by a later pair of the first phrase is NOT evaluated by the test.
      *> §2023:8.4.2.3.4 GR2: a subscript past the highest occurrence sets EC-BOUND-SUBSCRIPT.
      *>
      *> EXPECTED OUTPUT, DERIVED FROM THE RULES (X = 2, J = 9, R has 3 occurrences):
      *>   1  EVALUATE X ALSO R (J), WHEN 1 ALSO 5: the first pair is false, yet R (J) is evaluated at the
      *>      beginning, so the declarative runs once and RESUME NEXT STATEMENT continues after END-EVALUATE:
      *>      "RAISED" then "1 CNT=01".
      *>   2  the same with WHEN ANY ALSO 5: the first pair is constant true, R (J) is evaluated: "2 CNT=02".
      *>   3  J = 2, R (2) = 5 and X = 1: no condition, WHEN 1 ALSO 5 is selected: "3 A CNT=02".
       >>TURN EC-BOUND-SUBSCRIPT CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB6232EVL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC 9 VALUE 2.
       01 J PIC 9 VALUE 9.
       01 T.
          05 R PIC 9 OCCURS 3 VALUE 5.
       01 CNT PIC 99 VALUE 0.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D1 SECTION.
           USE AFTER EXCEPTION CONDITION EC-BOUND-SUBSCRIPT.
       D1P.
           ADD 1 TO CNT
           DISPLAY "RAISED".
           RESUME NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       M1.
           EVALUATE X ALSO R (J)
              WHEN 1 ALSO 5 DISPLAY "A"
              WHEN OTHER DISPLAY "O"
           END-EVALUATE
           DISPLAY "1 CNT=" CNT
           EVALUATE X ALSO R (J)
              WHEN ANY ALSO 5 DISPLAY "A"
              WHEN OTHER DISPLAY "O"
           END-EVALUATE
           DISPLAY "2 CNT=" CNT
           MOVE 2 TO J
           MOVE 1 TO X
           EVALUATE X ALSO R (J)
              WHEN 1 ALSO 5 DISPLAY "3 A" WITH NO ADVANCING
              WHEN OTHER DISPLAY "3 O" WITH NO ADVANCING
           END-EVALUATE
           DISPLAY " CNT=" CNT
           STOP RUN.
