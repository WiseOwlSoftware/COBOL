      *>--- conformance-case
      *> id: g2-value-sr17-count-ws
      *> rules: [2002-SR-13.16.61.2-17]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.61.2 SR17 - one subscript-2 for an entry under two
      *> OCCURS clauses,
      *> in working-storage.
      *> A prohibiting syntax rule: the program shall be refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2V17F.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  T2.
           05 R OCCURS 3.
              10 C PIC X OCCURS 2 VALUE "A" FROM (1 1) TO (2).
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY "NOT REFUSED".
           STOP RUN.
