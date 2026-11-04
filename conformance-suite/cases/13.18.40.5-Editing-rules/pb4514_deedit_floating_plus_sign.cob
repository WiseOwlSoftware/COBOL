      *>--- conformance-case
      *> id: pb4514_deedit_floating_plus_sign
      *> rules: [GR-13.18.40.5-6, GR-14.9.25.4-6]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4514 - de-editing a numeric-edited item recovers the sign of a FLOATING '+' string as
      *> well as of a floating '-' one.
      *> ISO/IEC 1989:2023 14.9.25.4 general rule 6 d) 1: "If the category of the sending operand is
      *> numeric-edited, the content of the sending operand shall be as specified in 14.6.13.2, Incompatible
      *> data, and de-editing establishes the operand's numeric value, which may be signed." The value an
      *> image holds is the one editing produced it from, so the sign is the one 13.18.40.5 rule 5 / rule 6
      *> / Table 9 put there: the floating '+' symbol is '+' for a positive or zero value and '-' for a
      *> negative one, the floating '-' symbol is a space for a positive or zero value and '-' for a negative
      *> one, and either lands at the rightmost suppressed position of its string (rule 6 a).
      *> Until kb/Work PB4514 the de-edit read the sign of a floating string only when the mask had a
      *> floating '-', so a negative PIC ++++9.99 image moved to a numeric item came out positive.
      *>
      *> The receiver is a SIGN LEADING SEPARATE numeric item, read back through a numeric-edited PIC
      *> +9(5).99 so the printed sign is the stored sign and nothing else. Each expected line is derived
      *> from the rules above, not from the compiler:
      *>   A  -1234.5 -> PIC ++++9.99: four integer digit positions, the leading '+' is the symbol itself and
      *>      lands at the leftmost position since 1234 fills the digit positions: "-1234.50"; de-edited
      *>      -1234.50 stored in S9(5)V99, printed "-01234.50".
      *>   B  1234.5 -> "+1234.50", printed "+01234.50" (the positive control that always worked).
      *>   C  -5.25 -> "   -5.25": the sign lands at the rightmost suppressed position, immediately left
      *>      of the fixed '9'; de-edited -5.25 -> "-00005.25".
      *>   D  -0.5 -> "   -0.50"; -> "-00000.50".
      *>   E  0 -> "   +0.00" (zero takes the positive-or-zero column of Table 9); de-edited zero is
      *>      unsigned -> "+00000.00".
      *>   F  -1234.5 -> PIC ----9.99: "-1234.50" -> "-01234.50" (the floating '-' control).
      *>   G  1234.5 -> PIC ----9.99: " 1234.50" -> "+01234.50".
      *>   H  -5.25 -> PIC ----9.99: "   -5.25" -> "-00005.25".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4514DEEDF.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-EP    PIC ++++9.99.
       01 WS-EM    PIC ----9.99.
       01 WS-N     PIC S9(5)V99 SIGN LEADING SEPARATE.
       01 WS-OUT   PIC +9(5).99.
       PROCEDURE DIVISION.
       PB4514-LEAD.
           MOVE -1234.5 TO WS-EP
           MOVE WS-EP TO WS-N
           MOVE WS-N TO WS-OUT
           DISPLAY "A[" WS-EP "] [" WS-OUT "]"
           MOVE 1234.5 TO WS-EP
           MOVE WS-EP TO WS-N
           MOVE WS-N TO WS-OUT
           DISPLAY "B[" WS-EP "] [" WS-OUT "]"
           MOVE -5.25 TO WS-EP
           MOVE WS-EP TO WS-N
           MOVE WS-N TO WS-OUT
           DISPLAY "C[" WS-EP "] [" WS-OUT "]"
           MOVE -0.5 TO WS-EP
           MOVE WS-EP TO WS-N
           MOVE WS-N TO WS-OUT
           DISPLAY "D[" WS-EP "] [" WS-OUT "]"
           MOVE 0 TO WS-EP
           MOVE WS-EP TO WS-N
           MOVE WS-N TO WS-OUT
           DISPLAY "E[" WS-EP "] [" WS-OUT "]"
           MOVE -1234.5 TO WS-EM
           MOVE WS-EM TO WS-N
           MOVE WS-N TO WS-OUT
           DISPLAY "F[" WS-EM "] [" WS-OUT "]"
           MOVE 1234.5 TO WS-EM
           MOVE WS-EM TO WS-N
           MOVE WS-N TO WS-OUT
           DISPLAY "G[" WS-EM "] [" WS-OUT "]"
           MOVE -5.25 TO WS-EM
           MOVE WS-EM TO WS-N
           MOVE WS-N TO WS-OUT
           DISPLAY "H[" WS-EM "] [" WS-OUT "]"
           STOP RUN.
