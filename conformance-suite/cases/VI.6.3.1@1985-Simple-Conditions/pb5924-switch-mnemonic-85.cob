      *>--- conformance-case
      *> id: pb5924-switch-mnemonic-85
      *> rules: [1985-SR-VI.6.3.1.4-p1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:VI.6.3.1.4 (cite.py: OK): "must be named
      *> in the SPECIAL-NAMES paragraph"
      *> The switch is named but no on or off value is: its mnemonic-
      *> name is no switch-status condition: refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5924S.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SWITCH-1 IS SW1.
       PROCEDURE DIVISION.
       P0.
           IF SW1 DISPLAY "ON" ELSE DISPLAY "OFF".
           STOP RUN.
