      *>--- conformance-case
      *> id: pb5169-date-compiled-hyphen-continuation
      *> rules: [1985-SR-VI.3.4.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5169 - §1985:VI.3.4.3 SR1 words the DATE-COMPILED
      *> paragraph's rule as §1985:VI.3.2.1.2 SR1 does: "The continuation
      *> of the comment-entry by the use of the hyphen in the indicator
      *> area is not permitted" (cite.py --check: OK). COBOL7165.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5169B.
       DATE-COMPILED. TODAY, A DATE THAT IS BROKEN IN THE MIDDLE OF A WO
      -    RD AND CONTINUED WITH A HYPHEN.
       PROCEDURE DIVISION.
       P1.
           DISPLAY "OK".
           STOP RUN.
