      *>--- conformance-case
      *> id: pb5564_qualified_subscript_data_name_85
      *> rules: [1985-FMT-IV.4.3.8.2.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5564. §1985:IV.4.3.8.2.3 SR5: "Data-name-2 may be qualified and must be a
      *>   numeric elementary item representing an integer". K is declared twice, so each
      *>   subscript qualifies it: ELEM (K OF G1) is element 2, ELEM (K IN G2 + 1) element 4
      *>   and ELEM (K OF G1 - 1) element 1. Expected output: B, D, A. Fails if the subscript
      *>   gate, narrowed to refuse a subscripted data-name-2, also refuses a qualified one.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5564QS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  TBL.
           05  ELEM PIC X OCCURS 5 TIMES.
       01  G1.
           05  K PIC 9 VALUE 2.
       01  G2.
           05  K PIC 9 VALUE 3.
       PROCEDURE DIVISION.
       P1.
           MOVE "ABCDE" TO TBL.
           DISPLAY ELEM (K OF G1).
           DISPLAY ELEM (K IN G2 + 1).
           DISPLAY ELEM (K OF G1 - 1).
           STOP RUN.
