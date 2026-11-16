      *>--- conformance-case
      *> id: g2-renames-cd-occurs-operand
      *> rules: [2002-SR-13.16.43.2-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.43.2 SR3 - "Neither data-name-2 nor data-name-3 may
      *> have an OCCURS clause in its data description entry nor be
      *> subordinate to an item that has an OCCURS clause in its data
      *> description entry." In the record CREC under CD CDIN, data-name-3
      *> CT carries OCCURS 2, so the 66 entry is refused at its line. The
      *> range CA THRU CT is otherwise legal (fixed size, elementary
      *> alphanumeric items, CT ends after CA). The subordinate-to-OCCURS
      *> leg is g2-renames-occurs-ancestor; the accepted control is
      *> g2-renames-qualified-by-record. Fails if the entry is accepted.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2REN22.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CDIN FOR INPUT
           SYMBOLIC QUEUE IS SQ
           MESSAGE COUNT IS MC.
       01  CREC.
           05 CA PIC X(3).
           05 CT PIC X(2) OCCURS 2.
           05 CZ PIC X(80).
       66  RX RENAMES CA THRU CT.
       PROCEDURE DIVISION.
           DISPLAY CA
           STOP RUN.
