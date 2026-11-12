      *>--- conformance-case
      *> id: pb5177-varying-by-index-name
      *> rules: [FMT-14.9.28.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5177 - an index-name in the BY phrase of PERFORM VARYING. §2023:14.9.28.2's varying-phrase prints
      *> FROM { identifier-3 | index-name-2 | literal-1 } but BY { identifier-4 | literal-2 }, and §1985:VI.6.21.2
      *> prints the same two braces, so at every edition IX in the BY slot is not an operand the format admits.
      *> Expected: COBOL2119 at every edition (it compiled and augmented by the index's occurrence number, 2).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5177BY.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       77  I   PIC 9 VALUE 0.
       01  TBL.
           05  ELEM PIC X OCCURS 10 TIMES INDEXED BY IX.
       PROCEDURE DIVISION.
       MAIN-PARA.
           SET IX TO 2.
           PERFORM PA VARYING I FROM 1 BY IX UNTIL I > 5.
           STOP RUN.
       PA.
           DISPLAY "A" I.
