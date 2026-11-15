      *>--- conformance-case
      *> id: g2-value-sr12-f1-sync-cd
      *> rules: [2002-SR-13.16.61.2-12]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.61.2 SR12 - a group-level format 1 VALUE whose
      *> subordinate item
      *> is SYNCHRONIZED, in a record under an output CD.
      *> A prohibiting syntax rule: the program shall be refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2V12A.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT.
       01  G VALUE SPACES.
           05 G1 PIC X(2) SYNCHRONIZED.
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY "NOT REFUSED".
           STOP RUN.
