*>--- conformance-case
*> id: pb6292-index-name-compound-subscript
*> rules: [FMT-8.4.2.3.2]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB6292. ISO 8.4.2.3.2 writes a subscript as ALL, arithmetic-expression-1, or index-name-1 [ { + | - }
      *> integer-1 ] (the 2002 and 2014 texts the same). 8.8.1.1: "An arithmetic expression may be an
      *> identifier referencing a numeric data item, a numeric literal, the figurative constant ZERO" (cite.py OK;
      *> §2002:8.8.1 and §2014:8.8.1 the same words): an index-name is no operand of arithmetic-expression-1, so an
      *> index-name that is a multiplier, the left or right operand of a sum with a data-name, or the base of **
      *> is neither the whole subscript nor index-name-1 { + | - } integer-1. COMPUTE N = IX * 2 is refused by
      *> the same rule (COBOL1637); the subscript took the occurrence number silently. T (IX * 2) is read by the
      *> direct walk of the position, T (IX ** 2) by the general expression binder: ONE screen, ahead of both,
      *> gives both the one verdict. 85 refuses the same shapes by its own subscript shape gate.
      *> WHY EACH LEG FAILS. A screen on the direct walk alone passes the ** line; a screen that asks only the
      *> left operand passes J + IX. The expected diagnostic is COBOL1637 at 2002, 2014 and 2023.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB6292REFUSE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TB.
          05 T PIC 9 OCCURS 10 TIMES INDEXED BY IX.
       01 J PIC 9 VALUE 1.
       01 R PIC 9.
       PROCEDURE DIVISION.
           SET IX TO 2.
           MOVE T (IX * 2) TO R.
           MOVE T (J + IX) TO R.
           MOVE T (IX + J) TO R.
           MOVE T (IX ** 2) TO R.
           STOP RUN.
