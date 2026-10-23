      *>--- conformance-case
      *> id: pb3567_value_signed_float_literal_signed_subject
      *> rules: [SR-13.18.63.3-3]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3567, review round 1 (R1-1) - the over-rejection
      *> guard for pb3567-value-plus-float-literal-unsigned-subject.
      *> ISO 1989:2023 8.3.3.3.3 2) - "If the significand is signed,
      *> the floating-point numeric literal is considered to be
      *> signed" - and 13.18.63.3 SR3: a SIGNED subject takes a
      *> floating-point literal written with either sign, and the
      *> literal keeps its exact value (8.3.3.3.3 GR5: the significand
      *> times ten raised to the exponent). An UNSIGNED floating-point
      *> literal takes an unsigned subject.
      *>   SP S9(4)V9 VALUE +1.5E+0  -> 1.5    SN S9(4)V9 VALUE -1.5E+0 -> -1.5
      *>   SZ S9(4)   VALUE +0.0E+0  -> 0      UQ 9(4)V9  VALUE 1.5E+0  -> 1.5
      *> The values are read back through comparisons, which are
      *> edition-independent, never through the DISPLAY image of a
      *> signed item.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3567SIGNEDF.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 SP PIC S9(4)V9 VALUE +1.5E+0.
       01 SN PIC S9(4)V9 VALUE -1.5E+0.
       01 SZ PIC S9(4) VALUE +0.0E+0.
       01 UQ PIC 9(4)V9 VALUE 1.5E+0.
       PROCEDURE DIVISION.
       MAIN-PARA.
           IF SP = 1.5
               DISPLAY "SP IS PLUS ONE AND A HALF"
           ELSE
               DISPLAY "SP WRONG"
           END-IF
           IF SN = -1.5
               DISPLAY "SN IS MINUS ONE AND A HALF"
           ELSE
               DISPLAY "SN WRONG"
           END-IF
           IF SZ = 0
               DISPLAY "SZ IS ZERO"
           ELSE
               DISPLAY "SZ WRONG"
           END-IF
           IF UQ = 1.5
               DISPLAY "UQ IS ONE AND A HALF"
           ELSE
               DISPLAY "UQ WRONG"
           END-IF
           STOP RUN.
