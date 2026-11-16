      *>--- conformance-case
      *> id: g2-dde-fd-global-unnamed-record
      *> rules: [2002-SR-13.14.2-7]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.14.2 SR7 - "The data-name format of the entry-name
      *> clause shall be specified for any entry containing the GLOBAL
      *> or EXTERNAL clause, or for record descriptions associated with
      *> a file description entry that contains the EXTERNAL or GLOBAL
      *> clause."
      *> Leg: a record description of an FD GLOBAL
      *> written with no entry-name.
      *> Accepting it fails the case.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2DDE05.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "g2sr7.dat".
       DATA DIVISION.
       FILE SECTION.
       FD  F1 IS GLOBAL.
       01   PIC X(10).
       PROCEDURE DIVISION.
           STOP RUN.
