      *>--- conformance-case
      *> id: pb5179-bare-class-name-object-at-85
      *> rules: [SR-14.9.13.3-5]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5179 - the bare class-name spelling of a partial-expression selection object (§2023:14.9.13.3 SR5's
      *> "class condition without the identifier", which the grammar reads as identifier-2 and the binder resolves to
      *> the class). The COBOL-85 object list (§1985:VI.6.13.2) has no partial expression in any spelling (2002 Annex
      *> F.2 item 48 added it). Expected: COBOL0900 at 85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5179NBC.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           CLASS VOWEL IS "A" "E" "I" "O" "U".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W PIC X VALUE "E".
       PROCEDURE DIVISION.
       P1.
           EVALUATE W
               WHEN VOWEL DISPLAY "VOWEL"
               WHEN OTHER DISPLAY "OTHER"
           END-EVALUATE.
           STOP RUN.
