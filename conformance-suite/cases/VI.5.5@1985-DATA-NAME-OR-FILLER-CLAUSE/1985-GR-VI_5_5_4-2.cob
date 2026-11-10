      *>--- conformance-case
      *> id: 1985-GR-VI_5_5_4-2
      *> rules: [1985-GR-VI.5.5.4-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.5.5.4 GR2 (THE DATA-NAME OR FILLER CLAUSE). An output
      *> test: the program must translate and run at 85 and DISPLAY
      *> exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from written and run by the witness-drafting
      *> pass (a rule the audit never drafted) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. FILLERCV.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-REC.
           05 WS-A    PIC X(3) VALUE "ABC".
           05 FILLER  PIC X    VALUE "-".
           05 FILLER  PIC X    VALUE "N".
              88 FLAG-ON    VALUE "Y".
              88 FLAG-OFF   VALUE "N".
           05 FILLER  PIC 9    VALUE 3.
              88 LOW-DIGIT  VALUE 0 THRU 4.
              88 HIGH-DIGIT VALUE 5 THRU 9.
           05 FILLER.
              88 PAIR-IS-ZZ VALUE "ZZ".
              10 WS-B    PIC X VALUE "D".
              10 FILLER  PIC X VALUE "E".
       01  FILLER     PIC X(2) VALUE "GO".
           88 SWITCH-GO   VALUE "GO".
           88 SWITCH-STOP VALUE "NO".
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY WS-REC.
           IF FLAG-OFF DISPLAY "FLAG-OFF"
               ELSE DISPLAY "NOT FLAG-OFF".
           IF FLAG-ON DISPLAY "FLAG-ON"
               ELSE DISPLAY "NOT FLAG-ON".
           IF LOW-DIGIT DISPLAY "LOW-DIGIT"
               ELSE DISPLAY "NOT LOW-DIGIT".
           IF PAIR-IS-ZZ DISPLAY "PAIR-IS-ZZ"
               ELSE DISPLAY "NOT PAIR-IS-ZZ".
           IF SWITCH-GO DISPLAY "SWITCH-GO"
               ELSE DISPLAY "NOT SWITCH-GO".
           SET FLAG-ON TO TRUE.
           SET HIGH-DIGIT TO TRUE.
           SET PAIR-IS-ZZ TO TRUE.
           SET SWITCH-STOP TO TRUE.
           DISPLAY WS-REC.
           IF FLAG-OFF DISPLAY "FLAG-OFF"
               ELSE DISPLAY "NOT FLAG-OFF".
           IF FLAG-ON DISPLAY "FLAG-ON"
               ELSE DISPLAY "NOT FLAG-ON".
           IF HIGH-DIGIT DISPLAY "HIGH-DIGIT"
               ELSE DISPLAY "NOT HIGH-DIGIT".
           IF PAIR-IS-ZZ DISPLAY "PAIR-IS-ZZ"
               ELSE DISPLAY "NOT PAIR-IS-ZZ".
           IF SWITCH-GO DISPLAY "SWITCH-GO"
               ELSE DISPLAY "NOT SWITCH-GO".
           IF SWITCH-STOP DISPLAY "SWITCH-STOP"
               ELSE DISPLAY "NOT SWITCH-STOP".
           MOVE "X" TO WS-B.
           IF PAIR-IS-ZZ DISPLAY "PAIR-IS-ZZ"
               ELSE DISPLAY "NOT PAIR-IS-ZZ".
           DISPLAY WS-REC.
           STOP RUN.
