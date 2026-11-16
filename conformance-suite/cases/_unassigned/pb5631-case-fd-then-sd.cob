      *>--- conformance-case
      *> id: pb5631-case-fd-then-sd
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5631, the file-name's case: ISO 8.1.3.2 GR3 (cite.py
      *> --check OK): "Equivalence of uppercase and lowercase basic letters
      *> is achieved by folding", so "fa" and "FA" are one file-name, and
      *> ISO 8.4.2.1 (cite.py --check OK): "a statement shall contain a
      *> reference that uniquely identifies that resource". The second
      *> description of the file (SD after FD, spelled in another case)
      *> repeats the first, which stands.
      *> Expected: COBOL4250 at the LATER entry in source.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5631C-FDSD.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FA ASSIGN TO "dupa.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  fa.
       01  RA PIC X(5).
       SD  FA.
       01  RB PIC X(7).
       PROCEDURE DIVISION.
       MAIN-PARA.
           STOP RUN.
