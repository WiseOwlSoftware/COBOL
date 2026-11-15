      *>--- conformance-case
      *> id: pb5568_debug_empty_section_subject_85
      *> rules: [1985-GR-XV.3.2.4-3, 1985-GR-XV.3.2.4-16]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5568. §1985:XV.3.2.4 GR3 a: the debugging section
      *> for procedure-name-1 runs "Immediately before each execution
      *> of the named procedure". A section may hold no paragraph
      *> (§1985:IV.6.4.1.2: "zero, one, or more successive
      *> paragraphs"), and the PERFORM of it and the fall-through into
      *> it are each an execution of that procedure. Here S0, the first
      *> procedure of the program, and the middle section S2 are both
      *> empty, and the debugging section names each. GR16: the first
      *> execution of the first nondeclarative procedure (S0) is
      *> DEBUG-CONTENTS 'START PROGRAM'; GR20: the PERFORM's control
      *> mechanism is 'PERFORM LOOP'; GR22: the implicit transfer from
      *> the previous paragraph is 'FALL THROUGH'.
      *> Expected output, in order: S0 START, M1, S2 PERF (PERFORM S2),
      *> BACK, S2 FALL (fall-through from M1 into S2), then P3 and the
      *> run ends. Why it can fail: an empty section was no pc, so the
      *> generated C# did not compile (the DEBUG-ITEM name was never
      *> declared) and, were it compiled, no trigger could fire.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5568NAM.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. X WITH DEBUGGING MODE.
       PROCEDURE DIVISION.
       DECLARATIVES.
       DS SECTION.
           USE FOR DEBUGGING ON S0 S2.
       DS-1.
           DISPLAY "DS " DEBUG-NAME(1:2) " " DEBUG-CONTENTS(1:5).
       END DECLARATIVES.
       S0 SECTION.
       S1 SECTION.
       M1.
           DISPLAY "M1".
           PERFORM S2.
           DISPLAY "BACK".
       S2 SECTION.
       S3 SECTION.
       P3.
           DISPLAY "P3".
           STOP RUN.
