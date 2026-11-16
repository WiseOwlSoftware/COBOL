      *>--- conformance-case
      *> id: g2-dde-cd-locale-sign
      *> rules: [2002-SR-13.14.2-17]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.14.2 SR17 - "If the LOCALE phrase of the PICTURE
      *> clause is specified, the SIGN clause shall not be specified."
      *> The item is in a record description entry after a CD, which
      *> format 1 governs (§2002:A.1.1). Accepting it fails the case.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2DDE03.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       01  CDREC.
           05 L1 PIC +ZZZ9.99 LOCALE SIZE IS 14 SIGN IS LEADING.
       PROCEDURE DIVISION.
           STOP RUN.
