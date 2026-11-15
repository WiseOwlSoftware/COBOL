      *>--- conformance-case
      *> id: pb5470-copy-star-greater-is-a-text-word
      *> rules: [1985-GR-XII.2.4-5, 1985-GR-XII.2.4-6]
      *> accept: [85]
      *> files: [PB5470LB.cpy, PB5470LC.cpy]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5470 - COBOL-85 has no floating comment indicator, so
      *> *> is an ordinary text-word (§1985:III, "Text-word": "Any other
      *> sequence of contiguous COBOL characters except comment lines
      *> and the word 'COPY'"), in library text and in pseudo-text
      *> alike: GR6 of §1985:XII.2.4 reads "the sequence of text words
      *> in the library text, if any, and in pseudo-text-1" as "determined
      *> by the rules for reference format" (cite.py --check: OK).
      * LIBRARY TEXT PB5470LB IS:   DISPLAY "3" *> "Z".
      * LIBRARY TEXT PB5470LC IS:   DISPLAY "1" *> J.
      * THE FIRST COPY REMOVES THE TEXT-WORDS *> "Z"; THE SECOND
      * MATCHES THE FOUR TEXT-WORDS DISPLAY "1" *> J. BEFORE, THE
      * *> WAS A COMMENT: COBOL0900, AND THE PSEUDO-TEXT LOST ITS END.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5470A.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       PROCEDURE DIVISION.
       P1.
           COPY PB5470LB REPLACING ==*> "Z"== BY == ==.
           COPY PB5470LC REPLACING ==DISPLAY "1" *> J== BY
               ==DISPLAY "2"==.
           STOP RUN.
