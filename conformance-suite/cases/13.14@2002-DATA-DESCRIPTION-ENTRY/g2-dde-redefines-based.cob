      *>--- conformance-case
      *> id: g2-dde-redefines-based
      *> rules: [2002-SR-13.14.2-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.14.2 SR3 - "The REDEFINES clause shall not be
      *> specified in the same data description entry as the TYPEDEF or
      *> BASED clause."
      *> Leg: the BASED leg, a working-storage level 1 entry B written
      *> REDEFINES A BASED. The TYPEDEF leg is
      *> g2-dde-redefines-typedef. Accepting the entry fails the case.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2DDE14.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A PIC X(4) VALUE "ABCD".
       01  B REDEFINES A BASED PIC X(4).
       PROCEDURE DIVISION.
           DISPLAY A
           STOP RUN.
