      *>--- conformance-case
      *> id: g2002-cd-usage-national-inherited-pic-x
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
      *> CC inherits NATIONAL from group CG and has PICTURE X(2), an
      *> alphanumeric item: refused; C9 PIC 9(2) is the numeric control.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2UR12.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CO FOR OUTPUT.
       01  CR.
           05 CG USAGE NATIONAL.
              10 C9 PIC 9(2).
              10 CC PIC X(2).
       PROCEDURE DIVISION.
           STOP RUN.
