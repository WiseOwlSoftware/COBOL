      *>--- conformance-case
      *> id: pb5185_move_corresponding_subscript_per_pair
      *> rules: [1985-GR-VI.6.19.4-1]
      *> accept: [85]
      *> source-format: fixed
      *>---
      *> §1985:VI.6.19.4 GR1 (THE MOVE STATEMENT): "The results are the same as
      *> if the user had referred to each pair of corresponding identifiers in
      *> separate MOVE statements." Each separate MOVE evaluates its own
      *> subscripts, so a pair that stores into a subscript of a group operand
      *> changes the element the later pairs use. TBL holds A(1,1)=22,
      *> A(1,2)=85, A(2,1)=57, A(2,2)=66, and I OF B and J OF B start at 1.
      *> Pair I stores I OF A(1,1) = 2 into I OF B. Pair J then identifies
      *> A(2,1) and stores its J, 7, into J OF B, so B is "27", the result of
      *> the two separate MOVEs in the order written. A compiler that
      *> identifies A(I OF B, J OF B) once, at the start, prints "22".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5185A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  TBL.
           05  R OCCURS 2 TIMES.
               10  A OCCURS 2 TIMES.
                   15  I  PIC 9.
                   15  J  PIC 9.
       01  B.
           05  I  PIC 9.
           05  J  PIC 9.
       PROCEDURE DIVISION.
       P1.
           MOVE "22855766" TO TBL.
           MOVE 1 TO I OF B.
           MOVE 1 TO J OF B.
           MOVE CORRESPONDING A (I OF B, J OF B) TO B.
           DISPLAY B.
           STOP RUN.
