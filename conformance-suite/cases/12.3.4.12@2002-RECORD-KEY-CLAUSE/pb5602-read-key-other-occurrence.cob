*>--- conformance-case
*> id: pb5602-read-key-other-occurrence
*> rules: [2002-GR-12.3.4.12.3-3]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB5602 - K(1) is not the RECORD KEY: the key is K(2), so READ KEY IS K(1) names no key of the file
      *> (§2002:14.8.29.2 SR10).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5602M5.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "pb5602-read-key-other-occurrence.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS K(2).
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
           OPEN INPUT F1
           MOVE "AAA" TO K(1)
           READ F1 KEY IS K(1)
           CLOSE F1
           STOP RUN.
