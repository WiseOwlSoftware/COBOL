      *>--- conformance-case
      *> id: g2002-cd-usage-comp-group-alpha
      *> rules: [2002-SR-13.16.58.2-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.58.2 SR3 - "... or an elementary data item
      *> subordinate to a group item whose declaration contains, a USAGE
      *> clause specifying BINARY, COMPUTATIONAL, or PACKED-DECIMAL shall
      *> be specified only with a picture character-string that
      *> describes a numeric item." At 2002 the record description
      *> entries after a CD entry are ordinary data description entries
      *> (§2002:A.1.1.2): X is subordinate to the USAGE COMP group CR of
      *> the CD and has PIC X(4), so it is refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2UCD07.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CO FOR OUTPUT
           TEXT LENGTH IS CO-LEN
           STATUS KEY IS CO-STAT.
       01  CR USAGE COMP.
           05  X PIC X(4).
       PROCEDURE DIVISION.
           MOVE "AB" TO X.
           STOP RUN.
