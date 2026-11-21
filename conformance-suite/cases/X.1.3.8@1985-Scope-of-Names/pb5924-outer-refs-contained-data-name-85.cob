      *>--- conformance-case
      *> id: pb5924-outer-refs-contained-data-name-85
      *> rules: [1985-SR-X.1.3.8.2-p3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:X.1.3.8.2 (cite.py: OK): "A program cannot reference any condition-name"
      *> The containing program references a GLOBAL data-name declared in the program it contains: refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. OUTER.
       PROCEDURE DIVISION.
       P1.
           CALL "INNER".
           DISPLAY INNER-X.
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. INNER.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  INNER-X GLOBAL PIC X(3) VALUE "ABC".
       PROCEDURE DIVISION.
       P2.
           DISPLAY INNER-X.
           EXIT PROGRAM.
       END PROGRAM INNER.
       END PROGRAM OUTER.
