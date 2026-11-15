      *>--- conformance-case
      *> id: g2-char-national-integer-argument
      *> rules: [2002-AR-15.14.2-1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:15.14.2 AR1 - "Argument-1 shall be an integer."
      *> §2002:15.2 type 5 - "An arithmetic expression that will
      *> always result in an integer value or an integer data item
      *> shall be specified." Each leg writes argument-1 as an integer
      *> of a different shape - unsigned display item, signed binary
      *> item, integer arithmetic expression, integer function, and the
      *> TEXT LENGTH item of a 2002 input CD (an unsigned 4-digit
      *> integer, §2002:A.1.2.3 GR2h) - and must be ACCEPTED.
      *> Each ordinal comes from FUNCTION ORD of a national literal,
      *> "the ordinal position of argument-1 in the current national
      *> program collating sequence" (§2002:15.57.3 RV2), and
      *> §2002:15.14.3 RV1 returns "the character in the national
      *> program collating sequence having the ordinal position
      *> specified by argument-1", so CHAR-NATIONAL(ORD(n)) = n. The
      *> result is compared with the national literal, and the
      *> comparison's outcome is displayed, so the expectation needs
      *> neither the native sequence nor the DISPLAY form of national:
      *>   ITEM  W-ORD = ORD(N"Q")            -> Y
      *>   COMP  W-OC  = ORD(N"k")            -> Y
      *>   EXPR  W-ORD + 2 - 2                -> Y
      *>   FUNC  FUNCTION ORD(N"Z")           -> Y
      *>   CD    IN-LEN = ORD(N"7")           -> Y
      *>   DIFF  the FUNC result vs N"Q"      -> N (the control: a
      *>         function returning a constant would fail here)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CHN01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W-ORD   PIC 9(5).
       01  W-OC    PIC S9(5) COMP.
       COMMUNICATION SECTION.
       CD  CDIN FOR INPUT
           TEXT LENGTH IS IN-LEN.
       PROCEDURE DIVISION.
       P-MAIN.
           MOVE FUNCTION ORD(N"Q") TO W-ORD
           MOVE FUNCTION ORD(N"k") TO W-OC
           MOVE FUNCTION ORD(N"7") TO IN-LEN
           IF FUNCTION CHAR-NATIONAL(W-ORD) = N"Q"
               DISPLAY "ITEM=Y" ELSE DISPLAY "ITEM=N" END-IF
           IF FUNCTION CHAR-NATIONAL(W-OC) = N"k"
               DISPLAY "COMP=Y" ELSE DISPLAY "COMP=N" END-IF
           IF FUNCTION CHAR-NATIONAL(W-ORD + 2 - 2) = N"Q"
               DISPLAY "EXPR=Y" ELSE DISPLAY "EXPR=N" END-IF
           IF FUNCTION CHAR-NATIONAL(FUNCTION ORD(N"Z")) = N"Z"
               DISPLAY "FUNC=Y" ELSE DISPLAY "FUNC=N" END-IF
           IF FUNCTION CHAR-NATIONAL(IN-LEN) = N"7"
               DISPLAY "CD=Y" ELSE DISPLAY "CD=N" END-IF
           IF FUNCTION CHAR-NATIONAL(FUNCTION ORD(N"Z")) = N"Q"
               DISPLAY "DIFF=Y" ELSE DISPLAY "DIFF=N" END-IF
           STOP RUN.
