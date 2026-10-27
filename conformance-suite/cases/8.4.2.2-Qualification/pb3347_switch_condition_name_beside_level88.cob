      *>--- conformance-case
      *> id: pb3347_switch_condition_name_beside_level88
      *> rules: [SR-8.4.2.2.1-1, SR-8.4.2.2.3-1, SR-8.4.2.2.3-5]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3347. A SPECIAL-NAMES switch condition-name (8.3.2.2.4: "identifies an on or off status defined in
      *> the SPECIAL-NAMES paragraph") and a level-88 are the SAME type of user-defined word, so one source element
      *> may declare both spellings (8.3.2.2 asks for one TYPE per word) - but the name is then not unique, and
      *> 8.4.2.2.1 requires qualification unless "1) No other name has the identical spelling". The condition-name
      *> qualifies by its conditional variable (8.4.2.2.3 SR5), so `S-ON OF V` names the level-88 and nothing else;
      *> the switch condition-name has no hierarchy and takes no qualifier. S-OFF is declared once, so the bare
      *> reference is unique and is the switch's OFF status condition. The program declares the pair legally and
      *> refers to it only where the reference identifies one resource (8.4.2.1).
      *>   cite.py --check 8.4.2.2.1 "No other name has the identical spelling." -> OK
      *>   cite.py --check 8.4.2.2.3 "The qualification of a condition-name may include the conditional variable with
      *>     which the condition-name is associated" -> OK
      *> V = 2: S-ON OF V (the level-88, VALUE 1) is false -> Q1-FALSE. SWITCH-31 is not set externally, so it is OFF
      *> (12.3.7 GR2 status; the implementor-defined default): S-OFF is true -> SW-OFF. V = 1: S-ON IN V -> Q2-TRUE.
      *> A leg fails if the bare declaration pair draws a diagnostic or `S-ON OF V` reads the switch.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3347QUAL.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SWITCH-31 ON STATUS IS S-ON OFF STATUS IS S-OFF.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 V PIC 9 VALUE 2.
          88 S-ON VALUE 1.
       PROCEDURE DIVISION.
       MAIN.
           IF S-ON OF V DISPLAY "Q1-TRUE" ELSE DISPLAY "Q1-FALSE".
           IF S-OFF DISPLAY "SW-OFF" ELSE DISPLAY "SW-ON".
           MOVE 1 TO V.
           IF S-ON IN V DISPLAY "Q2-TRUE" ELSE DISPLAY "Q2-FALSE".
           STOP RUN.
