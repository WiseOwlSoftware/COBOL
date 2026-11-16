      *>--- conformance-case
      *> id: g85_bf40_03
      *> rules: [1989-RV-A.2.25.4-1, 1989-RV-A.2.25.4-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.25.4 RV1 - the content of the argument-1 with the
      *> middle value once all are sorted; §1989:A.2.25.4 RV3 - the sort
      *> compares by the rules for simple conditions (algebraic value).
      *> §1985:VI.6.3.1.1.1 - numeric operands compare by algebraic
      *> value; the length of a literal is not significant. MED1 5 1 3
      *> sorts 1 3 5; MED2 10 9.5 -20 sorts -20 9.5 10 (a text or
      *> unsigned sort picks another); MED3 TE(ALL) 9 7 1 5 3 sorts 1 3
      *> 5 7 9. An unsorted middle (the 2nd written) fails each line.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF4003.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 NEG20 PIC S9(2) VALUE -20.
       01 N1 PIC S9(3) VALUE -5.
       01 N2 PIC S9(3)V9 VALUE +3.5.
       01 N3 PIC 9(1) VALUE 2.
       01 R PIC -9(3).9(3).
       01 I PIC 9.
       01 TV PIC X(10) VALUE "0907010503".
       01 T REDEFINES TV.
           05 TE PIC 99 OCCURS 5 TIMES.
       PROCEDURE DIVISION.
       P1.
           COMPUTE R = FUNCTION MEDIAN (5 1 3).
           DISPLAY "MED1 " R.
           COMPUTE R = FUNCTION MEDIAN (10 9.5 NEG20).
           DISPLAY "MED2 " R.
           COMPUTE R = FUNCTION MEDIAN (TE (ALL)).
           DISPLAY "MED3 " R.
           STOP RUN.
