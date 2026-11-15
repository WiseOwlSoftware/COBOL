      *>--- conformance-case
      *> id: g2fni01_paren_after_function_is_its_argument_list
      *> rules: [2002-SR-8.4.2.2.2-4]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:8.4.2.2.2 SR4 - "If a function's definition permits
      *> arguments and a left parenthesis immediately follows
      *> function-prototype-name-1 or intrinsic-function-name-1, the
      *> left parenthesis is always treated as the left parenthesis of
      *> that function's arguments." RANDOM permits an optional
      *> argument (§2002:15.62.1 FUNCTION RANDOM [ ( [ argument-1 ] ) ]).
      *> §2002:15.62.3 RV1: RANDOM's "returned value is greater than or
      *> equal to zero and less than one"; §2002:15.47.3 RV1: MAX returns
      *> "the content of the argument-1 having the greatest value".
      *> A = 9, B = 5, N PIC 9 (MOVE keeps the integer part).
      *>   L1 MAX (RANDOM (A) B): the "(" after RANDOM is RANDOM's
      *>      argument list, so MAX sees two arguments RANDOM(9) < 1
      *>      and 5: N = 5. Reading "(A)" as a second MAX argument
      *>      would give 9 - the leg fails on that misparse.
      *>   L2 MAX ((RANDOM) (A) B): no "(" follows RANDOM, so A is a
      *>      MAX argument: 9.
      *>   L3 MAX (RANDOM () A B): RANDOM's argument list is empty: 9.
      *>   L4 MAX (RANDOM A B): RANDOM takes no argument list: 9.
      *> L2-L4 fail if a following operand is swallowed as RANDOM's.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2FNI01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A   PIC 9 VALUE 9.
       01  B   PIC 9 VALUE 5.
       01  N   PIC 9.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE FUNCTION MAX (FUNCTION RANDOM (A) B) TO N.
           DISPLAY "L1 " N.
           MOVE FUNCTION MAX ( (FUNCTION RANDOM) (A) B) TO N.
           DISPLAY "L2 " N.
           MOVE FUNCTION MAX (FUNCTION RANDOM () A B) TO N.
           DISPLAY "L3 " N.
           MOVE FUNCTION MAX (FUNCTION RANDOM A B) TO N.
           DISPLAY "L4 " N.
           STOP RUN.
