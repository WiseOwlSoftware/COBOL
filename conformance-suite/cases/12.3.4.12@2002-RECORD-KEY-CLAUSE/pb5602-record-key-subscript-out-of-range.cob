*>--- conformance-case
*> id: pb5602-record-key-subscript-out-of-range
*> rules: [2002-SR-12.3.4.12.2-2]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB5602 - the table has two occurrences: occurrence 3 of K does not exist (§2002:8.4.1.2.3 GR2).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5602M2.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "pb5602-record-key-subscript-out-of-range.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS K(3).
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
