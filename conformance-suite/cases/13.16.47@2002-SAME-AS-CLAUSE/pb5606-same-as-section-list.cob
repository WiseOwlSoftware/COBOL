      *>--- conformance-case
      *> id: pb5606-same-as-section-list
      *> rules: [2002-SR-13.16.47.2-7]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5606. §2002:13.16.47.2 SR7: "Data-name-1 shall reference an
      *> elementary item or a level 1 group item described in the file,
      *> working-storage, local-storage, or linkage section." A CD record
      *> entry may copy a working-storage description: C1 SAME AS W1B takes
      *> PIC X(4) from W1B, so MOVE "ABCD" TO C1 stores all four characters.
      *> The communication-section target is refused
      *> (pb5606-same-as-communication-item). Why it can fail: the legal
      *> target is refused for the CD it sits beside, or the copy loses its
      *> description (C1 shows fewer than four characters).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5606C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W1B PIC X(4).
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       01  CDREC.
           05 C1 SAME AS W1B.
       PROCEDURE DIVISION.
           MOVE "ABCD" TO C1
           DISPLAY C1.
           STOP RUN.
