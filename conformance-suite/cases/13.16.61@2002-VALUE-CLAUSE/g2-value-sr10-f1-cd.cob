      *>--- conformance-case
      *> id: g2-value-sr10-f1-cd
      *> rules: [2002-SR-13.16.61.2-10]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.61.2 SR10 - format 1 VALUE on an entry that
      *> contains a REDEFINES
      *> clause, in a record description entry under an output CD
      *> (§2002:A.1.1).
      *> Fails if the compiler accepts the VALUE in the communication
      *> section.
      *> A prohibiting syntax rule: the program shall be refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2V10A.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT.
       01  A.
           05 A1 PIC X(2).
           05 A2 REDEFINES A1 PIC X(2) VALUE "AB".
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY "NOT REFUSED".
           STOP RUN.
