      *>--- conformance-case
      *> id: 1985-GR-VI_6_3_3-L2_2
      *> rules: [1985-GR-VI.6.3.3-L2.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.3.3 GRL2.2 (CONDITIONAL EXPRESSIONS). An output
      *> test: the program must translate and run at 85 and DISPLAY
      *> exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from written and run by the witnesses-85m
      *> drafting pass (a first draft for a 1985 rule no pass had
      *> drafted, its expectation derived from the rule's text and its
      *> citations checked with cite.py) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. ABBREV.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A PIC 9.
       01  B PIC 9.
       01  C PIC 9.
       01  D PIC 9.
       01  R PIC X(5).
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE 5 TO A. MOVE 3 TO B. MOVE 7 TO C. MOVE 4 TO D.
           PERFORM EVAL-PARA.
           MOVE 2 TO A. MOVE 3 TO B. MOVE 1 TO C. MOVE 9 TO D.
           PERFORM EVAL-PARA.
           MOVE 5 TO A. MOVE 5 TO B. MOVE 5 TO C. MOVE 5 TO D.
           PERFORM EVAL-PARA.
           MOVE 3 TO A. MOVE 5 TO B. MOVE 6 TO C. MOVE 1 TO D.
           PERFORM EVAL-PARA.
           MOVE 5 TO A. MOVE 5 TO B. MOVE 7 TO C. MOVE 9 TO D.
           PERFORM EVAL-PARA.
           STOP RUN.
       EVAL-PARA.
           MOVE "FFFFF" TO R.
           IF A > B AND NOT < C OR D
               MOVE "T" TO R (1:1).
           IF A NOT EQUAL B OR C
               MOVE "T" TO R (2:1).
           IF NOT A = B OR C
               MOVE "T" TO R (3:1).
           IF NOT (A GREATER B OR < C)
               MOVE "T" TO R (4:1).
           IF NOT (A NOT > B AND C AND NOT D)
               MOVE "T" TO R (5:1).
           DISPLAY A B C D " " R.
