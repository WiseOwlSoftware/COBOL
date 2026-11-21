      *>--- conformance-case
      *> id: pb5555-declarative-segment-number-three-digits
      *> rules: [1985-FMT-XVI.3.2.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5555 - §1985:III.2: "A segment-number may be
      *>   expressed either as a one or two digit number". The header
      *>   of the declarative section ERR-SEC is SECTION 020, which is
      *>   three digits though its value is 20, below the limit of 50
      *>   that §1985:XVI.3.2.3 SR3 sets for a declarative section, so
      *>   the form rule is the only one the header breaks. Pins the
      *>   declarative-section caller of the form check, beside
      *>   pb5379-section-segment-number-three-digits (an ordinary
      *>   section) and pb5379-segment-limit-three-digits (the
      *>   SEGMENT-LIMIT clause). Expected: COBOL6819 at 85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5555DS.
       DATA DIVISION.
       PROCEDURE DIVISION.
       DECLARATIVES.
       ERR-SEC SECTION 020.
           USE AFTER STANDARD EXCEPTION PROCEDURE ON INPUT.
       ERR-PARA.
           EXIT.
       END DECLARATIVES.
       MAIN-SEC SECTION.
       MAIN-PARA.
           DISPLAY "OK".
           STOP RUN.
