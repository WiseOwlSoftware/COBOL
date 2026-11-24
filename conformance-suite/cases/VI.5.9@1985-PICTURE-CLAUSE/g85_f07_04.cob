      *>--- conformance-case
      *> id: g85_f07_04
      *> rules: [1985-SR-VI.5.9.3-1]
      *> accept: [85]
      *> source-format: fixed
      *>---
      *> §1985:VI.5.9.3 SR1 - "A PICTURE clause can be specified only
      *> at the elementary item level." Every PICTURE below is on an
      *> elementary item (levels 05 and 10); the groups WS-G and WS-S
      *> carry none, so each group's size is the sum of its elementary
      *> items. The compiler must accept the program. Line 1 shows the
      *> group WS-G as its elementary items in order: "AB", 012
      *> (PIC 999 VALUE 12) and the subgroup "XY" 7; line 2 the
      *> subgroup alone; line 3 an elementary item after a MOVE. A
      *> compiler that refuses a PICTURE on an elementary item fails.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F0704.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-G.
           05  WS-A    PIC XX   VALUE "AB".
           05  WS-N    PIC 999  VALUE 12.
           05  WS-S.
               10  WS-X  PIC XX VALUE "XY".
               10  WS-D  PIC 9  VALUE 7.
       01  WS-N-E PIC ZZ9.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "[" WS-G "]".
           DISPLAY "[" WS-S "]".
           MOVE 345 TO WS-N.
           MOVE WS-N TO WS-N-E
           DISPLAY "[" WS-N-E "]".
           STOP RUN.
