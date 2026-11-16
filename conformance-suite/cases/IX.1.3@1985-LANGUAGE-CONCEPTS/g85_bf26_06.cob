      *>--- conformance-case
      *> id: g85_bf26_06
      *> rules: [1985-GR-IX.1.3.5-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IX.1.3.5 GR1 - on an indexed file the invalid key
      *> condition sets the I-O status to a value indicating it, and
      *> does so before the INVALID KEY imperative runs: 22 for a WRITE
      *> of a duplicate prime record key, 23 for the random READ,
      *> REWRITE and DELETE of an absent key and for a START no record
      *> satisfies (§1985:IX.1.3.4, I-O Status = 22 and 23). Each
      *> status is planted as 99 first, so a statement that does not
      *> set it, or sets it after the imperative, prints 99.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF2606.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IX-FILE ASSIGN TO "bf2606.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS IX-KEY
               FILE STATUS IS WS-STAT.
       DATA DIVISION.
       FILE SECTION.
       FD  IX-FILE.
       01  IX-REC.
           05  IX-KEY  PIC X(4).
           05  IX-DATA PIC X(6).
       WORKING-STORAGE SECTION.
       01  WS-STAT PIC XX VALUE "??".
       PROCEDURE DIVISION.
       P-1.
           OPEN OUTPUT IX-FILE.
           MOVE "K001" TO IX-KEY.
           MOVE "DATA01" TO IX-DATA.
           WRITE IX-REC INVALID KEY DISPLAY "W0 INV " WS-STAT.
           CLOSE IX-FILE.
           OPEN I-O IX-FILE.
           MOVE "K001" TO IX-KEY.
           MOVE "99" TO WS-STAT.
           WRITE IX-REC INVALID KEY DISPLAY "WR " WS-STAT.
           MOVE "K009" TO IX-KEY.
           MOVE "99" TO WS-STAT.
           READ IX-FILE INVALID KEY DISPLAY "RD " WS-STAT.
           MOVE "K009" TO IX-KEY.
           MOVE "99" TO WS-STAT.
           REWRITE IX-REC INVALID KEY DISPLAY "RW " WS-STAT.
           MOVE "K009" TO IX-KEY.
           MOVE "99" TO WS-STAT.
           DELETE IX-FILE INVALID KEY DISPLAY "DL " WS-STAT.
           MOVE "K009" TO IX-KEY.
           MOVE "99" TO WS-STAT.
           START IX-FILE KEY IS EQUAL TO IX-KEY
               INVALID KEY DISPLAY "ST " WS-STAT.
           CLOSE IX-FILE.
           STOP RUN.
