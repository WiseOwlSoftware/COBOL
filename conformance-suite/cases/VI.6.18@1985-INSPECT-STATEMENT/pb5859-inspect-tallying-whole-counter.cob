      *>--- conformance-case
      *> id: pb5859-inspect-tallying-whole-counter
      *> rules: [1985-SR-VI.6.18.3-6]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> The control of pb5859-inspect-tallying-refmod-counter: the
      *> whole numeric item N1 is an elementary numeric data item. N1
      *> starts at 1234 and ALL "A" matches 5 times in AAXAAYA
      *> (positions 1, 2, 4, 5 and 7), so DISPLAY N1 prints 1239.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5859C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC X(7) VALUE "AAXAAYA".
       01 N1 PIC 9(4) VALUE 1234.
       PROCEDURE DIVISION.
       P1.
           INSPECT X TALLYING N1 FOR ALL "A".
           DISPLAY N1.
           STOP RUN.
