      *>--- conformance-case
      *> id: g2rnd02-random-seed-class-and-domain
      *> rules: [2002-AR-15.62.2-1, 2002-AR-15.62.2-2]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:15.62.2 AR1 - "Argument-1 shall be of class numeric."
      *> §2002:15.62.2 AR2 - "If argument-1 is specified, it shall be
      *>   zero or a positive integer. It is used as the seed value to
      *>   generate a sequence of pseudo-random numbers."
      *> No pseudo-random value is displayed (§2002:15.62.3 RV3 leaves
      *> the sequences to the implementor); every line is a fact the
      *> 2002 rules fix:
      *> LIT..CD - a class-numeric seed of value 7 in six shapes
      *>   (literal, binary item, packed item, arithmetic expression,
      *>   PIC 9 USAGE NATIONAL item - class numeric in 2002 by
      *>   §2002:8.5.2.9 item 1, kb/Work PB5625 reading B - and an
      *>   input-CD MESSAGE COUNT item) is accepted (AR1) and used as
      *>   the seed (AR2): AR3 starts a new sequence at each reference
      *>   with an argument and RV2 makes a given seed's sequence
      *>   always the same, so its first value equals that of
      *>   RANDOM(7), and RV1 puts it in [0, 1): "SAME". No exception
      *>   is raised for a legal seed, so W-RAISED stays N.
      *> ZERO - zero is a legal seed (AR2): RANDOM(0) twice is SAME.
      *> NEG - -1 is not "zero or a positive integer":
      *>   an incorrect argument value, so §2002:15.2 sets
      *>   EC-ARGUMENT-FUNCTION; checking is on (>>TURN), so the
      *>   declarative runs (§2002:14.8.45.3 GR3) and records the
      *>   exception-name (§2002:15.28.2 RV1); RESUME AT NEXT
      *>   STATEMENT returns after the COMPUTE (§2002:14.8.31.3 GR2):
      *>   "Y EC-ARGUMENT-FUNCTION".
      *> (A non-integer seed ITEM is refused at compile time instead:
      *> Table 22 of §2002:15.5 types the RANDOM argument Int1, and
      *> §2002:15.2 AR5 asks "an integer data item"; see g2rnd04.)
      *> A leg fails if a legal shape is refused, if a seed is not
      *> used (a different first value), if a legal seed raises, or
      *> if an illegal one does not.
       >>TURN EC-ARGUMENT-FUNCTION CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2RND02.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W-BN  PIC S9(4) USAGE BINARY VALUE 7.
       01  W-PK  PIC S9(3) USAGE PACKED-DECIMAL VALUE 7.
       01  W-NN  PIC 9(3) USAGE NATIONAL VALUE 7.
       01  W-NEG PIC S9 VALUE -1.
       01  R1    PIC 9V9(9).
       01  R2    PIC 9V9(9).
       01  W-RAISED PIC X VALUE "N".
       01  ES    PIC X(20) VALUE SPACES.
       COMMUNICATION SECTION.
       CD  CDIN FOR INPUT
           MESSAGE COUNT IS IN-COUNT.
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-ARGUMENT-FUNCTION.
       H-P.
           MOVE "Y" TO W-RAISED
           MOVE FUNCTION EXCEPTION-STATUS TO ES
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       P-MAIN.
           MOVE 7 TO IN-COUNT
           COMPUTE R1 = FUNCTION RANDOM(7)
           COMPUTE R2 = FUNCTION RANDOM(7)
           IF R1 = R2 AND R1 >= 0 AND R1 < 1
               DISPLAY "LIT=SAME " W-RAISED
           ELSE
               DISPLAY "LIT=DIFF " W-RAISED
           END-IF
           COMPUTE R1 = FUNCTION RANDOM(W-BN)
           COMPUTE R2 = FUNCTION RANDOM(7)
           IF R1 = R2 AND R1 >= 0 AND R1 < 1
               DISPLAY "ITEM=SAME " W-RAISED
           ELSE
               DISPLAY "ITEM=DIFF " W-RAISED
           END-IF
           COMPUTE R1 = FUNCTION RANDOM(W-PK)
           COMPUTE R2 = FUNCTION RANDOM(7)
           IF R1 = R2 AND R1 >= 0 AND R1 < 1
               DISPLAY "PACK=SAME " W-RAISED
           ELSE
               DISPLAY "PACK=DIFF " W-RAISED
           END-IF
           COMPUTE R1 = FUNCTION RANDOM(3 + 4)
           COMPUTE R2 = FUNCTION RANDOM(7)
           IF R1 = R2 AND R1 >= 0 AND R1 < 1
               DISPLAY "EXPR=SAME " W-RAISED
           ELSE
               DISPLAY "EXPR=DIFF " W-RAISED
           END-IF
           COMPUTE R1 = FUNCTION RANDOM(W-NN)
           COMPUTE R2 = FUNCTION RANDOM(7)
           IF R1 = R2 AND R1 >= 0 AND R1 < 1
               DISPLAY "NAT=SAME " W-RAISED
           ELSE
               DISPLAY "NAT=DIFF " W-RAISED
           END-IF
           COMPUTE R1 = FUNCTION RANDOM(IN-COUNT)
           COMPUTE R2 = FUNCTION RANDOM(7)
           IF R1 = R2 AND R1 >= 0 AND R1 < 1
               DISPLAY "CD=SAME " W-RAISED
           ELSE
               DISPLAY "CD=DIFF " W-RAISED
           END-IF
           COMPUTE R1 = FUNCTION RANDOM(0)
           COMPUTE R2 = FUNCTION RANDOM(0)
           IF R1 = R2 AND R1 >= 0 AND R1 < 1
               DISPLAY "ZERO=SAME " W-RAISED
           ELSE
               DISPLAY "ZERO=DIFF " W-RAISED
           END-IF
           MOVE "N" TO W-RAISED
           MOVE SPACES TO ES
           COMPUTE R1 = FUNCTION RANDOM(W-NEG)
           DISPLAY "NEG=" W-RAISED " " ES
           STOP RUN.
