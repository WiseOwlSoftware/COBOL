      *>--- conformance-case
      *> id: pb5603-send-with-constant-name
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5603 - ISO/IEC 1989:2002 A.2.6.1 Format 2 writes WITH
      *> identifier-2 and no literal, and A.2.6.2 SR3 asks identifier-2
      *> to "reference a one-character integer without an operational
      *> sign". A constant-name stands where "a format specifies a
      *> literal" (13.10.3 SR2 of 2023; the 2002 constant entry says the
      *> same), so it stands beside ADVANCING's integer-1 and never for
      *> identifier-2. It was accepted here as though it were integer-1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5603K.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 MSG PIC X(10) VALUE "HELLO".
       01 K-EMI CONSTANT AS 2.
       COMMUNICATION SECTION.
       CD OUTCD FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           DESTINATION TABLE OCCURS 1 TIMES
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SYMD.
       PROCEDURE DIVISION.
       MAIN-PARA.
           SEND OUTCD FROM MSG WITH K-EMI.
           STOP RUN.
