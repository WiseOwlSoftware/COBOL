      *>--- conformance-case
      *> id: g2002u-occ-key-cd-boolean
      *> rules: [2002-SR-13.16.36.2-8]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.36.2 SR8 - "The KEY phrase shall not be specified
      *> for a data item of class boolean, object, or pointer." K is
      *> PIC 1(4), an elementary boolean item (class boolean), named by
      *> the ASCENDING KEY phrase of a table in a record under an OUTPUT
      *> CD (§2002:A.1.2.2 SR9: such a record redefines the CD's record
      *> area). The compiler must refuse the entry at the KEY phrase;
      *> it fails if the CD record escapes the class check.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2OCKB.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       01  R1.
           05 E OCCURS 3 TIMES ASCENDING KEY IS K.
              10 K PIC 1(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "COMPILED"
           STOP RUN.
