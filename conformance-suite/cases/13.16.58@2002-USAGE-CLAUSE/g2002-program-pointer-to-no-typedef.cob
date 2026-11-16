      *>--- conformance-case
      *> id: g2002-program-pointer-to-no-typedef
      *> rules: [2002-SR-13.16.58.2-18]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.58.2 SR18 - "If program-prototype-name-1 is specified,
      *> theTYPEDEF clause shall be specified for the subject of the entry."
      *> PP writes USAGE PROGRAM-POINTER TO G2URPT, a prototype the
      *> repository names, without TYPEDEF: refused (and nothing else is
      *> wrong with the program).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2URPT IS PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01  L-X PIC 9(4).
       PROCEDURE DIVISION USING L-X.
       END PROGRAM G2URPT.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2UR17.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM G2URPT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  PP USAGE PROGRAM-POINTER TO G2URPT.
       PROCEDURE DIVISION.
           STOP RUN.
       END PROGRAM G2UR17.
