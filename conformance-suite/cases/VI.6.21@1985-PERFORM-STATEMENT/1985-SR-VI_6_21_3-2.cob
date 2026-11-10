      *>--- conformance-case
      *> id: 1985-SR-VI_6_21_3-2
      *> rules: [1985-SR-VI.6.21.3-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.21.3 SR2 (THE PERFORM STATEMENT). A refusal: the
      *> rule forbids line 10 of the program below, and WiseOwl refuses
      *> it at 85 with COBOL0900 there; a compiler that accepts it
      *> fails. Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PERF4.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  I PIC 9 VALUE 0.
       01  J PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN-PARA.
           PERFORM VARYING I FROM 1 BY 1 UNTIL I > 2
               AFTER J FROM 1 BY 1 UNTIL J > 2
               DISPLAY I J
           END-PERFORM.
           STOP RUN.
