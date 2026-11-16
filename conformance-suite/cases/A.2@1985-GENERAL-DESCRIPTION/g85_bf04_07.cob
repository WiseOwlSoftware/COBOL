      *>--- conformance-case
      *> id: g85_bf04_07
      *> rules: [1989-AR-A.2.2-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.2 AR2 - alphabetic arguments: an alphabetic item or
      *> an
      *> all-alphabetic nonnumeric literal; its size can be used
      * Every argument here is an alphabetic one AR2 admits: AL1, AL2,
      * AL3 are PIC A items, "ABCDEFG" and "ABC DEF" literals of
      * alphabetic characters only. "The size associated with the
      * argument can be used in determining the value of the function":
      * LENGTH(AL1) = 5, LENGTH("ABCDEFG") = 7; LOWER-CASE keeps the
      * argument's length 11 (A.2.22.4 RV2) and lowers it: [hello world
      * ]; MAX(AL1 AL2) is alphanumeric (A.2.23.1) of the selected
      * argument's size (A.2.23.4 RV3): PEAR > APPLE ("PEAR " against
      * "APPLE"), so [PEAR ...], length 4, and ORD-MAX = 2. A result
      * sized from the wrong argument answers MAXLEN=0005.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF0407.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 AL1 PIC A(5) VALUE "APPLE".
       01 AL2 PIC A(4) VALUE "PEAR".
       01 AL3 PIC A(11) VALUE "HELLO WORLD".
       01 W-N4 PIC 9(4).
       01 W-X12 PIC X(12).
       PROCEDURE DIVISION.
       P1.
           COMPUTE W-N4 = FUNCTION LENGTH (AL1).
           DISPLAY "LENITEM=" W-N4.
           COMPUTE W-N4 = FUNCTION LENGTH ("ABCDEFG").
           DISPLAY "LENLIT=" W-N4.
           MOVE FUNCTION LOWER-CASE (AL3) TO W-X12.
           DISPLAY "LOWER=[" W-X12 "]".
           COMPUTE W-N4 = FUNCTION LENGTH (FUNCTION LOWER-CASE (AL3)).
           DISPLAY "LOWERLEN=" W-N4.
           MOVE FUNCTION LOWER-CASE ("ABC DEF") TO W-X12.
           DISPLAY "LOWERLIT=[" W-X12 "]".
           MOVE FUNCTION MAX (AL1 AL2) TO W-X12.
           DISPLAY "MAX=[" W-X12 "]".
           COMPUTE W-N4 = FUNCTION LENGTH (FUNCTION MAX (AL1 AL2)).
           DISPLAY "MAXLEN=" W-N4.
           COMPUTE W-N4 = FUNCTION ORD-MAX (AL1 AL2).
           DISPLAY "ORDMAX=" W-N4.
           STOP RUN.
