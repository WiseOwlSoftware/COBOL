      *>--- conformance-case
      *> id: g2-dde-cd-redefines-late
      *> rules: [2002-SR-13.14.2-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.14.2 SR4 - "The REDEFINES clause, if specified, shall
      *> immediately follow the entry-name clause, if specified".
      *> Leg: in a record after a CD, B is written PIC X(2) REDEFINES A,
      *> the PICTURE clause between the entry-name and REDEFINES.
      *> The accepted orders are g2-dde-cd-entry-order.
      *> Accepting the entry fails the case.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2DDE16.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       01  CDREC.
           05 A PIC X(2) VALUE "AB".
           05 B PIC X(2) REDEFINES A.
           05 C PIC X(2) VALUE "CD".
       PROCEDURE DIVISION.
           DISPLAY CDREC
           STOP RUN.
