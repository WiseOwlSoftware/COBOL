      *>--- conformance-case
      *> id: g85_f06_09
      *> rules: [1989-AR-A.2.9.3-2, 1989-RV-A.2.9.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> The program collating sequence is MY-ALPHA (§1985:VI.4.4.4
      *> GR5): "M" ALSO "K" ALSO "Q" share position 1, "D" ALSO "C"
      *> share position 2, "W" is position 3, and every other native
      *> character follows in native order (§1985:VI.4.5.4 GR4).
      *> §1989:A.2.9.4 RV1 - where several characters share a
      *>   position, CHAR returns the first literal written for it:
      *>   CHAR(1) is "M" (not "K" or "Q"), CHAR(2) is "D" (not "C").
      *> §1989:A.2.9.3 AR2 - argument-1 ranges from 1 to the number
      *>   of positions in the collating sequence. The low end is
      *>   CHAR(1); the high end is the position of HIGH-VALUE, the
      *>   character with the highest position (§1985:VI.4.5.4 GR5),
      *>   found with ORD (§1989:A.2.31.4 RV1): CHAR of that number
      *>   is HIGH-VALUE again. The count itself is the native set's
      *>   and is not displayed; it exceeds the 3 written positions.
      *> A leg fails if CHAR returns a later literal of an ALSO group,
      *> if ORD does not give ALSO characters one shared position, or
      *> if the top position does not map back to HIGH-VALUE.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F0609.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. XX.
       OBJECT-COMPUTER. XX
           PROGRAM COLLATING SEQUENCE IS MY-ALPHA.
       SPECIAL-NAMES.
           ALPHABET MY-ALPHA IS "M" ALSO "K" ALSO "Q"
                                "D" ALSO "C"
                                "W".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-K PIC 9(9).
       01  WS-N PIC 9(9).
       01  WS-C PIC X.
       01  WS-H PIC X.
       01  WS-L PIC X.
       PROCEDURE DIVISION.
       P-MAIN.
           MOVE 1 TO WS-K.
           MOVE FUNCTION CHAR (WS-K) TO WS-C.
           DISPLAY "CHAR(1)=" WS-C.
           MOVE 2 TO WS-K.
           MOVE FUNCTION CHAR (WS-K) TO WS-C.
           DISPLAY "CHAR(2)=" WS-C.
           MOVE FUNCTION CHAR (3) TO WS-C.
           DISPLAY "CHAR(3)=" WS-C.
           MOVE "Q" TO WS-L.
           COMPUTE WS-N = FUNCTION ORD (WS-L).
           DISPLAY "ORD(Q)=" WS-N.
           MOVE "C" TO WS-L.
           COMPUTE WS-N = FUNCTION ORD (WS-L).
           DISPLAY "ORD(C)=" WS-N.
           MOVE HIGH-VALUE TO WS-H.
           COMPUTE WS-N = FUNCTION ORD (WS-H).
           IF WS-N > 3
               DISPLAY "TOP POSITION > 3 YES"
           ELSE
               DISPLAY "TOP POSITION > 3 NO".
           MOVE FUNCTION CHAR (WS-N) TO WS-C.
           IF WS-C = WS-H
               DISPLAY "CHAR(TOP)=HIGH-VALUE YES"
           ELSE
               DISPLAY "CHAR(TOP)=HIGH-VALUE NO".
           STOP RUN.
