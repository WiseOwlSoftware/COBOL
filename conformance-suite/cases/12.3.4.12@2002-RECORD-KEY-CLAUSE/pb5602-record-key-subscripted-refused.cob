*>--- conformance-case
*> id: pb5602-record-key-subscripted-refused
*> rules: [2002-SR-12.3.4.12.2-2]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB5602 - the subscripted RECORD KEY and ALTERNATE RECORD KEY operands of COBOL-2002 are refused at the
      *> other editions: §2014:12.4.4.11.2 SR1 ("Data-name-1 and data-name-2 shall not be subscripted"), and COBOL-85
      *> keeps its own reading of data-name-n.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5602M4.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "pb5602-record-key-subscripted-refused.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS K(2)
               ALTERNATE RECORD KEY IS A(1) WITH DUPLICATES.
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
