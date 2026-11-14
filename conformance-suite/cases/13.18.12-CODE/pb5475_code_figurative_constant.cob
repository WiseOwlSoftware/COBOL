      *>--- conformance-case
      *> id: pb5475_code_figurative_constant
      *> rules: [SR-13.18.12.3-1]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5475. 2023:8.3.3.6.3 SR1: "A figurative constant may
      *> be used whenever 'literal' appears in a format" (cite.py: OK),
      *> and 2023:13.18.12.3 SR1 asks only for an alphanumeric literal,
      *> which a figurative constant's value is (2023:8.3.3.6.4 GR1).
      *> CODE SPACE not associated with another item is one character
      *> (2023:8.3.3.6.4 GR3b), so each logical record begins with one
      *> space and the detail's HELLO follows at position 2: the file is
      *> read back and IN-REST holds HELLO. A compiler that refuses the
      *> figurative constant (COBOL2713) fails; one that gives it two
      *> positions shows a shifted HELLO. conformance:cases/
      *> XIII.3.6@1985-CODE-CLAUSE/pb5475_code_figurative_one_character
      *> is the refused side at COBOL-85, where SR1 asks two characters.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5475C.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO "PB5475C.TXT".
           SELECT IN-FILE ASSIGN TO "PB5475C.TXT".
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
       RD  RPT CODE SPACE.
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
