      *>--- conformance-case
      *> id: pb5972-condition-name-unqualified-85
      *> rules: [1985-SR-IV.4.3.8.1-L2.1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5972. §1985:IV.4.2.2.1.1 (cite.py: OK) puts
      *> "condition-names, data-names, and record-names" in one
      *> disjoint set, and §1985:IV.4.3.8.1 SRL2.1 (cite.py: OK):
      *> "For each nonunique user-defined name that is explicitly
      *> referenced, uniqueness must be established through a
      *> sequence of qualifiers which precludes any ambiguity of
      *> reference." IF X in a condition position names neither the
      *> level-88 X nor the data item X uniquely, so it is refused
      *> (COBOL1639). A compiler that resolves X in the
      *> condition-names alone compiles it and shows COND.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5972B.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  R1.
           05  F           PIC 9 VALUE 1.
               88  X       VALUE 1.
       01  R2.
           05  X           PIC 9 VALUE 7.
       PROCEDURE DIVISION.
       MAIN-1.
           IF X DISPLAY "COND".
           STOP RUN.
