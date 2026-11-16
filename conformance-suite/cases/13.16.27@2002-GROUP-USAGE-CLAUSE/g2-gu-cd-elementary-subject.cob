      *>--- conformance-case
      *> id: g2-gu-cd-elementary-subject
      *> rules: [2002-SR-13.16.27.2-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.27.2 SR1 - "may be specified only if the subject of
      *> the entry is a group item". CE, an elementary PIC N(4) entry of
      *> a record after a CD (§2002:A.1.1.2), writes GROUP-USAGE NATIONAL.
      *> Accepting it fails the case.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2GU02.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       01  CREC.
           05 CQ PIC X(12).
           05 CE PIC N(4) GROUP-USAGE NATIONAL.
       PROCEDURE DIVISION.
           DISPLAY "ACCEPTED".
           STOP RUN.
