      *> reject-at: 85 2002 2014 2023
      *> ISO 8.3.3.6.3 SR1 a: "If the literal is restricted to a numeric literal,
      *> the only figurative constant permitted is ZERO (ZEROS, ZEROES) WITHOUT
      *> the ALL phrase." A sign condition's operand is an arithmetic expression
      *> (8.8.4.7.3 SR1), and 8.8.1.1 enumerates what one may be - "a numeric
      *> literal, the figurative constant ZERO (ZEROS, ZEROES)" - so the literal
      *> here IS restricted to a numeric literal and the ALL phrase is barred,
      *> ZERO spelling included.
      *> The bar was QUOTED in the diagnostic and never enforced: the grammar has
      *> a distinct `ALL ZERO` alternative, so fig.ZERO() is non-null for
      *> ALL ZEROS and the screen's first arm admitted it. Measured before the
      *> fix: this program compiled clean and evaluated 0 > 0. kb/Work PB218.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB218N1.
       PROCEDURE DIVISION.
       MAIN.
           IF ALL ZEROS IS POSITIVE
               DISPLAY "T"
           ELSE
               DISPLAY "F"
           END-IF
           STOP RUN.
