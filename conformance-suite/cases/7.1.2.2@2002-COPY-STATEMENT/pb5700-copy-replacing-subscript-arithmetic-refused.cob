      *>--- conformance-case
      *> id: pb5700-copy-replacing-subscript-arithmetic-refused
      *> rules: [2002-SR-7.1.2.2-9]
      *> files: [PB5700S.cpy]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:7.1.2.2 SR9 - "If subscripting is specified, it shall not
      *> include any arithmetic expressions as subscripts with the
      *> exception of a single literal or identifier, or an identifier
      *> plus or minus an integer".
      *> Text-1 is T(I * 2): its subscript is an arithmetic expression,
      *> so the program is refused at 2002 and at 2014 (§2014:7.2.2.2
      *> SR9 has the same text). COBOL-2023 removed the identifier
      *> operand (COBOL0902).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5700G.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TT VALUE "abc".
          05 T PIC X OCCURS 3.
       01 I PIC 9 VALUE 1.
       01 J PIC 9 VALUE 3.
       PROCEDURE DIVISION.
       MAIN-P.
           COPY PB5700S REPLACING T(I * 2) BY T(3).
           STOP RUN.
