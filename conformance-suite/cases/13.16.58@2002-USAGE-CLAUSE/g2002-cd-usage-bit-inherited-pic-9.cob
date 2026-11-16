      *>--- conformance-case
      *> id: g2002-cd-usage-bit-inherited-pic-9
      *> rules: [2002-SR-13.16.58.2-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.58.2 SR4 - "a USAGE clause specifying BIT shall be
      *> specified only with a picture character-string that describes a
      *> boolean data item", written on the item or on a group above it.
      *> The record description entries after an output CD are ordinary
      *> data description entries that implicitly redefine its record
      *> area (§2002:A.1.2.2 SR9), so the USAGE syntax rules reach them.
      *> CB inherits BIT from group CG and has PICTURE 9: refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2UR03.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CO FOR OUTPUT.
       01  CR.
           05 CG USAGE BIT.
              10 CA PIC 1.
              10 CB PIC 9.
       PROCEDURE DIVISION.
           STOP RUN.
