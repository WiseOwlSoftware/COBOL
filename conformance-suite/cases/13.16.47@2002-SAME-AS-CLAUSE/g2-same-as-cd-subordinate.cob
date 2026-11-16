      *>--- conformance-case
      *> id: g2-same-as-cd-subordinate
      *> rules: [2002-SR-13.16.47.2-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.47.2 SR2 - "A data description entry that specifies
      *> the SAME AS clause shall not be immediately followed by a
      *> subordinate data description entry or level 88 entry." X SAME
      *> AS W sits in a record description entry under an output CD
      *> (§2002:A.1.1.2: "A record description consists of a set of data
      *> description entries") and is immediately followed by the
      *> subordinate entry 10 Y, so the entry is refused (COBOL1555).
      *> Fails if the compiler accepts it in the communication section.
      *> The level 88 leg is g2-same-as-cd-level-88.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2SAC03.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W PIC X(2).
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           TEXT LENGTH IS TL
           STATUS KEY IS SK.
       01  CR.
           05 X SAME AS W.
              10 Y PIC X.
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY "NOT REFUSED".
           STOP RUN.
