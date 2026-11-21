      *>--- conformance-case
      *> id: pb5924-switch-status-85
      *> rules: [1985-SR-VI.6.3.1.4-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:VI.6.3.1.4 (cite.py: OK): "must be named
      *> in the SPECIAL-NAMES paragraph"
      *> Condition-names the SPECIAL-NAMES paragraph names for a
      *> switch's on and off status: compiles; exactly one of the pair
      *> is true, and each follows a SET of the switch (VI.6.23 format
      *> 3).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5924R.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SWITCH-1 IS SW1 ON STATUS IS SW1-ON OFF STATUS IS SW1-OFF.
       PROCEDURE DIVISION.
       P0.
           IF SW1-ON AND NOT SW1-OFF OR SW1-OFF AND NOT SW1-ON
               DISPLAY "T1 OK" ELSE DISPLAY "T1 BAD".
           SET SW1 TO ON.
           IF SW1-ON DISPLAY "T2 ON" ELSE DISPLAY "T2 OFF".
           SET SW1 TO OFF.
           IF SW1-OFF DISPLAY "T3 OFF" ELSE DISPLAY "T3 ON".
           STOP RUN.
