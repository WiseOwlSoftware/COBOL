      *>--- conformance-case
      *> id: 1985-FMT-VI_4_5_2
      *> rules: [1985-FMT-VI.4.5.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.4.5.2 (THE SPECIAL-NAMES PARAGRAPH). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WEAK original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SPNAMES.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET ASC IS STANDARD-1
           ALPHABET REV IS "Z" THRU "A"
           SYMBOLIC CHARACTERS STAR CROSS ARE 43 44 IN ASC
           CLASS HEX-DIGIT IS "0" THRU "9" "A" THRU "F"
           CLASS VOWEL IS "AEIOU"
           CURRENCY SIGN IS "L"
           DECIMAL-POINT IS COMMA.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-A PIC X(4) VALUE "1F9C".
       01  WS-B PIC X(4) VALUE "1G9C".
       01  WS-V PIC X(3) VALUE "AEI".
       01  WS-S PIC X(3) VALUE ALL STAR.
       01  WS-P PIC X VALUE CROSS.
       01  WS-M PIC L9,99.
       PROCEDURE DIVISION.
       MAIN-PARA.
           IF WS-A IS HEX-DIGIT
               DISPLAY "A HEX"
           ELSE
               DISPLAY "A NOT HEX".
           IF WS-B IS HEX-DIGIT
               DISPLAY "B HEX"
           ELSE
               DISPLAY "B NOT HEX".
           IF WS-V IS VOWEL
               DISPLAY "V VOWEL"
           ELSE
               DISPLAY "V NOT VOWEL".
           DISPLAY WS-S WS-P.
           MOVE 1,5 TO WS-M.
           DISPLAY WS-M.
           STOP RUN.
