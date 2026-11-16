      *>--- conformance-case
      *> id: g2002-cd-object-reference-in-group
      *> rules: [2002-SR-13.16.58.2-13]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.58.2 SR13 - the OBJECT REFERENCE, POINTER or
      *> PROGRAM-POINTER phrase "may be specified only for an elementary
      *> data item with level 1 or subordinate to a type declaration that
      *> includes the STRONG phrase".
      *> The record description entries after an output CD are ordinary
      *> data description entries that implicitly redefine its record
      *> area (§2002:A.1.2.2 SR9), so the USAGE syntax rules reach them.
      *> CJ is at level 05 under the ordinary group CR: refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2UR15.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CO FOR OUTPUT.
       01  CR.
           05 CN PIC X(4).
           05 CJ USAGE OBJECT REFERENCE.
       PROCEDURE DIVISION.
           STOP RUN.
