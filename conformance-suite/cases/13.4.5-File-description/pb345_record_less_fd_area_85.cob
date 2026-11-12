      *>--- conformance-case
      *> id: pb345_record_less_fd_area_85
      *> rules: [SR-13.4.5.3-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> The COBOL-85 twin of 2023/pb345_record_less_fd_area (kb/Work
      *> PB345), now its edition-gate witness (kb/Work PB5071). The
      *> permission of §2023:13.4.5.3 SR3 to write a file description
      *> entry with NO record description entries is COBOL-2002's:
      *> 1985 states the requirement with no exception in each I-O
      *> module, §1985:VII.3.2.3 SR3 (sequential) and §1985:VIII.3.2.3
      *> SR3 (relative): "One or more record description entries must
      *> follow the file description entry." So the two record-less
      *> FDs below (FIN, RLF), legal from 2002, are refused at 85.
      *> This twin had claimed the opposite ("carry no edition gate")
      *> and pinned the acceptance; the 1985 text overturns it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB345R85.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FOUT ASSIGN TO "pb345r85.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS FS-OUT.
           SELECT FIN ASSIGN TO "pb345r85.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS FS-IN.
           SELECT RLF ASSIGN TO "pb345l85.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS RK
               FILE STATUS IS FS-RL.
       DATA DIVISION.
       FILE SECTION.
       FD  FOUT.
       01  OREC PIC X(20).
       FD  FIN RECORD CONTAINS 20 CHARACTERS.
       FD  RLF RECORD CONTAINS 5 CHARACTERS.
       WORKING-STORAGE SECTION.
       01  FS-OUT PIC XX.
       01  FS-IN  PIC XX.
       01  FS-RL  PIC XX.
       01  RK     PIC 9(4).
       01  BUF    PIC X(20).
       01  NUMBUF PIC 9(5).
       01  RBUF   PIC X(5).
       PROCEDURE DIVISION.
       PB4305-LEAD.
           OPEN OUTPUT FOUT
           MOVE "ABCDEFGHIJKLMNOPQRST" TO OREC
           WRITE OREC
           MOVE "12345ABCDEFGHIJKLMNO" TO OREC
           WRITE OREC
           CLOSE FOUT
           OPEN INPUT FIN
           DISPLAY "OPEN=" FS-IN
           READ FIN INTO BUF
               AT END DISPLAY "UNEXPECTED-AT-END-1"
           END-READ
           DISPLAY "R1=" FS-IN " [" BUF "]"
           MOVE 99999 TO NUMBUF
           READ FIN INTO NUMBUF
               AT END DISPLAY "UNEXPECTED-AT-END-2"
           END-READ
           DISPLAY "R2=" FS-IN " [" NUMBUF "]"
           READ FIN INTO BUF
               AT END DISPLAY "R3=" FS-IN
           END-READ
           CLOSE FIN
           MOVE 1 TO RK
           MOVE "ZZ" TO FS-RL
           READ RLF INTO RBUF
               INVALID KEY CONTINUE
           END-READ
           DISPLAY "RD1=" FS-RL
           OPEN OUTPUT RLF
           DISPLAY "OO=" FS-RL
           CLOSE RLF
           DISPLAY "CL=" FS-RL
           OPEN I-O RLF
           MOVE 1 TO RK
           MOVE "ZZ" TO FS-RL
           READ RLF INTO RBUF
               INVALID KEY CONTINUE
           END-READ
           DISPLAY "RD2=" FS-RL
           CLOSE RLF
           STOP RUN.
