      *>--- conformance-case
      *> id: g2-goto-not-last-exit-function
      *> rules: [2002-SR-14.8.16.2-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.16.2 SR2 - EXIT FUNCTION (2002-only, table 13: EXIT
      *>   has no conditional phrase) is imperative (§2002:14.4), so
      *>   GO TO P1 EXIT FUNCTION puts the GO TO before another
      *>   imperative statement in its sequence: refused.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. G2GOTO04.
       DATA DIVISION.
       LINKAGE SECTION.
       01  P-R     PIC 9.
       PROCEDURE DIVISION RETURNING P-R.
       MAIN-P.
           MOVE 1 TO P-R.
           GO TO P1 EXIT FUNCTION.
       P1.
           GOBACK.
       END FUNCTION G2GOTO04.
