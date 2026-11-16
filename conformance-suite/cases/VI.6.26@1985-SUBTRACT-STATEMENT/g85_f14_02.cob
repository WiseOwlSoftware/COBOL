      *>--- conformance-case
      *> id: g85_f14_02
      *> rules: [1985-GR-VI.6.26.4-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.26.4 GR3 - 'data items in identifier-1 are
      *> subtracted from and stored into corresponding data items in
      *> identifier-2'; correspondence is §1985:VI.6.4.3 (same name and
      *> qualifiers up to the groups, both elementary numeric for
      *> SUBTRACT). X, Y and V OF G pair: B gets 100-5=95, 50-20=30,
      *> 10-3=7. W and Z have no partner, Q OF A and Q OF R OF B differ
      *> in qualifier R, N is alphanumeric: each B item keeps its value
      *> (9, 50, BB) and A is unchanged. Fails if the direction is
      *> reversed (B from A), if a non-pair is touched, or if A is
      *> stored into.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F1402.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A.
           05  X  PIC 9(3).
           05  Y  PIC 9(3).
           05  Z  PIC 9(3).
           05  G.
               10  V  PIC 99.
           05  Q  PIC 99.
           05  N  PIC XX.
       01  B.
           05  X  PIC 9(3).
           05  Y  PIC 9(3).
           05  W  PIC 9(3).
           05  G.
               10  V  PIC 99.
           05  R.
               10  Q  PIC 99.
           05  N  PIC XX.
       PROCEDURE DIVISION.
       P1.
           MOVE 5 TO X OF A.
           MOVE 20 TO Y OF A.
           MOVE 7 TO Z OF A.
           MOVE 3 TO V OF G OF A.
           MOVE 1 TO Q OF A.
           MOVE "AA" TO N OF A.
           MOVE 100 TO X OF B.
           MOVE 50 TO Y OF B.
           MOVE 9 TO W OF B.
           MOVE 10 TO V OF G OF B.
           MOVE 50 TO Q OF R OF B.
           MOVE "BB" TO N OF B.
           SUBTRACT CORRESPONDING A FROM B.
           DISPLAY "B " X OF B " " Y OF B " " W OF B " " V OF G OF B
               " " Q OF R OF B " " N OF B.
           DISPLAY "A " A.
           STOP RUN.
