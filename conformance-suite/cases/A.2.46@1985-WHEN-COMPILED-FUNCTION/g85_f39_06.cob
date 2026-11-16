      *>--- conformance-case
      *> id: g85_f39_06
      *> rules: [1989-FMT-A.2.46.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.46.2 - the general format FUNCTION WHEN-COMPILED,
      *> with no argument list, written as a MOVE source and as the
      *> argument of FUNCTION LENGTH. The value is the compilation
      *> time, so no digit of it is printed: the program prints the
      *> length, 21 by the positions 1 through 21 of §1989:A.2.46.3
      *> RV1, and whether positions 1 through 16 are all digits, as
      *> RV1 states they are. A leg fails on a refused format or on a
      *> value not of that shape.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F3906.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WC.
           02  WC-STAMP    PIC X(16).
           02  WC-REST     PIC X(5).
       01  LEN             PIC 99.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE FUNCTION WHEN-COMPILED TO WC.
           COMPUTE LEN = FUNCTION LENGTH (FUNCTION WHEN-COMPILED).
           DISPLAY "LEN=" LEN.
           IF WC-STAMP IS NUMERIC
               DISPLAY "DIGITS=YES"
           ELSE
               DISPLAY "DIGITS=NO"
           END-IF.
           STOP RUN.
