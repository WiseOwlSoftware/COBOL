      *>--- conformance-case
      *> id: g85_f38_06
      *> rules: [1989-RV-A.2.32.4-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.32.4 RV3 - when more than one argument-1 has the
      *> greatest value, ORD-MAX returns the position of the LEFTMOST
      *> one. Legs: a numeric tie in the middle (3 7 7 2 -> 2); a tie
      *> at both ends (9 1 9 -> 1); all equal (5 5 5 -> 1); a tie of
      *> equal values of different scale (1 7.00 7 -> 2); an
      *> alphanumeric tie ("B" "C" "A" "C" -> 2); and a tie that
      *> holds only by the space padding of a simple condition
      *> (§1989:A.2.32.4 RV2: "AB" = "AB  ", greater than "A" -> 1).
      *> Taking the rightmost tie prints 3, 3, 3, 3, 4, 2.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F3806.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W-AB2       PIC X(2) VALUE "AB".
       01  W-AB4       PIC X(4) VALUE "AB".
       01  W-A1        PIC X    VALUE "A".
       01  W-N         PIC 9.
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE W-N = FUNCTION ORD-MAX (3 7 7 2).
           DISPLAY "O1 " W-N.
           COMPUTE W-N = FUNCTION ORD-MAX (9 1 9).
           DISPLAY "O2 " W-N.
           COMPUTE W-N = FUNCTION ORD-MAX (5 5 5).
           DISPLAY "O3 " W-N.
           COMPUTE W-N = FUNCTION ORD-MAX (1 7.00 7).
           DISPLAY "O4 " W-N.
           COMPUTE W-N = FUNCTION ORD-MAX ("B" "C" "A" "C").
           DISPLAY "O5 " W-N.
           COMPUTE W-N = FUNCTION ORD-MAX (W-AB2 W-AB4 W-A1).
           DISPLAY "O6 " W-N.
           STOP RUN.
