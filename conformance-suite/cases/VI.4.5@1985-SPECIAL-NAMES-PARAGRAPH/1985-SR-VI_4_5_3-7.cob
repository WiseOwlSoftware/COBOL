      *>--- conformance-case
      *> id: 1985-SR-VI_4_5_3-7
      *> rules: [1985-SR-VI.4.5.3-7]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.4.5.3 SR7 (THE SPECIAL-NAMES PARAGRAPH). A refusal:
      *> the rule forbids line 6 of the program below, and WiseOwl
      *> refuses it at 85 with COBOL1670 there; a compiler that accepts
      *> it fails. Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SYMDUP.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS SYM-A SYM-A ARE 66 67.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "DONE".
           STOP RUN.
