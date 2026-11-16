      *>--- conformance-case
      *> id: g2-add-native-no-digit-loss
      *> rules: [2002-GR-14.8.2.3-4]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.2.3 GR4 - "When native arithmetic is in effect and
      *> none of the operands is described with usage binary-char,
      *> binary-short, binary-long, binary-double, float-short,
      *> float-long, or float-extended, enough places shall be carried
      *> so as not to lose any significant digits during execution."
      *> No OPTIONS paragraph, so native arithmetic is in effect
      *> (§2002:11.8.4.2 GR3: "it is as if the ARITHMETIC clause were
      *> specified with the NATIVE phrase"); every
      *> operand is USAGE DISPLAY. Each sum needs more places than any
      *> one operand or than a 64-bit binary or a double carries:
      *> SUM3  3 x 999999999999999999 = 2999999999999999997 (19
      *>       digits) GIVING 9(20) -> 02999999999999999997.
      *> WIDE  999999999999999999 + 0.000000001 GIVING 9(18).9(9)
      *>       -> 999999999999999999.000000001 (edited receiver).
      *> MIXED 99999999.99 + .000000001 GIVING 9(8).9(9)
      *>       -> 99999999.990000001.
      *> CANCEL 999999999999999999 + (-999999999999999998) + 0.5
      *>       GIVING Z9.9 -> " 1.5" (a double loses the 0.5).
      *> CD    operands that are fields of an input CD: TEXT LENGTH
      *>       9(4) = 9999, MESSAGE COUNT 9(6) = 123456; ADD both TO
      *>       W (100) -> 133555; ADD 0.25 IN-LEN GIVING 9(4).99
      *>       -> 9999.25. The receivers of WIDE, MIXED and CD are
      *>       numeric-edited (§2002:14.8.2.2 SR4) so the decimal point
      *>       is in the data, not a DISPLAY convention.
      *> A leg fails if any low-order digit is lost.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2ADD04.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X1  PIC 9(18) VALUE 999999999999999999.
       01 X2  PIC 9(18) VALUE 999999999999999999.
       01 X3  PIC 9(18) VALUE 999999999999999999.
       01 RES PIC 9(20).
       01 TINY PIC V9(9) VALUE .000000001.
       01 WIDE PIC 9(18).9(9).
       01 F1  PIC 9(8)V99 VALUE 99999999.99.
       01 FR  PIC 9(8).9(9).
       01 SP  PIC S9(18) VALUE 999999999999999999.
       01 SN  PIC S9(18) VALUE -999999999999999998.
       01 HALF PIC V9 VALUE .5.
       01 ED  PIC Z9.9.
       01 W   PIC 9(6) VALUE 100.
       01 R   PIC 9(4).99.
       COMMUNICATION SECTION.
       CD  CDIN FOR INPUT
           SYMBOLIC QUEUE IS IN-QUEUE
           MESSAGE DATE IS IN-MDATE
           MESSAGE TIME IS IN-MTIME
           SYMBOLIC SOURCE IS IN-SOURCE
           TEXT LENGTH IS IN-LEN
           END KEY IS IN-END
           STATUS KEY IS IN-STATUS
           MESSAGE COUNT IS IN-COUNT.
       PROCEDURE DIVISION.
       MAIN.
           ADD X1 X2 X3 GIVING RES
           DISPLAY "SUM3 " RES
           ADD X1 TINY GIVING WIDE
           DISPLAY "WIDE " WIDE
           ADD F1 TINY GIVING FR
           DISPLAY "MIXED " FR
           ADD SP SN HALF GIVING ED
           DISPLAY "CANCEL [" ED "]"
           MOVE 9999 TO IN-LEN
           MOVE 123456 TO IN-COUNT
           ADD IN-LEN IN-COUNT TO W
           DISPLAY "CD-TO " W
           ADD 0.25 IN-LEN GIVING R
           DISPLAY "CD-GIVING " R
           STOP RUN.
