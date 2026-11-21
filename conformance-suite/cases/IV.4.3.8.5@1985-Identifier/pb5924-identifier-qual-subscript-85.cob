      *>--- conformance-case
      *> id: pb5924-identifier-qual-subscript-85
      *> rules: [1985-SR-IV.4.3.8.5.1-p2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:IV.4.3.8.5.1 (cite.py: OK): "must be followed by a syntactically correct combination of qualifiers, subscripts, or reference modifiers"
      *> A nonunique table element T is referenced by qualifier + subscript (+ reference modifier);
      *> each combination names exactly one element (or part of one).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. IDQS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  G1.
           05  T  PIC X(3) OCCURS 3 TIMES.
       01  G2.
           05  T  PIC X(3) OCCURS 3 TIMES.
       PROCEDURE DIVISION.
       P1.
           MOVE "ABC" TO T OF G1 (1).
           MOVE "DEF" TO T OF G1 (2).
           MOVE "GHI" TO T OF G1 (3).
           MOVE "JKL" TO T IN G2 (1).
           MOVE "MNO" TO T IN G2 (2).
           MOVE "PQR" TO T IN G2 (3).
           DISPLAY T OF G1 (2) " " T IN G2 (3) " " T OF G2 (1) (2:2).
           DISPLAY G1 " " G2.
           STOP RUN.
