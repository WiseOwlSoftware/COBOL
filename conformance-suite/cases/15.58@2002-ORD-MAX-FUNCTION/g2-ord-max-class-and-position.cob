      *>--- conformance-case
      *> id: g2-ord-max-class-and-position
      *> rules: [2002-AR-15.58.2-2, 2002-RV-15.58.3-1,
      *>    2002-RV-15.58.3-3]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:15.58.2 AR2 - "All arguments shall be of the same class
      *>   with the exception that mixing of arguments of alphabetic
      *>   and alphanumeric classes is allowed."
      *> §2002:15.58.3 RV1 - "the ordinal number that corresponds to
      *>   the position of the argument-1 having the greatest value"
      *> §2002:15.58.3 RV3 - several equal to the greatest value: "the
      *>   position of the leftmost argument-1 having that value"
      *> RV2 compares by the simple-condition rules (§2002:8.8.4.1):
      *> numeric operands algebraically, alphanumeric and national
      *> operands with the shorter one padded with spaces. Each leg
      *> is one class throughout (numeric, alphanumeric, national),
      *> or the allowed alphabetic/alphanumeric mix; NATNUM mixes
      *> PIC 9 USAGE NATIONAL items with a display item - all class
      *> numeric in 2002 (§2002:8.5.2.9 item 1, kb/Work PB5625
      *> reading B) - so AR2 accepts it. Expected positions:
      *>   NUM    002  RV1: 9 is greatest, position 2
      *>   NEG    002  RV1: -1 > -2 > -5 algebraically
      *>   SCALE  002  RV1: 1.30 > 1.299 > 1.25
      *>   ONE    001  RV1: a single argument is position 1
      *>   TIE    002  RV3: two 7s, the leftmost is 2
      *>   TIEALL 001  RV3: all equal, leftmost 1
      *>   NATNUM 002  AR2: -5, 7, 4 all class numeric
      *>   ALNUM  003  RV1: GAMMA greatest
      *>   MIX    002  AR2 exception: PIC A with PIC X; ABCE
      *>   PADTIE 001  RV3: "AB" = "AB  " (space pad)
      *>   NAT    002  RV1: N"C" greatest
      *>   NATTIE 001  RV3: N"B" = W-N, leftmost 1
      *> A leg fails if a legal class combination is refused, if the
      *> comparison is not the simple-condition one (sign, scale,
      *> padding, national), or if a tie does not take the leftmost.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2OMX01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W-S   PIC S9(3) VALUE -1.
       01  W-S2  PIC S9(3) VALUE -10.
       01  W-SC  PIC 9V99 VALUE 1.25.
       01  W-PK  PIC S9V999 USAGE PACKED-DECIMAL VALUE 1.299.
       01  W-D   PIC S9(3) VALUE 7.
       01  W-NN  PIC S9(3) USAGE NATIONAL VALUE -5.
       01  W-NU  PIC 9(3) USAGE NATIONAL VALUE 4.
       01  W-A   PIC A(4) VALUE "ABCD".
       01  W-X   PIC X(4) VALUE "ABCE".
       01  W-X2  PIC X(4) VALUE "AB".
       01  W-N   PIC N VALUE N"B".
       01  W-N2  PIC N VALUE N"A".
       01  I     PIC 9(3).
       PROCEDURE DIVISION.
       P-MAIN.
           MOVE FUNCTION ORD-MAX(3 9 4) TO I
           DISPLAY "NUM=" I
           MOVE FUNCTION ORD-MAX(-5 W-S -2) TO I
           DISPLAY "NEG=" I
           MOVE FUNCTION ORD-MAX(W-SC 1.30 W-PK) TO I
           DISPLAY "SCALE=" I
           MOVE FUNCTION ORD-MAX(5) TO I
           DISPLAY "ONE=" I
           MOVE FUNCTION ORD-MAX(3 7 7 1) TO I
           DISPLAY "TIE=" I
           MOVE FUNCTION ORD-MAX(4 4 4) TO I
           DISPLAY "TIEALL=" I
           MOVE FUNCTION ORD-MAX(W-NN W-D W-NU) TO I
           DISPLAY "NATNUM=" I
           MOVE FUNCTION ORD-MAX("BETA" "ALPHA" "GAMMA") TO I
           DISPLAY "ALNUM=" I
           MOVE FUNCTION ORD-MAX(W-A W-X "AB") TO I
           DISPLAY "MIX=" I
           MOVE FUNCTION ORD-MAX("AB" W-X2 "A") TO I
           DISPLAY "PADTIE=" I
           MOVE FUNCTION ORD-MAX(N"B" N"C" N"A") TO I
           DISPLAY "NAT=" I
           MOVE FUNCTION ORD-MAX(N"B" W-N N"A") TO I
           DISPLAY "NATTIE=" I
           STOP RUN.
