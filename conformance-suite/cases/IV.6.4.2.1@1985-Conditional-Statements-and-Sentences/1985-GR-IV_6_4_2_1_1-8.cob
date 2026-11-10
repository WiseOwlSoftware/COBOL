      *>--- conformance-case
      *> id: 1985-GR-IV_6_4_2_1_1-8
      *> rules: [1985-GR-IV.6.4.2.1.1-8]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.6.4.2.1.1 GR8 (Conditional Statements and Sentences).
      *> A refusal: the rule forbids line 9 of the program below, and
      *> WiseOwl refuses it at 85 with COBOL2796 there; a compiler that
      *> accepts it fails. Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. T.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC 9 VALUE 9.
       PROCEDURE DIVISION.
       P1.
           ADD 1 TO X ON SIZE ERROR
               CALL "SUB" ON EXCEPTION DISPLAY "E"
           END-ADD.
           STOP RUN.
