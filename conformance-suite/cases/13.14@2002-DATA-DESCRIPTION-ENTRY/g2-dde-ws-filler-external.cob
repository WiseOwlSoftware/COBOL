      *>--- conformance-case
      *> id: g2-dde-ws-filler-external
      *> rules: [2002-SR-13.14.2-7]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.14.2 SR7 - "The data-name format of the entry-name
      *> clause shall be specified for any entry containing the GLOBAL
      *> or EXTERNAL clause, or for record descriptions associated with
      *> a file description entry that contains the EXTERNAL or GLOBAL
      *> clause."
      *> Leg: a working-storage level 1 entry with EXTERNAL
      *> written as FILLER.
      *> Accepting it fails the case.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2DDE06.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  FILLER PIC X IS EXTERNAL.
       PROCEDURE DIVISION.
           STOP RUN.
