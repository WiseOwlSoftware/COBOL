      *>--- conformance-case
      *> id: pb5924-cfg-contained-85
      *> rules: [1985-SR-VI.4.2-p3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:VI.4.2 (cite.py: OK): "The Configuration
      *> Section must not be stated in a program which is contained
      *> directly or indirectly within another program"
      *> A program contained within another (here two levels deep)
      *> states a Configuration Section: refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB59241.
       PROCEDURE DIVISION.
       P0.
           CALL "PB5924M".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5924M.
       PROCEDURE DIVISION.
       P1.
           CALL "PB5924I".
           EXIT PROGRAM.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5924I.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER. WISEOWL.
       PROCEDURE DIVISION.
       P2.
           DISPLAY "INNER".
           EXIT PROGRAM.
       END PROGRAM PB5924I.
       END PROGRAM PB5924M.
       END PROGRAM PB59241.
