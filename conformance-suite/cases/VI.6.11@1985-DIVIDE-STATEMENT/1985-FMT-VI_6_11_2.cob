      *>--- conformance-case
      *> id: 1985-FMT-VI_6_11_2
      *> rules: [1985-FMT-VI.6.11.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.11.2 (THE DIVIDE STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WEAK original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DIVTST.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC 99 VALUE 10.
       01 B PIC 99 VALUE 22.
       01 C PIC 99 VALUE 0.
       01 D PIC 99 VALUE 0.
       01 Q PIC 99 VALUE 0.
       01 R PIC 99 VALUE 0.
       01 Z PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       P1.
           DIVIDE 4 INTO A B ROUNDED
               ON SIZE ERROR DISPLAY "F1 SIZE"
               NOT ON SIZE ERROR DISPLAY "F1 OK"
           END-DIVIDE.
           DISPLAY A " " B.
           DIVIDE Z INTO A
               ON SIZE ERROR DISPLAY "F1 SIZE".
           DISPLAY A.
           DIVIDE A INTO 7 GIVING C ROUNDED D.
           DISPLAY C " " D.
           DIVIDE 15 BY A GIVING C D ROUNDED.
           DISPLAY C " " D.
           DIVIDE 5 INTO 17 GIVING Q REMAINDER R.
           DISPLAY Q " " R.
           DIVIDE 19 BY 5 GIVING Q ROUNDED REMAINDER R
               ON SIZE ERROR DISPLAY "F5 SIZE"
               NOT ON SIZE ERROR DISPLAY "F5 OK"
           END-DIVIDE.
           DISPLAY Q " " R.
           DIVIDE 19 BY Z GIVING Q REMAINDER R
               ON SIZE ERROR DISPLAY "F5 SIZE"
               NOT ON SIZE ERROR DISPLAY "F5 OK"
           END-DIVIDE.
           DISPLAY Q " " R.
           STOP RUN.
