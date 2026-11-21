      *>--- conformance-case
      *> id: pb5924-fn-arg-class-85
      *> rules: [1989-AR-A.2.2-p2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1989:A.2.2 (cite.py: OK): "Arguments may be required to have a certain class or a subset of a certain class"
      *> UPPER-CASE requires an alphabetic or alphanumeric argument; a numeric data item is refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. FNPRB.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9 VALUE 5.
       01 X PIC 9V9 VALUE 6.5.
       01 W PIC X(3) VALUE "abc".
       01 R PIC 9.
       01 A PIC X(3).
       PROCEDURE DIVISION.
       P0.
           MOVE FUNCTION UPPER-CASE(N) TO A.
           STOP RUN.
