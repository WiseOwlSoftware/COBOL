      *>--- conformance-case
      *> id: g2-char-integer-argument
      *> rules: [2002-AR-15.13.2-1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:15.13.2 AR1 - "Argument-1 shall be an integer."
      *> §2002:15.2 type 5 - "An arithmetic expression that will
      *> always result in an integer value or an integer data item
      *> shall be specified." Each leg writes argument-1 as an integer
      *> of a different shape - unsigned display item, signed binary
      *> item, integer arithmetic expression, integer function, and the
      *> TEXT LENGTH item of a 2002 input CD (an unsigned 4-digit
      *> integer, §2002:A.1.2.3 GR2h) - and must be ACCEPTED.
      *> The expected characters need no knowledge of the native
      *> sequence: each ordinal is taken from FUNCTION ORD, which
      *> returns "the ordinal position of argument-1 in the current
      *> alphanumeric program collating sequence" (§2002:15.57.3 RV1),
      *> and §2002:15.13.3 RV1 returns "the character in the
      *> alphanumeric program collating sequence having the ordinal
      *> position specified by argument-1", so CHAR(ORD(c)) = c:
      *>   ITEM  CHAR(W-ORD)            W-ORD = ORD("Q")  -> Q
      *>   COMP  CHAR(W-OC)             W-OC  = ORD("k")  -> k
      *>   EXPR  CHAR(W-ORD + 2 - 2)    integer expression -> Q
      *>   FUNC  CHAR(FUNCTION ORD("Z"))                  -> Z
      *>   CD    CHAR(IN-LEN)           IN-LEN = ORD("7") -> 7
      *> A leg fails if a legal integer shape is refused or if the
      *> ordinal reaches CHAR off by one (CHAR's ordinal is 1-based).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CHR01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W-ORD   PIC 9(4).
       01  W-OC    PIC S9(4) COMP.
       COMMUNICATION SECTION.
       CD  CDIN FOR INPUT
           TEXT LENGTH IS IN-LEN.
       PROCEDURE DIVISION.
       P-MAIN.
           MOVE FUNCTION ORD("Q") TO W-ORD
           MOVE FUNCTION ORD("k") TO W-OC
           MOVE FUNCTION ORD("7") TO IN-LEN
           DISPLAY "ITEM=" FUNCTION CHAR(W-ORD)
           DISPLAY "COMP=" FUNCTION CHAR(W-OC)
           DISPLAY "EXPR=" FUNCTION CHAR(W-ORD + 2 - 2)
           DISPLAY "FUNC=" FUNCTION CHAR(FUNCTION ORD("Z"))
           DISPLAY "CD=" FUNCTION CHAR(IN-LEN)
           STOP RUN.
