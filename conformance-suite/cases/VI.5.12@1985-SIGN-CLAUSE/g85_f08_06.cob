      *>--- conformance-case
      *> id: g85_f08_06
      *> rules: [1985-GR-VI.5.12.4-5]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.5.12.4 GR5 - without SEPARATE CHARACTER the sign "will
      *> be presumed to be associated with the leading (or,
      *> respectively, trailing) digit position", "The letter 'S' in a
      *> PICTURE character-string is not counted in determining the size
      *> of the item", and "The implementor defines what constitutes
      *> valid sign(s) for data items" (so the sign-carrying character
      *> itself is never pinned here).
      *> Expected (derived):
      *>   G1: L PIC S9(3) SIGN LEADING -123, then "|".  'S' not
      *>     counted:
      *>   L is 3 positions, so G1 (4:1) = "|".  The sign is on the
      *>     leading
      *>   digit, so the other digit positions hold plain digits:
      *>   G1 (2:2) = "23".
      *>   G2: T PIC S9(3) SIGN TRAILING -123, then "|": G2 (4:1) = "|",
      *>   G2 (1:2) = "12".
      *>   L and T still hold -123: moved to S9(3) SIGN LEADING
      *>   SEPARATE (§1985:VI.6.19.4 GR4b, "Conversion of the
      *>   representation of the sign takes place as necessary"), each
      *>   is "-123"; each is NUMERIC (the sign the compiler stored is
      *>   one of its valid signs) and NEGATIVE (§1985:VI.5.12.4 GR7,
      *>   conversion for comparisons).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F0806.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  G1.
           05  L              PIC S9(3) SIGN IS LEADING VALUE -123.
           05  M1             PIC X VALUE "|".
       01  G2.
           05  T              PIC S9(3) SIGN IS TRAILING VALUE -123.
           05  M2             PIC X VALUE "|".
       01  GS.
           05  S1             PIC S9(3) SIGN IS LEADING SEPARATE.
       PROCEDURE DIVISION.
       P1.
           DISPLAY "L-SIZE=[" G1 (4:1) "] L-DIGITS=[" G1 (2:2) "]".
           DISPLAY "T-SIZE=[" G2 (4:1) "] T-DIGITS=[" G2 (1:2) "]".
           MOVE L TO S1.
           DISPLAY "L=[" GS "]".
           MOVE T TO S1.
           DISPLAY "T=[" GS "]".
           IF L IS NUMERIC AND L IS NEGATIVE
               DISPLAY "L NUMERIC NEGATIVE"
           ELSE
               DISPLAY "L WRONG".
           IF T IS NUMERIC AND T IS NEGATIVE
               DISPLAY "T NUMERIC NEGATIVE"
           ELSE
               DISPLAY "T WRONG".
           STOP RUN.
