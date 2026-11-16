      *>--- conformance-case
      *> id: g2-same-as-cd-occurs-target
      *> rules: [2002-SR-13.16.47.2-5]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.47.2 SR5 - "The description of data-name-1 shall
      *> not contain an OCCURS clause." The subject X sits in a record
      *> description entry under an output CD (§2002:A.1.1.2); its
      *> data-name-1, WE, is a working-storage item whose own
      *> description contains OCCURS 2, so the entry is refused
      *> (COBOL1556). Fails if the compiler accepts it in the
      *> communication section. The accepted form (OCCURS only below
      *> data-name-1) is g2-same-as-cd-occurs-subordinate.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2SAC02.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WT.
           05 WE PIC X OCCURS 2.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           TEXT LENGTH IS TL
           STATUS KEY IS SK.
       01  CR.
           05 X SAME AS WE.
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY "NOT REFUSED".
           STOP RUN.
