      *>--- conformance-case
      *> id: pb5867_picture_separator_85
      *> rules: [1985-SR-IV.4.2.1-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5867 - §1985:IV.4.2.1 SR2: "the punctuation
      *> characters comma and semicolon, immediately followed by a
      *> space, are separators that may be used anywhere the
      *> separator space is used". The word PICTURE (PIC, and its IS)
      *> and the character-string are separated by the separator
      *> space, so a comma or semicolon and a space may stand there.
      *> The test is the space after it: in PIC ,999 the comma is the
      *> string's first character, the comma insertion symbol.
      *>   A  PIC, X(3)          X(3)   "ABC"            A=ABC
      *>   B  PICTURE; 9(3)      9(3)   12               B=012
      *>   C  PICTURE IS, X(2)   X(2)   "CD"             C=CD
      *>   D  PIC IS; 99         99     7                D=07
      *>   E  PIC ,999           ,999   MOVE 5: ",005"   E=,005
      *>   F  PICTURE , 9(2)     9(2)   4                F=04
      *>   G  PIC 9,999;         9,999  MOVE 1234        G=1,234
      *>   I  PIC, at line end   9(2)   55               I=55
      *> The defect refused A to D, F and I (COBOL0001 or COBOL2419).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5867A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A PIC, X(3) VALUE "ABC".
       01  B PICTURE; 9(3) VALUE 12.
       01  C PICTURE IS, X(2) VALUE "CD".
       01  D PIC IS; 99 VALUE 7.
       01  E PIC ,999.
       01  F PICTURE , 9(2) VALUE 4.
       01  G PIC 9,999; USAGE DISPLAY.
       01  I PIC,
               9(2) VALUE 55.
       PROCEDURE DIVISION.
       P1.
           MOVE 5 TO E.
           MOVE 1234 TO G.
           DISPLAY "A=" A " B=" B " C=" C " D=" D.
           DISPLAY "E=" E " F=" F " G=" G.
           DISPLAY "I=" I.
           STOP RUN.
