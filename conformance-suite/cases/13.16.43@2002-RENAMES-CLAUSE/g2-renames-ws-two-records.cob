      *>--- conformance-case
      *> id: g2-renames-ws-two-records
      *> rules: [2002-SR-13.16.43.2-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.43.2 SR4 - data-name-2 and data-name-3 "shall be names
      *> of elementary items or groups of elementary items in the same
      *> record". A1 (record A) THRU B1 (record B) in working storage is
      *> refused.
      *> cite.py --check of the quoted rule: OK. Refused at --std 2002.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2RENN5.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A.
           05 A1 PIC X(2).
           05 A2 PIC X(2).
       01  B.
           05 B1 PIC X(2).
       66  R RENAMES A1 THRU B1.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "SHOULD NOT COMPILE"
           STOP RUN.
