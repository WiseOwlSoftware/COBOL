      *>--- conformance-case
      *> id: pb5382-replace-match-crossing-debugging-lines
      *> rules: [1985-GR-XII.3.4-11]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XII.3.4 GR11 decides the indicator of the INTRODUCED lines only
      *> (from the line the replaced text begins on); the text after the
      *> replaced text keeps the indicator of the line the match ENDS on.
      *> Without WITH DEBUGGING MODE a debugging line is a comment.
      *> Match 1 begins on a debugging line and ends on a source line: R1 is
      *> introduced as a debugging line (hidden), DISPLAY "S1" is source and
      *> runs. Match 2 begins on a source line and ends on a debugging line:
      *> R2 is introduced as source and runs, DISPLAY "D1" is a comment. Why
      *> it can fail: an indicator taken from the line the match begins on
      *> hides S1 and runs D1 (kb/Work PB5382).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5382C.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. COMP1.
       DATA DIVISION.
       PROCEDURE DIVISION.
           REPLACE ==XX YY== BY ==DISPLAY "R1"==
                   ==PP QQ== BY ==DISPLAY "R2"==.
       MAIN-PARA.
           DISPLAY "S0".
      D    XX
           YY DISPLAY "S1".
           PP
      D    QQ DISPLAY "D1".
           DISPLAY "S2".
           STOP RUN.
