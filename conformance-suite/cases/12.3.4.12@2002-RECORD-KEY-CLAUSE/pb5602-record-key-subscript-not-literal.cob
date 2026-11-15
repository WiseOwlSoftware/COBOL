*>--- conformance-case
*> id: pb5602-record-key-subscript-not-literal
*> rules: [2002-SR-12.3.4.12.2-2]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB5602 - the RECORD KEY subscript is a data-name, not an integer literal: the key location would not
      *> be fixed (§2002:12.3.4.12.3 GR3).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5602M1.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "pb5602-record-key-subscript-not-literal.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS K(WS-I).
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
