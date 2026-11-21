      *>--- conformance-case
      *> id: pb5924-cond-name-not-a-switch-85
      *> rules: [1985-SR-IV.4.2.2.1.1.1-p2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:IV.4.2.2.1.1.1 (cite.py: OK): "condition-name must be assigned to the on status or off status"
      *> A SPECIAL-NAMES condition-name is assigned only to a switch's
      *> status: one on the CONSOLE device is refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CNDEVI.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           CONSOLE IS CRT ON STATUS IS CRT-ON.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "NO".
           STOP RUN.
