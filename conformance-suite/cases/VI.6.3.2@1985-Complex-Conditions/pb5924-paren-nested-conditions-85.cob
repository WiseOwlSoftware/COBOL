      *>--- conformance-case
      *> id: pb5924-paren-nested-conditions-85
      *> rules: [1985-SR-VI.6.3.2.3-p3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:VI.6.3.2.3 (cite.py: OK): "There must be a one-to-one correspondence between left and right parentheses"
      *> Properly paired nested parentheses compile and bind as written; without
      *> them NOT, AND, OR precedence applies (T3).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PARENOK.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A PIC 9 VALUE 1.
       01  B PIC 9 VALUE 2.
       01  C PIC 9 VALUE 3.
       PROCEDURE DIVISION.
       P1.
           IF ((A = 1) AND ((NOT B = 9) OR (C = 0)))
               DISPLAY "T1"
           ELSE
               DISPLAY "F1"
           END-IF.
           IF (A = 0 OR (NOT B = 2)) AND C = 3
               DISPLAY "T2"
           ELSE
               DISPLAY "F2"
           END-IF.
           IF A = 1 OR NOT B = 2 AND C = 0
               DISPLAY "T3"
           ELSE
               DISPLAY "F3"
           END-IF.
           STOP RUN.
