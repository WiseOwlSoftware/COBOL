      *>--- conformance-case
      *> id: g85_f08_02
      *> rules: [1985-GR-VI.5.9.5-5]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.5.9.5 GR5 - special insertion editing: "The '.'
      *> (period) is used as the insertion character. In addition to
      *> being an insertion character it also represents the decimal
      *> point for alignment purposes. The insertion character used for
      *> the actual decimal point is counted in the size of the item." A
      *> final '.' is followed by the separator period ("two consecutive
      *> periods"), and "the appearance of the insertion character in
      *> the item in the same position as shown in the
      *> character-string."
      *> Expected (derived):
      *>   E1 PIC 999.99 <- 12.34: aligned on the '.', "012.34"; G1
      *>     shows E1 then "|", so "012.34|" proves the '.' is counted
      *>     (6 chars).
      *>   E1 <- 5.6: "005.60".
      *>   E2 PIC 9.999 <- 12.34: one integer place, "2.340".
      *>   E3 PIC 99.. (final '.', then the separator) <- 12.34: "12.".
      *> A leg fails if the '.' is not counted (G1 shifts), if alignment
      *> is not on the '.', or if "99.." is refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F0802.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  SRC                PIC 9(3)V99 VALUE 12.34.
       01  SRC2               PIC 9V9 VALUE 5.6.
       01  G1.
           05  E1             PIC 999.99.
           05  M1             PIC X VALUE "|".
       01  E2                 PIC 9.999.
       01  E3                 PIC 99..
       PROCEDURE DIVISION.
       P1.
           MOVE SRC TO E1.
           DISPLAY "G1=[" G1 "]".
           MOVE SRC2 TO E1.
           DISPLAY "E1=[" E1 "]".
           MOVE SRC TO E2.
           DISPLAY "E2=[" E2 "]".
           MOVE SRC TO E3.
           DISPLAY "E3=[" E3 "]".
           STOP RUN.
