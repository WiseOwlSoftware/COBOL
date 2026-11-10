      *>--- conformance-case
      *> id: pb5084-declarative-segment-number-out-of-range
      *> rules: [FMT-14.2.1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5084 - 1985 XVI.3.2.3 SR3: "Sections in the
      *>   declaratives must contain segment-numbers less than 50."
      *>   Expected: COBOL6819 at 85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5084NSD.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER. XYZ SEGMENT-LIMIT IS 49.
       PROCEDURE DIVISION.
       DECLARATIVES.
       ERR-SEC SECTION 50.
           USE AFTER STANDARD ERROR PROCEDURE ON INPUT.
       ERR-P.
           EXIT.
       END DECLARATIVES.
       MAIN-SECT SECTION 99.
       MAIN-PARA.
           DISPLAY "OK".
           STOP RUN.
