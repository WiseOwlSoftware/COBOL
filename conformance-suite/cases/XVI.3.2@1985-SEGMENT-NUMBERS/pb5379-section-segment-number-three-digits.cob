      *>--- conformance-case
      *> id: pb5379-section-segment-number-three-digits
      *> rules: [1985-FMT-XVI.3.2.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5379 - §1985:III.2: "A segment-number may be
      *>   expressed either as a one or two digit number". SECTION 020
      *>   is three digits though its value is 20. Expected: COBOL6819
      *>   at 85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5379NS.
       PROCEDURE DIVISION.
       MAIN-SECT SECTION 020.
       MAIN-PARA.
           DISPLAY "OK".
           STOP RUN.
