      *>--- conformance-case
      *> id: pb3566_numeric_edited_value_zero_figurative
      *> rules: [SR-13.18.63.3-6]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3566 - the complement of
      *> pb3566-numeric-edited-value-zero-below-2023: the FIGURATIVE
      *> constant ZERO is not a numeric literal, so the COBOL-2023
      *> permission of 13.18.63.3 SR6 (Annex E.3.3 item 43) does not
      *> gate it, and it seeds a numeric-edited item at EVERY edition.
      *> Annex E.2 item 28 records only that its stored image changed
      *> at 2023 ("The figurative constant ZERO or ZEROES with or
      *> without the ALL phrase, when used as the value for a
      *> numeric-edited data item, is now treated as the numeric
      *> literal zero, such that the result is no longer left
      *> justified or potentially a simple string of zeroes"), so this
      *> program tests ACCEPTANCE only: the images differ by edition
      *> and are pinned where they differ (value_numeric_edited); the
      *> constant line below is the one expected output.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3566NEZF.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 F0 PIC ZZ9.99 VALUE ZERO.
       01 F1 PIC ZZ9.99 VALUE ZEROES.
       01 F2 PIC ZZ9.99 VALUE ALL ZEROS.
       01 F3 PIC ZZ9.99.
          88 F3-ZERO VALUE ZERO.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "ZERO FIGURATIVES ACCEPTED"
           STOP RUN.
