      *>--- conformance-case
      *> id: g85_f23_04
      *> rules: [1985-GR-IX.4.3.4-7]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IX.4.3.4 GR7 - DELETE updates the I-O status of an
      *> indexed file: FS is set to 99 before each DELETE and must read
      *> 00 after the removal of key 0002 and 23 after a DELETE of key
      *> 0002 again and of key 0009, which no record has
      *> (§1985:IX.1.3.4; §1985:IX.4.3.4 GR3). Fails if FS keeps 99 or
      *> takes another value; the re-read shows record 0001 survives.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F2304.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IDX-FILE ASSIGN TO F2304IDX
               ORGANIZATION IS INDEXED
               ACCESS MODE IS RANDOM
               RECORD KEY IS IDX-KEY
               FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD  IDX-FILE
           LABEL RECORDS ARE STANDARD.
       01  IDX-REC.
           05  IDX-KEY  PIC X(4).
           05  IDX-DATA PIC X(5).
       WORKING-STORAGE SECTION.
       01  FS PIC XX.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT IDX-FILE.
           MOVE "0001" TO IDX-KEY.
           MOVE "ONE" TO IDX-DATA.
           WRITE IDX-REC INVALID KEY DISPLAY "W1 INVALID".
           MOVE "0002" TO IDX-KEY.
           MOVE "TWO" TO IDX-DATA.
           WRITE IDX-REC INVALID KEY DISPLAY "W2 INVALID".
           CLOSE IDX-FILE.
           OPEN I-O IDX-FILE.
           MOVE "0002" TO IDX-KEY.
           MOVE "99" TO FS.
           DELETE IDX-FILE RECORD
               INVALID KEY DISPLAY "D2 INVALID"
               NOT INVALID KEY DISPLAY "D2 DELETED"
           END-DELETE.
           DISPLAY "D2 " FS.
           MOVE "0002" TO IDX-KEY.
           MOVE "99" TO FS.
           DELETE IDX-FILE RECORD
               INVALID KEY DISPLAY "D2 AGAIN INVALID"
           END-DELETE.
           DISPLAY "D2 AGAIN " FS.
           MOVE "0009" TO IDX-KEY.
           MOVE "99" TO FS.
           DELETE IDX-FILE RECORD
               INVALID KEY DISPLAY "D9 INVALID"
           END-DELETE.
           DISPLAY "D9 " FS.
           MOVE "0001" TO IDX-KEY.
           READ IDX-FILE INVALID KEY DISPLAY "R1 INVALID".
           DISPLAY "R1 " FS " " IDX-DATA "|".
           CLOSE IDX-FILE.
           STOP RUN.
