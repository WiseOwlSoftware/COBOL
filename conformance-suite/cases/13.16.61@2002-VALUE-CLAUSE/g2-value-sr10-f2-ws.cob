      *>--- conformance-case
      *> id: g2-value-sr10-f2-ws
      *> rules: [2002-SR-13.16.61.2-10]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.61.2 SR10 (banded FORMATS 1 AND 2) - a format 2
      *> (table) VALUE
      *> subordinate to a REDEFINES entry in working-storage.
      *> A prohibiting syntax rule: the program shall be refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2V10D.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A.
           05 A1 PIC X(4).
           05 A2 REDEFINES A1.
              10 E PIC X OCCURS 4 VALUE "A" FROM (1).
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY "NOT REFUSED".
           STOP RUN.
