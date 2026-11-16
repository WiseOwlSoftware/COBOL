      *>--- conformance-case
      *> id: pb5814_subtract_corresponding_subscript_once_85
      *> rules: [1985-GR-VI.6.26.4-3]
      *> accept: [85]
      *> source-format: fixed
      *>---
      *> kb/Work PB5814 - §1985:VI.6.26.4 (THE SUBTRACT STATEMENT, format 3)
      *> states no exception to the general sentence of §1985:IV.4.3.8 as
      *> amended: "any subscripting and reference modification are
      *> evaluated only once as the first operation of the execution of
      *> that statement", so R (I OF B) is identified once, at the start
      *> (D-CORR85; MOVE CORRESPONDING alone is per pair).
      *> TBL holds R(1) = I 0, J 5 and R(2) = I 1, J 7; I OF B starts at 2
      *> and J OF B at 9. Pair I subtracts R(2)'s I, 1, from I OF B, the
      *> subscript of the group operand R (I OF B), leaving 1. Pair J still
      *> reads R(2), J 7, and gives 9 - 7 = 2, so B is "12". Identified
      *> afresh per pair (the defect), pair J would read R(1), J 5, and
      *> print "14".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5185C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  TBL.
           05  R OCCURS 2 TIMES.
               10  I  PIC 9.
               10  J  PIC 9.
       01  B.
           05  I  PIC 9.
           05  J  PIC 9.
       PROCEDURE DIVISION.
       P1.
           MOVE "0517" TO TBL.
           MOVE 2 TO I OF B.
           MOVE 9 TO J OF B.
           SUBTRACT CORRESPONDING R (I OF B)
               FROM B.
           DISPLAY B.
           STOP RUN.
