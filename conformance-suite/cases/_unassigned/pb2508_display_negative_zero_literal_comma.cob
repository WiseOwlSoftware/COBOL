      *>--- conformance-case
      *> id: pb2508_display_negative_zero_literal_comma
      *> rules: []
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB2508 under DECIMAL-POINT IS COMMA (12.3.7.4 GR14 a:
      *> the comma is "the character written in numeric literals to
      *> represent the decimal separator"). ISO 14.9.11.4 GR1 leaves the
      *> literal's device conversion to the implementor (DOC-A.1-56): as
      *> written, the program's separator included, except that a literal
      *> whose value is zero shows '+' for a written '-' (GnuCOBOL 3.2.0,
      *> -std=default and -std=cobol2014, measured). The two spellings
      *> compose: the sign follows the value, the separator the program.
      *> EXPECTED, one line each, bracketed:
      *>   A[+0,0] B[+0] C[+,0] D[-1,50] E[+0,0] F[+0,00]
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2508B.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           DECIMAL-POINT IS COMMA.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 C-NZ CONSTANT AS -0,0.
       PROCEDURE DIVISION.
           DISPLAY "A[" -0,0 "]"
           DISPLAY "B[" -0 "]"
           DISPLAY "C[" -,0 "]"
           DISPLAY "D[" -1,50 "]"
           DISPLAY "E[" C-NZ "]"
           DISPLAY "F[" -0,00 "]"
           STOP RUN.
