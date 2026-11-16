      *>--- conformance-case
      *> id: g2-arg-alphabetic-alphanumeric
      *> rules: [2002-AR-15.2-1, 2002-AR-15.2-2]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:15.2 AR1 - an alphabetic argument: an elementary data
      *> item of class alphabetic, or an alphanumeric literal of only
      *> alphabetic characters, given to functions Table 22 marks Alph
      *> (UPPER-CASE, LENGTH, REVERSE, MAX, ORD-MAX, ORD-MIN).
      *> §2002:15.2 AR2 - an alphanumeric argument: a data item of class
      *> alphabetic or alphanumeric or an alphanumeric literal; a
      *> strongly-typed group item is treated as class and category
      *> alphanumeric (LOWER-CASE, LENGTH, UPPER-CASE, REVERSE of ST1).
      *> Expected lines, each from the 2002 function definitions:
      *>   AL1 HELLO   §2002:15.78.3 RV1, "hello" upper-cased.
      *>   AL2 05 03   §2002:15.39.3 RV3, alphanumeric positions of
      *>               PIC A(5) and of the literal "abc".
      *>   AL3 cba     §2002:15.65.3 RV1, position j takes n-j+1.
      *>   AL4 olleh   REVERSE of the PIC A item itself.
      *>   AL5 WORLD   §2002:15.47.3 RV1, a single argument-1 is the
      *>               greatest.
      *>   AL6 3 OK    ORD-MAX and ORD-MIN of two DIFFERENT values name
      *>               the two positions (§2002:15.58.3 RV1, 15.59.3
      *>               RV1), so they sum to 3 whatever the collating
      *>               sequence; MAX is the argument ORD-MAX names.
      *>   AN1 abcd    §2002:15.45.3 RV1 on a PIC X item.
      *>   AN2 ab-1    LOWER-CASE of a literal that is not alphabetic.
      *>   AN3 03 ABC cBa  LENGTH, UPPER-CASE and REVERSE of the
      *>               strongly-typed group ST1 holding "aBc".
      *>   AN4 1 1     alphabetic and alphanumeric arguments mixed
      *>               (§2002:15.58.2 AR2); equal values, so the
      *>               leftmost is named (15.58.3 RV3).
      *> A refusal of any argument, or a wrong size, fails the case.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2ARG01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A-LOW   PIC A(5) VALUE "hello".
       01 A-ONE   PIC A(5) VALUE "WORLD".
       01 A-TWO   PIC A(5) VALUE "HELLO".
       01 X-AN    PIC X(4) VALUE "abCD".
       01 X-TWO   PIC X(5) VALUE "HELLO".
       01 ST-T TYPEDEF STRONG.
          05 STF  PIC X(3).
       01 ST1 TYPE ST-T.
       01 N2A     PIC 99.
       01 N2B     PIC 99.
       01 N1A     PIC 9.
       01 N1B     PIC 9.
       01 N1C     PIC 9.
       01 W5      PIC X(5).
       PROCEDURE DIVISION.
       P0.
           DISPLAY "AL1 " FUNCTION UPPER-CASE(A-LOW)
           MOVE FUNCTION LENGTH(A-LOW) TO N2A
           MOVE FUNCTION LENGTH("abc") TO N2B
           DISPLAY "AL2 " N2A " " N2B
           DISPLAY "AL3 " FUNCTION REVERSE("abc")
           DISPLAY "AL4 " FUNCTION REVERSE(A-LOW)
           DISPLAY "AL5 " FUNCTION MAX(A-ONE)
           MOVE FUNCTION ORD-MAX(A-ONE A-TWO) TO N1A
           MOVE FUNCTION ORD-MIN(A-ONE A-TWO) TO N1B
           COMPUTE N1C = N1A + N1B
           MOVE FUNCTION MAX(A-ONE A-TWO) TO W5
           IF (N1A = 1 AND W5 = A-ONE) OR (N1A = 2 AND W5 = A-TWO)
               DISPLAY "AL6 " N1C " OK"
           ELSE
               DISPLAY "AL6 " N1C " BAD " W5
           END-IF
           DISPLAY "AN1 " FUNCTION LOWER-CASE(X-AN)
           DISPLAY "AN2 " FUNCTION LOWER-CASE("AB-1")
           MOVE "aBc" TO STF OF ST1
           MOVE FUNCTION LENGTH(ST1) TO N2A
           DISPLAY "AN3 " N2A " " FUNCTION UPPER-CASE(ST1) " "
                   FUNCTION REVERSE(ST1)
           MOVE FUNCTION ORD-MAX(A-TWO X-TWO) TO N1A
           MOVE FUNCTION ORD-MIN(X-TWO A-TWO) TO N1B
           DISPLAY "AN4 " N1A " " N1B
           STOP RUN.
