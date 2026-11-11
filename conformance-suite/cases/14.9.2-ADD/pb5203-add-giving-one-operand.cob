      *>--- conformance-case
      *> id: pb5203-add-giving-one-operand
      *> rules: [FMT-14.9.2.2]
      *> source-format: fixed
      *>---
      *> ISO 14.9.2.2 Format 2 (and 1985 VI.6.6.2 Format 2) prints a
      *> second addend, {identifier-2 | literal-2}, after the optional
      *> word TO and before GIVING; the braces make it required
      *> (5.2.6.3 "shall be explicitly specified"). ADD A GIVING D
      *> writes one addend and is no ADD format. It compiled at every
      *> edition and stored A in D (kb/Work PB5203); it is refused
      *> with COBOL1689 at every edition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5203N.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC 9(3) VALUE 5.
       01 D PIC 9(3) VALUE 0.
       PROCEDURE DIVISION.
       P1.
           ADD A GIVING D.
           DISPLAY D.
           STOP RUN.
