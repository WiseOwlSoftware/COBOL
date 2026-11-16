      *>--- conformance-case
      *> id: g2-dde-cd-binary-long-picture
      *> rules: [2002-SR-13.14.2-8]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.14.2 SR8 - "The PICTURE clause shall not be specified
      *> for ... an item whose usage is binary-char, binary-short,
      *> binary-long". M is BINARY-LONG with a PICTURE in a record
      *> after a CD. Accepting it fails the case.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2DDP03.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       01  CDREC.
           05 A PIC X(2).
           05 M PIC S9(9) BINARY-LONG.
       PROCEDURE DIVISION.
           STOP RUN.
