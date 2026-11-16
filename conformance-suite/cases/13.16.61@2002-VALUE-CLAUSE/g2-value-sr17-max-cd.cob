      *>--- conformance-case
      *> id: g2-value-sr17-max-cd
      *> rules: [2002-SR-13.16.61.2-17]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.61.2 SR17 - subscript-2 (4) exceeds OCCURS 3, in a
      *> record under
      *> an output CD.
      *> A prohibiting syntax rule: the program shall be refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2V17B.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT.
       01  T.
           05 TE PIC X OCCURS 3 VALUE "P" FROM (1) TO (4).
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY "NOT REFUSED".
           STOP RUN.
