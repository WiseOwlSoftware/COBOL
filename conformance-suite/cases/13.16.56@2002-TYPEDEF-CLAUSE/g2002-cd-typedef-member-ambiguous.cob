      *>--- conformance-case
      *> id: g2002-cd-typedef-member-ambiguous
      *> rules: [2002-GR-13.16.56.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.56.3 GR1 - "If there is more than one such group,
      *> qualification with the name of the group is necessary." TC is
      *> a type declaration under an output CD (§2002:A.1.1.2); CX and
      *> CY are both defined using TC, so the unqualified TA identifies
      *> no single item and the MOVE is refused. This refusal stands
      *> beside the accepted witness g2002-cd-typedef-member-refs.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2TDF05.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CO FOR OUTPUT
           TEXT LENGTH IS CO-LEN
           STATUS KEY IS CO-STAT.
       01  TC TYPEDEF.
           05  TA PIC X(2).
       01  CR.
           05  CX TYPE TC.
           05  CY TYPE TC.
       PROCEDURE DIVISION.
           MOVE "AB" TO TA.
           STOP RUN.
