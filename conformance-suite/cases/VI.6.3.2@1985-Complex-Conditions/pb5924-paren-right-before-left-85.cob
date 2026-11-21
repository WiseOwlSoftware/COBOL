      *>--- conformance-case
      *> id: pb5924-paren-right-before-left-85
      *> rules: [1985-SR-VI.6.3.2.3-p3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:VI.6.3.2.3 (cite.py: OK): "There must be a one-to-one correspondence between left and right parentheses"
      *> A right parenthesis to the left of its left parenthesis in a condition
      *> ("A = 1 ) AND ( B = 2") is refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PARENO.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A PIC 9 VALUE 1.
       01  B PIC 9 VALUE 2.
       PROCEDURE DIVISION.
       P1.
           IF A = 1 ) AND ( B = 2
               DISPLAY "BOTH"
           END-IF.
           STOP RUN.
