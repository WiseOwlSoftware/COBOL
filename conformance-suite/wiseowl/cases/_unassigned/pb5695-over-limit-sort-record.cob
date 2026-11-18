      *>--- conformance-case
      *> id: pb5695-over-limit-sort-record
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5695 review R1-1: the limit on one data item (D-ITEM-SIZE,
      *> 134217728 character positions) refuses the group with COBOL7641 and
      *> the compile STOPS there, so no later pass measures it.
      *> SR = 10 + 4000000000 positions, a sort file's record beside a
      *> second record SR2.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5695R5.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT S ASSIGN TO "pb5695r5.tmp".
       DATA DIVISION.
       FILE SECTION.
       SD S.
       01 SR.
          05 K PIC X(10).
          05 A PIC X(100000) OCCURS 20000.
          05 B PIC X(100000) OCCURS 20000.
       01 SR2 PIC X(10).
       PROCEDURE DIVISION.
       MAIN-P.
           SORT S ON ASCENDING KEY K
               INPUT PROCEDURE IS P1 OUTPUT PROCEDURE IS P2.
           STOP RUN.
       P1.
           RELEASE SR.
       P2.
           RETURN S AT END CONTINUE END-RETURN.
