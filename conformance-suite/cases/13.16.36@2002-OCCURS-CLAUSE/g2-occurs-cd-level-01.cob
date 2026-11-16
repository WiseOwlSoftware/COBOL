      *>--- conformance-case
      *> id: g2-occurs-cd-level-01
      *> rules: [2002-SR-13.16.36.2-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.36.2 SR1a - the OCCURS clause shall not be
      *> specified in a data description entry that "Has a level-number
      *> of 01, 66, 77, or 88". R1 is the level-01 record description
      *> entry after an OUTPUT CD and carries OCCURS 3 TIMES, so the
      *> clause is refused. Accepting it fails the case.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2OCCN1A.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  COUT FOR OUTPUT.
       01  R1 PIC X(2) OCCURS 3 TIMES.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "COMPILED"
           STOP RUN.
