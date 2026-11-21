      *>--- conformance-case
      *> id: pb5924-outer-refs-contained-condition-name-85
      *> rules: [1985-SR-X.1.3.8.2-p3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:X.1.3.8.2 (cite.py: OK): "A program cannot reference any condition-name"
      *> The containing program references a condition-name declared in the program it contains: refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. OUTER.
       PROCEDURE DIVISION.
       P1.
           CALL "INNER".
           IF INNER-YES DISPLAY "YES".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. INNER.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  INNER-F GLOBAL PIC X VALUE "Y".
           88  INNER-YES VALUE "Y".
       PROCEDURE DIVISION.
       P2.
           EXIT PROGRAM.
       END PROGRAM INNER.
       END PROGRAM OUTER.
