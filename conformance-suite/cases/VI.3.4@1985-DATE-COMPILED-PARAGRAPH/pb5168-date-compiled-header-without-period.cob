      *>--- conformance-case
      *> id: pb5168-date-compiled-header-without-period
      *> rules: [1985-FMT-VI.3.4.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5168 - §1985:VI.3.4.2 prints the paragraph as
      *> `DATE-COMPILED.` and §1985:IV.2.1.6 gives the separator period
      *> "the status of a required word" (cite.py --check: OK). A bare
      *> DATE-COMPILED word, with no period: COBOL7166.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5168B.
       DATE-COMPILED
       PROCEDURE DIVISION.
       P1.
           DISPLAY "D".
           STOP RUN.
