      *>--- conformance-case
      *> id: pb5419-signed-literal-opposite-group
      *> rules: [1985-GR-VI.6.3.1.1.2-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5419. §1985:VI.6.3.1.1.2 GR2: "If the nonnumeric
      *> operand is a group item, the numeric operand is treated as
      *> though it were moved to a group item of the same size as the
      *> numeric data item (in terms of standard data format
      *> characters)", and the size of a numeric literal "in standard
      *> data format characters is equal to the number of digits"
      *> (§1985:IV.4.2.2.2.2 GR4). The literal -5 is one character,
      *> "5", and +5 and -12 are the digit runs 5 and 12 whatever the
      *> sign, the group image a MOVE of the literal to a group stores
      *> (MOVE -5 TO G2 leaves "5 "). Opposite a group of the same size
      *> the comparison is therefore equal for the group "5" and "5 "
      *> and "12 " and not equal for "-5" and "-12", with the literal
      *> on either side of the relation.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5419A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  G1.
           05  G1A PIC X.
       01  G2.
           05  G2A PIC X(2).
       01  G3.
           05  G3A PIC X(3).
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE "5" TO G1A.
           IF G1 = -5 DISPLAY "A EQ" ELSE DISPLAY "A NE".
           MOVE "-5" TO G2A.
           IF G2 = -5 DISPLAY "B EQ" ELSE DISPLAY "B NE".
           IF -5 = G2 DISPLAY "C EQ" ELSE DISPLAY "C NE".
           MOVE "5 " TO G2A.
           IF G2 = -5 DISPLAY "D EQ" ELSE DISPLAY "D NE".
           IF G2 = +5 DISPLAY "E EQ" ELSE DISPLAY "E NE".
           IF +5 = G2 DISPLAY "F EQ" ELSE DISPLAY "F NE".
           MOVE "-12" TO G3A.
           IF G3 = -12 DISPLAY "G EQ" ELSE DISPLAY "G NE".
           MOVE "12 " TO G3A.
           IF G3 = -12 DISPLAY "H EQ" ELSE DISPLAY "H NE".
           IF G3 > -11 DISPLAY "I GT" ELSE DISPLAY "I NOT GT".
           MOVE -5 TO G2.
           DISPLAY "J [" G2 "]".
      * The same comparison nested in the ELSE of another condition.
           IF G1 = "X"
               DISPLAY "K EQ X"
           ELSE
               IF G2 = -5 DISPLAY "K EQ" ELSE DISPLAY "K NE"
           END-IF.
           STOP RUN.
