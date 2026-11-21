      *>--- conformance-case
      *> id: pb5924-identifier-subscript-unqualified-85
      *> rules: [1985-SR-IV.4.3.8.5.1-p2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:IV.4.3.8.5.1 (cite.py: OK): "must be followed by a syntactically correct combination of qualifiers, subscripts, or reference modifiers"
      *> T is not unique; T (2) carries a subscript but no qualifier, so it is not an identifier: refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. IDSU.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  G1.
           05  T  PIC X(3) OCCURS 3 TIMES.
       01  G2.
           05  T  PIC X(3) OCCURS 3 TIMES.
       PROCEDURE DIVISION.
       P1.
           MOVE "ABC" TO T (2).
           STOP RUN.
