      *>--- conformance-case
      *> id: pb5123_not_is_simple_operator_2002
      *> rules: [2002-FMT-8.8.4.2.4.1, 2002-GR-8.8.4.2.4.3-2]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5123, review R1-1. §2002:8.8.4.2.4.1 prints
      *> "[ NOT ] simple-relational-operator" in the abbreviated tail,
      *> and §2002:8.8.4.2.4.3 GR2b: "If the relational operator
      *> following the word NOT is a simple relational operator, then
      *> the NOT participates as part of the simple relational
      *> operator". §2002:8.7.5.1 Format 1 spells the simple operators
      *> IS [NOT] GREATER THAN, IS [NOT] >, IS [NOT] LESS THAN,
      *> IS [NOT] <, IS [NOT] EQUAL TO and IS [NOT] =. So
      *>   A > B AND NOT IS > C OR D  is
      *>   ((A > B) AND (A NOT > C)) OR (A NOT > D):
      *> the negated operator is the stated one, carried to D (GR1).
      *> At 85 the NOT is logical (§1985:VI.6.3.3 GRL2.2) and > is
      *> carried; every leg below answers the other way under that
      *> reading. The 2014 format drops the bracketed NOT
      *> (§2014:8.8.4.2.4.1), so the program is 2002's alone.
      *> A = 5 throughout.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5123B.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A PIC 9 VALUE 5.
       01  B PIC 9.
       01  C PIC 9.
       01  D PIC 9.
       PROCEDURE DIVISION.
       MAIN-PARA.
      *> (F AND 5 NOT > 6) OR 5 NOT > 3 = F (85: T).
           MOVE 9 TO B. MOVE 6 TO C. MOVE 3 TO D.
           IF A > B AND NOT IS > C OR D DISPLAY "T1 TRUE"
           ELSE DISPLAY "T1 FALSE".
      *> (T AND 5 NOT > 4) OR 5 NOT > 7 = T (85: F).
           MOVE 1 TO B. MOVE 4 TO C. MOVE 7 TO D.
           IF A > B AND NOT IS GREATER THAN C OR D DISPLAY "T2 TRUE"
           ELSE DISPLAY "T2 FALSE".
      *> (F AND 5 NOT < 3) OR 5 NOT < 9 = F (85: T).
           MOVE 9 TO B. MOVE 3 TO C. MOVE 9 TO D.
           IF A > B AND NOT IS < C OR D DISPLAY "T3 TRUE"
           ELSE DISPLAY "T3 FALSE".
      *> (T AND 5 NOT < 6) OR 5 NOT < 9 = F (85: T).
           MOVE 1 TO B. MOVE 6 TO C. MOVE 9 TO D.
           IF A > B AND NOT IS LESS THAN C OR D DISPLAY "T4 TRUE"
           ELSE DISPLAY "T4 FALSE".
      *> (F AND 5 NOT = 1) OR 5 NOT = 5 = F (85: T).
           MOVE 9 TO B. MOVE 1 TO C. MOVE 5 TO D.
           IF A > B AND NOT IS = C OR D DISPLAY "T5 TRUE"
           ELSE DISPLAY "T5 FALSE".
      *> (T AND 5 NOT = 5) OR 5 NOT = 2 = T (85: F).
           MOVE 1 TO B. MOVE 5 TO C. MOVE 2 TO D.
           IF A > B AND NOT IS EQUAL TO C OR D DISPLAY "T6 TRUE"
           ELSE DISPLAY "T6 FALSE".
           STOP RUN.
