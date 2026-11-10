      *>--- conformance-case
      *> id: 1985-FMT-VI_6_16_2
      *> rules: [1985-FMT-VI.6.16.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.16.2 (THE IF STATEMENT). An output test: the program
      *> must translate and run at 85 and DISPLAY exactly the lines its
      *> .out holds, the outcome the rule requires. Placed from the
      *> confirmation pass's redraft (WEAK original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. IFTEST.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC 9 VALUE 1.
       01 B PIC 9 VALUE 2.
       PROCEDURE DIVISION.
       P1.
           IF A = 1 THEN
               DISPLAY "YES"
           ELSE
               DISPLAY "NO"
           END-IF.
           IF A = 2 DISPLAY "WRONG-1" END-IF DISPLAY "AFTER END-IF".
           IF A = 2 NEXT SENTENCE ELSE DISPLAY "ELSE-1".
           IF A = 1 NEXT SENTENCE ELSE DISPLAY "WRONG-2".
           IF A = 1 DISPLAY "THEN-2" ELSE NEXT SENTENCE.
           IF A = 2 DISPLAY "WRONG-3" ELSE NEXT SENTENCE.
           IF A = 1 DISPLAY "THEN-3".
           IF A = 1
               IF B = 1
                   DISPLAY "WRONG-4"
               ELSE
                   DISPLAY "INNER ELSE"
           ELSE
               DISPLAY "WRONG-5".
           IF A = 2
               IF B = 2
                   DISPLAY "WRONG-6"
               ELSE
                   DISPLAY "WRONG-7"
               END-IF
           ELSE
               DISPLAY "OUTER ELSE"
           END-IF.
           IF A = 1
               DISPLAY "OUTER"
               IF B = 2
                   NEXT SENTENCE
               ELSE
                   DISPLAY "WRONG-8".
           DISPLAY "DONE".
           STOP RUN.
