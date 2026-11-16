      *>--- conformance-case
      *> id: g2-value-sr17-back-cd
      *> rules: [2002-SR-13.16.61.2-17]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.61.2 SR17 - the TO element (2) precedes the FROM
      *> element (3),
      *> in a record under an output CD.
      *> A prohibiting syntax rule: the program shall be refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2V17A.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT.
       01  T.
           05 TE PIC X OCCURS 3 VALUE "P" FROM (3) TO (2).
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY "NOT REFUSED".
           STOP RUN.
