      *>--- conformance-case
      *> id: g2-boolean-of-integer-arguments
      *> rules: [2002-AR-15.11.2-1, 2002-AR-15.11.2-2]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:15.11.2 AR1 - "Argument-1 shall be a positive integer."
      *> §2002:15.11.2 AR2 - "Argument-2 shall be a positive nonzero
      *> integer." §2002:15.2 type 5 - "An arithmetic expression that
      *> will always result in an integer value or an integer data
      *> item shall be specified." Each leg writes both arguments as
      *> an integer of a different shape (literal, binary item, display
      *> item, integer expression, the TEXT LENGTH and MESSAGE COUNT
      *> items of a 2002 input CD - unsigned integers of 4 and 6 digits,
      *> §2002:A.1.2.3 GR2h and GR2k) and must be ACCEPTED.
      *> §2002:15.11.3 RV1 - the result has "the same bit configuration
      *> as the binary representation of the value of argument-1" and
      *> is "zero-filled or truncated on the left" to argument-2
      *> boolean positions:
      *>   LIT   (5, 8)     5 = 101          -> 00000101
      *>   TRUNC (544, 6)   544 = 1000100000 -> 100000 (left-truncated)
      *>   ONE   (1, 1)     the smallest legal arg-1 and arg-2 -> 1
      *>   FULL  (255, 8)   11111111
      *>   ITEM  (W-I=11, W-L=10)            -> 0000001011
      *>   EXPR  (W-I*2+1=23, W-L-5=5)       -> 10111
      *>   CD    (IN-COUNT=6, IN-LEN=4)      -> 0110
      *> A leg fails if a legal integer shape is refused, if the wrong
      *> length is returned (argument-2 not honoured), or if the fill
      *> or truncation is on the wrong side.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2BOI01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W-I     PIC S9(4) COMP VALUE 11.
       01  W-L     PIC 99 VALUE 10.
       COMMUNICATION SECTION.
       CD  CDIN FOR INPUT
           TEXT LENGTH IS IN-LEN
           MESSAGE COUNT IS IN-COUNT.
       PROCEDURE DIVISION.
       P-MAIN.
           DISPLAY "LIT=" FUNCTION BOOLEAN-OF-INTEGER(5, 8)
           DISPLAY "TRUNC=" FUNCTION BOOLEAN-OF-INTEGER(544, 6)
           DISPLAY "ONE=" FUNCTION BOOLEAN-OF-INTEGER(1, 1)
           DISPLAY "FULL=" FUNCTION BOOLEAN-OF-INTEGER(255, 8)
           DISPLAY "ITEM=" FUNCTION BOOLEAN-OF-INTEGER(W-I, W-L)
           DISPLAY "EXPR=" FUNCTION BOOLEAN-OF-INTEGER(W-I * 2 + 1,
               W-L - 5)
           MOVE 6 TO IN-COUNT
           MOVE 4 TO IN-LEN
           DISPLAY "CD=" FUNCTION BOOLEAN-OF-INTEGER(IN-COUNT, IN-LEN)
           STOP RUN.
