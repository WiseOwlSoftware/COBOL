      *>--- conformance-case
      *> id: g2-usage-cd-pic-n-display
      *> rules: [2002-SR-13.16.58.2-19]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.58.2 SR19 - "Only the NATIONAL phrase may be
      *> specified in a USAGE clause associated with an elementary data
      *> item whose explicit or implicit picture character-string
      *> contains the symbol 'N'." N1 PIC N(2) USAGE DISPLAY sits in a
      *> record description entry under an output CD (§2002:A.1.1.2), so
      *> the entry is refused (COBOL0881). Fails if the compiler accepts
      *> it in the communication section. The accepted form is
      *> g2-usage-pic-n-national.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2USG06.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           TEXT LENGTH IS TL
           STATUS KEY IS SK.
       01  CR.
           05 N1 PIC N(2) USAGE DISPLAY.
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY "NOT REFUSED".
           STOP RUN.
