      *>--- conformance-case
      *> id: pb4092-turn-size-per-name-85
      *> rules: [GR-7.3.25.4-6, GR-14.7.5-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> PB4092 and PB4093 - the edition boundary of
      *> pb4092_size_checking_is_per_name: COBOL-85 has no compiler
      *> directives and no exception checking, so the >>TURN directive
      *> that enables checking for ONE EC-SIZE name is refused there
      *> (ISO 7.3.25, the TURN directive, is new in COBOL 2002); the
      *> arithmetic itself is COBOL-85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4092T85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-A      PIC 999   VALUE 100.
       01  WS-C      PIC 999   VALUE 7.
       PROCEDURE DIVISION.
       M-1.
       >>TURN EC-SIZE-OVERFLOW CHECKING ON
           COMPUTE WS-C = WS-A * WS-A.
       >>TURN EC-SIZE-OVERFLOW CHECKING OFF
           DISPLAY WS-C.
           STOP RUN.
