      *>--- conformance-case
      *> id: pb4240-defined-spelled-with-dotted-capital-i
      *> rules: [GR-8.1.3.2-3, GR-8.1.3.2-4, GR-7.3.8.4.4-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4240 - the 7.3.8.4.4 defined condition's DEFINED is a
      *> keyword, matched on the basic letters alone (8.1.3.2 3) a); the
      *> Annex C fold of 4) governs user-defined words only, owner
      *> decision 2026-10-31). DEFİNED, written with U+0130, is not
      *> DEFINED, so the constant conditional expression after >>IF is
      *> malformed (COBOL1619). It fails when DEFINED is compared by the
      *> fold, which reads the condition as X IS DEFINED and compiles.
       >>DEFINE X 1
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4240D.
       PROCEDURE DIVISION.
       >>IF X DEFİNED
           DISPLAY "DEFINED".
       >>END-IF
           STOP RUN.
