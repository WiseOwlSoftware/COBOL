      *>--- conformance-case
      *> id: 1985-SR-VI_1_3_3-p1
      *> rules: [1985-SR-VI.1.3.3-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.1.3.3 p1 (RESTRICTIONS ON OVERALL LANGUAGE). An output
      *> test: the program must translate and run at 85 and DISPLAY
      *> exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-08.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * 1985-SR-VI.1.3.3-p1: level 2 figurative constants: the level 1
      * set, symbolic-character, ALL literal, ALL figurative constant,
      * ALL symbolic-character. Expect: [0 ] ["] [ABAB] [00] [!] [!!]
       IDENTIFICATION DIVISION.
       PROGRAM-ID. FIGL2.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS BANG IS 34.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 Z1 PIC X VALUE ZERO.
       01 S1 PIC X VALUE SPACE.
       01 H1 PIC X VALUE HIGH-VALUE.
       01 L1 PIC X VALUE LOW-VALUES.
       01 Q1 PIC X VALUE QUOTE.
       01 A4 PIC X(4).
       01 Z2 PIC X(2).
       01 B1 PIC X.
       01 B2 PIC X(2).
       PROCEDURE DIVISION.
       P1.
           MOVE ZEROS TO Z2.
           MOVE ZEROES TO Z2.
           MOVE SPACES TO S1.
           MOVE HIGH-VALUES TO H1.
           MOVE LOW-VALUE TO L1.
           MOVE QUOTES TO Q1.
           MOVE ALL "AB" TO A4.
           MOVE ALL ZERO TO Z2.
           MOVE BANG TO B1.
           MOVE ALL BANG TO B2.
           IF H1 = HIGH-VALUE AND L1 = LOW-VALUE
             DISPLAY "[" Z1 S1 "] [" Q1 "] [" A4 "] [" Z2 "] [" B1
                     "] [" B2 "]"
           ELSE DISPLAY "WRONG".
           STOP RUN.
