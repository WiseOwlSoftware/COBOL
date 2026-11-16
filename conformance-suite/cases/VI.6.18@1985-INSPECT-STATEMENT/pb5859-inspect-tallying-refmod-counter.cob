      *>--- conformance-case
      *> id: pb5859-inspect-tallying-refmod-counter
      *> rules: [1985-SR-VI.6.18.3-6]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.18.3 SR6 (cite.py --check: OK): "Identifier-2 must
      *> reference an elementary numeric data item". A reference
      *> modification of a numeric item creates a data item of class
      *> and category alphanumeric (§1985:IV.4.3.8.4.4 GR5, cite.py
      *> --check: OK), so N1 (1:2) is no elementary numeric data item
      *> and the counter is refused with COBOL0847 (kb/Work PB5859); it
      *> passed the binder and failed in the generated C# (CS1503). The
      *> control names N1 whole: the counter starts at 1234 and ALL "A"
      *> matches 5 times in AAXAAYA (positions 1, 2, 4, 5 and 7), so
      *> DISPLAY N1 prints 1239.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5859A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC X(7) VALUE "AAXAAYA".
       01 N1 PIC 9(4) VALUE 1234.
       PROCEDURE DIVISION.
       P1.
           INSPECT X TALLYING N1 (1:2) FOR ALL "A".
           DISPLAY N1.
           STOP RUN.
