      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1030 - ISO 8.4.3.3.3 SR4: "Leftmost-position and length shall be arithmetic
      *> expressions." A string literal is not one.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1030C.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET AL IS NATIVE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TB.
          05 E PIC X OCCURS 5.
       01 X PIC X(4) VALUE "ABCD".
       PROCEDURE DIVISION.
           DISPLAY X ("A" : 1).
           STOP RUN.
