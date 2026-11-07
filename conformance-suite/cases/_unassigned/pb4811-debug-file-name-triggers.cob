      *>--- conformance-case
      *> id: pb4811-debug-file-name-triggers
      *> rules: []
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4811. X3.23-1985 USE FOR DEBUGGING general rule 2:
      *> the section for file-name-1 runs "(a) after the execution of any
      *> OPEN or CLOSE statement that references file-name-1", "(b) after
      *> the execution of any READ statement (after any other specified
      *> USE procedure) not resulting in the execution of an associated
      *> AT END or INVALID KEY imperative statement", and "(c) after the
      *> execution of any DELETE or START statement that references
      *> file-name-1". Rule 23: DEBUG-NAME holds the file-name, "for
      *> READ, DEBUG-CONTENTS contains the entire record read" and "for
      *> all other references to file-name-1, DEBUG-CONTENTS contains
      *> spaces". WRITE and REWRITE are not listed and run no section.
      *> Rule 7: each imperative verb in a phrase is a statement of its
      *> own, so a DELETE or START section runs when the I-O is done and
      *> before its INVALID KEY imperative statement.
      *> Why each leg can fail: a trigger placed after the whole READ
      *> runs for the AT END path too; one that skips DELETE or START on
      *> an invalid key misses rule 2 c; a WRITE that triggers breaks
      *> rule 2's list; the contents of a READ that are not the record
      *> read, or of an OPEN that are not spaces, break rule 23.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4811FT.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. IBM-PC WITH DEBUGGING MODE.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SF ASSIGN TO "pb4811-seq.dat"
               ORGANIZATION IS SEQUENTIAL FILE STATUS IS FS1.
           SELECT KF ASSIGN TO "pb4811-idx.dat"
               ORGANIZATION IS INDEXED ACCESS MODE IS DYNAMIC
               RECORD KEY IS KK FILE STATUS IS FS2.
       DATA DIVISION.
       FILE SECTION.
       FD SF.
       01 SREC PIC X(8).
       FD KF.
       01 KREC.
          02 KK PIC X(3).
          02 KV PIC X(5).
       WORKING-STORAGE SECTION.
       01 FS1 PIC XX.
       01 FS2 PIC XX.
       01 W PIC X(8).
       PROCEDURE DIVISION.
       DECLARATIVES.
       DBS SECTION.
           USE FOR DEBUGGING ON SF.
       DBS-B.
           DISPLAY "  DBS [" DEBUG-NAME (1:2) "] [" DEBUG-CONTENTS (1:8)
               "]".
       DBK SECTION.
           USE FOR DEBUGGING ON KF.
       DBK-B.
           DISPLAY "  DBK [" DEBUG-NAME (1:2) "] [" DEBUG-CONTENTS (1:8)
               "]".
       END DECLARATIVES.
       MAIN SECTION.
       M1.
           DISPLAY "OPEN OUTPUT SF".
           OPEN OUTPUT SF.
           MOVE "ABCDEFGH" TO SREC.
           DISPLAY "WRITE SF".
           WRITE SREC.
           MOVE "IJKLMNOP" TO SREC.
           WRITE SREC.
           DISPLAY "CLOSE SF".
           CLOSE SF.
           DISPLAY "OPEN INPUT SF".
           OPEN INPUT SF.
           DISPLAY "READ SF".
           READ SF AT END DISPLAY "END".
           DISPLAY "READ SF INTO W".
           READ SF INTO W AT END DISPLAY "END".
           DISPLAY "W = " W.
           DISPLAY "READ SF AT END".
           READ SF AT END DISPLAY "END".
           DISPLAY "CLOSE SF".
           CLOSE SF.
           DISPLAY "OPEN OUTPUT KF".
           OPEN OUTPUT KF.
           MOVE "AAA11111" TO KREC.
           DISPLAY "WRITE KF".
           WRITE KREC.
           MOVE "BBB22222" TO KREC.
           WRITE KREC.
           DISPLAY "CLOSE KF".
           CLOSE KF.
           DISPLAY "OPEN I-O KF".
           OPEN I-O KF.
           MOVE "AAA" TO KK.
           DISPLAY "START KF AAA".
           START KF KEY = KK INVALID KEY DISPLAY "INVALID".
           DISPLAY "READ KF NEXT".
           READ KF NEXT AT END DISPLAY "END".
           MOVE "ZZZ" TO KK.
           DISPLAY "START KF ZZZ".
           START KF KEY = KK INVALID KEY DISPLAY "INVALID".
           MOVE "BBB" TO KK.
           DISPLAY "READ KF BBB".
           READ KF INVALID KEY DISPLAY "INVALID".
           MOVE "QQQ" TO KK.
           DISPLAY "READ KF QQQ".
           READ KF INVALID KEY DISPLAY "INVALID".
           MOVE "BBB" TO KK.
           DISPLAY "DELETE KF BBB".
           DELETE KF INVALID KEY DISPLAY "INVALID".
           DISPLAY "DELETE KF BBB AGAIN".
           DELETE KF INVALID KEY DISPLAY "INVALID".
           DISPLAY "CLOSE KF".
           CLOSE KF.
           DISPLAY "END".
           STOP RUN.
