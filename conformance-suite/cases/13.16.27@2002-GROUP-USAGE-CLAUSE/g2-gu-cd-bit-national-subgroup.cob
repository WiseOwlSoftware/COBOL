      *>--- conformance-case
      *> id: g2-gu-cd-bit-national-subgroup
      *> rules: [2002-SR-13.16.27.2-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.27.2 SR2 - "All subordinate group items shall be
      *> explicitly or implicitly described as GROUP-USAGE BIT": CS, a
      *> GROUP-USAGE NATIONAL group under CG in a CD record. Accepting it fails.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2GU07.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       01  CREC.
           05 CG GROUP-USAGE BIT.
              10 CS GROUP-USAGE NATIONAL.
                 15 CN PIC N(2).
       PROCEDURE DIVISION.
           DISPLAY "ACCEPTED".
           STOP RUN.
