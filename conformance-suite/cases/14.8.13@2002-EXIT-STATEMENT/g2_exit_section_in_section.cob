      *>--- conformance-case
      *> id: g2_exit_section_in_section
      *> rules: [2002-SR-14.8.13.2-11]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.13.2 SR11 - "The EXIT statement with the SECTION
      *>   phrase may be specified only in a section." The granted form:
      *>   EXIT SECTION in paragraph P1 of section S1, accepted and run.
      *>   The refusal is g2-exit-section-outside-section (COBOL0827).
      *> §2002:14.8.13.3 GR12 - "The execution of an EXIT SECTION
      *>   statement causes control to be passed to an unnamed empty
      *>   paragraph immediately following the last paragraph of the
      *>   current section, preceding any return mechanisms for that
      *>   section."
      *> EXPECTED (derived): PERFORM S1 prints IN-S1; EXIT SECTION skips
      *>   P1-TAIL and the whole of P2, reaches the end of S1, and the
      *>   PERFORM's return mechanism resumes at DISPLAY "BACK".
      *> FAILS IF: EXIT SECTION is refused inside a section, or P1-TAIL
      *>   or NOT-REACHED is printed, or BACK is lost.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2EXT05.
       PROCEDURE DIVISION.
       S0 SECTION.
       M0.
           PERFORM S1.
           DISPLAY "BACK".
           STOP RUN.
       S1 SECTION.
       P1.
           DISPLAY "IN-S1".
           EXIT SECTION.
           DISPLAY "P1-TAIL".
       P2.
           DISPLAY "NOT-REACHED".
