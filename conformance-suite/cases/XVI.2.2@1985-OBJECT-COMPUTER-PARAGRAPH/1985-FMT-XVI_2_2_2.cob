      *>--- conformance-case
      *> id: 1985-FMT-XVI_2_2_2
      *> rules: [1985-FMT-XVI.2.2.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XVI.2.2.2 (THE OBJECT-COMPUTER PARAGRAPH). An output
      *> test: the program must translate and run at 85 and DISPLAY
      *> exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from the confirmation pass's redraft (WEAK
      *> original) by scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. OBJSEG.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. TEST-MACHINE.
       OBJECT-COMPUTER. TEST-MACHINE
           MEMORY SIZE 8192 CHARACTERS
           PROGRAM COLLATING SEQUENCE IS REV-SEQ
           SEGMENT-LIMIT IS 10.
       SPECIAL-NAMES.
           ALPHABET REV-SEQ IS "Z" THRU "A".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-A PIC X VALUE "A".
       01  WS-B PIC X VALUE "B".
       PROCEDURE DIVISION.
       MAIN-SEC SECTION 5.
       MAIN-PARA.
           IF WS-A > WS-B
               DISPLAY "A AFTER B"
           ELSE
               DISPLAY "A BEFORE B".
           PERFORM OVL-PARA.
           DISPLAY "BACK IN 5".
           STOP RUN.
       OVL-SEC SECTION 20.
       OVL-PARA.
           DISPLAY "OVERLAYABLE 20".
