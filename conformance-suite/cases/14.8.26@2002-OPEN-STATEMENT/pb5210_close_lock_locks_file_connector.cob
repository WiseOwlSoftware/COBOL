      *>--- conformance-case
      *> id: pb5210_close_lock_locks_file_connector
      *> rules: [2002-GR-14.8.26.3-24]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5210 -- from COBOL-2002 a CLOSE WITH LOCK locks the
      *> FILE CONNECTOR. §2002:14.8.26.3 GR24 (§2014:14.9.26.3 GR24):
      *> "If the file connector referenced by file-name-1 is locked by a
      *> previously-executed CLOSE statement with the LOCK phrase, the
      *> execution of the OPEN statement is unsuccessful" with I-O
      *> status 38. Another file-name whose ASSIGN names the same
      *> physical file is another file connector, which no CLOSE locked:
      *> its OPEN succeeds and reads the record. (COBOL-85 locks the
      *> physical file instead: kb/Work PB5210's 1985 witness.)
      *> Each organization is exercised: sequential, relative, indexed.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5210C.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT S1 ASSIGN TO "pb5210cs.dat" FILE STATUS IS ST.
           SELECT S2 ASSIGN TO "pb5210cs.dat" FILE STATUS IS ST.
           SELECT R1 ASSIGN TO "pb5210cr.dat" ORGANIZATION IS RELATIVE
               ACCESS MODE IS DYNAMIC RELATIVE KEY IS RK1
               FILE STATUS IS ST.
           SELECT R2 ASSIGN TO "pb5210cr.dat" ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM RELATIVE KEY IS RK2
               FILE STATUS IS ST.
           SELECT I1 ASSIGN TO "pb5210ci.dat" ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC RECORD KEY IS K1
               FILE STATUS IS ST.
           SELECT I2 ASSIGN TO "pb5210ci.dat" ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC RECORD KEY IS K2
               FILE STATUS IS ST.
       DATA DIVISION.
       FILE SECTION.
       FD  S1.
       01  S1-REC PIC X(4).
       FD  S2.
       01  S2-REC PIC X(4).
       FD  R1.
       01  R1-REC PIC X(4).
       FD  R2.
       01  R2-REC PIC X(4).
       FD  I1.
       01  I1-REC.
           05 K1 PIC X(4).
       FD  I2.
       01  I2-REC.
           05 K2 PIC X(4).
       WORKING-STORAGE SECTION.
       01  ST  PIC XX.
       01  RK1 PIC 9(4).
       01  RK2 PIC 9(4).
       PROCEDURE DIVISION.
       P-SEQ.
           OPEN OUTPUT S1.
           MOVE "SSSS" TO S1-REC.
           WRITE S1-REC.
           CLOSE S1 WITH LOCK.
           DISPLAY "SEQ CLOSE S1 WITH LOCK " ST.
           OPEN INPUT S1.
           DISPLAY "SEQ OPEN INPUT S1 " ST.
           OPEN INPUT S2.
           DISPLAY "SEQ OPEN INPUT S2 " ST.
           MOVE SPACES TO S2-REC.
           READ S2 AT END CONTINUE END-READ.
           DISPLAY "SEQ READ S2 " ST " " S2-REC.
           CLOSE S2.
           OPEN EXTEND S1.
           DISPLAY "SEQ OPEN EXTEND S1 " ST.
       P-REL.
           OPEN OUTPUT R1.
           MOVE 1 TO RK1.
           MOVE "RRRR" TO R1-REC.
           WRITE R1-REC INVALID KEY CONTINUE END-WRITE.
           CLOSE R1 WITH LOCK.
           DISPLAY "REL CLOSE R1 WITH LOCK " ST.
           OPEN I-O R1.
           DISPLAY "REL OPEN I-O R1 " ST.
           OPEN INPUT R2.
           DISPLAY "REL OPEN INPUT R2 " ST.
           MOVE 1 TO RK2.
           MOVE SPACES TO R2-REC.
           READ R2 INVALID KEY CONTINUE END-READ.
           DISPLAY "REL READ R2 " ST " " R2-REC.
           CLOSE R2.
       P-IDX.
           OPEN OUTPUT I1.
           MOVE "IIII" TO K1.
           WRITE I1-REC INVALID KEY CONTINUE END-WRITE.
           CLOSE I1 WITH LOCK.
           DISPLAY "IDX CLOSE I1 WITH LOCK " ST.
           OPEN OUTPUT I1.
           DISPLAY "IDX OPEN OUTPUT I1 " ST.
           OPEN INPUT I2.
           DISPLAY "IDX OPEN INPUT I2 " ST.
           MOVE "IIII" TO K2.
           READ I2 INVALID KEY CONTINUE END-READ.
           DISPLAY "IDX READ I2 " ST " " K2.
           CLOSE I2.
           STOP RUN.
