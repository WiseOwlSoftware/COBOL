*>--- conformance-case
*> id: pb5709-constant-length-function-undefined-name
*> rules: [2002-SR-13.9.2-3]
*> source-format: free
*> portable: unverified
*>---
      *> At 2002 a constant entry's LENGTH OF subscript is an ordinary one (8.4.1.2.2), so it names a data item, an
      *> index-name or a constant-name; a name inside a function argument is checked too (kb/Work PB5709, review R1-1).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5709FU.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T.
          05 E PIC X(3) OCCURS 4.
       01 K CONSTANT AS LENGTH OF E (FUNCTION MIN (NOSUCH 1)).
       PROCEDURE DIVISION.
           STOP RUN.
