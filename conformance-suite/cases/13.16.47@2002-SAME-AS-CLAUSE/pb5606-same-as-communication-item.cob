      *>--- conformance-case
      *> id: pb5606-same-as-communication-item
      *> rules: [2002-SR-13.16.47.2-7]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5606. §2002:13.16.47.2 SR7: "Data-name-1 shall reference an
      *> elementary item or a level 1 group item described in the file,
      *> working-storage, local-storage, or linkage section." C2 is described
      *> in a CD record, in the communication section, so C1 SAME AS C2 is
      *> refused (COBOL1556). Control: pb5606-same-as-section-list.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5606D.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       01  CDREC.
           05 C1 SAME AS C2.
           05 C2 PIC X(4).
       PROCEDURE DIVISION.
           MOVE "ABCD" TO C1
           STOP RUN.
