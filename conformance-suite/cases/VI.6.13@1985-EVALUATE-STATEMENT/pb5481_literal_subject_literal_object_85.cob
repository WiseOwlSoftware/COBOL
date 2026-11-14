      *>--- conformance-case
      *> id: pb5481_literal_subject_literal_object_85
      *> rules: [1985-SR-VI.6.13.3-6]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5481. §1985:VI.6.13.3 SR6 a: "Identifiers, literals,
      *> or arithmetic expressions appearing within a selection object
      *> must be valid operands for comparison to the corresponding
      *> operand in the set of selection subjects", and §1985:VI.6.13.4
      *> GR1 gives a literal subject and a literal object "the value
      *> and class of the specified literal". 1985 has no combination
      *> table, so a literal subject against a literal object is legal
      *> and the object is selected by value. From 2002 the same
      *> pairing is refused (cases/14.9.13-EVALUATE/
      *> pb5481_literal_subject_literal_object_2002). The expected
      *> lines follow the selection rule: equal values select the
      *> phrase, NOT inverts the test, OTHER takes the rest.
      *>   1 ZERO against 0         equal            -> SELECTED-1
      *>   2 5 against 5            equal            -> SELECTED-2
      *>   3 "A" against "A"        equal            -> SELECTED-3
      *>   4 5 against NOT 4        not equal, NOT   -> SELECTED-4
      *>   5 5 ALSO N3 against 5 ALSO 12, N3 = 12    -> SELECTED-5
      *>   6 5 against 6 then OTHER                  -> OTHER-6
      *>   7 5 against NOT 5 then OTHER              -> OTHER-7
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5481A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  N3 PIC 99 VALUE 12.
       PROCEDURE DIVISION.
       MAIN-PARA.
           EVALUATE ZERO
               WHEN 0 DISPLAY "SELECTED-1"
               WHEN OTHER DISPLAY "OTHER-1"
           END-EVALUATE.
           EVALUATE 5
               WHEN 5 DISPLAY "SELECTED-2"
               WHEN OTHER DISPLAY "OTHER-2"
           END-EVALUATE.
           EVALUATE "A"
               WHEN "A" DISPLAY "SELECTED-3"
               WHEN OTHER DISPLAY "OTHER-3"
           END-EVALUATE.
           EVALUATE 5
               WHEN NOT 4 DISPLAY "SELECTED-4"
               WHEN OTHER DISPLAY "OTHER-4"
           END-EVALUATE.
           EVALUATE 5 ALSO N3
               WHEN 5 ALSO 12 DISPLAY "SELECTED-5"
               WHEN OTHER DISPLAY "OTHER-5"
           END-EVALUATE.
           EVALUATE 5
               WHEN 6 DISPLAY "SELECTED-6"
               WHEN OTHER DISPLAY "OTHER-6"
           END-EVALUATE.
           EVALUATE 5
               WHEN NOT 5 DISPLAY "SELECTED-7"
               WHEN OTHER DISPLAY "OTHER-7"
           END-EVALUATE.
           STOP RUN.
