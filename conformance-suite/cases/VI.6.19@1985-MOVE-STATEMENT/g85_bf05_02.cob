      *>--- conformance-case
      *> id: g85_bf05_02
      *> rules: [1985-GR-VI.6.19.4-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      * §1985:VI.6.19.4 GR2 - one sender, several receivers:
      * R1 "MOVE a (b) TO b, c (b)" with A = 2 5 7, B = 1: temp = A (1)
      *    = 2, B = 2, then C (B) = C (2) = 2, so C-TBL = 020. A sender
      *    subscript re-evaluated per receiver gives 050; a receiver
      *    subscript evaluated before the statement gives 200.
      * R2 a reference-modified sender X (N:1) "is evaluated only once,
      *    immediately before data is moved to the first of the
      *    receiving operands": N = 1 so temp = "3", N becomes 3 and Y
      *    receives "3", not X (3:1) = "5".
      * R3 the length of a sender X (1:N) is evaluated once: N = 2 so
      *    temp = "34"; N (PIC 9) receives the integer 34 truncated to
      *    4; Y2 receives "34" padded, not "3456".
      * R4 the length of a receiver W (1:N) is evaluated "immediately
      *    before the data is moved to the respective data item": N
      *    becomes 2, then W (1:2) receives "2" padded, so W = "2 cd",
      *    not "2  d" (N = 3).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF0502.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A-TBL.
           02  A           PIC 9 OCCURS 3 TIMES.
       01  C-TBL.
           02  C           PIC 9 OCCURS 3 TIMES.
       01  B               PIC 9.
       01  N               PIC 9.
       01  X               PIC X(4) VALUE "3456".
       01  Y               PIC X.
       01  Y2              PIC X(4).
       01  W               PIC X(4).
       PROCEDURE DIVISION.
       P-MAIN.
           MOVE 2 TO A (1).
           MOVE 5 TO A (2).
           MOVE 7 TO A (3).
           MOVE ZERO TO C-TBL.
           MOVE 1 TO B.
           MOVE A (B) TO B, C (B).
           DISPLAY "R1=" B " " C-TBL.
           MOVE 1 TO N.
           MOVE X (N:1) TO N, Y.
           DISPLAY "R2=" N " " Y.
           MOVE 2 TO N.
           MOVE X (1:N) TO N, Y2.
           DISPLAY "R3=" N " [" Y2 "]".
           MOVE 3 TO N.
           MOVE "abcd" TO W.
           MOVE "2" TO N, W (1:N).
           DISPLAY "R4=" N " [" W "]".
           STOP RUN.
