      *>--- conformance-case
      *> id: g2-value-sr15-name-cd
      *> rules: [2002-SR-13.16.61.2-15]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.61.2 SR15 - subscript-1 written as a data-name,
      *> not a literal,
      *> in a record under an output CD.
      *> A prohibiting syntax rule: the program shall be refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2V15B.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  K PIC 9 VALUE 1.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT.
       01  A.
           05 E PIC X OCCURS 4 VALUE "A" FROM (K).
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY "NOT REFUSED".
           STOP RUN.
