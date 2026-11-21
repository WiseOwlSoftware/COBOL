      *>--- conformance-case
      *> id: pb5924-cond-name-switch-status-85
      *> rules: [1985-SR-IV.4.2.2.1.1.1-p2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:IV.4.2.2.1.1.1 (cite.py: OK): "condition-name must be assigned to the on status or off status"
      *> Condition-names on a switch's on and off status (SPECIAL-NAMES)
      *> and an 88 in the Data Division are both defined and tested.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CNSWIT.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SWITCH-1 IS SW1 ON STATUS IS SW1-ON OFF STATUS IS SW1-OFF.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  FLAG              PIC X VALUE "Y".
           88  FLAG-YES      VALUE "Y".
       PROCEDURE DIVISION.
       MAIN-PARA.
           SET SW1 TO OFF.
           IF SW1-OFF DISPLAY "SWITCH OFF".
           SET SW1 TO ON.
           IF SW1-ON DISPLAY "SWITCH ON".
           IF FLAG-YES DISPLAY "FLAG YES".
           STOP RUN.
