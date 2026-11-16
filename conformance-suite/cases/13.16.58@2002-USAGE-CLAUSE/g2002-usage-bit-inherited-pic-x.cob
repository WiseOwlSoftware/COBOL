      *>--- conformance-case
      *> id: g2002-usage-bit-inherited-pic-x
      *> rules: [2002-SR-13.16.58.2-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.58.2 SR4 - "a USAGE clause specifying BIT shall be
      *> specified only with a picture character-string that describes a
      *> boolean data item", written on the item or on a group above it.
      *> BA below writes no USAGE; it inherits BIT from group G, and its
      *> PICTURE X describes an alphanumeric item, so the entry is refused.
      *> The written form is refused by cases/13.18.60-USAGE/bit-usage-
      *> numeric-pic; BB PIC 1 is the boolean control and is not diagnosed.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2UR01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  G USAGE BIT.
           05 BB PIC 1.
           05 BA PIC X.
       PROCEDURE DIVISION.
           STOP RUN.
