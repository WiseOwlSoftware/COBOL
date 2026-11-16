      *>--- conformance-case
      *> id: pb5631-sd-then-fd
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5631 - ISO 8.4.2.1 (cite.py --check OK): "In order to use a
      *> resource, a statement shall contain a reference that uniquely identifies
      *> that resource". A file-name has no qualifier, so the description of FA
      *> written second (SD then FD) repeats the first, which stands.
      *> Expected: COBOL4250 at the LATER entry in source, whatever its kind.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5631-SDFD.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FA ASSIGN TO "dupa.txt".
       DATA DIVISION.
       FILE SECTION.
       SD  FA.
       01  RA PIC X(5).
       FD  FA.
       01  RB PIC X(7).
       PROCEDURE DIVISION.
       MAIN-PARA.
           STOP RUN.
