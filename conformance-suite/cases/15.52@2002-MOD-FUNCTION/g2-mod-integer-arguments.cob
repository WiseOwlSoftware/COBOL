      *>--- conformance-case
      *> id: g2-mod-integer-arguments
      *> rules: [2002-AR-15.52.2-1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:15.52.2 AR1 - "Argument-1 and argument-2 shall be
      *> integers." An integer argument is "An arithmetic expression
      *> that will always result in an integer value or an integer data
      *> item" (§2002:15.2 AR5). Every integer form is written and
      *> evaluated: signed and unsigned integer literals, signed DISPLAY
      *> and COMP integer data items, and always-integral expressions.
      *> §2002:15.52.3 RV1 gives the value as
      *>   (a1) - ((a2) * FUNCTION INTEGER ((a1) / (a2)))
      *> with INTEGER the greatest integer <= its argument
      *> (§2002:15.34.3 RV2):
      *>   M1 MOD(11 5)       11 - 5 * INT(2.2)  = 11 - 10   -> +1
      *>   M2 MOD(-11 5)     -11 - 5 * INT(-2.2) = -11 + 15  -> +4
      *>   M3 MOD(11 -5)      11 + 5 * INT(-2.2) = 11 - 15   -> -4
      *>   M4 MOD(-11 -5)    -11 + 5 * INT(2.2)  = -11 + 10  -> -1
      *>      (M1-M4 are the 2002 NOTE table)
      *>   M5 MOD(N1 N2)      N1 = -11, N2 = 5 (items)       -> +4
      *>   M6 MOD(N1 N2 * 2) -11 - 10 * INT(-1.1) = -11 + 20 -> +9
      *>   M7 MOD(N1 - 12 5) -23 - 5 * INT(-4.6) = -23 + 25  -> +2
      *>   M8 MOD(-10 N2)    -10 - 5 * INT(-2)   = 0         -> +0
      *> A leg fails if an integer argument form is refused or if the
      *> sign follows truncation toward zero instead of INTEGER's floor
      *> (M2, M3, M6, M7 would then read -1, +1, -1, -3).
      *> The non-integer arm is refused by g2-mod-noninteger-refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2MOD01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  R       PIC +9.
       01  N1      PIC S99 VALUE -11.
       01  N2      PIC 9 COMP VALUE 5.
       PROCEDURE DIVISION.
       P-MAIN.
           MOVE FUNCTION MOD(11 5) TO R
           DISPLAY "M1=" R
           MOVE FUNCTION MOD(-11 5) TO R
           DISPLAY "M2=" R
           MOVE FUNCTION MOD(11 -5) TO R
           DISPLAY "M3=" R
           MOVE FUNCTION MOD(-11 -5) TO R
           DISPLAY "M4=" R
           MOVE FUNCTION MOD(N1 N2) TO R
           DISPLAY "M5=" R
           MOVE FUNCTION MOD(N1 N2 * 2) TO R
           DISPLAY "M6=" R
           MOVE FUNCTION MOD(N1 - 12 5) TO R
           DISPLAY "M7=" R
           MOVE FUNCTION MOD(-10 N2) TO R
           DISPLAY "M8=" R
           STOP RUN.
