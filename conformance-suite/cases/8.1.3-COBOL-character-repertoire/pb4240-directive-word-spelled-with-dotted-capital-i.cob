      *>--- conformance-case
      *> id: pb4240-directive-word-spelled-with-dotted-capital-i
      *> rules: [GR-8.1.3.2-3, GR-8.1.3.2-4, SR-7.3.3-6]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4240 - a compiler-directive word (8.12) is a keyword,
      *> matched on the basic letters alone (8.1.3.2 3) a); the Annex C
      *> fold of 4) governs user-defined words only, owner decision
      *> 2026-10-31). >>İF, written with U+0130, heads no directive,
      *> so the line is not the IF directive. It fails when the
      *> directive word is read by the fold, which makes >>İF and
      *> >>END-İF the IF directive and compiles the program.
       >>DEFINE X 1
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4240I.
       PROCEDURE DIVISION.
       >>İF X DEFINED
           DISPLAY "DEFINED".
       >>END-İF
           STOP RUN.
