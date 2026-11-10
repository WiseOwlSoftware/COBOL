      *>--- conformance-case
      *> id: pb5084-segment-limit-out-of-range
      *> rules: [FMT-12.3.6.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5084 - 1985 XVI.2.3.3 SR1: "Segment-number must be an
      *>   integer ranging in value from 1 through 49." Expected:
      *>   COBOL6819 at 85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5084NSL.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER. XYZ SEGMENT-LIMIT IS 50.
       PROCEDURE DIVISION.
       DECLARATIVES.
       ERR-SEC SECTION 49.
           USE AFTER STANDARD ERROR PROCEDURE ON INPUT.
       ERR-P.
           EXIT.
       END DECLARATIVES.
       MAIN-SECT SECTION 99.
       MAIN-PARA.
           DISPLAY "OK".
           STOP RUN.
