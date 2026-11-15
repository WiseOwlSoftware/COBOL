      *>--- conformance-case
      *> id: g2002-pic-cd-sr31-locale-undeclared
      *> rules: [2002-SR-13.16.38.2-31]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.38.2 SR31 - locale-name not in SPECIAL-NAMES LOCALE
      *> The entry sits in a record description entry written after an
      *> OUTPUT CD entry, an ordinary data description entry there
      *> (§2002:A.1.1.2), so the rule reaches it. The compiler must
      *> refuse the program; accepting it fails the case.
      *> FR is declared; NOLOC is not.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2P331.
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
           05 P1 PIC ZZ9 LOCALE IS NOLOC SIZE IS 6.
       PROCEDURE DIVISION.
           DISPLAY "NOT REFUSED".
           STOP RUN.
