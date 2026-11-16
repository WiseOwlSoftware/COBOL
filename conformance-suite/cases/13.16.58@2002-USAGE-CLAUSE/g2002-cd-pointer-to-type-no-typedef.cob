      *>--- conformance-case
      *> id: g2002-cd-pointer-to-type-no-typedef
      *> rules: [2002-SR-13.16.58.2-17]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.58.2 SR17 - "If type-name-1 is specified, theTYPEDEF
      *> clause shall be specified for the subject of the entry."
      *> The record description entries after an output CD are ordinary
      *> data description entries that implicitly redefine its record
      *> area (§2002:A.1.2.2 SR9), so the USAGE syntax rules reach them.
      *> CT writes USAGE POINTER TO T without TYPEDEF: refused. (A
      *> TYPEDEF entry cannot stand under a CD at all, so no CD entry may
      *> carry type-name-1.)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2UR16.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  T TYPEDEF PIC X(4).
       COMMUNICATION SECTION.
       CD  CO FOR OUTPUT.
       01  CT USAGE POINTER TO T.
       PROCEDURE DIVISION.
           STOP RUN.
