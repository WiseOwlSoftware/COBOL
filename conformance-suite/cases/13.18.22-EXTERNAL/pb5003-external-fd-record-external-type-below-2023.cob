*>--- conformance-case
*> id: pb5003-external-fd-record-external-type-below-2023
*> rules: [SR-13.18.22.3-5]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB5003 - the same program as pb5003-external-fd-record-external-type: a strongly typed EXTERNAL type declaration is
      *> a COBOL-2023 construct (ISO 13.18.22.3 SR1/SR5), so below 2023 the edition gate refuses it (COBOL0900).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5003B.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb5003b.dat" ORGANIZATION SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD F IS EXTERNAL.
       01 R TYPE TG.
       WORKING-STORAGE SECTION.
       01 TG IS TYPEDEF STRONG EXTERNAL.
          05 RX PIC X(4).
          05 RY PIC 9(2).
       01 W TYPE TG.
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE "ABCD" TO RX OF W
           MOVE 42 TO RY OF W
           OPEN OUTPUT F
           MOVE "WXYZ" TO RX OF R
           MOVE 7 TO RY OF R
           WRITE R
           CLOSE F
           OPEN INPUT F
           READ F
           CLOSE F
           DISPLAY RX OF R RY OF R " " RX OF W RY OF W
           STOP RUN.
