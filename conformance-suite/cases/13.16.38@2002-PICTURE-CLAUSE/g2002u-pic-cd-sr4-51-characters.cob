      *>--- conformance-case
      *> id: g2002u-pic-cd-sr4-51-characters
      *> rules: [2002-SR-13.16.38.2-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.38.2 SR4 - "The maximum number of characters allowed
      *> in character-string-1 is 50." The picture below is 51 'X', so
      *> the entry in a record under an OUTPUT CD must be refused at
      *> 2002 (the 2014 and 2023 limit is 63, so it is legal there).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2PSR4.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       01  CDREC.
           05 A
               PIC XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "COMPILED"
           STOP RUN.
