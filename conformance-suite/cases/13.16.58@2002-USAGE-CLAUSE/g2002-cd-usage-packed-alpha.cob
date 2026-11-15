      *>--- conformance-case
      *> id: g2002-cd-usage-packed-alpha
      *> rules: [2002-SR-13.16.58.2-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.58.2 SR3 - "An elementary data item whose
      *> declaration contains ... a USAGE clause specifying BINARY,
      *> COMPUTATIONAL, or PACKED-DECIMAL shall be specified only with a
      *> picture character-string that describes a numeric item." At
      *> 2002 the record description entries after a CD entry are
      *> ordinary data description entries (§2002:A.1.1.2): X in the CD
      *> record CR is PACKED-DECIMAL with PIC X(4) and is refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2UCD06.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CO FOR OUTPUT
           TEXT LENGTH IS CO-LEN
           STATUS KEY IS CO-STAT.
       01  CR.
           05  X PIC X(4) USAGE PACKED-DECIMAL.
       PROCEDURE DIVISION.
           MOVE "AB" TO X.
           STOP RUN.
