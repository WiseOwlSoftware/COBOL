      *>--- conformance-case
      *> id: g2rai04_raise_numeric
      *> rules: [2002-SR-14.8.28.2-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.28.2 SR2 - "Identifier-1 shall be an object
      *>   reference; the predefined object references NULL and SUPER
      *>   shall not be specified."
      *>   N4 is a numeric PIC 9(4) item, not an object reference:
      *>   RAISE N4 is refused at the RAISE line.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2RAI04.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N4 PIC 9(4) VALUE 1.
       PROCEDURE DIVISION.
       MAIN-P.
           RAISE N4
           DISPLAY "CONTINUED"
           STOP RUN.
