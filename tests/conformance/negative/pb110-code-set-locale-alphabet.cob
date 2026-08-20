      *> reject-at: 2002 2014 2023
      *> ISO 13.18.13.3 SR1 via 12.3.7.4 GR7 Table 6: a LOCALE alphabet defines a collating sequence and NO coded
      *> character set, so CODE-SET cannot name it - COBOLNET1669 through the ONE resolver (kb/Work PB110).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB110CL.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET LOC IS LOCALE.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "f1.dat".
       DATA DIVISION.
       FILE SECTION.
       FD  F1 CODE-SET IS LOC.
       01  R1 PIC X(4).
       PROCEDURE DIVISION.
           STOP RUN.
