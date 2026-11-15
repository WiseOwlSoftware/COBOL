      *>--- conformance-case
      *> id: g2-value-sr17-count-cd
      *> rules: [2002-SR-13.16.61.2-17]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.61.2 SR17 - one subscript-2 for an entry under two
      *> OCCURS clauses,
      *> in a record under an output CD.
      *> A prohibiting syntax rule: the program shall be refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2V17C.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT.
       01  T2.
           05 R OCCURS 3.
              10 C PIC X OCCURS 2 VALUE "A" FROM (1 1) TO (2).
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY "NOT REFUSED".
           STOP RUN.
