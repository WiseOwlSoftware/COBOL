      *>--- conformance-case
      *> id: g2cmp02_group_receiver
      *> rules: [2002-SR-14.8.7.2-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.7.2 SR1 -- "Identifier-1 shall reference either an
      *> elementary numeric item or an elementary numeric-edited item."
      *> G1 is a group item, not elementary, though its only subordinate
      *> is numeric: the COMPUTE is refused at its line.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CMP2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G1.
          02 N1 PIC 9.
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE G1 = 1 + 2
           STOP RUN.
