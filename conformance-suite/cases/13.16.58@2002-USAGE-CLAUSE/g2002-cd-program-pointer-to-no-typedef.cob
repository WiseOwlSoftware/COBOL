      *>--- conformance-case
      *> id: g2002-cd-program-pointer-to-no-typedef
      *> rules: [2002-SR-13.16.58.2-18]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.58.2 SR18 - "If program-prototype-name-1 is specified,
      *> theTYPEDEF clause shall be specified for the subject of the entry."
      *> The record description entries after an output CD are ordinary
      *> data description entries that implicitly redefine its record
      *> area (§2002:A.1.2.2 SR9), so the USAGE syntax rules reach them.
      *> CQ writes USAGE PROGRAM-POINTER TO G2URPTC without TYPEDEF:
      *> refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2URPTC IS PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01  L-X PIC 9(4).
       PROCEDURE DIVISION USING L-X.
       END PROGRAM G2URPTC.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2UR18.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM G2URPTC.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CO FOR OUTPUT.
       01  CQ USAGE PROGRAM-POINTER TO G2URPTC.
       PROCEDURE DIVISION.
           STOP RUN.
       END PROGRAM G2UR18.
