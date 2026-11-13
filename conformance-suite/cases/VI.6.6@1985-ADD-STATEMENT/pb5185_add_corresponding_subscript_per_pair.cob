      *>--- conformance-case
      *> id: pb5185_add_corresponding_subscript_per_pair
      *> rules: [1985-GR-VI.6.6.4-3]
      *> accept: [85]
      *> source-format: fixed
      *>---
      *> §1985:VI.6.6.4 GR3 (THE ADD STATEMENT, format 3) says only that data
      *> items in identifier-1 are added to and stored in corresponding data
      *> items in identifier-2; it prints no identification time, so below 2002
      *> the choice is the implementor's and GnuCOBOL's: each pair identifies
      *> R (I OF B) afresh, as MOVE CORRESPONDING does (D-CORR85).
      *> TBL holds R(1) = I 1, J 5 and R(2) = I 0, J 7; I OF B starts at 1.
      *> The pair I result stores into I OF B, the subscript of the group
      *> operand R (I OF B).
      *> Pair I makes I OF B 2, so pair J reads R(2), J 7: B is "27". Frozen
      *> at the start, pair J would read R(1), J 5, and print "25".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5185B.
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
           MOVE "1507" TO TBL.
           MOVE 1 TO I OF B.
           MOVE 0 TO J OF B.
           ADD CORRESPONDING R (I OF B) TO B.
           DISPLAY B.
           STOP RUN.
