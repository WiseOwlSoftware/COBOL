      *>--- conformance-case
      *> id: 1985-SR-VI_5_15_3-3
      *> rules: [1985-SR-VI.5.15.3-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.5.15.3 SR3 (THE VALUE CLAUSE). A refusal: the rule
      *> forbids line 7 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL1570 there; a compiler that accepts it fails.
      *> Placed from written and run by the witness-drafting pass (a
      *> rule the audit never drafted) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. VAL85SR3.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-FIT              PIC ZZ9  VALUE " 12".
       01  WS-EDIT             PIC ZZ9  VALUE "1234".
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "[" WS-FIT "]".
           DISPLAY "[" WS-EDIT "]".
           STOP RUN.
