      *>--- conformance-case
      *> id: g2-renames-cd-level-01
      *> rules: [2002-SR-13.16.43.2-5]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.43.2 SR5 - "Neither data-name-2 nor data-name-3 shall
      *> refer to an entry that is described with level-number 1, 66, 77, or
      *> 88." R renames the level-01 record A under a CD: refused.
      *> cite.py --check of the quoted rule: OK. Refused at --std 2002.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2RENN6.
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
       66  R RENAMES A.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "SHOULD NOT COMPILE"
           STOP RUN.
