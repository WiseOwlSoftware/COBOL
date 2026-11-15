*>--- conformance-case
*> id: pb5602-relative-key-subscripted-refused
*> rules: [2002-SR-12.3.4.13.2-1]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB5602 - the subscripted RELATIVE KEY and FILE STATUS operands of COBOL-2002 are refused at the other
      *> editions: §2014:12.4.4.12.2 SR1 ("Data-name-1 shall not be subscripted"), and COBOL-85 keeps its own reading
      *> of data-name-n.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5602N6.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "pb5602-relative-key-subscripted-refused.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS DYNAMIC
               RELATIVE KEY IS RK(2)
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
