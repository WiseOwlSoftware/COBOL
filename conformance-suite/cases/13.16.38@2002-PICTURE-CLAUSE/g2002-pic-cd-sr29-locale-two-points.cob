      *>--- conformance-case
      *> id: g2002-pic-cd-sr29-locale-two-points
      *> rules: [2002-SR-13.16.38.2-29]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.38.2 SR29 - format 2 string with '.' twice
      *> The entry sits in a record description entry written after an
      *> OUTPUT CD entry, an ordinary data description entry there
      *> (§2002:A.1.1.2), so the rule reaches it. The compiler must
      *> refuse the program; accepting it fails the case.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2P329.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           LOCALE FR IS "fr-FR".
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       01  CDREC.
           05 P1 PIC 9.9.9 LOCALE IS FR SIZE IS 8.
       PROCEDURE DIVISION.
           DISPLAY "NOT REFUSED".
           STOP RUN.
