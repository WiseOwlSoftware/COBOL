      *>--- conformance-case
      *> id: g2-constant-cd-figurative
      *> rules: [2002-SR-13.9.2-6]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.9.2 SR6 - "Neither literal-1 nor any of the literals
      *> in arithmetic-expression-1 shall be a figurative constant". KZ
      *> is a
      *> constant entry in the COMMUNICATION SECTION after a CD entry
      *> (§2002:13.1.1) whose literal-1 is the figurative constant ZERO;
      *> the program is refused at the KZ entry (it fails if a constant
      *> under a CD escapes the check and compiles).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CNP02.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       01  KZ CONSTANT AS ZERO.
       01  CDREC PIC X(10).
       PROCEDURE DIVISION.
           DISPLAY "COMPILED"
           STOP RUN.
