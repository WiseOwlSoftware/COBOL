      *>--- conformance-case
      *> id: g2-value-sr15-decimal-ws
      *> rules: [2002-SR-13.16.61.2-15]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.61.2 SR15 - subscript-1 written as a non-integer
      *> literal (1.5),
      *> in working-storage.
      *> A prohibiting syntax rule: the program shall be refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2V15C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A.
           05 E PIC X OCCURS 4 VALUE "A" FROM (1.5).
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY "NOT REFUSED".
           STOP RUN.
