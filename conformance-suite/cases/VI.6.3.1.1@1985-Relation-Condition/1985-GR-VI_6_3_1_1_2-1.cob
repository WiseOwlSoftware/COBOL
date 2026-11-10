      *>--- conformance-case
      *> id: 1985-GR-VI_6_3_1_1_2-1
      *> rules: [1985-GR-VI.6.3.1.1.2-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.3.1.1.2 GR1 (Relation Condition). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WEAK original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RELCOND.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  NUM PIC 9(3) VALUE 12.
       01  SN  PIC S9(3) SIGN IS LEADING SEPARATE CHARACTER
               VALUE -12.
       01  A4  PIC X(4) VALUE "-012".
       PROCEDURE DIVISION.
       MAIN-PARA.
           IF NUM = "012"
               DISPLAY "EQUAL-1"
           ELSE
               DISPLAY "NOT-EQUAL-1".
           IF NUM = "12"
               DISPLAY "EQUAL-2"
           ELSE
               DISPLAY "NOT-EQUAL-2".
           IF SN = "012"
               DISPLAY "EQUAL-3"
           ELSE
               DISPLAY "NOT-EQUAL-3".
           IF SN = A4
               DISPLAY "EQUAL-4"
           ELSE
               DISPLAY "NOT-EQUAL-4".
           STOP RUN.
