      *>--- conformance-case
      *> id: pb5924-fn-sending-elementary-85
      *> rules: [1989-GR-A.2.3-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1989:A.2.3 (cite.py: OK): "Data item functions are treated as elementary data items and can not be receiving operands"
      *> An alphanumeric function is an elementary sending item: moved, reference-modified, moved to a
      *> group (space-filled as an alphanumeric move) and displayed.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. FNSEND.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W PIC X(3) VALUE "abc".
       01 A PIC X(3).
       01 G.
          05 G1 PIC X(2).
          05 G2 PIC X(2).
       PROCEDURE DIVISION.
       P0.
           MOVE FUNCTION UPPER-CASE(W) TO A.
           DISPLAY A.
           MOVE FUNCTION UPPER-CASE(W)(2:2) TO A.
           DISPLAY A "|".
           MOVE FUNCTION UPPER-CASE(W) TO G.
           DISPLAY G "|".
           DISPLAY FUNCTION UPPER-CASE(W).
           STOP RUN.
