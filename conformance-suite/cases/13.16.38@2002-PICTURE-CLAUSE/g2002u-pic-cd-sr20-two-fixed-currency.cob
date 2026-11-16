      *>--- conformance-case
      *> id: g2002u-pic-cd-sr20-two-fixed-currency
      *> rules: [2002-SR-13.16.38.2-20]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.38.2 SR20 - two fixed currency symbols
      *> The entry sits in a record description entry written after an
      *> OUTPUT CD entry (§2002:A.1.2.2 SR9), so the rule reaches it:
      *> only one currency symbol may be used for fixed insertion;
      *> each '$' alone is leftmost or rightmost, so SR23 is kept.
      *> The compiler must refuse the program; accepting it fails
      *> the case.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2PU20.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS CDDC
           TEXT LENGTH IS CDTL
           STATUS KEY IS CDSK
           ERROR KEY IS CDEK
           SYMBOLIC DESTINATION IS CDSD.
       01  CDREC.
           05 P1 PIC $99$.
       PROCEDURE DIVISION.
           DISPLAY "NOT REFUSED".
           STOP RUN.
