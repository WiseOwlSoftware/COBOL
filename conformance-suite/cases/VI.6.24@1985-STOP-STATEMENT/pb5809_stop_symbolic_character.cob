      *>--- conformance-case
      *> id: pb5809_stop_symbolic_character
      *> rules: [1985-GR-VI.6.24.4-4, 1985-GR-IV.4.2.2.2.3-L2.3, 1985-GR-IV.4.2.2.2.3-L2.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5809 - a symbolic-character is a figurative
      *> constant (§1985:IV.4.2.2.2.3 GR7). By the rule
      *> §1985:IV.4.2.2.2.3 GRL2.3, "A figurative constant may be
      *> used whenever 'literal' appears in a format", and STOP
      *> literal-1 bars only a figurative constant that begins with
      *> the word ALL (§1985:VI.6.24.3 SR1). So
      *> STOP STAR translates, communicates the one character STAR
      *> names to the operator (GRL2.2, §1985:VI.6.24.4 GR4) and
      *> execution continues with the next statement: the program
      *> displays S1 and S2.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5809A.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS STAR IS 43.
       PROCEDURE DIVISION.
       P1.
           DISPLAY "S1".
           STOP STAR.
           DISPLAY "S2".
           STOP RUN.
