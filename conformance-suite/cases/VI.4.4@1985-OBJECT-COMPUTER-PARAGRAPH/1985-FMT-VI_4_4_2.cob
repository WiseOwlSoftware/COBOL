      *>--- conformance-case
      *> id: 1985-FMT-VI_4_4_2
      *> rules: [1985-FMT-VI.4.4.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.4.4.2 (THE OBJECT-COMPUTER PARAGRAPH). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WEAK original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. OBJCMP.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER. XYZ-1
           MEMORY SIZE 4096 WORDS
           PROGRAM COLLATING SEQUENCE IS REV-SEQ.
       SPECIAL-NAMES.
           ALPHABET REV-SEQ IS "Z" THRU "A".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-A PIC X VALUE "A".
       01  WS-B PIC X VALUE "B".
       PROCEDURE DIVISION.
       MAIN-PARA.
           IF WS-A > WS-B
               DISPLAY "A AFTER B"
           ELSE
               DISPLAY "A BEFORE B".
           STOP RUN.
