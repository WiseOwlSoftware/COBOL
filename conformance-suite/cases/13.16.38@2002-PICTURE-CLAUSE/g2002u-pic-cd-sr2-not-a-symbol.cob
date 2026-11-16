      *>--- conformance-case
      *> id: g2002u-pic-cd-sr2-not-a-symbol
      *> rules: [2002-SR-13.16.38.2-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.38.2 SR2 - character-string-1 "shall consist of an
      *> allowable combination of characters from the COBOL character
      *> repertoire used as symbols". 'K' is no PICTURE symbol (none
      *> of the symbols of §2002:13.16.38.3 GR13), and no CURRENCY SIGN
      *> clause makes it one, so "9K9" in a record under an OUTPUT CD
      *> must be refused
      *> at the PICTURE entry.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2PSR2A.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       01  CDREC.
           05 A PIC 9K9.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "COMPILED"
           STOP RUN.
