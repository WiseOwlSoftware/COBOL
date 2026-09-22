      *> reject-at: 2023
      *> kb/Work PB866 - an IS-form character-1 is admitted in the SIGNIFICAND only: 13.18.40.5
      *> Table 7 gives the floating-point edited category "Simple insertion, special insertion, and
      *> fixed insertion for the significand part" and "None for the exponent part".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB866FLOATEDITEDCHAR1INEXP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 FX PIC +9.9(3)E+9T EDITING T IS ":".
       PROCEDURE DIVISION.
           DISPLAY "X".
           STOP RUN.
