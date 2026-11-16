      *>--- conformance-case
      *> id: g2002u-pic-cd-sr21-plus-in-middle
      *> rules: [2002-SR-13.16.38.2-21]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.38.2 SR21 - '+' neither leftmost nor rightmost
      *> The entry sits in a record description entry written after an
      *> OUTPUT CD entry (§2002:A.1.2.2 SR9), so the rule reaches it:
      *> '+' shall be the leftmost or the rightmost symbol. The
      *> compiler must refuse the program; accepting it fails the
      *> case.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2PU21.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS CDDC
           TEXT LENGTH IS CDTL
           STATUS KEY IS CDSK
           ERROR KEY IS CDEK
           SYMBOLIC DESTINATION IS CDSD.
       01  CDREC.
           05 P1 PIC 9+9.
       PROCEDURE DIVISION.
           DISPLAY "NOT REFUSED".
           STOP RUN.
