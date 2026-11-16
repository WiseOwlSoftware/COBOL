      *>--- conformance-case
      *> id: g2-dde-typedef-late
      *> rules: [2002-SR-13.14.2-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.14.2 SR4 - "If the TYPEDEF clause is specified, the
      *> data-name format of the entry-name clause shall also be
      *> specified and theTYPEDEF clause shall immediately follow the
      *> entry-name clause."
      *> Leg: a level 1 type declaration T written PIC X(4) IS TYPEDEF,
      *> the PICTURE clause between the entry-name and TYPEDEF.
      *> Accepting the entry fails the case.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2DDE17.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  T PIC X(4) IS TYPEDEF.
       01  V TYPE T.
       PROCEDURE DIVISION.
           DISPLAY "X"
           STOP RUN.
