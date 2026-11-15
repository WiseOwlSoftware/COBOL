*>--- conformance-case
*> id: pb5602-file-status-unsubscripted-table-element
*> rules: [2002-SR-12.3.4.7.2-1]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB5602 - FS is a table element written with no subscript in the FILE STATUS clause (§2002:8.4.1.2.2 SR5).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5602N5.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "pb5602-file-status-unsubscripted-table-element.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS DYNAMIC
               RELATIVE KEY IS RK(1)
               FILE STATUS IS FS.
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
