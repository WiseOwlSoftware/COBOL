      *>--- conformance-case
      *> id: g2-renames-cd-after-other-record
      *> rules: [2002-SR-13.16.43.2-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.43.2 SR2 - "All RENAMES entries referring to data items
      *> within a given record shall immediately follow the last data
      *> description entry of the associated record description entry."
      *> R renames A1 of record A but is written after record B, so it does
      *> not immediately follow A's last entry: the program is refused.
      *> Fails if the compiler accepts a 66 entry separated from its record
      *> by another record under a CD (§2002:A.1.1 records).
      *> cite.py --check of the quoted rule: OK. Refused at --std 2002.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2RENN1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W PIC X.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       01  A.
           05 A1 PIC X(2).
           05 A2 PIC X(2).
       01  B PIC X(2).
       66  R RENAMES A1.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "SHOULD NOT COMPILE"
           STOP RUN.
