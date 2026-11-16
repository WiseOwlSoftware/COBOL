      *>--- conformance-case
      *> id: g85_f07_01
      *> rules: [1985-SR-VI.4.5.3-5]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.4.5.3 SR5 - literal-1 of the ALPHABET clause names
      *> SYM-A, a symbolic-character figurative constant this paragraph
      *> declares: "Literal-1, literal-2, literal-3, literal-4, and
      *> literal-5 must not specify a symbolic-character figurative
      *> constant." A refusal: a compiler that accepts line 17 fails.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F0701.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET ALPH-1 IS SYM-A "B" "C"
           SYMBOLIC CHARACTERS SYM-A IS 66.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "DONE".
           STOP RUN.
