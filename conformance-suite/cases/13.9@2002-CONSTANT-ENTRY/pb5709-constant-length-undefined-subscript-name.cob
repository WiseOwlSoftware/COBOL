*>--- conformance-case
*> id: pb5709-constant-length-undefined-subscript-name
*> rules: [2002-SR-13.9.2-3]
*> source-format: free
*> portable: unverified
*>---
      *> At 2002 a constant entry's LENGTH OF subscript is an ordinary one (8.4.1.2.2), so it names a data item, an
      *> index-name or a constant-name; NOSUCH names none and is refused (kb/Work PB5709, review R1-1).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5709UN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T.
          05 E PIC X(3) OCCURS 4.
       01 K CONSTANT AS LENGTH OF E (NOSUCH).
       PROCEDURE DIVISION.
           STOP RUN.
