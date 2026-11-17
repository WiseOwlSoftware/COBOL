*>--- conformance-case
*> id: pb124_always_integral_zoo
*> rules: [AR-15.3-6]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB124, PB5886 (AR-15.3-6) — always-integral shapes the 15.3 type-6 screen admits, pinned compiling
      *> AND running: a quotient by 2 is the product by 0.5 (its reciprocal terminates), so (I/2)*2 nets I's
      *> factor to 1; a product by a literal is exact, so S*10 cancels S's scale 1; S - S nets the scaled term
      *> to zero. The screen fails closed (kb/Work PB5886): a quotient by an item is no integer term, so I/I
      *> (a size error at I = 0) and 362880/D9 are refused (pb5886-item-quotients).
      *> Values hand-derived (CHAR returns the character at the given ordinal position; ORD("A")
      *> = 66 in the native collating sequence, so CHAR(66) = "A" and CHAR(65) = "@"): I = 6, (6/2)*2 + 60 = 66;
      *> 1.5*10 + 50 = 65; 1.5 - 1.5 + 66 = 66.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB124ZO.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 I PIC 9(4) VALUE 6.
       01 S PIC 9V9 VALUE 1.5.
       01 RS PIC X(1).
       PROCEDURE DIVISION.
       MAIN.
           MOVE FUNCTION CHAR((I / 2) * 2 + 60) TO RS
           IF RS = "A" DISPLAY "DIV2 OK" ELSE DISPLAY "DIV2 BAD " RS
           END-IF
           MOVE FUNCTION CHAR(S * 10 + 50) TO RS
           IF RS = "@" DISPLAY "CANC OK" ELSE DISPLAY "CANC BAD " RS
           END-IF
           MOVE FUNCTION CHAR(S - S + 66) TO RS
           IF RS = "A" DISPLAY "NET0 OK" ELSE DISPLAY "NET0 BAD " RS
           END-IF
           STOP RUN.
