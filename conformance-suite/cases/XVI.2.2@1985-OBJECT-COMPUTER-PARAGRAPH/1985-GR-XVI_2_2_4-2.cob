      *>--- conformance-case
      *> id: 1985-GR-XVI_2_2_4-2
      *> rules: [1985-GR-XVI.2.2.4-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XVI.2.2.4 GR2 (THE OBJECT-COMPUTER PARAGRAPH). An output
      *> test: the program must translate and run at 85 and DISPLAY
      *> exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from the confirmation pass's redraft (WEAK
      *> original) by scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SEGOC.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. GENERIC.
       OBJECT-COMPUTER. GENERIC
           PROGRAM COLLATING SEQUENCE IS REV-SEQ
           SEGMENT-LIMIT IS 49.
       SPECIAL-NAMES.
           ALPHABET REV-SEQ IS "B" "A".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-A PIC X VALUE "A".
       01  WS-B PIC X VALUE "B".
       PROCEDURE DIVISION.
       MAIN-SEC SECTION 10.
       BEGIN-PARA.
           IF WS-B < WS-A
               DISPLAY "PROGRAM SEQUENCE"
           ELSE
               DISPLAY "NATIVE SEQUENCE".
           STOP RUN.
