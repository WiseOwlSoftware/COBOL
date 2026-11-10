      *>--- conformance-case
      *> id: 1985-SR-VI_6_25_3-2
      *> rules: [1985-SR-VI.6.25.3-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.25.3 SR2 (THE STRING STATEMENT). A refusal: the rule
      *> forbids line 8 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL1626 there; a compiler that accepts it fails.
      *> Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. STRTST.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  OUT-X PIC X(5) VALUE SPACES.
       PROCEDURE DIVISION.
       MAIN-PARA.
           STRING 5 DELIMITED BY SIZE INTO OUT-X.
           DISPLAY OUT-X.
           STOP RUN.
