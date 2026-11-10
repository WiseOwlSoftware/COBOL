      *>--- conformance-case
      *> id: 1985-GR-IV_6_4_2_1_1-7
      *> rules: [1985-GR-IV.6.4.2.1.1-7]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.6.4.2.1.1 GR7 (Conditional Statements and Sentences).
      *> An output test: the program must translate and run at 85 and
      *> DISPLAY exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. STRCOND.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  D PIC X(3).
       01  E PIC X(5).
       PROCEDURE DIVISION.
       P1.
           STRING "ABCDE" DELIMITED BY SIZE INTO D
               ON OVERFLOW DISPLAY "OVF"
               NOT ON OVERFLOW DISPLAY "NOOVF".
           DISPLAY D.
           STRING "XY" DELIMITED BY SIZE INTO E
               ON OVERFLOW DISPLAY "OVF2"
               NOT ON OVERFLOW DISPLAY "NOOVF2".
           STOP RUN.
