      *>--- conformance-case
      *> id: g85_bf26_08
      *> rules: [1985-GR-IX.4.8.4-5]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IX.4.8.4 GR5 - which USE procedure runs for an indexed
      *> file. a: IX-A is named, so its failed OPEN INPUT (35) runs
      *> USE-FILE and not USE-INPUT (the "except those files" clause
      *> of b). e: OPEN EXTEND of an absent file runs USE-EXTEND. c: a
      *> duplicate WRITE with no INVALID KEY on a file open OUTPUT runs
      *> USE-OUTPUT; with INVALID KEY that phrase takes precedence and
      *> no USE runs. b: on a file open INPUT the AT END phrase takes
      *> precedence (AT-END 10, no USE) and a random READ of an absent
      *> key with no INVALID KEY runs USE-INPUT. d: the same READ on a
      *> file open I-O runs USE-I-O. Each leg fails if the wrong
      *> procedure, or any procedure where a phrase takes precedence,
      *> displays its name. WiseOwl COBOL continues after a critical
      *> 35 (docs/conformance/3.5, kb/Work PB5591).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF2608.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IX-A ASSIGN TO "bf2608a.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS A-KEY
               FILE STATUS IS FS-A.
           SELECT IX-B ASSIGN TO "bf2608b.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS B-KEY
               FILE STATUS IS FS-B.
           SELECT IX-C ASSIGN TO "bf2608c.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS C-KEY
               FILE STATUS IS FS-C.
       DATA DIVISION.
       FILE SECTION.
       FD  IX-A.
       01  A-REC.
           05  A-KEY  PIC X(4).
           05  A-DATA PIC X(6).
       FD  IX-B.
       01  B-REC.
           05  B-KEY  PIC X(4).
           05  B-DATA PIC X(6).
       FD  IX-C.
       01  C-REC.
           05  C-KEY  PIC X(4).
           05  C-DATA PIC X(6).
       WORKING-STORAGE SECTION.
       01  FS-A PIC XX VALUE "??".
       01  FS-B PIC XX VALUE "??".
       01  FS-C PIC XX VALUE "??".
       PROCEDURE DIVISION.
       DECLARATIVES.
       D-FILE SECTION.
           USE AFTER STANDARD EXCEPTION PROCEDURE ON IX-A.
       D-FILE-1.
           DISPLAY "USE-FILE".
       D-IN SECTION.
           USE AFTER STANDARD EXCEPTION PROCEDURE ON INPUT.
       D-IN-1.
           DISPLAY "USE-INPUT".
       D-OUT SECTION.
           USE AFTER STANDARD EXCEPTION PROCEDURE ON OUTPUT.
       D-OUT-1.
           DISPLAY "USE-OUTPUT".
       D-IO SECTION.
           USE AFTER STANDARD EXCEPTION PROCEDURE ON I-O.
       D-IO-1.
           DISPLAY "USE-I-O".
       D-EXT SECTION.
           USE AFTER STANDARD EXCEPTION PROCEDURE ON EXTEND.
       D-EXT-1.
           DISPLAY "USE-EXTEND".
       END DECLARATIVES.
       MAIN-1 SECTION.
       P-1.
           OPEN INPUT IX-A.
           DISPLAY "OPEN-A " FS-A.
           OPEN EXTEND IX-C.
           DISPLAY "OPEN-C " FS-C.
           OPEN OUTPUT IX-B.
           MOVE "K001" TO B-KEY.
           MOVE "DATA01" TO B-DATA.
           WRITE B-REC.
           DISPLAY "WR-1 " FS-B.
           WRITE B-REC.
           DISPLAY "WR-DUP " FS-B.
           WRITE B-REC
               INVALID KEY DISPLAY "WR-INVALID-KEY " FS-B.
           CLOSE IX-B.
           OPEN INPUT IX-B.
           READ IX-B NEXT RECORD
               AT END DISPLAY "AT-END-1 " FS-B.
           DISPLAY "READ-NEXT " B-KEY.
           READ IX-B NEXT RECORD
               AT END DISPLAY "AT-END " FS-B.
           MOVE "K009" TO B-KEY.
           READ IX-B.
           DISPLAY "RD-MISS " FS-B.
           CLOSE IX-B.
           OPEN I-O IX-B.
           MOVE "K009" TO B-KEY.
           READ IX-B.
           DISPLAY "RD-IO-MISS " FS-B.
           CLOSE IX-B.
           STOP RUN.
