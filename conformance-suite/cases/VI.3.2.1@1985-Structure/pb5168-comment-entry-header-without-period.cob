      *>--- conformance-case
      *> id: pb5168-comment-entry-header-without-period
      *> rules: [1985-FMT-VI.3.2.1.1, 1985-FMT-IV.6.1.3.1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5168 - §1985:III "Paragraph Header. A reserved word,
      *> followed by the separator period" and §1985:IV.2.1.6 "The
      *> separator period, when used in the formats, has the status of a
      *> required word" (cite.py --check: OK). The format prints
      *> `AUTHOR.`; this AUTHOR has no period after it: COBOL7166.
      *> Before, the logical conversion wrote the header's periods
      *> itself, so the program compiled and ran.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5168A.
       AUTHOR JOHN SMITH.
       PROCEDURE DIVISION.
       P1.
           DISPLAY "D".
           STOP RUN.
