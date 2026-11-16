      *>--- conformance-case
      *> id: g2-renames-ws-same-name
      *> rules: [2002-SR-13.16.43.2-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.43.2 SR4 - data-name-2 and data-name-3 "shall not be
      *> the same data-name". A1 THRU A1 in a working-storage record is
      *> refused.
      *> cite.py --check of the quoted rule: OK. Refused at --std 2002.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2RENN4.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A.
           05 A1 PIC X(2).
           05 A2 PIC X(2).
       66  R RENAMES A1 THRU A1.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "SHOULD NOT COMPILE"
           STOP RUN.
