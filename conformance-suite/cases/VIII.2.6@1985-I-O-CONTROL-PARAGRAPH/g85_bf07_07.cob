      *>--- conformance-case
      *> id: g85_bf07_07
      *> rules: [1985-GR-VIII.2.6.3-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VIII.2.6.3 GR2 - "The SAME clause for the Relative I-O
      *> module is the same as the SAME clause for the Sequential I-O
      *> module. Thus the specifications for the SAME clause are
      *> located on page VII-19." So the Sequential I-O SAME clause
      *> holds for RELATIVE files:
      *> §1985:VII.2.13.3 SR3 - more than one SAME clause, with c's
      *>   additional file-name: SAME AREA R1 R2; SAME RECORD AREA
      *>   R1 R2 R3 (all relative). Accepted.
      *> §1985:VII.2.13.4 GR2 - a record in the SAME RECORD AREA is a
      *>   record of each file open in the output mode and of the most
      *>   recently read input file; records align on the leftmost
      *>   character position.
      *> W1 - R1 and R3 open OUTPUT; MOVE "ABCDEFGH" to R1-REC: R3's
      *>   record (fields of 3 + 5) reads ABC/DEFGH.
      *> W2 - WRITE R3-REC (relative record 1) with no MOVE to it; read
      *>   back from R3 by key: ABCDEFGH.
      *> R1 - R3 open INPUT, R1 open OUTPUT; R1-REC set to all Z; READ
      *>   R3 key 1: R1-REC shows ABCDEFGH (an unshared area keeps Z).
      *> The Sequential I-O rules quoted are the content GR2 imports;
      *> only GR2 is this case's row.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF0707.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT R1 ASSIGN TO "BF0707R1"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS RK1.
           SELECT R2 ASSIGN TO "BF0707R2"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS RK2.
           SELECT R3 ASSIGN TO "BF0707R3"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS RK3
               FILE STATUS IS ST3.
       I-O-CONTROL.
           SAME AREA FOR R1 R2
           SAME RECORD AREA FOR R1 R2 R3.
       DATA DIVISION.
       FILE SECTION.
       FD  R1.
       01  R1-REC PIC X(8).
       FD  R2.
       01  R2-REC PIC X(8).
       FD  R3.
       01  R3-REC.
           05  R3-A PIC X(3).
           05  R3-B PIC X(5).
       WORKING-STORAGE SECTION.
       01  RK1 PIC 9.
       01  RK2 PIC 9.
       01  RK3 PIC 9.
       01  ST3 PIC XX.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT R1 R3.
           MOVE "ABCDEFGH" TO R1-REC.
           DISPLAY "W1 [" R3-A "][" R3-B "]".
           MOVE 1 TO RK3.
           WRITE R3-REC INVALID KEY DISPLAY "W1 INVALID".
           CLOSE R1 R3.
           OPEN INPUT R3.
           MOVE 1 TO RK3.
           READ R3 INVALID KEY DISPLAY "W2 INVALID".
           DISPLAY "W2 [" R3-REC "] " ST3.
           CLOSE R3.
           OPEN INPUT R3.
           OPEN OUTPUT R1.
           MOVE ALL "Z" TO R1-REC.
           MOVE 1 TO RK3.
           READ R3 INVALID KEY DISPLAY "R1 INVALID".
           DISPLAY "R1 [" R1-REC "]".
           CLOSE R1 R3.
           STOP RUN.
