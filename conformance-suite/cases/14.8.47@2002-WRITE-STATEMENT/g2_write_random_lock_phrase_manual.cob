      *>--- conformance-case
      *> id: g2_write_random_lock_phrase_manual
      *> rules: [2002-SR-14.8.47.2-21]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.47.2 SR21 - "If automatic locking has been specified
      *> for the write file, neither the WITH LOCK phrase nor the WITH NO
      *> LOCK phrase shall be specified." The converse leg: with LOCK MODE
      *> IS MANUAL both phrases are legal in Format 2 (random), placed
      *> after the retry position and before INVALID KEY (§2002:14.8.47.1
      *> Format 2). Both WRITEs succeed (status 00, NOT INVALID KEY) and
      *> both records read back. The AUTOMATIC twin is
      *> g2_write_random_lock_phrase_automatic_neg.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2WR21R.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RLF ASSIGN TO "g2wr21r.tmp" ORGANIZATION RELATIVE
               ACCESS MODE IS RANDOM RELATIVE KEY IS RK
               LOCK MODE IS MANUAL FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD RLF.
       01 R-REC PIC X(4).
       WORKING-STORAGE SECTION.
       01 FS PIC XX.
       01 RK PIC 9(4).
       01 N  PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT RLF
           MOVE 1 TO RK
           MOVE "AAAA" TO R-REC
           WRITE R-REC WITH LOCK
               INVALID KEY DISPLAY "W1 INVALID"
               NOT INVALID KEY ADD 1 TO N
           END-WRITE
           DISPLAY "W1 " FS
           MOVE 2 TO RK
           MOVE "BBBB" TO R-REC
           WRITE R-REC WITH NO LOCK
               INVALID KEY DISPLAY "W2 INVALID"
               NOT INVALID KEY ADD 1 TO N
           END-WRITE
           DISPLAY "W2 " FS " N=" N
           CLOSE RLF
           OPEN INPUT RLF
           MOVE 1 TO RK
           READ RLF INVALID KEY DISPLAY "R1 INVALID" END-READ
           DISPLAY "R1 " FS " " R-REC
           MOVE 2 TO RK
           READ RLF INVALID KEY DISPLAY "R2 INVALID" END-READ
           DISPLAY "R2 " FS " " R-REC
           CLOSE RLF
           STOP RUN.
