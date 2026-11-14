      *>--- conformance-case
      *> id: pb5475_code_figurative_one_character
      *> rules: [1985-SR-XIII.3.6.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5475. §1985:XIII.3.6.3 SR1: "Literal-1 must be a
      *> two-character nonnumeric literal." A figurative constant is
      *> admitted by §1985:IV.4.2.2.2.3, but SPACE not associated with
      *> another item is ONE character, so CODE SPACE is refused at 85
      *> (COBOL2713) by its length, not by its class. The accepted twin
      *> is pb5475_code_all_literal (ALL "AB", two characters).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5475B.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO PB5475B.
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE
           REPORT IS RPT.
       REPORT SECTION.
       RD  RPT
           CODE SPACE.
       01  DET TYPE DETAIL LINE PLUS 1.
           05  COLUMN 1 PIC X(5) VALUE "HELLO".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RPT-FILE.
           INITIATE RPT.
           GENERATE DET.
           TERMINATE RPT.
           CLOSE RPT-FILE.
           STOP RUN.
