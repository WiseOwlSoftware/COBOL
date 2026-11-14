      *>--- conformance-case
      *> id: pb5169-comment-entry-hyphen-continuation
      *> rules: [1985-SR-VI.3.2.1.2-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5169 - §1985:VI.3.2.1.2 SR1: "The continuation of the
      *> comment-entry by the use of the hyphen in the indicator area is
      *> not permitted; however, the comment-entry may be contained on
      *> one or more lines." (cite.py --check: OK). The AUTHOR entry's
      *> second line has a hyphen in the indicator area: COBOL7165.
      *> Before, the entry was discarded with the commentary and the
      *> program compiled with no diagnostic.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5169A.
       AUTHOR. A LONG NAME THAT IS BROKEN IN THE MIDDLE OF A WO
      -    RD AND CONTINUED WITH A HYPHEN.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       PROCEDURE DIVISION.
       P1.
           DISPLAY "OK".
           STOP RUN.
