      *>--- conformance-case
      *> id: g2-gu-ws-bit-subject-usage
      *> rules: [2002-SR-13.16.27.2-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.27.2 SR2 - "A USAGE clause shall not be specified for
      *> the subject of the entry": WG in working-storage writes
      *> GROUP-USAGE BIT USAGE BIT. Accepting it fails the case.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2GU04.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WREC.
           05 WG GROUP-USAGE BIT USAGE BIT.
              10 WA PIC 1(4).
       PROCEDURE DIVISION.
           DISPLAY "ACCEPTED".
           STOP RUN.
