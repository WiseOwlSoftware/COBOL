      *>--- conformance-case
      *> id: g2-value-sr10-sub-cd
      *> rules: [2002-SR-13.16.61.2-10]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.61.2 SR10 - format 1 VALUE on an entry subordinate
      *> to an entry
      *> containing a REDEFINES clause, in a record under an output CD.
      *> A prohibiting syntax rule: the program shall be refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2V10B.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT.
       01  A.
           05 A1 PIC X(2).
           05 A2 REDEFINES A1.
              10 A3 PIC X(2) VALUE "AB".
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY "NOT REFUSED".
           STOP RUN.
