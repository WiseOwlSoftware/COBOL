      *>--- conformance-case
      *> id: pb5482-contained-program-text-name
      *> rules: [1985-SR-VI.5.8.3-14]
      *> accept: [85]
      *> files: [pb5482tx.cpy]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.2.2.1.1 (cite.py --check: OK): "Within a given source
      *> program, but excluding any contained program, the user-defined
      *> words are grouped into the following disjoint sets". The outer
      *> program's data-name PB5482TX is a word of the outer program; the
      *> contained program's COPY pb5482tx names a text-name of the
      *> CONTAINED program, another source program, so no word is in two
      *> sets and the program runs: the outer displays X, the contained
      *> program displays the copied Y (kb/Work PB5482: the text-names are
      *> assigned to the program they stand in).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5482B.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  PB5482TX PIC X VALUE "X".
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY PB5482TX.
           CALL "PB5482C".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5482C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       COPY pb5482tx.
       PROCEDURE DIVISION.
       INNER-PARA.
           DISPLAY PB5482Y.
           EXIT PROGRAM.
       END PROGRAM PB5482C.
       END PROGRAM PB5482B.
