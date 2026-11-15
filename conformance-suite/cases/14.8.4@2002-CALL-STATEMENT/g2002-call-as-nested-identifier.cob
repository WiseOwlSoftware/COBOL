      *>--- conformance-case
      *> id: g2002-call-as-nested-identifier
      *> rules: [2002-SR-14.8.4.2-13]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.4.2 SR13 sentence 1 - "If the NESTED phrase is
      *> specified, literal-1 shall be specified." CALL identifier-1 AS
      *> NESTED violates it, though PN holds the name of a contained program.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CAL04.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 PN PIC X(8) VALUE "G2CAL04I".
       PROCEDURE DIVISION.
       MAIN.
           CALL PN AS NESTED
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CAL04I.
       PROCEDURE DIVISION.
       MAIN.
           GOBACK.
       END PROGRAM G2CAL04I.
       END PROGRAM G2CAL04.
