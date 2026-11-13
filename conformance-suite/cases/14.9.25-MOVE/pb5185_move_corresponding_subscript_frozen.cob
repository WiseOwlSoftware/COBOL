      *>--- conformance-case
      *> id: pb5185_move_corresponding_subscript_frozen
      *> rules: [GR-14.9.25.4-11]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *>---
      *> §2023:14.9.25.4 GR11 (MOVE, Format 2): the results are those of separate
      *> MOVE statements "except that if subscripting is specified for
      *> identifier-3 or identifier-4, the subscript value used is that
      *> resulting from the evaluation of the subscript at the start of the
      *> execution of the statement." TBL holds A(1,1)=22, A(1,2)=85,
      *> A(2,1)=57, A(2,2)=66, and I OF B and J OF B start at 1. Both pairs
      *> read A(1,1) although the first pair stores into I OF B, so B is "22".
      *> A compiler that re-evaluates the subscripts per pair prints "27".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5185B.
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
