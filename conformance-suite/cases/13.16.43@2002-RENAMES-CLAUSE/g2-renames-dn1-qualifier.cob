      *>--- conformance-case
      *> id: g2-renames-dn1-qualifier
      *> rules: [2002-SR-13.16.43.2-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.43.2 SR3 - "Data-name-1 shall not be used as a
      *> qualifier." RX renames CA THRU CB of the CD record CREC; the
      *> reference CA OF RX uses data-name-1 as a qualifier of CA, an item
      *> inside its range, and is refused at that line (the DISPLAY). The
      *> accepted control is g2-renames-qualified-by-record (RX itself
      *> qualified by CREC and CDIN). Fails if CA OF RX is accepted.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2REN21.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CDIN FOR INPUT
           SYMBOLIC QUEUE IS SQ
           MESSAGE COUNT IS MC.
       01  CREC.
           05 CA PIC X(3).
           05 CB PIC X(3).
           05 CZ PIC X(81).
       66  RX RENAMES CA THRU CB.
       PROCEDURE DIVISION.
           MOVE "AAA" TO CA
           DISPLAY CA OF RX
           STOP RUN.
