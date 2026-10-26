      *>--- conformance-case
      *> id: pb3830-compute-boolean-size-error
      *> rules: [GR-14.5.3.3-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3830, branch review R1-1. ISO 14.5.3.3: "Any phrase
      *> encountered is the next phrase of the most-recently preceding
      *> unterminated statement with which that phrase may be syntactically
      *> associated." (cite.py --check 14.5.3.3 -> OK.) A phrase may be
      *> associated with a statement only when a general format of the
      *> statement prints it beside everything the statement already wrote.
      *> A COMPUTE whose right side is a boolean-expression is Format 2
      *> (boolean-compute, 14.9.8.2), which prints no SIZE ERROR phrase.
      *> (Boolean operators are a COBOL 2002 introduction; at 85 the parse
      *> refuses the phrase before the edition gate is asked.)
      *> No other statement is open, so the phrase may be syntactically
      *> associated with no statement: refused COBOL2269 (the
      *> statement-format-shape code) at every edition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3830S4.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  B1   PIC 1(4) VALUE B"1100".
       01  B2   PIC 1(4) VALUE B"1010".
       01  B3   PIC 1(4).
       01  F    PIC X VALUE "-".
       PROCEDURE DIVISION.
       MAIN-P.
           COMPUTE B3 = B1 B-AND B2
               ON SIZE ERROR MOVE "E" TO F
           END-COMPUTE
           STOP RUN.
