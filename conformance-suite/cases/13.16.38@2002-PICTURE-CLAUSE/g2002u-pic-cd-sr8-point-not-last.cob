      *>--- conformance-case
      *> id: g2002u-pic-cd-sr8-point-not-last
      *> rules: [2002-SR-13.16.38.2-8]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.38.2 SR8 - a string ending in '.' not last
      *> The entry sits in a record description entry written after an
      *> OUTPUT CD entry (§2002:A.1.2.2 SR9), so the rule reaches it:
      *> a string ending in '.' must be the last clause, followed
      *> at once by the separator period. The compiler must refuse
      *> the program; accepting it fails the case.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2PU08.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS CDDC
           TEXT LENGTH IS CDTL
           STATUS KEY IS CDSK
           ERROR KEY IS CDEK
           SYMBOLIC DESTINATION IS CDSD.
       01  CDREC.
           05 P1 PIC 999., USAGE DISPLAY.
       PROCEDURE DIVISION.
           DISPLAY "NOT REFUSED".
           STOP RUN.
