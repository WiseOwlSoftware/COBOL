      *>--- conformance-case
      *> id: g2002-cd-usage-national-pic-x
      *> rules: [2002-SR-13.16.58.2-11]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.58.2 SR11 - an item with USAGE NATIONAL, written or
      *> inherited from a group, "shall be declared with a picture
      *> character-string that describes a boolean, national,
      *> national-edited, numeric, or numeric-edited data item".
      *> The record description entries after an output CD are ordinary
      *> data description entries that implicitly redefine its record
      *> area (§2002:A.1.2.2 SR9), so the USAGE syntax rules reach them.
      *> CX writes USAGE NATIONAL with PICTURE X(3), an alphanumeric
      *> item: refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2UR11.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CO FOR OUTPUT.
       01  CR.
           05 CX PIC X(3) USAGE NATIONAL.
       PROCEDURE DIVISION.
           STOP RUN.
