      *>--- conformance-case
      *> id: g2002-pic-cd-sr18-star-bwz
      *> rules: [2002-SR-13.16.38.2-18]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.38.2 SR18 - '*' with BLANK WHEN ZERO
      *> The entry sits in a record description entry written after an
      *> OUTPUT CD entry, an ordinary data description entry there
      *> (§2002:A.1.1.2), so the rule reaches it. The compiler must
      *> refuse the program; accepting it fails the case.
      *> **9 is numeric-edited, so 13.16.8.2 SR1 admits BLANK
      *> WHEN ZERO; only SR18 forbids the '*'.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2P318.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       01  CDREC.
           05 P1 PIC **9 BLANK WHEN ZERO.
       PROCEDURE DIVISION.
           DISPLAY "NOT REFUSED".
           STOP RUN.
