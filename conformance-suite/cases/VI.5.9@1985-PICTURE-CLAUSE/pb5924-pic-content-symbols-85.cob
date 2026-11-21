      *>--- conformance-case
      *> id: pb5924-pic-content-symbols-85
      *> rules: [1985-SR-VI.5.9.6-p2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:VI.5.9.6 (cite.py: OK): "at least two
      *> occurrences of one of the symbols '+', '-', or 'cs' must be
      *> present"
      *> Each admitted shape: two of '+', '-' or cs alone (floating
      *> insertion), or one of A X Z 9 '*'. Compiles, and each edits as
      *> the floating-insertion and suppression rules give.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB59240.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  E1 PIC ++.
       01  E2 PIC --.
       01  E3 PIC $$.
       01  E4 PIC **.
       01  E5 PIC Z.
       01  E6 PIC 9.
       01  E7 PIC A.
       01  E8 PIC X.
       PROCEDURE DIVISION.
       P0.
           MOVE -5 TO E1.
           MOVE -3 TO E2.
           MOVE 7 TO E3.
           MOVE 4 TO E4.
           MOVE 6 TO E5.
           MOVE 8 TO E6.
           MOVE "Q" TO E7.
           MOVE "R" TO E8.
           DISPLAY "[" E1 "][" E2 "][" E3 "][" E4 "][" E5 "][" E6
               "][" E7 "][" E8 "]".
           MOVE 5 TO E1.
           MOVE 3 TO E2.
           DISPLAY "[" E1 "][" E2 "]".
           STOP RUN.
