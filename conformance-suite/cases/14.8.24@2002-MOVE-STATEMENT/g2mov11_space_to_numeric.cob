      *>--- conformance-case
      *> id: g2mov11_space_to_numeric
      *> rules: [2002-SR-14.8.24.2-5]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.24.2 SR5 - "The figurative constant SPACE shall
      *>   not be moved to a numeric or numeric-edited item." The MOVE
      *>   of SPACE to the numeric item N (PIC 9(3)) violates it and is
      *>   refused at the MOVE line. Fails if the compiler accepts it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2MOV11.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N  PIC 9(3).
       PROCEDURE DIVISION.
       MAIN.
           MOVE SPACE TO N.
           STOP RUN.
