      *>--- conformance-case
      *> id: g2-gu-ws-bit-nonboolean-sub
      *> rules: [2002-SR-13.16.27.2-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.27.2 SR2 - every elementary item subordinate to a
      *> GROUP-USAGE BIT subject shall be usage bit, class and category
      *> boolean: WA PIC X(4) under WG in working-storage. Accepting it fails.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2GU06.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WREC.
           05 WG GROUP-USAGE BIT.
              10 WA PIC X(4).
       PROCEDURE DIVISION.
           DISPLAY "ACCEPTED".
           STOP RUN.
