      *>--- conformance-case
      *> id: g2002-cd-usage-on-level-66
      *> rules: [2002-SR-13.16.58.2-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.58.2 SR1 - "The USAGE clause shall not be specified
      *> in a data description entry that has a level-number of 66 or
      *> 88." At 2002 the record description entries after a CD entry
      *> are ordinary data description entries (§2002:A.1.1.2), so the
      *> rule governs the CD record CR: the level-66 entry R writes a
      *> USAGE clause and is refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2UCD04.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CO FOR OUTPUT
           TEXT LENGTH IS CO-LEN
           STATUS KEY IS CO-STAT.
       01  CR.
           05  C1 PIC X(2).
       66  R RENAMES C1 USAGE DISPLAY.
       PROCEDURE DIVISION.
           MOVE "OK" TO C1.
           STOP RUN.
