      *>--- conformance-case
      *> id: pb5074_display_decimal_literal_2002
      *> rules: [FMT-14.9.11.2]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5074 - 14.9.11.3 prints no rule on a numeric
      *>   literal-1, so a literal with a fractional part is a DISPLAY
      *>   operand; 14.9.11.4 GR1 leaves the conversion to the device
      *>   to the implementor, which shows the literal as written.
      *>   Expected: "1.5". It fails if the literal is refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5074DD.
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY 1.5
           STOP RUN.
