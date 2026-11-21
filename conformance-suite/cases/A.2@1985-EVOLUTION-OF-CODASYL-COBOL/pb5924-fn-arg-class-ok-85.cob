      *>--- conformance-case
      *> id: pb5924-fn-arg-class-ok-85
      *> rules: [1989-AR-A.2.2-p2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1989:A.2.2 (cite.py: OK): "Arguments may be required to have a certain class or a subset of a certain class"
      *> Arguments of the required class (alphanumeric, integer, numeric expression) are accepted and run.
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
           MOVE FUNCTION UPPER-CASE(W) TO A.
           DISPLAY A.
           COMPUTE R = FUNCTION MOD(7 3).
           DISPLAY R.
           COMPUTE R = FUNCTION SQRT(N + 4).
           DISPLAY R.
           STOP RUN.
