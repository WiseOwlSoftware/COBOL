      *>--- conformance-case
      *> id: pb5379-segment-number-two-digit-form
      *> rules: [1985-FMT-XVI.3.2.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:III.2: "A segment-number may be expressed either as a one
      *> or two digit number". SEGMENT-LIMIT IS 09 and SECTION 07 are
      *> two-digit spellings and compile; the program prints its output.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5379OK.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER. XYZ SEGMENT-LIMIT IS 09.
       PROCEDURE DIVISION.
       MAIN-SECT SECTION 07.
       MAIN-PARA.
           DISPLAY "TWO DIGITS".
           STOP RUN.
