      *>--- conformance-case
      *> id: pb5210_close_lock_locks_physical_file_85
      *> rules: [1985-GR-IX.1.3.4-L2.4, 1985-GR-VIII.1.3.4-L2.4, 1985-GR-VII.1.3.5-L2.3, 1985-GR-VIII.4.2.4-2, 1985-GR-IX.4.2.4-2, 1985-GR-VII.4.2.4-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5210 -- at COBOL-85 a CLOSE WITH LOCK locks the
      *> PHYSICAL FILE, not the file-name. §1985:II.2: "The terms file
      *> referenced by file-name and file mean the physical file
      *> regardless of the file-name used in the COBOL program".
      *> §1985:IX.4.2.4 GR2 (and §1985:VIII.4.2.4 GR2, §1985:VII.4.2.4
      *> GR3): "The file is locked and cannot be opened again during
      *> this execution of this run unit." So an OPEN of that file
      *> through a SECOND file-name, in every open mode, is unsuccessful
      *> with I-O status 38 (§1985:IX.1.3.4 item d, §1985:VIII.1.3.4
      *> item d, §1985:VII.1.3.5 item e: "an OPEN statement is attempted
      *> on a file previously closed with lock"), and a READ through it
      *> then finds the file not open, 47 (§1985:VII.1.3.5 item f).
      *> Each organization is exercised: sequential, relative, indexed.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5210P.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT S1 ASSIGN TO "pb5210s.dat" FILE STATUS IS ST.
           SELECT S2 ASSIGN TO "pb5210s.dat" FILE STATUS IS ST.
           SELECT R1 ASSIGN TO "pb5210r.dat" ORGANIZATION IS RELATIVE
               ACCESS MODE IS DYNAMIC RELATIVE KEY IS RK1
               FILE STATUS IS ST.
           SELECT R2 ASSIGN TO "pb5210r.dat" ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM RELATIVE KEY IS RK2
               FILE STATUS IS ST.
           SELECT I1 ASSIGN TO "pb5210i.dat" ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC RECORD KEY IS K1
               FILE STATUS IS ST.
           SELECT I2 ASSIGN TO "pb5210i.dat" ORGANIZATION IS INDEXED
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
           DISPLAY "SEQ READ S2 " ST.
           OPEN OUTPUT S2.
           DISPLAY "SEQ OPEN OUTPUT S2 " ST.
           OPEN EXTEND S2.
           DISPLAY "SEQ OPEN EXTEND S2 " ST.
           OPEN I-O S2.
           DISPLAY "SEQ OPEN I-O S2 " ST.
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
           READ R2 INVALID KEY CONTINUE END-READ.
           DISPLAY "REL READ R2 " ST.
           OPEN OUTPUT R2.
           DISPLAY "REL OPEN OUTPUT R2 " ST.
           OPEN I-O R2.
           DISPLAY "REL OPEN I-O R2 " ST.
       P-IDX.
           OPEN OUTPUT I1.
           MOVE "IIII" TO K1.
           WRITE I1-REC INVALID KEY CONTINUE END-WRITE.
           CLOSE I1 WITH LOCK.
           DISPLAY "IDX CLOSE I1 WITH LOCK " ST.
           OPEN INPUT I1.
           DISPLAY "IDX OPEN INPUT I1 " ST.
           OPEN INPUT I2.
           DISPLAY "IDX OPEN INPUT I2 " ST.
           MOVE "IIII" TO K2.
           READ I2 INVALID KEY CONTINUE END-READ.
           DISPLAY "IDX READ I2 " ST.
           OPEN OUTPUT I2.
           DISPLAY "IDX OPEN OUTPUT I2 " ST.
           OPEN I-O I2.
           DISPLAY "IDX OPEN I-O I2 " ST.
           STOP RUN.
