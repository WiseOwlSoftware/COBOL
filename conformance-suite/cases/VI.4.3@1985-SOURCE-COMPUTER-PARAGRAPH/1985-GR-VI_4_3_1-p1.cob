      *>--- conformance-case
      *> id: 1985-GR-VI_4_3_1-p1
      *> rules: [1985-GR-VI.4.3.1-p1, 1985-GR-VI.4.4.1-p1, 1985-GR-VI.4.5.1-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.4.3.1 p1; §1985:VI.4.4.1 p1; §1985:VI.4.5.1 p1 (THE
      *> SOURCE-COMPUTER PARAGRAPH). An output test: the program must
      *> translate and run at 85 and DISPLAY exactly the lines its .out
      *> holds, the outcome the rule requires. Placed from written and
      *> run by the PB5453 reader of lineage-reads-1985/s-09.json by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
      * s-09 probe: Configuration Section paragraphs (VI.4.3, VI.4.4,
      * VI.4.5): SOURCE-COMPUTER, OBJECT-COMPUTER with MEMORY SIZE and
      * PROGRAM COLLATING SEQUENCE, SPECIAL-NAMES with CURRENCY SIGN,
      * SYMBOLIC CHARACTERS, ALPHABET and CLASS.
      * Expect: accepted, DISPLAYs  #12.50 / SYM-OK / CLS-OK / ALPHA-OK
       IDENTIFICATION DIVISION.
       PROGRAM-ID. S09ENV.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. ANY-COMPUTER.
       OBJECT-COMPUTER. ANY-COMPUTER
           MEMORY SIZE 8000 WORDS
           PROGRAM COLLATING SEQUENCE IS STD-ALPHA.
       SPECIAL-NAMES.
           ALPHABET STD-ALPHA IS STANDARD-1
           SYMBOLIC CHARACTERS LETTER-A IS 66
           CLASS HEX-DIGIT IS "0" THRU "9" "A" THRU "F"
           CURRENCY SIGN IS "#".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       77  AMT PIC ##9.99.
       77  CHR  PIC X.
       PROCEDURE DIVISION.
       P0.
           MOVE 12.5 TO AMT.
           DISPLAY AMT.
           MOVE LETTER-A TO CHR.
           IF CHR = "A" DISPLAY "SYM-OK" ELSE DISPLAY "SYM-BAD".
           MOVE "C" TO CHR.
           IF CHR IS HEX-DIGIT DISPLAY "CLS-OK" ELSE DISPLAY "CLS-BAD".
           MOVE "A" TO CHR.
           IF CHR < "B" DISPLAY "ALPHA-OK" ELSE DISPLAY "ALPHA-BAD".
           STOP RUN.
