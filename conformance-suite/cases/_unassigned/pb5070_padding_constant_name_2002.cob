      *>--- conformance-case
      *> id: pb5070_padding_constant_name_2002
      *> rules: []
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5070 - a constant-name stands where a format specifies a
      *> literal (2023:13.10.3 SR2; constant entries begin at 2002), so
      *> PADDING CHARACTER IS PAD-C names literal-1, the one-character
      *> alphanumeric literal "*" (2002:12.3.4.10.2 SR1), not a data-name.
      *> The clause is obsolete at 2002 and deleted from 2014.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5070PC.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PAD-FILE ASSIGN TO "PB5070PC.DAT"
               PADDING CHARACTER IS PAD-C.
       DATA DIVISION.
       FILE SECTION.
       FD  PAD-FILE.
       01  PAD-REC PIC X(10).
       WORKING-STORAGE SECTION.
       01  PAD-C CONSTANT AS "*".
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "OK".
           STOP RUN.
