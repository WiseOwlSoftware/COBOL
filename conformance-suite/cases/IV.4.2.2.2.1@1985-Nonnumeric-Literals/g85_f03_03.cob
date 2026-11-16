      *>--- conformance-case
      *> id: g85_f03_03
      *> rules: [1985-SR-IV.4.2.2.2.1.2-2, 1985-GR-IV.4.2.2.2.1.3-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.2.2.2.1.2 SR2 - two contiguous quotation marks in a
      *> nonnumeric literal stand for one quotation mark (lines 1-3, 5).
      *> §1985:IV.4.2.2.2.1.3 GR2 - the delimiting quotation marks are not
      *> part of the literal's value (lines 4, 6): a two-character
      *> literal moved to a four-character item is padded, not quoted.
      *> Each leg fails if the doubled mark is kept as two characters, if
      *> the delimiters are kept in the value, or if the program is
      *> refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F0303.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  Q1 PIC X VALUE SPACE.
       01  Q3 PIC X(3) VALUE SPACES.
       01  X4 PIC X(4) VALUE SPACES.
       01  X4B PIC X(4) VALUE "ABxx".
       PROCEDURE DIVISION.
       P1.
           DISPLAY "A""B".
           MOVE """" TO Q1.
           DISPLAY "[" Q1 "]".
           MOVE "X""Y" TO Q3.
           DISPLAY "[" Q3 "]".
           MOVE "AB" TO X4.
           DISPLAY "[" X4 "]".
           IF Q3 = "X""Y"
               DISPLAY "DOUBLED EQUAL"
           ELSE
               DISPLAY "DOUBLED NOT EQUAL"
           END-IF.
           IF X4 (1:2) = X4B (1:2) AND X4 (3:2) = SPACES
               DISPLAY "DELIMITERS NOT IN VALUE"
           ELSE
               DISPLAY "DELIMITERS IN VALUE"
           END-IF.
           STOP RUN.
