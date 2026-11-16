      *>--- conformance-case
      *> id: g2-dde-cd-88-pointer
      *> rules: [2002-SR-13.14.2-22]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.14.2 SR22 - a condition-name shall not be associated
      *> with e) "A data item of the class index, object, or pointer":
      *> PX is USAGE POINTER, the first record after an output CD
      *> (level 1, as §2002:13.16.58.2 SR13 asks of a pointer; the
      *> first record, where §2002:A.1.2.2 SR9 admits VALUE). Accepting
      *> it fails the case.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2DDP21.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       01  PX USAGE POINTER.
           88 PX-N VALUE "A".
       01  CDREC PIC X(4).
       PROCEDURE DIVISION.
           STOP RUN.
