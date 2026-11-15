      *>--- conformance-case
      *> id: pb5379-segment-limit-three-digits
      *> rules: [1985-FMT-XVI.2.3.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5379 - §1985:III.2: "A segment-number may be
      *>   expressed either as a one or two digit number". SEGMENT-LIMIT
      *>   IS 010 is three digits though its value is 10. Expected:
      *>   COBOL6819 at 85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5379NL.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER. XYZ SEGMENT-LIMIT IS 010.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "OK".
           STOP RUN.
