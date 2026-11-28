*>--- conformance-case
*> id: pb6292_index_name_subscript_forms
*> rules: [FMT-8.4.2.3.2, GR-8.4.2.3.4-1]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB6292 - the LEGAL subscript shapes around an index-name, the control of its refusal
      *> (pb6292-index-name-compound-subscript). ISO 8.4.2.3.2 writes a subscript as ALL, arithmetic-expression-1,
      *> or index-name-1 [ { + | - } integer-1 ] (the same three alternatives in the 2002 and 2014 texts).
      *> GR1c: "If index-name-1 is specified, the subscript is the occurrence number represented by
      *> the value of the index referenced by index-name-1 modified by integer-1, if specified." (cite.py OK,
      *> §2023:8.4.2.3.4 GR1c; §2002:8.4.1.2.3 GR1c; §2014:8.4.1.2.3 GR1c). 8.8.1.1: "An arithmetic expression may be
      *> an identifier referencing a numeric data item, a numeric literal, the figurative constant ZERO" (cite.py
      *> OK) - a data-name is an operand of arithmetic-expression-1, an index-name is not.
      *> Expected values, DERIVED: T holds the digits 1 to 9 in occurrence order, so T (n) is n. IX is set to 3.
      *>   A  T (IX)          index-name-1 alone                 occurrence 3 -> 3
      *>   B  T (IX + 1)      index-name-1 + integer-1           occurrence 4 -> 4
      *>   C  T (IX - 1)      index-name-1 - integer-1           occurrence 2 -> 2
      *>   D  T (J * 2)       arithmetic-expression-1, J = 2     occurrence 4 -> 4
      *>   E  T (J + 1)       arithmetic-expression-1            occurrence 3 -> 3
      *>   F  T (J + 2 * 2)   arithmetic-expression-1            occurrence 6 -> 6
      *> WHY EACH LEG FAILS. A screen that refused every subscript naming an index-name would fail A to C; a
      *> screen that admitted an index-name only beside a data-name would still pass them, which is why the
      *> refusal test is separate. D to F fail a screen that confuses the data-name J with an index-name.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB6292LEGAL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TB-IMAGE PIC X(9) VALUE "123456789".
       01 TB REDEFINES TB-IMAGE.
          05 T PIC 9 OCCURS 9 TIMES INDEXED BY IX.
       01 J PIC 9 VALUE 2.
       PROCEDURE DIVISION.
           SET IX TO 3.
           DISPLAY "A=" T (IX).
           DISPLAY "B=" T (IX + 1).
           DISPLAY "C=" T (IX - 1).
           DISPLAY "D=" T (J * 2).
           DISPLAY "E=" T (J + 1).
           DISPLAY "F=" T (J + 2 * 2).
           STOP RUN.
