      *>--- conformance-case
      *> id: 1985-GR-VI_6_18_4-5
      *> rules: [1985-GR-VI.6.18.4-5]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.18.4 GR5 (THE INSPECT STATEMENT). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the witness-drafting pass (a rule the
      *> audit never drafted) by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. INSPONCE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  D4  PIC X(4).
       01  I   PIC 9.
       01  CNT-TABLE.
           05  CNT  PIC 9 OCCURS 3 TIMES.
       01  ROW-TABLE.
           05  ROW  PIC X(3) OCCURS 3 TIMES.
       01  LIT-TABLE.
           05  LIT  PIC X OCCURS 3 TIMES.
       01  C1  PIC 99.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE "BABA" TO D4.
           MOVE ZERO TO CNT (1) CNT (2) CNT (3).
           MOVE 1 TO I.
           INSPECT D4 TALLYING I FOR ALL "B"
                               CNT (I) FOR ALL "A".
           DISPLAY "COUNTER " I " " CNT (1) CNT (2) CNT (3).
           MOVE "AABBBBAAA" TO ROW-TABLE.
           MOVE 1 TO I.
           INSPECT ROW (I) TALLYING I FOR ALL "A".
           DISPLAY "SUBJECT " I.
           MOVE "XAXA" TO D4.
           MOVE "ABB" TO LIT-TABLE.
           MOVE 1 TO I.
           MOVE ZERO TO C1.
           INSPECT D4 TALLYING I FOR ALL "X"
                               C1 FOR ALL LIT (I).
           DISPLAY "OPERAND " I " " C1.
           MOVE "abb" TO LIT-TABLE.
           MOVE 1 TO I.
           MOVE ZERO TO C1.
           INSPECT D4 TALLYING I FOR ALL "X"
               C1 FOR ALL FUNCTION UPPER-CASE (LIT (I)).
           DISPLAY "FUNCTION " I " " C1.
           STOP RUN.
