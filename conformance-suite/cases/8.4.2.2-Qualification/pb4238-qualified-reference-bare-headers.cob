      *>--- conformance-case
      *> id: pb4238-qualified-reference-bare-headers
      *> rules: [FMT-8.4.2.2.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4238. ISO 14.4.1: "It consists of a paragraph-name (that may be
      *> qualified) or a section-name" (cite.py --check 14.4.1 -> OK): the QUALIFIED
      *> form belongs to the procedure-name that REFERS to a paragraph, and the
      *> general formats of 14.2.1 print the headers bare (section-name-1 SECTION.,
      *> paragraph-name-1.). Paragraph P1 is written in both sections, which 8.4.2.2.1
      *> allows because every reference qualifies it, so PERFORM P1 OF S1 runs the
      *> paragraph of S1 and PERFORM P1 IN S2 the one of S2 (OF and IN are the same
      *> qualifier). Each leg can fail: a compiler that resolves a header's glued text
      *> (P1OFS1) or that merges the two P1 declarations prints the wrong line, twice
      *> the same line, or refuses the program as ambiguous.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4238OK.
       PROCEDURE DIVISION.
       MAIN-S SECTION.
       MAIN-P.
           PERFORM P1 OF S1.
           PERFORM P1 IN S2.
           PERFORM P1 OF S1.
           STOP RUN.
       S1 SECTION.
       P1.
           DISPLAY "S1-P1".
       S2 SECTION.
       P1.
           DISPLAY "S2-P1".
