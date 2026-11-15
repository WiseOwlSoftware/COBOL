      *>--- conformance-case
      *> id: pb5475_code_all_symbolic_character
      *> rules: [SR-13.18.12.3-1]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5475, review R1-2. 2023:8.3.3.6.3 SR1: "A figurative
      *> constant may be used whenever 'literal' appears in a format",
      *> and ALL symbolic-character-1 (2023:12.3.7.4 GR11 a) is a
      *> figurative constant of one alphanumeric character: CODE ALL SYM
      *> is the one character 66 stands for, so each record begins with
      *> B and HELLO follows at position 2 (read back as IN-REST).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5475S.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS SYM IS 66.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO "PB5475S.TXT".
           SELECT IN-FILE ASSIGN TO "PB5475S.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE
           RECORD CONTAINS 11 CHARACTERS
           REPORT IS RPT.
       FD  IN-FILE
           RECORD CONTAINS 11 CHARACTERS.
       01  IN-REC.
           05  IN-CODE PIC X.
           05  IN-REST PIC X(10).
       REPORT SECTION.
       RD  RPT CODE ALL SYM.
       01  DET TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           05  COLUMN NUMBER IS 1 PIC X(5) VALUE "HELLO".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RPT-FILE.
           INITIATE RPT.
           GENERATE DET.
           TERMINATE RPT.
           CLOSE RPT-FILE.
           OPEN INPUT IN-FILE.
           READ IN-FILE
               AT END DISPLAY "NO RECORD".
           DISPLAY IN-REST(1:5).
           CLOSE IN-FILE.
           STOP RUN.
