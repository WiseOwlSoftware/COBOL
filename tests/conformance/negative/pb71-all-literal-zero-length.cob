      *> reject-at: 85 2002 2014 2023
      *> ISO 8.3.3.6.3 SR2: literal-1 of the figurative ALL literal-1 "shall be neither a figurative constant nor
      *> a zero-length literal". kb/Work PB71: `MOVE ALL "" TO AR` compiled and stored spaces; COBOLNET1648 now.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB71NZERO.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 AR PIC X(4).
       PROCEDURE DIVISION.
           MOVE ALL "" TO AR.
           STOP RUN.
