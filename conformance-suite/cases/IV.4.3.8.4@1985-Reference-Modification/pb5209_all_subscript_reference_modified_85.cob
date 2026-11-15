      *>--- conformance-case
      *> id: pb5209_all_subscript_reference_modified_85
      *> rules: [1985-GR-IV.4.3.8.4.4-3, 1985-FMT-IV.4.3.8.4.2,
      *>     1985-FMT-IV.4.3.8.5.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5209. §1985:IV.4.3.8.5.2 prints the identifier as
      *> data-name-1 [qualifiers] [({subscript} ...)] [reference-
      *> modifier]; §1985:IV.4.3.8.2.3 SR7: "The subscript ALL may be
      *> used only when the subscripted identifier is used as a function
      *> argument"; §1985:IV.4.3.8.4.4 GR3: "If an ALL subscript is
      *> specified for an operand, the reference-modifier is applied to
      *> each of the implicitly specified elements of the table".
      *> TE holds ABC DEF GHI, NE 419 275 638, C2 PQ RS / TU VW.
      *>   MAX (TE (ALL) (2:1))          B E H        -> H  (was COBOL2363)
      *>   MAX (TE OF TT (ALL) (2:1))    qualified    -> H
      *>   MIN (TE (ALL) (K:)), K = 2    BC EF HI     -> BC (no length)
      *>   MAX (NE (ALL) (3:1))          9 5 8        -> 9  (a DISPLAY
      *>                                 numeric element, SR1)
      *>   MIN (C2 (ALL ALL) (2:1))      Q S U W      -> Q
      *>   MAX (C2 (1 ALL) (1:1) "A")    P R A        -> R
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5209A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  TT.
           05 TE PIC X(3) OCCURS 3.
       01  TN.
           05 NE PIC 9(3) OCCURS 3.
       01  T2.
           05 R2 OCCURS 2.
              10 C2 PIC X(2) OCCURS 2.
       01  W PIC X(3).
       01  K PIC 9 VALUE 2.
       PROCEDURE DIVISION.
       P1.
           MOVE "ABCDEFGHI" TO TT.
           MOVE "419275638" TO TN.
           MOVE "PQRSTUVW" TO T2.
           MOVE FUNCTION MAX (TE (ALL) (2:1)) TO W.
           DISPLAY "REFMOD [" W "]".
           MOVE FUNCTION MAX (TE OF TT (ALL) (2:1)) TO W.
           DISPLAY "QUAL [" W "]".
           MOVE FUNCTION MIN (TE (ALL) (K:)) TO W.
           DISPLAY "OPEN [" W "]".
           MOVE FUNCTION MAX (NE (ALL) (3:1)) TO W.
           DISPLAY "NUM [" W "]".
           MOVE FUNCTION MIN (C2 (ALL ALL) (2:1)) TO W.
           DISPLAY "TWO [" W "]".
           MOVE FUNCTION MAX (C2 (1 ALL) (1:1) "A") TO W.
           DISPLAY "MIX [" W "]".
           STOP RUN.
