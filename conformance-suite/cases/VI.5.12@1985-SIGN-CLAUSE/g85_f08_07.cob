      *>--- conformance-case
      *> id: g85_f08_07
      *> rules: [1985-GR-VI.5.12.4-6]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.5.12.4 GR6 - with SEPARATE CHARACTER: a. the sign is
      *> the leading (or trailing) character position, "this character
      *> position is not a digit position"; b. "The letter 'S' in a
      *> PICTURE character-string is counted in determining the size of
      *> the item"; c. "The operational signs for positive and negative
      *> are the standard data format characters '+' and '-',
      *> respectively."
      *> Expected (derived):
      *>   G1 = L S9(3) LEADING SEPARATE + T S9(3) TRAILING SEPARATE +
      *>     "|".
      *>   L=-12, T=45:  "-012" + "045+" + "|" = "-012045+|" (each item
      *>     4
      *>   positions: 3 digits and the sign).
      *>   L=7, T=-8:    "+007" + "008-" + "|" = "+007008-|".
      *> A leg fails on a wrong sign character, position, or width.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F0807.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  G1.
           05  L              PIC S9(3) SIGN IS LEADING SEPARATE
                              CHARACTER VALUE -12.
           05  T              PIC S9(3) SIGN IS TRAILING SEPARATE
                              CHARACTER VALUE 45.
           05  M1             PIC X VALUE "|".
       PROCEDURE DIVISION.
       P1.
           DISPLAY "G1=[" G1 "]".
           MOVE 7 TO L.
           MOVE -8 TO T.
           DISPLAY "G1=[" G1 "]".
           STOP RUN.
