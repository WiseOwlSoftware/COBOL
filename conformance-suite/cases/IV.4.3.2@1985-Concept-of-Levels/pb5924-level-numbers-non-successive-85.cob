      *>--- conformance-case
      *> id: pb5924-level-numbers-non-successive-85
      *> rules: [1985-SR-IV.4.3.2.1-p2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:IV.4.3.2.1 (cite.py: OK): "must be described using identical level-numbers"
      *> Identical, non-successive level-numbers greater than the
      *> group's (05/10, 07/49) describe the record as written.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. LVLNSU.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  G.
           05  A.
               10  A1        PIC X VALUE "1".
               10  A2        PIC X VALUE "2".
           05  B             PIC X VALUE "B".
       01  H.
           07  H1            PIC XX VALUE "H1".
           07  H2.
               49  H21       PIC X VALUE "Z".
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY G.
           DISPLAY A.
           DISPLAY H.
           STOP RUN.
