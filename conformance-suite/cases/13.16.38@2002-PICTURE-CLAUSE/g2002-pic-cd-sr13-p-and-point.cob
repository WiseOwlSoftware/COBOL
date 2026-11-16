      *>--- conformance-case
      *> id: g2002-pic-cd-sr13-p-and-point
      *> rules: [2002-SR-13.16.38.2-13]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.38.2 SR13 - 'P' and '.' in one string
      *> PP9.9 writes both symbols the rule makes mutually exclusive.
      *> The entry sits in a record description entry written after an
      *> OUTPUT CD entry, an ordinary data description entry there
      *> (§2002:A.1.1.2), so the rule reaches it. The compiler must
      *> refuse the program; accepting it fails the case.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2P213.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       01  CDREC.
           05 P1 PIC PP9.9.
       PROCEDURE DIVISION.
           DISPLAY "NOT REFUSED".
           STOP RUN.
