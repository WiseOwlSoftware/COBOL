      *>--- conformance-case
      *> id: pb4305-sentences-before-first-paragraph-at-85
      *> rules: [FMT-14.2.1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4305 - the edition half of
      *>   pb4305_sentences_before_first_paragraph_2002. COBOL-85
      *>   required a paragraph-name before the first sentence of the
      *>   procedure division (2002 Annex F.2 item 90). Expected:
      *>   COBOL0900 at 85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4305NPN.
       PROCEDURE DIVISION.
           DISPLAY "X"
           STOP RUN.
