      *>--- conformance-case
      *> id: pb5475_code_all_literal
      *> rules: [1985-SR-XIII.3.6.3-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5475. §1985:XIII.3.6.3 SR1: "Literal-1 must be a
      *> two-character nonnumeric literal." §1985:IV.4.2.2.2.3: "A
      *> figurative constant may be used whenever 'literal' appears in a
      *> format", and the CODE clause is none of its exceptions; ALL "AB"
      *> not associated with another item is the string of its literal,
      *> two characters, so CODE ALL "AB" is legal and every logical
      *> record of the report file begins with AB (§1985:XIII.3.6.4
      *> GR1). The file is read back: IN-CODE must hold AB and IN-REST
      *> the detail's HELLO. A compiler that refuses the figurative
      *> constant (COBOL2713) fails; one that drops the code shows HELLO
      *> in IN-CODE. The negative twin (CODE SPACE, one character) is
      *> wiseowl/diagnostics/pb5475_code_figurative_one_character.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5475A.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO PB5475A.
           SELECT IN-FILE ASSIGN TO PB5475A.
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE
           RECORD CONTAINS 22 CHARACTERS
           REPORT IS RPT.
       FD  IN-FILE
           RECORD CONTAINS 22 CHARACTERS.
       01  IN-REC.
           05  IN-CODE PIC XX.
           05  IN-REST PIC X(20).
       REPORT SECTION.
       RD  RPT
           CODE ALL "AB".
       01  DET TYPE DETAIL LINE PLUS 1.
           05  COLUMN 1 PIC X(5) VALUE "HELLO".
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
           DISPLAY IN-CODE.
           DISPLAY IN-REST(1:5).
           CLOSE IN-FILE.
           STOP RUN.
