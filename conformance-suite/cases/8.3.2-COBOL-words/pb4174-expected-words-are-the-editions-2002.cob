      *>--- conformance-case
      *> id: pb4174-expected-words-are-the-editions-2002
      *> rules: [SR-8.3.2.1-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4174 - the twin of pb4174-expected-words-are-the-
      *> editions-85 at 2002 and 2014. ISO 8.3.2.1 1): "Reserved words
      *> shall not be used as user-defined words or system-names."
      *> (cite.py --check 8.3.2.1 -> OK). B-AND, B-OR and B-XOR are
      *> reserved from 2002 (reserved-words.json), so the sentence for an
      *> unclosed parenthesis (8.3.5 4)) names them from here on; the
      *> shift operators B-SHIFT-L, B-SHIFT-LC, B-SHIFT-R and B-SHIFT-RC
      *> are 2023's (ISO Annex E.2 3), cite.py --check E.2 "B-SHIFT-L" ->
      *> OK) and stay out of the list at 2002 and 2014.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P4174E02.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N-A PIC 999.
       01 N-B PIC 999 VALUE 4.
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE N-A = (N-B + 1.
           STOP RUN.
