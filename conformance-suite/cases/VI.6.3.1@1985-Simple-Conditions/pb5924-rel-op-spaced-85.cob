      *>--- conformance-case
      *> id: pb5924-rel-op-spaced-85
      *> rules: [1985-SR-VI.6.3.1.1-p4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:VI.6.3.1.1 (cite.py: OK): "A space must
      *> precede and follow each reserved word comprising the relational
      *> operator"
      *> Each relational operator, words and relation characters, with a
      *> space before and after: compiles and compares.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5924Q.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  N  PIC 9(3) VALUE 5.
       01  M  PIC 9(3) VALUE 2.
       PROCEDURE DIVISION.
       P0.
           IF N > M DISPLAY "T1 Y" ELSE DISPLAY "T1 N".
           IF N NOT = M DISPLAY "T2 Y" ELSE DISPLAY "T2 N".
           IF N >= ( M ) DISPLAY "T3 Y" ELSE DISPLAY "T3 N".
           IF N IS NOT GREATER THAN M DISPLAY "T4 Y"
               ELSE DISPLAY "T4 N".
           IF N LESS M DISPLAY "T5 Y" ELSE DISPLAY "T5 N".
           STOP RUN.
