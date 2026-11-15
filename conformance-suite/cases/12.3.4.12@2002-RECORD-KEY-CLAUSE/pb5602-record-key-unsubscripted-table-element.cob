*>--- conformance-case
*> id: pb5602-record-key-unsubscripted-table-element
*> rules: [2002-SR-12.3.4.12.2-2]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB5602 - K is a table element written with no subscript: each table element reference shall be
      *> subscripted (§2002:8.4.1.2.2 SR5).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5602M3.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "pb5602-record-key-unsubscripted-table-element.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS K.
       DATA DIVISION.
       FILE SECTION.
       FD F1.
       01 R1.
          05 T OCCURS 2.
             10 K PIC X(3).
             10 A PIC X(2).
       WORKING-STORAGE SECTION.
       01 WS-I PIC 9 VALUE 1.
       PROCEDURE DIVISION.
           OPEN OUTPUT F1
           CLOSE F1
           STOP RUN.
