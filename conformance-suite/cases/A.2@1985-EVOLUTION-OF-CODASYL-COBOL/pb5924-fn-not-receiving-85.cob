      *>--- conformance-case
      *> id: pb5924-fn-not-receiving-85
      *> rules: [1989-GR-A.2.3-p1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1989:A.2.3 (cite.py: OK): "Data item functions are treated as elementary data items and can not be receiving operands"
      *> A function-identifier as the receiving operand of ADD is refused.
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
           ADD 1 TO FUNCTION INTEGER(N).
           STOP RUN.
