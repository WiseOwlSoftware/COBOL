      *>--- conformance-case
      *> id: pb3567_value_signed_literal_signed_subject
      *> rules: [SR-13.18.63.3-3]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3567 - the over-rejection guard for the SR3 sign
      *> screen of pb3567-value-plus-literal-unsigned-subject.
      *> ISO 1989:2023 13.18.63.3 SR3 - "If a signed numeric literal
      *> is specified, the subject of the entry shall be either a
      *> signed numeric data item or a numeric-edited data item with a
      *> representation of a sign": a SIGNED subject takes a literal
      *> written with either sign, and an UNSIGNED literal takes any
      *> subject. Every item below compiles at every edition, and
      *> each holds its literal's value (8.3.3.3.2 SR2: the sign is
      *> part of the literal; an unsigned literal is nonnegative):
      *>   SP S9(4) VALUE +5   -> 5     SN S9(4) VALUE -5   -> -5
      *>   SZ S9(4) VALUE +0   -> 0     UQ 9(4)  VALUE 5    -> 5
      *> (UQ writes no sign, so it is never refused.) The values are
      *> read back through comparisons, which are edition-independent,
      *> never through the DISPLAY image of a signed item (a signed
      *> item's stored sign form is implementor-defined).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3567SIGNED.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 SP PIC S9(4) VALUE +5.
       01 SN PIC S9(4) VALUE -5.
       01 SZ PIC S9(4) VALUE +0.
       01 UQ PIC 9(4) VALUE 5.
       PROCEDURE DIVISION.
       MAIN-PARA.
           IF SP = 5
               DISPLAY "SP IS PLUS FIVE"
           ELSE
               DISPLAY "SP WRONG"
           END-IF
           IF SN = -5
               DISPLAY "SN IS MINUS FIVE"
           ELSE
               DISPLAY "SN WRONG"
           END-IF
           IF SZ = 0
               DISPLAY "SZ IS ZERO"
           ELSE
               DISPLAY "SZ WRONG"
           END-IF
           IF UQ = 5
               DISPLAY "UQ IS FIVE"
           ELSE
               DISPLAY "UQ WRONG"
           END-IF
           STOP RUN.
