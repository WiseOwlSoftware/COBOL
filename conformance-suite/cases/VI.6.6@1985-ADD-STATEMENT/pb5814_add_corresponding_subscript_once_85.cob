      *>--- conformance-case
      *> id: pb5814_add_corresponding_subscript_once_85
      *> rules: [1985-GR-VI.6.6.4-3]
      *> accept: [85]
      *> source-format: fixed
      *>---
      *> kb/Work PB5814 - §1985:VI.6.6.4 GR3 (THE ADD STATEMENT, format 3):
      *> "data items in identifier-1 are added to and stored in
      *> corresponding data items in identifier-2". The ADD rules state no
      *> exception to the general sentence of §1985:IV.4.3.8 as amended:
      *> "any subscripting and reference modification are evaluated only
      *> once as the first operation of the execution of that statement",
      *> so R (I OF B) is identified once, at the start (D-CORR85; MOVE
      *> CORRESPONDING alone is per pair, §1985:VI.6.19.4 GR1).
      *> TBL holds R(1) = I 1, J 5 and R(2) = I 0, J 7; I OF B starts at 1.
      *> The pair I result stores into I OF B, the subscript of the group
      *> operand R (I OF B): pair I makes I OF B 2, and pair J still reads
      *> R(1), J 5: B is "25". Identified afresh per pair (the defect),
      *> pair J would read R(2), J 7, and print "27".
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
