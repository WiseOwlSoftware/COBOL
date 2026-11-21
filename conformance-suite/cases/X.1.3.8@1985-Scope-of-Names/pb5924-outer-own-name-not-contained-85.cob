      *>--- conformance-case
      *> id: pb5924-outer-own-name-not-contained-85
      *> rules: [1985-SR-X.1.3.8.2-p3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:X.1.3.8.2 (cite.py: OK): "A program cannot reference any condition-name"
      *> Both programs declare SHARED-X (the contained one GLOBAL): each program's reference is to its own item,
      *> never to the one declared in the program it contains.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. OUTER.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  SHARED-X PIC X(5) VALUE "OUTER".
       PROCEDURE DIVISION.
       P1.
           CALL "INNER".
           DISPLAY SHARED-X.
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. INNER.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  SHARED-X GLOBAL PIC X(5) VALUE "INNER".
       PROCEDURE DIVISION.
       P2.
           DISPLAY SHARED-X.
           EXIT PROGRAM.
       END PROGRAM INNER.
       END PROGRAM OUTER.
