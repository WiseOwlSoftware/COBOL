*>--- conformance-case
*> id: pb3468-figurative-constant-boolean-below-2002
*> rules: [SR-14.9.25.3-7]
*> source-format: free
*> portable: unverified
*>---
      *> The NEGATIVE below the introducing edition of
      *> conformance-suite/cases/14.9.25-MOVE/pb3468_figurative_constant_receivers.
      *> ISO 14.9.25.3 SR7 governs a figurative constant moved to a boolean data item, and boolean data (the
      *> PICTURE symbol 1, USAGE BIT, the boolean literal B"...") is a COBOL-2002 introduction, so below 2002
      *> the receiver itself is refused at its DATA DIVISION entry and no figurative MOVE can reach it.
      *>   cite.py --check 14.9.25.3 "Any figurative constant for which the associated character or characters
      *>   are not boolean characters shall not be moved to a boolean data item" -> OK 14.9.25.3 7)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3468NEG85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 B              PIC 1(5).
       PROCEDURE DIVISION.
           MOVE ZERO TO B
           DISPLAY "B=" B
           STOP RUN.
