      *>--- conformance-case
      *> id: g2002u-pic-cd-sr9-no-required-symbol
      *> rules: [2002-SR-13.16.38.2-9]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.38.2 SR9 - none of A N X Z 1 9 *, no pair
      *> The entry sits in a record description entry written after an
      *> OUTPUT CD entry (§2002:A.1.2.2 SR9), so the rule reaches it:
      *> B/0 holds none of A N X Z 1 9 * and no two of '+', '-' or
      *> the currency symbol. The compiler must refuse the program;
      *> accepting it fails the case.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2PU09.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS CDDC
           TEXT LENGTH IS CDTL
           STATUS KEY IS CDSK
           ERROR KEY IS CDEK
           SYMBOLIC DESTINATION IS CDSD.
       01  CDREC.
           05 P1 PIC B/0.
       PROCEDURE DIVISION.
           DISPLAY "NOT REFUSED".
           STOP RUN.
