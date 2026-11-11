      *>--- conformance-case
      *> id: pb5084-section-segment-number-out-of-range
      *> rules: [FMT-14.2.1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5084 - 1985 XVI.3.2.3 SR1: "The segment-number must be
      *>   an integer ranging in value from 0 through 99." Expected:
      *>   COBOL6819 at 85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5084NSS.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER. XYZ SEGMENT-LIMIT IS 49.
       PROCEDURE DIVISION.
       DECLARATIVES.
       ERR-SEC SECTION 49.
           USE AFTER STANDARD ERROR PROCEDURE ON INPUT.
       ERR-P.
           EXIT.
       END DECLARATIVES.
       MAIN-SECT SECTION 100.
       MAIN-PARA.
           DISPLAY "OK".
           STOP RUN.
