      *>--- conformance-case
      *> id: pb5543_concat_national_symbolic_character
      *> rules: [SR-8.8.3.2-1, GR-8.8.3.3-1, GR-8.8.3.3-2]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5543. A symbolic-character declared SYMBOLIC
      *> CHARACTERS FOR NATIONAL is a figurative constant whose value
      *> is a national character (2023:12.3.7.4 GR11 c), so it takes
      *> the class of its partner (2023:8.8.3.3 GR1 a) only where that
      *> class is national.
      *>   SYM-N = ordinal 67 of the native national set = "B" (ordinal
      *>   n is code unit n-1), SYM-A = ordinal 66 = "A".
      *>   N"X" & SYM-N         = XB, class national
      *>   SYM-N & N"X"         = BX, class national
      *>   N"X" & SYM-N & SYM-A = XBA: SYM-A, an alphanumeric figurative,
      *>                          takes the national class of its partner
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5543POS.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS FOR NATIONAL SYM-N IS 67
           SYMBOLIC CHARACTERS SYM-A IS 66.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-N PIC N(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE N"X" & SYM-N TO W-N.
           DISPLAY "A=[" W-N "]".
           MOVE SYM-N & N"X" TO W-N.
           DISPLAY "B=[" W-N "]".
           MOVE N"X" & SYM-N & SYM-A TO W-N.
           DISPLAY "C=[" W-N "]".
           STOP RUN.
