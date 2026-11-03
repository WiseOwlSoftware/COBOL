      *>--- conformance-case
      *> id: pb4305_display_signed_numeric_literal_2002
      *> rules: [FMT-14.9.11.2]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4305 - The DISPLAY format names literal-1 with no
      *>   restriction on its sign (2002 Annex F.2 item 46: "If the
      *>   literal in a DISPLAY statement is numeric, it may be
      *>   signed"); 14.9.11.4 GR1 leaves the conversion to the device
      *>   to the implementor, which shows the literal as written.
      *>   Expected: "-5" and "+7". It fails if a signed literal is
      *>   refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4305DS.
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY -5
           DISPLAY +7
           STOP RUN.
