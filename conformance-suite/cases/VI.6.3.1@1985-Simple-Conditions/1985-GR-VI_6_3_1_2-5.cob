      *>--- conformance-case
      *> id: 1985-GR-VI_6_3_1_2-5
      *> rules: [1985-GR-VI.6.3.1.2-5]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.3.1.2 GR5 (Simple Conditions). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the witnesses-85m drafting pass (a
      *> first draft for a 1985 rule no pass had drafted, its
      *> expectation derived from the rule's text and its citations
      *> checked with cite.py) by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CLSCND.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           CLASS HEXDIG IS "0" THRU "9" "A" THRU "F".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  N PIC 9 VALUE 0.
       01  G.
           05  E PIC X OCCURS 0 TO 5 TIMES DEPENDING ON N.
       01  W1 PIC X(4) VALUE "12 4".
       01  W2 PIC 9(3) VALUE 123.
       01  W3 PIC X(4) VALUE "ab c".
       01  W4 PIC S99 SIGN IS LEADING SEPARATE VALUE +12.
       01  W5 PIC X(3) VALUE "AbC".
       01  W6 PIC X(2) VALUE "1F".
       01  W7 PIC X(2) VALUE "1G".
       PROCEDURE DIVISION.
       MAIN-PARA.
           IF G NUMERIC DISPLAY "Z1 T" ELSE DISPLAY "Z1 F".
           IF G NOT NUMERIC DISPLAY "Z2 T" ELSE DISPLAY "Z2 F".
           IF G ALPHABETIC DISPLAY "Z3 T" ELSE DISPLAY "Z3 F".
           IF G NOT ALPHABETIC DISPLAY "Z4 T" ELSE DISPLAY "Z4 F".
           IF G HEXDIG DISPLAY "Z5 T" ELSE DISPLAY "Z5 F".
           IF W1 NUMERIC DISPLAY "W1 T" ELSE DISPLAY "W1 F".
           IF W2 NUMERIC DISPLAY "W2 T" ELSE DISPLAY "W2 F".
           IF W3 ALPHABETIC-LOWER DISPLAY "W3 T" ELSE DISPLAY "W3 F".
           IF W3 ALPHABETIC-UPPER DISPLAY "W3U T" ELSE DISPLAY "W3U F".
           IF W4 NUMERIC DISPLAY "W4 T" ELSE DISPLAY "W4 F".
           IF W5 ALPHABETIC DISPLAY "W5 T" ELSE DISPLAY "W5 F".
           IF W6 HEXDIG DISPLAY "W6 T" ELSE DISPLAY "W6 F".
           IF W7 HEXDIG DISPLAY "W7 T" ELSE DISPLAY "W7 F".
           IF FUNCTION UPPER-CASE (W3) ALPHABETIC-UPPER
               DISPLAY "FN T" ELSE DISPLAY "FN F".
           MOVE 3 TO N.
           MOVE "ABC" TO G.
           IF G ALPHABETIC-UPPER DISPLAY "G3 T" ELSE DISPLAY "G3 F".
           STOP RUN.
