      *>--- conformance-case
      *> id: 1985-SR-IV_4_2_1-p1
      *> rules: [1985-SR-IV.4.2.1-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.2.1 p1 (LANGUAGE STRUCTURE). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-99.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * PB5453 s-99: 1985-SR-IV.4.2.1-p1, a separator is one character
      * or two contiguous characters (space; comma, semicolon, period
      * each with a space; parentheses; quotation mark; colon;
      * pseudo-text delimiter ==).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G1SEP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A   PIC 9(2) VALUE 3.
       01  B   PIC 9(2) VALUE 4.
       01  C   PIC 9(2) VALUE 0.
       01  T   PIC X(5) VALUE "ABCDE".
       01  R   PIC X(2) VALUE SPACES.
       01  TB.
           05  E  PIC 9 OCCURS 3 TIMES.
       PROCEDURE DIVISION.
       P1.
           REPLACE ==XADD== BY ==ADD==.
           MOVE 1 TO E (1); MOVE 2 TO E (2), MOVE 3 TO E (3).
           XADD A, B GIVING C.
           MOVE T (2:2) TO R.
           REPLACE OFF.
           IF C = 7 AND R = "BC" AND E (3) = 3
               DISPLAY "SEP-OK"
           ELSE
               DISPLAY "SEP-BAD " C " " R.
           STOP RUN.
