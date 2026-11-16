      *>--- conformance-case
      *> id: g2-value-sr16-max-cd
      *> rules: [2002-SR-13.16.61.2-16]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.61.2 SR16 - subscript-1 (5) exceeds OCCURS 4, in a
      *> record under
      *> an output CD.
      *> A prohibiting syntax rule: the program shall be refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2V16B.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT.
       01  A.
           05 E PIC X OCCURS 4 VALUE "A" FROM (5).
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY "NOT REFUSED".
           STOP RUN.
