      *>--- conformance-case
      *> id: pb5523_code_all_national_symbolic_character
      *> rules: [SR-13.18.12.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5523. "Literal-1 shall be an alphanumeric literal."
      *> (2023:13.18.12.3 SR1). A symbolic character declared SYMBOLIC
      *> CHARACTERS FOR NATIONAL is a national figurative constant:
      *> "When NATIONAL is specified, the value of figurative constant
      *> symbolic-character-1 is the internal representation of the
      *> character at ordinal position integer-1 in the native national
      *> character set" (2023:12.3.7.4 GR11 c). CODE ALL SYM names a
      *> national literal-1, so it is refused as CODE ALL N"B" is.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5523N.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS FOR NATIONAL SYM IS 66.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO "PB5523N.TXT".
           SELECT IN-FILE ASSIGN TO "PB5523N.TXT".
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
