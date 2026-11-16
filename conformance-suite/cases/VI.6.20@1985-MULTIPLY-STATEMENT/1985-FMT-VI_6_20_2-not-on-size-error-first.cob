      *>--- conformance-case
      *> id: 1985-FMT-VI_6_20_2-not-on-size-error-first
      *> rules: [1985-FMT-VI.6.20.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> A refusal at 85. §1985:VI.6.20.2 prints MULTIPLY's
      *> [ ON SIZE ERROR imperative-statement-1 ] before its
      *> [ NOT ON SIZE ERROR imperative-statement-2 ], and
      *> §1985:IV.2.1 requires the clauses "in the sequence given in
      *> the general format" (both cite.py --check OK). The program
      *> writes the NOT phrase first, so WiseOwl refuses it at 85 with
      *> COBOL0900 at its ON SIZE ERROR (the free order is ISO/IEC
      *> 1989:2002 Annex F.2's relaxation); a compiler that accepts it
      *> fails. The printed order runs in 1985-FMT-VI_6_20_2
      *> (registrar re-read, 2026-11-15).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RR85M2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A PIC 99 VALUE 50.
       01  R PIC 99 VALUE 0.
       PROCEDURE DIVISION.
       P1.
           MULTIPLY 2 BY A NOT ON SIZE ERROR DISPLAY "OK"
               ON SIZE ERROR DISPLAY "SE" END-MULTIPLY
           STOP RUN.
