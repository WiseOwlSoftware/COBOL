      *>--- conformance-case
      *> id: pb5185_subtract_corresponding_subscript_per_pair
      *> rules: [1985-GR-VI.6.26.4-3]
      *> accept: [85]
      *> source-format: fixed
      *>---
      *> §1985:VI.6.26.4 (THE SUBTRACT STATEMENT, format 3) prints no
      *> identification time for CORRESPONDING, so below 2002 each pair
      *> identifies R (I OF B) afresh, as MOVE CORRESPONDING does (D-CORR85).
      *> TBL holds R(1) = I 0, J 5 and R(2) = I 1, J 7; I OF B starts at 2
      *> and J OF B at 9. Pair I subtracts R(2)'s I, 1, from I OF B, the
      *> subscript of the group operand R (I OF B), leaving 1. Pair J then
      *> identifies R(1) and gives 9 - 5 = 4, so B is "14". Frozen at the
      *> start, pair J would read R(2), J 7, and print "12".
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
