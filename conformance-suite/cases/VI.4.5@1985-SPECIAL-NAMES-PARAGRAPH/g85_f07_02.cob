      *>--- conformance-case
      *> id: g85_f07_02
      *> rules: [1985-SR-VI.4.5.3-5]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.4.5.3 SR5 - literal-4 of the CLASS clause names
      *> SYM-A, a symbolic-character figurative constant the SYMBOLIC
      *> CHARACTERS clause before it declares: "Literal-1, literal-2,
      *> literal-3, literal-4, and literal-5 must not specify a
      *> symbolic-character figurative constant." A refusal: a
      *> compiler that accepts line 19 fails.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F0702.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS SYM-A IS 66
           CLASS CLS-1 IS SYM-A "X".
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "DONE".
           STOP RUN.
