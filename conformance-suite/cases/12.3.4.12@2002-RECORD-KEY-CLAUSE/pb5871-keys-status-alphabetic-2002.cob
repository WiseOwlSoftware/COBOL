      *>--- conformance-case
      *> id: pb5871-keys-status-alphabetic-2002
      *> rules: [2002-SR-12.3.4.12.2-2, 2002-SR-12.3.4.5.2-2, 2002-SR-12.3.4.7.2-2]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5871 - §2002:12.3.4.12.2 SR2 (RECORD KEY) and
      *>   §2002:12.3.4.5.2 SR2 (ALTERNATE RECORD KEY) require "a data
      *>   item of category alphanumeric or category national" /
      *>   "category alphanumeric or national"; §2002:12.3.4.7.2 SR2
      *>   (FILE STATUS) "a two-character data item of the category
      *>   alphanumeric". §2002:8.5.2.1: "Where an alphabetic data item
      *>   is not expressly permitted, the alphabetic data item is
      *>   treated as though it were an alphanumeric data item." So at
      *>   COBOL-2002 PIC A items are admitted as the prime key, the
      *>   alternate key and the status item. Two records are written,
      *>   then each is read back by its prime key and by its alternate
      *>   key; every I-O statement succeeds with status 00. COBOL-2014
      *>   withdrew the sentence (§2014:E.2), so the same source is
      *>   refused at 2014 and 2023 (pb5871-keys-status-alphabetic-
      *>   refused).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5871K.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb5871k.dat"
              ORGANIZATION IS INDEXED
              ACCESS MODE IS DYNAMIC
              RECORD KEY IS RK
              ALTERNATE RECORD KEY IS AK
              FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 REC.
          05 RK PIC A(4).
          05 AK PIC A(3).
          05 DT PIC X(5).
       WORKING-STORAGE SECTION.
       01 FS PIC AA.
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT F
           MOVE "ABCD" TO RK
           MOVE "XYZ" TO AK
           MOVE "HELLO" TO DT
           WRITE REC
           DISPLAY "W1 " FS
           MOVE "ABCE" TO RK
           MOVE "XYW" TO AK
           MOVE "WORLD" TO DT
           WRITE REC
           DISPLAY "W2 " FS
           CLOSE F
           OPEN INPUT F
           MOVE "ABCD" TO RK
           READ F KEY IS RK
           DISPLAY "R1 " FS " " DT
           MOVE "XYW" TO AK
           READ F KEY IS AK
           DISPLAY "R2 " FS " " RK " " DT
           CLOSE F
           STOP RUN.
