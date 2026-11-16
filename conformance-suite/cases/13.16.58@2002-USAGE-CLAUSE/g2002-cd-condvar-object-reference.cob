      *>--- conformance-case
      *> id: g2002-cd-condvar-object-reference
      *> rules: [2002-SR-13.16.58.2-10]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.58.2 SR10 - "An elementary data item of class index,
      *> object, or pointer shall not be a conditional variable."
      *> The record description entries after an output CD are ordinary
      *> data description entries that implicitly redefine its record
      *> area (§2002:A.1.2.2 SR9), so the USAGE syntax rules reach them.
      *> CJ, the only record of the CD, is an object reference (class
      *> object); its level-88 entry is refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2UR07.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CO FOR OUTPUT.
       01  CJ USAGE OBJECT REFERENCE.
           88 CJ-X VALUE 1.
       PROCEDURE DIVISION.
           STOP RUN.
