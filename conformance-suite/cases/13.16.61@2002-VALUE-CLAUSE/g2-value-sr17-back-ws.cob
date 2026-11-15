      *>--- conformance-case
      *> id: g2-value-sr17-back-ws
      *> rules: [2002-SR-13.16.61.2-17]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.61.2 SR17 - in two dimensions, TO (1 2) is the 2nd
      *> element in
      *> subscripted-reference order and FROM (2 1) the 3rd, so the TO
      *> element precedes
      *> the FROM element although subscript-2 of the second dimension
      *> is larger.
      *> A prohibiting syntax rule: the program shall be refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2V17D.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  T2.
           05 R OCCURS 3.
              10 C PIC X OCCURS 2 VALUE "A" FROM (2 1) TO (1 2).
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY "NOT REFUSED".
           STOP RUN.
