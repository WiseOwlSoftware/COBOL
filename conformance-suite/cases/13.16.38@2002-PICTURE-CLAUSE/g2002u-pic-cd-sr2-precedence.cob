      *>--- conformance-case
      *> id: g2002u-pic-cd-sr2-precedence
      *> rules: [2002-SR-13.16.38.2-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.38.2 SR2 - the allowable combinations are those of
      *> 13.16.38.5, Precedence rules: in Table 11 no 'Z' (leading zero
      *> suppression, left of the point) may follow a '9', so "9Z" in a
      *> record under an OUTPUT CD must be refused at the PICTURE entry.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2PSR2B.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       01  CDREC.
           05 A PIC 9Z.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "COMPILED"
           STOP RUN.
