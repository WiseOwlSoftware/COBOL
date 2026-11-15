      *>--- conformance-case
      *> id: pb5185_add_corresponding_subscript_frozen
      *> rules: [GR-14.7.6-6]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *>---
      *> §2023:14.7.6 GR6 identifies the implied statements' operands once, at the
      *> start of the statement, for ADD and SUBTRACT CORRESPONDING.
      *> TBL holds R(1) = I 1, J 5 and R(2) = I 0, J 7; I OF B starts at 1.
      *> The pair I result stores into I OF B, the subscript of the group
      *> operand R (I OF B).
      *> Pair J reads R(1), J 5, so B is "25".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5185D.
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
