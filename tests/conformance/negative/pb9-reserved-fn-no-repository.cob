      *> reject-at: 2002 2014 2023
      *> ISO 8.4.3.2.3 SR2 allows the word FUNCTION to be omitted ONLY for an intrinsic the REPOSITORY paragraph
      *> declares. There is no REPOSITORY here, so `SUM(1 2 3)` is not a function reference - and because SUM is
      *> an 8.9 RESERVED word it cannot be a data name either (8.3.2.4.1), so there is no other reading to fall
      *> back on and the rejection is unambiguous rather than an unresolved-name guess.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB9NEGREPO.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC S9(4).
       PROCEDURE DIVISION.
           COMPUTE N = SUM(1 2 3)
           STOP RUN.
