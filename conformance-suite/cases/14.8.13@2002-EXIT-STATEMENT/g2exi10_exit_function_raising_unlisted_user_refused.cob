      *>--- conformance-case
      *> id: g2exi10_exit_function_raising_unlisted_user_refused
      *> rules: [2002-SR-14.8.13.2-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.13.2 SR3 (FORMATS 2, 3, AND 4) - "Exception-name-1
      *> shall be a level-3 exception-name as specified in the rules for
      *> exception conditions." "If exception-name-1 is a level-3
      *> exception-name for EC-USER, exception-name-1 shall be specified
      *> in the RAISING phrase of the procedure division header of the
      *> source element in which this EXIT statement is contained."
      *> (cite.py --check OK on both sentences, SR3.)
      *> Leg: EXIT FUNCTION RAISING EXCEPTION EC-USER-N10 - a level-3 EC-USER
      *> name that the header's RAISING phrase does not list (the header
      *> has no RAISING phrase at all).
      *> Every other line is legal 2002 source, so the one diagnosis is
      *> SR3 at the EXIT statement. The accepted side (listed level-3
      *> EC-USER names on all three formats) is g2exi04.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. G2EXI10.
       DATA DIVISION.
       LINKAGE SECTION.
       01 R PIC 9.
       PROCEDURE DIVISION RETURNING R.
       P1.
           MOVE 1 TO R
           EXIT FUNCTION RAISING EXCEPTION EC-USER-N10.
       END FUNCTION G2EXI10.
