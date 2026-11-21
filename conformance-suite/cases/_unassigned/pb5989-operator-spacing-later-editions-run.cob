      *>--- conformance-case
      *> id: pb5989-operator-spacing-later-editions-run
      *> rules: []
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5989. The spacing sentences of §1985:VI.6.2.2 (unary operator and parenthesis),
      *> §1985:VI.6.3.1.1 (relational operator) and §1985:VI.6.3.2 (logical operators) have no counterpart in
      *> 2002, 2014 or 2023: §2002:8.7.1 (cite.py: OK) "no space is required between a left parenthesis and a
      *> unary operator or between a unary operator and a left parenthesis", and no clause asks a space around a
      *> relation character or AND, OR, NOT. The shapes the 85 refusals pin compile and run.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB89LTR.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  N  PIC S9(3) VALUE 5.
       01  M  PIC S9(3) VALUE 2.
       PROCEDURE DIVISION.
       P0.
           COMPUTE N = -(M) + 9.
           IF (N=7)AND(M=2) DISPLAY "SEVEN".
           IF NOT(N=1)OR(M=9) DISPLAY "NOT-ONE".
           STOP RUN.
