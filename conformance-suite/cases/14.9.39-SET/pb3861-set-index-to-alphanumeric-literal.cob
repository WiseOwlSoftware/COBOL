      *>--- conformance-case
      *> id: pb3861-set-index-to-alphanumeric-literal
      *> rules: [FMT-14.9.39.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3861 - ISO 14.9.39.2 Format 1 (index-assignment):
      *> SET index-name-1 TO { arithmetic-expression-1 | index-name-2 |
      *> identifier-2 }, and 8.8.1.1: "An arithmetic expression may be
      *> an identifier referencing a numeric data item, a numeric
      *> literal, the figurative constant ZERO (ZEROS, ZEROES)". An
      *> alphanumeric literal is none of the three sending operands
      *> (8.3.3.1: "Each literal possesses a class and category:
      *> alphanumeric, boolean, national, or numeric."), so SET IX TO "A"
      *> is in no format. It drew a bare COBOL0001 once the guess "a
      *> data-name is expected here, not a literal" was silenced as false
      *> (kb/Work PB906): a numeric literal IS expected there. COBOL5506
      *> now names both sides at every edition: the literal written is
      *> alphanumeric, and of the literals only a numeric literal or the
      *> figurative constant ZERO is expected: a sole SET IX TO ZERO
      *> parses, the ZERO read as the arithmetic expression's operand
      *> where the slot refuses the figurative (kb/Work PB4120), so the
      *> message names it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. N3861SI.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TB.
          05 TE PIC X OCCURS 4 INDEXED BY IX.
       PROCEDURE DIVISION.
       MAIN-PARA.
           SET IX TO "A".
           DISPLAY "N3861SI".
           STOP RUN.
