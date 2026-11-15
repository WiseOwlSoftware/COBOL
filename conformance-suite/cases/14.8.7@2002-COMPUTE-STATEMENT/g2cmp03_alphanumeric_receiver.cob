      *>--- conformance-case
      *> id: g2cmp03_alphanumeric_receiver
      *> rules: [2002-SR-14.8.7.2-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.7.2 SR1 -- "Identifier-1 shall reference either an
      *> elementary numeric item or an elementary numeric-edited item."
      *> A1 is an elementary alphanumeric item: the COMPUTE is refused
      *> at its line.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CMP3.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A1 PIC X(3).
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE A1 = 1 + 2
           STOP RUN.
