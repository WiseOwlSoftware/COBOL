      *>--- conformance-case
      *> id: 1985-FMT-VI_6_8_2
      *> rules: [1985-FMT-VI.6.8.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.8.2 (THE COMPUTE STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the triage of the drafts that did not
      *> match (a suspect draft corrected) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CMPT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A PIC 9 VALUE 0.
       01  B PIC 9V9 VALUE 0.
       01  B-ED PIC 9.9.
       01  C PIC 99 VALUE 5.
       PROCEDURE DIVISION.
       P1.
           COMPUTE A ROUNDED B = 7 / 2
               ON SIZE ERROR DISPLAY "SIZE1"
               NOT ON SIZE ERROR DISPLAY "OK1"
           END-COMPUTE.
           DISPLAY A.
           MOVE B TO B-ED.
           DISPLAY B-ED.
           COMPUTE C = 100
               ON SIZE ERROR DISPLAY "SIZE2"
               NOT ON SIZE ERROR DISPLAY "OK2"
           END-COMPUTE.
           DISPLAY C.
           STOP RUN.
