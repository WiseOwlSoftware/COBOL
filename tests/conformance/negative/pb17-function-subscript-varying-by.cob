      *> reject-at: 2002 2014 2023
      *> ISO 8.8.4.13 r2 - a function is evaluated "if and when the conditions
      *> containing them are evaluated", and 14.9.28 GR12 re-evaluates a PERFORM
      *> VARYING BY operand PER AUGMENT. A function-bearing SUBSCRIPT hoisted once
      *> to a statement pre-op would under-evaluate it (fix-queue PB17 / D18).
      *>
      *> THIS IS THE INHERITED GAP, NOT A NEW ONE. The D18 route deliberately
      *> registers on the SAME statement-pending list the user-defined-function
      *> activations use, so the subscript case rides UdfAttachPerEvaluation where
      *> that machinery reaches (a PERFORM UNTIL condition, a SEARCH WHEN, an
      *> EVALUATE object, a non-first AND/OR operand - all of which WORK, and are
      *> covered positively by pb17_function_subscript) and stages LOUD through the
      *> narrowed COBOLNET1509 in the three windows it does not reach. Staging loud
      *> is what the design required: an over- or under-activating hoist would be a
      *> silent wrong answer, and this is a named, diagnosable refusal instead.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB17NEGBY.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-G.
          05 W-E PIC 9(2) OCCURS 5 TIMES.
       01 W-I PIC 9(2).
       PROCEDURE DIVISION.
       MAIN.
           MOVE 1 TO W-E (1)
           PERFORM VARYING W-I FROM 1 BY W-E (FUNCTION INTEGER(1))
               UNTIL W-I > 3
               CONTINUE
           END-PERFORM
           STOP RUN.
