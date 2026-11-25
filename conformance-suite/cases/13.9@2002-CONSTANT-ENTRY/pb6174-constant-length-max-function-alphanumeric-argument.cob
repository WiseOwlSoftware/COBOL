*>--- conformance-case
*> id: pb6174-constant-length-max-function-alphanumeric-argument
*> rules: [2002-SR-13.9.2-3]
*> source-format: free
*> portable: unverified
*>---
      *> At 2002 a constant entry's LENGTH OF subscript is an ordinary one (8.4.1.2.2), an arithmetic expression (8.8.1):
      *> FUNCTION MAX of an alphanumeric constant-name returns an alphanumeric value, no numeric operand (8.8.1) (kb/Work PB6174, review R1-1).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB6174N2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T.
          05 E PIC X(3) OCCURS 4.
       01 KC CONSTANT AS "AB".
       01 K CONSTANT AS LENGTH OF E (FUNCTION MAX (KC)).
       PROCEDURE DIVISION.
           STOP RUN.
