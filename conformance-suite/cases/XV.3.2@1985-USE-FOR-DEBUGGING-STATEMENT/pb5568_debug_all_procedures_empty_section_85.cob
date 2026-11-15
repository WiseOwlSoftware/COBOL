      *>--- conformance-case
      *> id: pb5568_debug_all_procedures_empty_section_85
      *> rules: [1985-GR-XV.3.2.4-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5568. §1985:XV.3.2.4 GR4: "The ALL PROCEDURES
      *> phrase causes the effects described in general rule 3 to
      *> occur for every procedure-name in the program, except those
      *> appearing within a debugging section." The middle section S2
      *> holds no paragraph (§1985:IV.6.4.1.2: "zero, one, or more
      *> successive paragraphs") and is a procedure-name, so the
      *> debugging section runs for the PERFORM of it (GR20, 'PERFORM
      *> LOOP') and for the fall-through into it (GR22, 'FALL THROUGH')
      *> like every other procedure.
      *> Expected output, in order: S0 and P0 at the start of the
      *> program, P0, S2 for the PERFORM, AFTER, then S2, S3 and P3 for
      *> the fall-through, P3. Why it can fail: an empty section was no
      *> pc, so ALL PROCEDURES skipped it (the two DS S2 lines are
      *> absent from the pre-fix output).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5568ALL.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. X WITH DEBUGGING MODE.
       PROCEDURE DIVISION.
       DECLARATIVES.
       DS SECTION.
           USE FOR DEBUGGING ON ALL PROCEDURES.
       DS-1.
           DISPLAY "DS " DEBUG-NAME(1:2) " " DEBUG-CONTENTS(1:5).
       END DECLARATIVES.
       S0 SECTION.
       P0.
           DISPLAY "P0".
           PERFORM S2.
           DISPLAY "AFTER".
       S2 SECTION.
       S3 SECTION.
       P3.
           DISPLAY "P3".
           STOP RUN.
