      *>--- conformance-case
      *> id: g85_bf25_06
      *> rules: [1985-GR-IX.2.5.4-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IX.2.5.4 GR2 - the alternate record keys presented when
      *> an indexed file is opened are those it was created with.
      *  GR2: "The data description of data-name-1 as well as its
      *  relative location within a record must be the same as that
      *  used when the file was created. The number of alternate
      *  record keys for the file must also be the same as that used
      *  when the file was created." §1985:IX.4.4.4 GR8: a file
      *  attribute conflict makes the OPEN unsuccessful, and "The
      *  implementor defines which of the fixed file attributes are
      *  validated"; WiseOwl COBOL validates the key count and each
      *  key's position and length (docs/conformance/7/DOC-A.1-129.md),
      *  so a difference is status 39 (§1985:IX.1.3.4).
      *  IX-MAKE creates the file: prime key X(4) at positions 1-4,
      *  one alternate key X(3) WITH DUPLICATES at positions 5-7.
      *  S1  Write three records, two sharing alternate value RED.
      *  S2  IX-SAME (other names, the same two keys): OPEN 00. A
      *      READ by the alternate key GRN finds K002; a READ by RED
      *      finds the first RED record written, K001
      *      (§1985:IX.4.5.4 GR17), with status 02 because the next
      *      record by that key, K003, has the same value
      *      (§1985:IX.1.3.4).
      *  S3  IX-MOVED has the alternate X(3) at positions 6-8: 39.
      *  S4  IX-NOALT declares no alternate key (count 0): 39.
      *  S5  IX-TWO declares two alternate keys (count 2): 39.
      *  S6  IX-MAKE still opens 00 and reads K003 by its prime key.
      *  A leg fails if a matching description is refused, a READ by
      *  the alternate key misses, or a moved key or a different key
      *  count is accepted.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF2506.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IX-MAKE ASSIGN TO "g85bf2506.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS RANDOM
               RECORD KEY IS MK-KEY
               ALTERNATE RECORD KEY IS MK-ALT WITH DUPLICATES
               FILE STATUS IS MK-ST.
           SELECT IX-SAME ASSIGN TO "g85bf2506.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS RANDOM
               RECORD KEY IS SM-KEY
               ALTERNATE RECORD KEY IS SM-ALT WITH DUPLICATES
               FILE STATUS IS SM-ST.
           SELECT IX-MOVED ASSIGN TO "g85bf2506.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS RANDOM
               RECORD KEY IS MV-KEY
               ALTERNATE RECORD KEY IS MV-ALT WITH DUPLICATES
               FILE STATUS IS MV-ST.
           SELECT IX-NOALT ASSIGN TO "g85bf2506.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS RANDOM
               RECORD KEY IS NA-KEY
               FILE STATUS IS NA-ST.
           SELECT IX-TWO ASSIGN TO "g85bf2506.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS RANDOM
               RECORD KEY IS TW-KEY
               ALTERNATE RECORD KEY IS TW-ALT WITH DUPLICATES
               ALTERNATE RECORD KEY IS TW-ALT2 WITH DUPLICATES
               FILE STATUS IS TW-ST.
       DATA DIVISION.
       FILE SECTION.
       FD  IX-MAKE.
       01  MK-REC.
           05 MK-KEY  PIC X(4).
           05 MK-ALT  PIC X(3).
           05 MK-DATA PIC X(13).
       FD  IX-SAME.
       01  SM-REC.
           05 SM-KEY  PIC X(4).
           05 SM-ALT  PIC X(3).
           05 SM-REST PIC X(13).
       FD  IX-MOVED.
       01  MV-REC.
           05 MV-KEY  PIC X(4).
           05 MV-GAP  PIC X.
           05 MV-ALT  PIC X(3).
           05 MV-REST PIC X(12).
       FD  IX-NOALT.
       01  NA-REC.
           05 NA-KEY  PIC X(4).
           05 NA-REST PIC X(16).
       FD  IX-TWO.
       01  TW-REC.
           05 TW-KEY  PIC X(4).
           05 TW-ALT  PIC X(3).
           05 TW-ALT2 PIC X(3).
           05 TW-REST PIC X(10).
       WORKING-STORAGE SECTION.
       01  MK-ST PIC XX.
       01  SM-ST PIC XX.
       01  MV-ST PIC XX.
       01  NA-ST PIC XX.
       01  TW-ST PIC XX.
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT IX-MAKE.
           MOVE "K001" TO MK-KEY.
           MOVE "RED" TO MK-ALT.
           MOVE "APPLE" TO MK-DATA.
           WRITE MK-REC INVALID KEY DISPLAY "S1-W1-INVALID".
           MOVE "K002" TO MK-KEY.
           MOVE "GRN" TO MK-ALT.
           MOVE "PEAR" TO MK-DATA.
           WRITE MK-REC INVALID KEY DISPLAY "S1-W2-INVALID".
           MOVE "K003" TO MK-KEY.
           MOVE "RED" TO MK-ALT.
           MOVE "CHERRY" TO MK-DATA.
           WRITE MK-REC INVALID KEY DISPLAY "S1-W3-INVALID".
           CLOSE IX-MAKE.
           DISPLAY "S1-MAKE=" MK-ST.
           OPEN INPUT IX-SAME.
           DISPLAY "S2-OPEN=" SM-ST.
           MOVE "GRN" TO SM-ALT.
           READ IX-SAME KEY IS SM-ALT
               INVALID KEY DISPLAY "S2-GRN-INVALID".
           DISPLAY "S2-GRN=" SM-ST " [" SM-REC "]".
           MOVE "RED" TO SM-ALT.
           READ IX-SAME KEY IS SM-ALT
               INVALID KEY DISPLAY "S2-RED-INVALID".
           DISPLAY "S2-RED=" SM-ST " [" SM-REC "]".
           CLOSE IX-SAME.
           OPEN INPUT IX-MOVED.
           DISPLAY "S3-MOVED=" MV-ST.
           IF MV-ST = "00"
               CLOSE IX-MOVED.
           OPEN INPUT IX-NOALT.
           DISPLAY "S4-NOALT=" NA-ST.
           IF NA-ST = "00"
               CLOSE IX-NOALT.
           OPEN INPUT IX-TWO.
           DISPLAY "S5-TWO=" TW-ST.
           IF TW-ST = "00"
               CLOSE IX-TWO.
           OPEN INPUT IX-MAKE.
           DISPLAY "S6-OPEN=" MK-ST.
           MOVE "K003" TO MK-KEY.
           READ IX-MAKE INVALID KEY DISPLAY "S6-INVALID".
           DISPLAY "S6-READ=" MK-ST " [" MK-REC "]".
           CLOSE IX-MAKE.
           STOP RUN.
