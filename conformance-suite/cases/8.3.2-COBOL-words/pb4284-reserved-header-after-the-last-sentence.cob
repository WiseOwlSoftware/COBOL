      *>--- conformance-case
      *> id: pb4284-reserved-header-after-the-last-sentence
      *> rules: [SR-8.3.2.1-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4284 - "ZERO." written as a paragraph header after the
      *>   last sentence of P0. ZERO is reserved at every edition and begins
      *>   no statement, so the text is a paragraph-name that is a reserved
      *>   word, which ISO 8.3.2.1 1) refuses ("Reserved words shall not be
      *>   used as user-defined words or system-names"): COBOL0901 naming
      *>   ZERO, as every other reserved paragraph-name draws. It fails if
      *>   the report is the bare COBOL0001 or COBOL5506 at ZERO.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4284N.
       PROCEDURE DIVISION.
       P0.
           STOP RUN.
       ZERO.
       P1.
           EXIT.
