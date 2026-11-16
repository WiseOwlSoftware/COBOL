      *>--- conformance-case
      *> id: g85_f23_03
      *> rules: [1985-GR-VIII.4.3.4-3, 1985-GR-VIII.4.3.4-7]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VIII.4.3.4 GR3 - random (RND-FILE) and dynamic (DYN-FILE)
      *> access: DELETE removes the record whose relative record number
      *> is in the relative key (2 of 1-3; 1 of 1-2); a key naming no
      *> record (2 again, 5) is the invalid key condition. Fails if
      *> another record goes, or a missing key does not take INVALID KEY.
      *> §1985:VIII.4.3.4 GR7 - FS is set to 99 before each DELETE and
      *> must read 00 after a success, 23 after an invalid key
      *> (§1985:VIII.1.3.4).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F2303.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RND-FILE ASSIGN TO F2303RND
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS RND-KEY
               FILE STATUS IS FS.
           SELECT DYN-FILE ASSIGN TO F2303DYN
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS DYNAMIC
               RELATIVE KEY IS DYN-KEY
               FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD  RND-FILE
           LABEL RECORDS ARE STANDARD.
       01  RND-REC PIC X(5).
       FD  DYN-FILE
           LABEL RECORDS ARE STANDARD.
       01  DYN-REC PIC X(5).
       WORKING-STORAGE SECTION.
       01  RND-KEY PIC 9(4).
       01  DYN-KEY PIC 9(4).
       01  FS PIC XX.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RND-FILE.
           MOVE 1 TO RND-KEY.
           MOVE "ONE" TO RND-REC.
           WRITE RND-REC INVALID KEY DISPLAY "W1 INVALID".
           MOVE 2 TO RND-KEY.
           MOVE "TWO" TO RND-REC.
           WRITE RND-REC INVALID KEY DISPLAY "W2 INVALID".
           MOVE 3 TO RND-KEY.
           MOVE "THREE" TO RND-REC.
           WRITE RND-REC INVALID KEY DISPLAY "W3 INVALID".
           CLOSE RND-FILE.
           OPEN I-O RND-FILE.
           MOVE 2 TO RND-KEY.
           MOVE "99" TO FS.
           DELETE RND-FILE RECORD
               INVALID KEY DISPLAY "D2 INVALID"
               NOT INVALID KEY DISPLAY "D2 DELETED"
           END-DELETE.
           DISPLAY "D2 " FS.
           MOVE "99" TO FS.
           DELETE RND-FILE RECORD
               INVALID KEY DISPLAY "D2 AGAIN INVALID"
               NOT INVALID KEY DISPLAY "D2 AGAIN DELETED"
           END-DELETE.
           DISPLAY "D2 AGAIN " FS.
           MOVE 5 TO RND-KEY.
           MOVE "99" TO FS.
           DELETE RND-FILE RECORD
               INVALID KEY DISPLAY "D5 INVALID"
               NOT INVALID KEY DISPLAY "D5 DELETED"
           END-DELETE.
           DISPLAY "D5 " FS.
           MOVE 1 TO RND-KEY.
           READ RND-FILE INVALID KEY DISPLAY "R1 INVALID".
           DISPLAY "R1 " FS " " RND-REC "|".
           MOVE 3 TO RND-KEY.
           READ RND-FILE INVALID KEY DISPLAY "R3 INVALID".
           DISPLAY "R3 " FS " " RND-REC "|".
           MOVE 2 TO RND-KEY.
           READ RND-FILE INVALID KEY DISPLAY "R2 INVALID".
           DISPLAY "R2 " FS.
           CLOSE RND-FILE.
           OPEN OUTPUT DYN-FILE.
           MOVE 1 TO DYN-KEY.
           MOVE "UNO" TO DYN-REC.
           WRITE DYN-REC INVALID KEY DISPLAY "V1 INVALID".
           MOVE 2 TO DYN-KEY.
           MOVE "DOS" TO DYN-REC.
           WRITE DYN-REC INVALID KEY DISPLAY "V2 INVALID".
           CLOSE DYN-FILE.
           OPEN I-O DYN-FILE.
           MOVE 1 TO DYN-KEY.
           MOVE "99" TO FS.
           DELETE DYN-FILE RECORD
               INVALID KEY DISPLAY "E1 INVALID"
               NOT INVALID KEY DISPLAY "E1 DELETED"
           END-DELETE.
           DISPLAY "E1 " FS.
           MOVE 1 TO DYN-KEY.
           READ DYN-FILE INVALID KEY DISPLAY "Q1 INVALID".
           DISPLAY "Q1 " FS.
           MOVE 2 TO DYN-KEY.
           READ DYN-FILE INVALID KEY DISPLAY "Q2 INVALID".
           DISPLAY "Q2 " FS " " DYN-REC "|".
           CLOSE DYN-FILE.
           STOP RUN.
