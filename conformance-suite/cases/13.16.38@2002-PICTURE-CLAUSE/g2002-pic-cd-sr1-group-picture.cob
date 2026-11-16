      *>--- conformance-case
      *> id: g2002-pic-cd-sr1-group-picture
      *> rules: [2002-SR-13.16.38.2-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.38.2 SR1 - PICTURE on a group item
      *> G1 has two subordinate entries, so it is not elementary.
      *> The entry sits in a record description entry written after an
      *> OUTPUT CD entry, an ordinary data description entry there
      *> (§2002:A.1.1.2), so the rule reaches it. The compiler must
      *> refuse the program; accepting it fails the case.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2P201.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       01  CDREC.
           05 G1 PIC X(2).
              10 H1 PIC X.
              10 H2 PIC X.
       PROCEDURE DIVISION.
           DISPLAY "NOT REFUSED".
           STOP RUN.
