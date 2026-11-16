      *>--- conformance-case
      *> id: g2002-cd-typedef-name-no-storage
      *> rules: [2002-GR-13.16.56.3-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.56.3 GR2 - "A type declaration has no storage
      *> associated with it." TC is a type declaration written under an
      *> output CD (§2002:A.1.1.2), so it is no record of the CD and
      *> names no data item: the MOVE to TC is refused. This refusal
      *> stands beside the accepted witness g2002-cd-typedef-no-storage.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2TDF04.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CO FOR OUTPUT
           TEXT LENGTH IS CO-LEN
           STATUS KEY IS CO-STAT.
       01  TC TYPEDEF.
           05  TA PIC X(2).
       01  CR.
           05  CX TYPE TC.
       PROCEDURE DIVISION.
           MOVE "AB" TO TC.
           STOP RUN.
