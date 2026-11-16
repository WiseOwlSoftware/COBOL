      *>--- conformance-case
      *> id: pb5606-address-of-communication-item
      *> rules: [2002-SR-8.4.2.11.2-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5606. §2002:8.4.2.11.2 SR1: "Identifier-1 shall reference a
      *> data item defined in the file section, working-storage section,
      *> local-storage section, or linkage section." A1 is described in the
      *> record of a CD, in the communication section, so ADDRESS OF A1 is
      *> refused (COBOL2785). Control: pb5606-address-of-section-list.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5606B.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  P   USAGE POINTER.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       01  CDREC.
           05 A1 PIC X(3).
       PROCEDURE DIVISION.
           SET P TO ADDRESS OF A1.
           STOP RUN.
