      *>--- conformance-case
      *> id: g2002-usage-national-pic-a
      *> rules: [2002-SR-13.16.58.2-11]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.58.2 SR11 - an item with USAGE NATIONAL, written or
      *> inherited from a group, "shall be declared with a picture
      *> character-string that describes a boolean, national,
      *> national-edited, numeric, or numeric-edited data item".
      *> WA writes USAGE NATIONAL with PICTURE A(3), an alphabetic item,
      *> which is none of the five: refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2UR09.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WA PIC A(3) USAGE NATIONAL.
       PROCEDURE DIVISION.
           STOP RUN.
