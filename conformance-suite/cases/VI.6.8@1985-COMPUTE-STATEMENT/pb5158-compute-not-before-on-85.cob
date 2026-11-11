      *>--- conformance-case
      *> id: pb5158-compute-not-before-on-85
      *> rules: [1985-FMT-VI.6.8.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5158. A refusal at 85. §1985:VI.6.8.2 prints
      *> COMPUTE's [ ON SIZE ERROR imperative-statement-1 ] before
      *> its [ NOT ON SIZE ERROR imperative-statement-2 ], and
      *> §1985:IV.2.1: "Clauses must be written in the sequence
      *> given in the general formats. (Clauses that are optional
      *> must appear in the sequence shown if they are used.)"
      *> (cite.py --check OK). The program writes the NOT phrase
      *> first, so WiseOwl refuses it at 85 with COBOL0900 at its
      *> ON SIZE ERROR (the free order is ISO/IEC 1989:2002 Annex
      *> F.2 item 33's relaxation); a compiler that accepts it
      *> fails. The program with the phrases in the printed order
      *> is 1985-FMT-VI_6_8_2.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5158CP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       P1.
           COMPUTE A = 100
               NOT ON SIZE ERROR DISPLAY "OK"
               ON SIZE ERROR DISPLAY "SIZE"
           END-COMPUTE.
           STOP RUN.
