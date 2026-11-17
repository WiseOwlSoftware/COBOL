*>--- conformance-case
*> id: pb124_always_integral_zoo
*> rules: [AR-15.3-6]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB124, PB5886 (AR-15.3-6) — the always-integral shapes of a scaled item the 15.3 type-6 screen
      *> admits, pinned compiling AND running: a product by a literal is exact, so S*10 cancels S's scale 1,
      *> and S - S nets the scaled term to zero. The screen fails closed since kb/Work PB5886, and a quotient
      *> over an item is no integer term ((I/2)*2, I/I and 362880/D9 are refused, pb5886-item-quotients).
      *> Values hand-derived (CHAR returns the character at the given ordinal position; ORD("A")
      *> = 66 in the native collating sequence, so CHAR(66) = "A" and CHAR(65) = "@").
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB124ZO.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 S PIC 9V9 VALUE 1.5.
       01 RS PIC X(1).
       PROCEDURE DIVISION.
       MAIN.
           MOVE FUNCTION CHAR(S * 10 + 50) TO RS
           IF RS = "@" DISPLAY "CANC OK" ELSE DISPLAY "CANC BAD " RS
           END-IF
           MOVE FUNCTION CHAR(S - S + 66) TO RS
           IF RS = "A" DISPLAY "NET0 OK" ELSE DISPLAY "NET0 BAD " RS
           END-IF
           STOP RUN.
