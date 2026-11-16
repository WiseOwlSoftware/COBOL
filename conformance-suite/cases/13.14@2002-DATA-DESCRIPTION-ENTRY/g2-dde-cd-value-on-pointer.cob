      *>--- conformance-case
      *> id: g2-dde-cd-value-on-pointer
      *> rules: [2002-SR-13.14.2-10]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.14.2 SR10 - "The VALUE clause shall not be specified
      *> for data items of class index, object, or pointer." PX is
      *> USAGE POINTER with VALUE 0, the first record after an output
      *> CD (level 1, as §2002:13.16.58.2 SR13 asks of a pointer; the
      *> first record, where §2002:A.1.2.2 SR9 admits VALUE). The
      *> literal 0 keeps the entry inside Format 1 of §2002:13.16.61.1
      *> (NULL is a predefined address, not a literal). Accepting it
      *> fails the case.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2DDP06.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       01  PX USAGE POINTER VALUE 0.
       01  CDREC PIC X(4).
       PROCEDURE DIVISION.
           STOP RUN.
