      *>--- conformance-case
      *> id: pb5501_size_error_in_condition_continues
      *> rules: [1985-GR-VI.6.4.2-3, 1985-GR-VI.6.4.2-2,
      *>     1985-GR-VI.6.4.2-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5501. A size error in a CONDITION: an arithmetic
      *> expression in a relation or a sign condition whose evaluation
      *> divides by zero (§1985:VI.6.4.2 GR2: "Division by zero always
      *> terminates the arithmetic operation and always causes a size
      *> error condition") or violates the exponentiation rules (GR1).
      *> The condition has no resultant identifier and no ON SIZE
      *> ERROR phrase, and §1985:B.2.2 item 45 says what follows: "If
      *> a size error condition occurs and an ON SIZE ERROR phrase has
      *> not been specified, the program will continue". The 1985 text
      *> gives the relation no truth value, which is the implementor's
      *> to choose (docs/CONFORMANCE.md DOC-A.1-70): a relation or sign
      *> condition whose expression cannot be evaluated is false, and a
      *> logical NOT outside it inverts that.
      *>
      *> Only the program's continuing is the standard's: the truth
      *> values below are this implementation's determination, and a
      *> condition with no size error (the last IF) keeps its true value.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5501A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  B    PIC 9 VALUE 5.
       01  Z    PIC 9 VALUE 0.
       01  N    PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN-P.
           IF B / Z > 1 DISPLAY "1 T" ELSE DISPLAY "1 F".
           IF B / Z NOT > 1 DISPLAY "2 T" ELSE DISPLAY "2 F".
           IF NOT B / Z > 1 DISPLAY "3 T" ELSE DISPLAY "3 F".
           IF B / Z IS POSITIVE DISPLAY "4 T" ELSE DISPLAY "4 F".
           IF Z ** 0 = 1 DISPLAY "5 T" ELSE DISPLAY "5 F".
           IF B / Z > 1 OR B = 5 DISPLAY "6 T" ELSE DISPLAY "6 F".
           PERFORM UNTIL N > 2 OR B / Z > 1
              ADD 1 TO N
           END-PERFORM.
           DISPLAY "N " N.
           IF B / 1 > 1 DISPLAY "7 T" ELSE DISPLAY "7 F".
           DISPLAY "AFTER".
           STOP RUN.
