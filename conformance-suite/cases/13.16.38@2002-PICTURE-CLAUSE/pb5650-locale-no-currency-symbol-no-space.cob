      *>--- conformance-case
      *> id: pb5650-locale-no-currency-symbol-no-space
      *> rules: [2002-GR-13.16.38.4-13, 2002-GR-13.16.38.4-14]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.38.3 GR17 - a currency string is included only when
      *>   the picture carries the currency symbol; a locale-edited
      *>   picture with none has no currency-to-value separation, so
      *>   the locale's sep_by_space adds no space (de-DE: none after
      *>   the value; pt-BR, nl-NL: none after the sign).
      *> §2002:13.16.38.4 GR14 - the data is right justified, with space
      *>   fill for the leftmost positions.
      *> §2002:13.16.38.4 GR13 - the '+' sign is placed as the locale
      *>   specifies (nl-NL positive '+', pt-BR negative '-').
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5650A.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           LOCALE GER IS "de-DE"
           LOCALE BRA IS "pt-BR"
           LOCALE NED IS "nl-NL".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 L5 PIC ZZZZZZ9.99 LOCALE IS GER SIZE IS 14.
       01 L7 PIC +ZZZ9.99 LOCALE IS BRA SIZE IS 14.
       01 L8 PIC +ZZZ9.99 LOCALE IS NED SIZE IS 14.
       PROCEDURE DIVISION.
           MOVE 1234567.89 TO L5
           MOVE -1234.5 TO L7
           MOVE 1234.5 TO L8
           DISPLAY "L5=[" L5 "] L7=[" L7 "] L8=[" L8 "]"
           STOP RUN.
