*>--- conformance-case
*> id: pb5602-relative-key-subscript-not-literal
*> rules: [2002-SR-12.3.4.13.2-1]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB5602 - the RELATIVE KEY subscript is a data-name, not an integer literal.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5602N4.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "pb5602-relative-key-subscript-not-literal.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS DYNAMIC
               RELATIVE KEY IS RK(WS-I)
               FILE STATUS IS FS(1).
       DATA DIVISION.
       FILE SECTION.
       FD F1.
       01 R1 PIC X(3).
       WORKING-STORAGE SECTION.
       01 RKT.
          05 RK PIC 9(2) OCCURS 3.
       01 FST.
          05 FS PIC XX OCCURS 2.
       01 WS-I   PIC 9 VALUE 1.
       PROCEDURE DIVISION.
           OPEN OUTPUT F1
           CLOSE F1
           STOP RUN.
