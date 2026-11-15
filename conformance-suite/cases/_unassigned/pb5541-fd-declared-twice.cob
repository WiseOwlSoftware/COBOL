      *>--- conformance-case
      *> id: pb5541-fd-declared-twice
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5541 - ISO 8.4.2.1 (cite.py --check OK): "In order to use a
      *> resource, a statement shall contain a reference that uniquely identifies
      *> that resource". A file-name has no qualifier, so a second FD of FA leaves
      *> every reference to FA without one description to identify. It compiled
      *> clean at every edition: the FD never went through the declaration funnel.
      *> Expected: COBOL4250 at the second FD FA.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5541-FD-TWICE.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FA ASSIGN TO "dupa.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  FA.
       01  RA PIC X(5).
       FD  FA.
       01  RB PIC X(7).
       PROCEDURE DIVISION.
       MAIN-PARA.
           STOP RUN.
