      *>--- conformance-case
      *> id: g2-dde-typedef-unnamed
      *> rules: [2002-SR-13.14.2-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.14.2 SR4 - "If the TYPEDEF clause is specified, the
      *> data-name format of the entry-name clause shall also be
      *> specified".
      *> Leg: a level 1 entry written IS TYPEDEF with no entry-name
      *> clause (so, by the same rule, as though FILLER were written).
      *> Accepting the entry fails the case.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2DDE18.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  IS TYPEDEF PIC X(4).
       PROCEDURE DIVISION.
           DISPLAY "X"
           STOP RUN.
