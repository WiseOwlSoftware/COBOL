      *>--- conformance-case
      *> id: g2002-usage-national-inherited-pic-xbx
      *> rules: [2002-SR-13.16.58.2-11]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.58.2 SR11 - an item with USAGE NATIONAL, written or
      *> inherited from a group, "shall be declared with a picture
      *> character-string that describes a boolean, national,
      *> national-edited, numeric, or numeric-edited data item".
      *> WD inherits NATIONAL from group WG; PICTURE XBX describes an
      *> alphanumeric-edited item, none of the five: refused. WN PIC N(2)
      *> is the national control and is not diagnosed.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2UR10.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WG USAGE NATIONAL.
           05 WN PIC N(2).
           05 WD PIC XBX.
       PROCEDURE DIVISION.
           STOP RUN.
