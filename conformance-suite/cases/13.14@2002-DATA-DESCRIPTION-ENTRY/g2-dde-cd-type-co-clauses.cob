      *>--- conformance-case
      *> id: g2-dde-cd-type-co-clauses
      *> rules: [2002-SR-13.14.2-13]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.14.2 SR13 - "The TYPE clause shall not be specified
      *> in the same data description entry with any clauses except
      *> BASED, CLASS, DEFAULT, DESTINATION, entry-name, ..., OCCURS,
      *> ..., VALUE, and VARYING." In a record after a CD, B1 writes
      *> TYPE alone, B2 TYPE with OCCURS and B3 TYPE with VALUE.
      *> Refusing any fails the case. Each is PIC X(4)
      *> (§2002:13.16.55.3 GR1); B3's VALUE applies (GR3) and takes
      *> effect in the communication section (§2002:13.16.61.3 GR4).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2DDP15.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  TD IS TYPEDEF PIC X(4).
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       01  CDREC.
           05 B1 TYPE TD.
           05 B2 TYPE TD OCCURS 2.
           05 B3 TYPE TD VALUE "WXYZ".
       PROCEDURE DIVISION.
           DISPLAY "B3=[" B3 "]"
           MOVE "ABCD" TO B1
           MOVE "EF" TO B2(1)
           MOVE "GHIJ" TO B2(2)
           DISPLAY "CDREC=[" CDREC "]"
           STOP RUN.
