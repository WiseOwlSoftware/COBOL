      *>--- conformance-case
      *> id: g2-value-sr12-f2-comp-ws
      *> rules: [2002-SR-13.16.61.2-12]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.61.2 SR12 (banded FORMATS 1 AND 2) - a group-level
      *> format 2 VALUE
      *> on an alphanumeric group with a subordinate item not of usage
      *> DISPLAY.
      *> A prohibiting syntax rule: the program shall be refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2V12E.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  T.
           05 E OCCURS 2 VALUE SPACES FROM (1) TO (2).
              10 E1 PIC 9(4) COMP.
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY "NOT REFUSED".
           STOP RUN.
