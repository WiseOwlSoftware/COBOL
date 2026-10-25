      *>--- conformance-case
      *> id: pb1601-copy-literal-name-below-2002
      *> rules: [FMT-7.2.3.2]
      *> files: [PB1601LN.cpy]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> A COPY statement naming its library text by LITERAL (7.2.3.2:
      *> COPY { text-name-1 | literal-1 } [ { OF | IN } { library-name-1
      *> | literal-2 } ]) is a COBOL-2002 introduction (constructs row
      *> copy-literal-name-2002, VCR row 7.42; kb/Work PB1601): ANSI
      *> X3.23-1985's COPY names its text by a user-defined word only.
      *> The statement below draws COBOLNET0900 at --std 85; the positive
      *> twin at 2002 is pb1355_library_text_literal.
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. PB1601LN.
000300 DATA DIVISION.
000400 WORKING-STORAGE SECTION.
000500 COPY "PB1601LN".
000600 PROCEDURE DIVISION.
000700     DISPLAY W-LN.
000800     STOP RUN.
