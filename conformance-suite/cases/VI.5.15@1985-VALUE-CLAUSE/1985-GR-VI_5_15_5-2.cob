      *>--- conformance-case
      *> id: 1985-GR-VI_5_15_5-2
      *> rules: [1985-GR-VI.5.15.5-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.5.15.5 GR2 (THE VALUE CLAUSE). A refusal: the rule
      *> forbids line 6 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL2961 there; a compiler that accepts it fails.
      *> Placed from the confirmation pass's redraft (WEAK original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. VALTHRU.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-NUM PIC 99 VALUE 5.
           88  IN-RANGE VALUE 9 THRU 1.
       PROCEDURE DIVISION.
       MAIN-PARA.
           IF IN-RANGE
               DISPLAY "IN RANGE"
           ELSE
               DISPLAY "OUT OF RANGE".
           MOVE 10 TO WS-NUM.
           IF IN-RANGE
               DISPLAY "IN RANGE"
           ELSE
               DISPLAY "OUT OF RANGE".
           STOP RUN.
