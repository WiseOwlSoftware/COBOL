      *> reject-at: 85 2002 2014 2023
      *> ISO 13.18.63.3 SR2 - an alphanumeric literal that is not even numeric in FORM on a numeric item is an error on
      *> both axes (it used to reach the C# backend as `abcL` - CS0103). kb/Work PB94.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB94N3.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 V PIC 9 VALUE "abc".
       PROCEDURE DIVISION.
           DISPLAY V.
           STOP RUN.
