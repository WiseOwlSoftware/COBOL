      *>--- conformance-case
      *> id: 1985-GR-VI_4_5_4-10
      *> rules: [1985-GR-VI.4.5.4-10]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.4.5.4 GR10 (THE SPECIAL-NAMES PARAGRAPH). An output
      *> test: the program must translate and run at 85 and DISPLAY
      *> exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from written and run by the witness-drafting
      *> pass (a rule the audit never drafted) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CLSNAME.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS SYM-65 SYM-66 SYM-67 SYM-68 SYM-69
               ARE 65 66 67 68 69
           CLASS VOWEL IS "AEIOU"
           CLASS LOW-LETTER IS "A" THRU "E"
           CLASS HIGH-DIGIT IS "9" THROUGH "5"
           CLASS MIXED IS "X" "0" THRU "3" "YZ"
           CLASS ORD-ONE IS 66
           CLASS ORD-RANGE IS 68 THRU 66.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-5       PIC X(5).
       01  WS-1       PIC X.
       01  WS-RESULT  PIC X(5).
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE "UOIEA" TO WS-5.
           IF WS-5 IS VOWEL MOVE "TRUE" TO WS-RESULT
               ELSE MOVE "FALSE" TO WS-RESULT.
           DISPLAY "UOIEA VOWEL " WS-RESULT.
           MOVE "AEIOX" TO WS-5.
           IF WS-5 IS VOWEL MOVE "TRUE" TO WS-RESULT
               ELSE MOVE "FALSE" TO WS-RESULT.
           DISPLAY "AEIOX VOWEL " WS-RESULT.
           IF WS-5 IS NOT VOWEL MOVE "TRUE" TO WS-RESULT
               ELSE MOVE "FALSE" TO WS-RESULT.
           DISPLAY "AEIOX NOT VOWEL " WS-RESULT.
           MOVE "AE IO" TO WS-5.
           IF WS-5 IS VOWEL MOVE "TRUE" TO WS-RESULT
               ELSE MOVE "FALSE" TO WS-RESULT.
           DISPLAY "AE IO VOWEL " WS-RESULT.
           MOVE "EDCBA" TO WS-5.
           IF WS-5 IS LOW-LETTER MOVE "TRUE" TO WS-RESULT
               ELSE MOVE "FALSE" TO WS-RESULT.
           DISPLAY "EDCBA LOW-LETTER " WS-RESULT.
           MOVE "ABCDF" TO WS-5.
           IF WS-5 IS LOW-LETTER MOVE "TRUE" TO WS-RESULT
               ELSE MOVE "FALSE" TO WS-RESULT.
           DISPLAY "ABCDF LOW-LETTER " WS-RESULT.
           MOVE "56789" TO WS-5.
           IF WS-5 IS HIGH-DIGIT MOVE "TRUE" TO WS-RESULT
               ELSE MOVE "FALSE" TO WS-RESULT.
           DISPLAY "56789 HIGH-DIGIT " WS-RESULT.
           MOVE "45678" TO WS-5.
           IF WS-5 IS HIGH-DIGIT MOVE "TRUE" TO WS-RESULT
               ELSE MOVE "FALSE" TO WS-RESULT.
           DISPLAY "45678 HIGH-DIGIT " WS-RESULT.
           MOVE "ZY3X0" TO WS-5.
           IF WS-5 IS MIXED MOVE "TRUE" TO WS-RESULT
               ELSE MOVE "FALSE" TO WS-RESULT.
           DISPLAY "ZY3X0 MIXED " WS-RESULT.
           MOVE "ZY4X0" TO WS-5.
           IF WS-5 IS MIXED MOVE "TRUE" TO WS-RESULT
               ELSE MOVE "FALSE" TO WS-RESULT.
           DISPLAY "ZY4X0 MIXED " WS-RESULT.
           MOVE SYM-66 TO WS-1.
           IF WS-1 IS ORD-ONE MOVE "TRUE" TO WS-RESULT
               ELSE MOVE "FALSE" TO WS-RESULT.
           DISPLAY "ORDINAL 66 ORD-ONE " WS-RESULT.
           MOVE SYM-65 TO WS-1.
           IF WS-1 IS ORD-ONE MOVE "TRUE" TO WS-RESULT
               ELSE MOVE "FALSE" TO WS-RESULT.
           DISPLAY "ORDINAL 65 ORD-ONE " WS-RESULT.
           MOVE SYM-67 TO WS-1.
           IF WS-1 IS ORD-ONE MOVE "TRUE" TO WS-RESULT
               ELSE MOVE "FALSE" TO WS-RESULT.
           DISPLAY "ORDINAL 67 ORD-ONE " WS-RESULT.
           MOVE SYM-65 TO WS-1.
           IF WS-1 IS ORD-RANGE MOVE "TRUE" TO WS-RESULT
               ELSE MOVE "FALSE" TO WS-RESULT.
           DISPLAY "ORDINAL 65 ORD-RANGE " WS-RESULT.
           MOVE SYM-66 TO WS-1.
           IF WS-1 IS ORD-RANGE MOVE "TRUE" TO WS-RESULT
               ELSE MOVE "FALSE" TO WS-RESULT.
           DISPLAY "ORDINAL 66 ORD-RANGE " WS-RESULT.
           MOVE SYM-67 TO WS-1.
           IF WS-1 IS ORD-RANGE MOVE "TRUE" TO WS-RESULT
               ELSE MOVE "FALSE" TO WS-RESULT.
           DISPLAY "ORDINAL 67 ORD-RANGE " WS-RESULT.
           MOVE SYM-68 TO WS-1.
           IF WS-1 IS ORD-RANGE MOVE "TRUE" TO WS-RESULT
               ELSE MOVE "FALSE" TO WS-RESULT.
           DISPLAY "ORDINAL 68 ORD-RANGE " WS-RESULT.
           MOVE SYM-69 TO WS-1.
           IF WS-1 IS ORD-RANGE MOVE "TRUE" TO WS-RESULT
               ELSE MOVE "FALSE" TO WS-RESULT.
           DISPLAY "ORDINAL 69 ORD-RANGE " WS-RESULT.
           STOP RUN.
