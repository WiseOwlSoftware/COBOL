      *>--- conformance-case
      *> id: g2-value-sr12-f1-comp-cd
      *> rules: [2002-SR-13.16.61.2-12]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.61.2 SR12 - a group-level format 1 VALUE on an
      *> alphanumeric group
      *> with a subordinate item not of usage DISPLAY, in a record
      *> under an output CD.
      *> A prohibiting syntax rule: the program shall be refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2V12B.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT.
       01  G VALUE SPACES.
           05 G1 PIC 9(4) COMP.
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY "NOT REFUSED".
           STOP RUN.
