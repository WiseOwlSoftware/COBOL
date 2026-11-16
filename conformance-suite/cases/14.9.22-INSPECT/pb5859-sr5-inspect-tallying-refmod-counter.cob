      *>--- conformance-case
      *> id: pb5859-sr5-inspect-tallying-refmod-counter
      *> rules: [SR-14.9.22.3-5]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2023:14.9.22.3 SR5 (cite.py --check: OK): "Identifier-2 shall
      *> reference an elementary numeric data item". A reference
      *> modification of a numeric item creates a data item of class
      *> and category alphanumeric (§2023:8.4.3.3.4 GR6), so N1 (1:2)
      *> is refused with COBOL0847 (kb/Work PB5859); it passed the
      *> binder and failed in the generated C# (CS1503). The control
      *> names N1 whole: the counter starts at 1234 and ALL "A" matches
      *> 5 times in AAXAAYA (positions 1, 2, 4, 5 and 7), so DISPLAY N1
      *> prints 1239.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5859B.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC X(7) VALUE "AAXAAYA".
       01 N1 PIC 9(4) VALUE 1234.
       PROCEDURE DIVISION.
       P1.
           INSPECT X TALLYING N1 (1:2) FOR ALL "A".
           DISPLAY N1.
           STOP RUN.
