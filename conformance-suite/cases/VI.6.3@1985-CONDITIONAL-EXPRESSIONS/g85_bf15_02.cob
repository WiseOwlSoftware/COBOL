      *>--- conformance-case
      *> id: g85_bf15_02
      *> rules: [1985-GR-VI.6.3.4-1, 1985-GR-VI.6.3.4-2,
      *>    1985-GR-VI.6.3.4-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.3.4 GR1 - connected conditions of a level are
      *> evaluated left to right and evaluation stops once the truth
      *> value is known; §1985:VI.6.3.4 GR2 - values are established
      *> for arithmetic expressions and functions, and negated
      *> conditions are evaluated, only if and when needed.
      *> The probe is FUNCTION RANDOM with a seed: "If a subsequent
      *> reference specifies argument-1, a new sequence of
      *> pseudo-random numbers is started" (§1989:A.2.35.3 AR2) and
      *> "For a given seed value on a given implementation, the
      *> sequence of pseudo-random numbers will always be the same"
      *> (§1989:A.2.35.4 RV2), so the next unseeded RANDOM tells which
      *> seed was applied last. R-REF is the second number of the
      *> seed-5 sequence. SEQ=5 means the last seed was 5, SEQ=X not.
      *> L1 seed 5; F = 1 OR RANDOM(7)*10+1 < 20: true at once, the
      *>    expression is never evaluated -> TRUE SEQ=5   (GR1, GR2)
      *> L2 seed 5; F = 2 AND RANDOM(7) < 2: false at once -> FALSE
      *>    SEQ=5                                            (GR1)
      *> L3 seed 7; F = 2 OR RANDOM(5) < 2: the second condition is
      *>    needed and evaluated -> TRUE SEQ=5             (GR1, GR2)
      *> L4 seed 9; RANDOM(7) < 2 AND RANDOM(5) < 2: left to right, so
      *>    5 is the last seed -> TRUE SEQ=5                   (GR1)
      *> L5 seed 5; F = 1 OR NOT RANDOM(7) > 2: the negated condition
      *>    is not needed -> TRUE SEQ=5                        (GR2)
      *> L6 seed 7; F = 2 OR NOT RANDOM(5) > 2: needed, evaluated ->
      *>    TRUE SEQ=5                                         (GR2)
      *> L7 seed 5; (F = 2 AND RANDOM(7) < 2) OR F = 1: the inner
      *>    level ends at F = 2 -> TRUE SEQ=5                  (GR1)
      *> Any leg that evaluates a skipped condition, or evaluates
      *> right to left, ends on another seed and prints SEQ=X.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF1502.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  F      PIC 9 VALUE 1.
       01  R-TMP  PIC 9V9(9).
       01  R-REF  PIC 9V9(9).
       01  R-GOT  PIC 9V9(9).
       01  TV     PIC X(5).
       01  SQ     PIC X.
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE R-TMP = FUNCTION RANDOM (5).
           COMPUTE R-REF = FUNCTION RANDOM.
      * L1
           COMPUTE R-TMP = FUNCTION RANDOM (5).
           IF F = 1 OR FUNCTION RANDOM (7) * 10 + 1 < 20
               MOVE "TRUE" TO TV
           ELSE
               MOVE "FALSE" TO TV.
           PERFORM CHECK-SEQ.
           DISPLAY "L1 " TV " SEQ=" SQ.
      * L2
           COMPUTE R-TMP = FUNCTION RANDOM (5).
           IF F = 2 AND FUNCTION RANDOM (7) < 2
               MOVE "TRUE" TO TV
           ELSE
               MOVE "FALSE" TO TV.
           PERFORM CHECK-SEQ.
           DISPLAY "L2 " TV " SEQ=" SQ.
      * L3
           COMPUTE R-TMP = FUNCTION RANDOM (7).
           IF F = 2 OR FUNCTION RANDOM (5) < 2
               MOVE "TRUE" TO TV
           ELSE
               MOVE "FALSE" TO TV.
           PERFORM CHECK-SEQ.
           DISPLAY "L3 " TV " SEQ=" SQ.
      * L4
           COMPUTE R-TMP = FUNCTION RANDOM (9).
           IF FUNCTION RANDOM (7) < 2 AND FUNCTION RANDOM (5) < 2
               MOVE "TRUE" TO TV
           ELSE
               MOVE "FALSE" TO TV.
           PERFORM CHECK-SEQ.
           DISPLAY "L4 " TV " SEQ=" SQ.
      * L5
           COMPUTE R-TMP = FUNCTION RANDOM (5).
           IF F = 1 OR NOT FUNCTION RANDOM (7) > 2
               MOVE "TRUE" TO TV
           ELSE
               MOVE "FALSE" TO TV.
           PERFORM CHECK-SEQ.
           DISPLAY "L5 " TV " SEQ=" SQ.
      * L6
           COMPUTE R-TMP = FUNCTION RANDOM (7).
           IF F = 2 OR NOT FUNCTION RANDOM (5) > 2
               MOVE "TRUE" TO TV
           ELSE
               MOVE "FALSE" TO TV.
           PERFORM CHECK-SEQ.
           DISPLAY "L6 " TV " SEQ=" SQ.
      * L7
           COMPUTE R-TMP = FUNCTION RANDOM (5).
           IF (F = 2 AND FUNCTION RANDOM (7) < 2) OR F = 1
               MOVE "TRUE" TO TV
           ELSE
               MOVE "FALSE" TO TV.
           PERFORM CHECK-SEQ.
           DISPLAY "L7 " TV " SEQ=" SQ.
           STOP RUN.
       CHECK-SEQ.
           COMPUTE R-GOT = FUNCTION RANDOM.
           IF R-GOT = R-REF
               MOVE "5" TO SQ
           ELSE
               MOVE "X" TO SQ.
