*>--- conformance-case
*> id: pb5602-record-key-source-unsubscripted-table-element
*> rules: [2002-SR-12.3.4.12.2-2]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB5602 - a SOURCE operand that is a table element written with no subscript: each table element reference
      *> shall be subscripted (§2002:8.4.1.2.2 SR5), and the 2002 clause has no OCCURS ban of its own to refuse it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5602M6.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "pb5602-record-key-source-unsubscripted-table-element.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS PK SOURCE IS K.
       DATA DIVISION.
       FILE SECTION.
       FD F1.
       01 R1.
          05 K PIC X(3) OCCURS 2.
       PROCEDURE DIVISION.
           OPEN OUTPUT F1
           CLOSE F1
           STOP RUN.
