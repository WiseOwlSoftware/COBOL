      *>--- conformance-case
      *> id: g2-value-sr12-f2-just-cd
      *> rules: [2002-SR-13.16.61.2-12]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.61.2 SR12 (banded FORMATS 1 AND 2) - a group-level
      *> format 2 VALUE
      *> with a JUSTIFIED subordinate, in a record under an output CD.
      *> A prohibiting syntax rule: the program shall be refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2V12C.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT.
       01  T.
           05 E OCCURS 2 VALUE "AB" FROM (1) TO (2).
              10 E1 PIC X(1) JUSTIFIED RIGHT.
              10 E2 PIC X(1).
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY "NOT REFUSED".
           STOP RUN.
