      *>--- conformance-case
      *> id: 1985-FMT-VI_6_20_2
      *> rules: [1985-FMT-VI.6.20.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.20.2 (THE MULTIPLY STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. MULT1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A PIC 9 VALUE 3.
       01  B PIC 99 VALUE 4.
       01  C PIC 99 VALUE 5.
       01  D PIC 999.
       PROCEDURE DIVISION.
       P1.
           MULTIPLY A BY B C
               ON SIZE ERROR DISPLAY "SIZE"
               NOT ON SIZE ERROR DISPLAY "OK"
           END-MULTIPLY.
           DISPLAY B.
           DISPLAY C.
           MULTIPLY 6 BY 7 GIVING D.
           DISPLAY D.
           STOP RUN.
