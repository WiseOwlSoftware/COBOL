      *>--- conformance-case
      *> id: g2002-cd-index-data-item-move
      *> rules: [2002-SR-13.16.58.2-9]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.58.2 SR9 - "An index data item may be referenced
      *> explicitly only in a SEARCH or SET statement, a relation
      *> condition, an intrinsic function argument, ..." A MOVE
      *> statement is none of those contexts. IX is an index data item
      *> of the CD record CR (§2002:A.1.1.2, §2002:13.16.58.3 GR9), so
      *> MOVE IX is refused. This refusal stands beside the accepted
      *> witness g2002-cd-index-data-item-contexts.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2UCD08.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  N PIC 9(4).
       COMMUNICATION SECTION.
       CD  CO FOR OUTPUT
           TEXT LENGTH IS CO-LEN
           STATUS KEY IS CO-STAT.
       01  CR.
           05  IX USAGE INDEX.
       PROCEDURE DIVISION.
           MOVE IX TO N.
           STOP RUN.
