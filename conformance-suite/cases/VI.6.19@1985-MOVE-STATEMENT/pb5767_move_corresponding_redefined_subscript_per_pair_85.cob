      *>--- conformance-case
      *> id: pb5767_move_corresponding_redefined_subscript_per_pair_85
      *> rules: [1985-GR-VI.6.19.4-1, 1985-GR-VI.6.19.4-2]
      *> accept: [85]
      *> source-format: fixed
      *>---
      *> kb/Work PB5767 - §1985:VI.6.19.4 GR1: "The results are the same as
      *> if the user had referred to each pair of corresponding identifiers
      *> in separate MOVE statements"; GR2: "Any length evaluation or
      *> subscripting associated with identifier-2 is evaluated immediately
      *> before the data is moved to the respective data item". So each pair
      *> evaluates the group operands' subscripts afresh, also when a
      *> subscript's data-name lies in a REDEFINED record (whose read is a
      *> statement-level temporary in this compiler: the defect stored it
      *> once, before the first pair, and every pair read it).
      *> Why each leg can fail:
      *>   SEND  T = "22111911", I OF R = J OF R = 1, R redefined by RX:
      *>         MOVE CORRESPONDING S (I OF R, J OF R) TO R. Pair I moves
      *>         I OF S (1, 1) = 2 to I OF R; pair J then reads S (2, 1),
      *>         J = 9: R is "29", the separate MOVEs in declaration order.
      *>         Frozen (the defect) it read S (1, 1) again: "22".
      *>   RECV  the subscripts KA, KB lie in TX, which redefines T itself:
      *>         MOVE CORRESPONDING SRC TO S (KA, KB) with SRC = K 2, L 2
      *>         and T = "11111111". Pair K stores 2 into K OF S (1, 1),
      *>         which is KA, so pair L stores into S (2, 1): "21111211".
      *>         Frozen (the defect): "22111111".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5767MC85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R.
          05 I PIC 9.
          05 J PIC 9.
       01 RX REDEFINES R PIC X(2).
       01 T.
          05 RW OCCURS 2.
             10 S OCCURS 2.
                15 I PIC 9.
                15 J PIC 9.
       01 SRC.
          05 K PIC 9 VALUE 2.
          05 L PIC 9 VALUE 2.
       01 U.
          05 UW OCCURS 2.
             10 US OCCURS 2.
                15 K PIC 9.
                15 L PIC 9.
       01 UX REDEFINES U.
          05 KA PIC 9.
          05 KB PIC 9.
       PROCEDURE DIVISION.
       P1.
           MOVE "22111911" TO T.
           MOVE 1 TO I OF R. MOVE 1 TO J OF R.
           MOVE CORRESPONDING S (I OF R, J OF R) TO R.
           DISPLAY "SEND " RX.
           MOVE "11111111" TO U.
           MOVE CORRESPONDING SRC TO US (KA, KB).
           DISPLAY "RECV " U.
           STOP RUN.
