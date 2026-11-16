      *>--- conformance-case
      *> id: g2002u-pic-cd-sr23-currency-in-middle
      *> rules: [2002-SR-13.16.38.2-23]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.38.2 SR23 - '$' neither leftmost nor rightmost
      *> The entry sits in a record description entry written after an
      *> OUTPUT CD entry (§2002:A.1.2.2 SR9), so the rule reaches it:
      *> the currency symbol shall be leftmost (after an optional
      *> '+' or '-') or rightmost (before an optional '+', '-',
      *> 'CR' or 'DB'). The compiler must refuse the program;
      *> accepting it fails the case.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2PU23.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS CDDC
           TEXT LENGTH IS CDTL
           STATUS KEY IS CDSK
           ERROR KEY IS CDEK
           SYMBOLIC DESTINATION IS CDSD.
       01  CDREC.
           05 P1 PIC 9$9.
       PROCEDURE DIVISION.
           DISPLAY "NOT REFUSED".
           STOP RUN.
