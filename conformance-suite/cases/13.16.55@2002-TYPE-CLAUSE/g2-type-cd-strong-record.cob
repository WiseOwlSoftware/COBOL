      *>--- conformance-case
      *> id: g2-type-cd-strong-record
      *> rules: [2002-SR-13.16.55.2-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.55.2 SR4 - "If type-name-1 is described with the
      *> STRONG phrase, the subject of the entry shall not be implicitly
      *> or explicitly redefined in whole or in part." §2002:A.1.2.2 SR9:
      *> "Record description entries subordinate to an output CD entry
      *> implicitly redefine the record area established by the output
      *> CD entry." So CD-OREC, the ONLY record of the output CD and of
      *> STRONG type T, is implicitly redefined: a 2002-only reach (2023
      *> has no CD). Expected: COBOL5991 at line 27. Fails if a single
      *> record under a CD is not treated as an implicit redefinition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2TYP04.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  T TYPEDEF STRONG.
           05  T-A PIC X(4).
           05  T-B PIC X(3).
       COMMUNICATION SECTION.
       CD  CD-OUT FOR OUTPUT
           TEXT LENGTH IS CO-LEN
           STATUS KEY IS CO-STAT.
       01  CD-OREC TYPE T.
       PROCEDURE DIVISION.
           DISPLAY "BAD".
           STOP RUN.
