      *>--- conformance-case
      *> id: g2-dde-ws-global-77
      *> rules: [2002-SR-13.14.2-6]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.14.2 SR6 - "The GLOBAL clause may be specified only in
      *> data description entries whose level-number is 1."
      *> Leg: a working-storage level 77 entry written IS GLOBAL.
      *> Accepting the entry fails the case.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2DDE20.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       77  G PIC X(2) IS GLOBAL.
       PROCEDURE DIVISION.
           DISPLAY "X"
           STOP RUN.
