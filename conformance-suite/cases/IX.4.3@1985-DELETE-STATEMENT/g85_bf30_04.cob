      *>--- conformance-case
      *> id: g85_bf30_04
      *> rules: [1985-GR-IX.4.3.4-1, 1985-GR-IX.4.3.4-3,
      *>    1985-GR-IX.4.3.4-5, 1985-GR-IX.4.3.4-6]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> Indexed I-O DELETE (§1985:IX.4.3).
      *> §1985:IX.4.3.4 GR1 - the file "must be open in the I-O mode at
      *> the time of the execution of this statement".  RDEL, DDEL1 and
      *> DDEL3 are the statement succeeding in the I-O mode (00).  R49,
      *> the program's LAST I-O statement: a DELETE on R open INPUT is
      *> status 49 (§1985:IX.1.3.4, "a file not open in the I-O mode"),
      *> not an invalid key (no R49-IK line), so the USE procedure on R
      *> runs (§1985:IX.1.3.5 GRL2.1, §1985:IX.4.8.4 GR5a) with the
      *> status already set and displays it.  What happens after a
      *> critical status is the implementor's (§1985:IX.4.8.4 GR6), so
      *> nothing follows but STOP RUN and no line depends on it.
      *> §1985:IX.4.3.4 GR3 - in random (file R) and dynamic (file D)
      *> access the MSCS removes "the record identified by the content
      *> of the prime record key data item"; an absent key is the
      *> invalid key condition.  RDEL runs with R-KEY = K02 while the
      *> ALTERNATE key field names K03's value A03 and the rest of the
      *> area is junk: K02 must go (RK02=23) and K03 must stay (RK03).
      *> RIK09 and DIK0: no such key, status 23, and the INVALID KEY
      *> imperative runs while NOT INVALID KEY does not
      *> (§1985:IX.1.3.5).  DK01/DK03: the dynamic deletes removed
      *> exactly the records whose prime keys were in D-KEY.
      *> §1985:IX.4.3.4 GR5 - the record area and the DEPENDING ON item
      *> WL are unaffected: sentinel bytes around the key and sentinel
      *> lengths are stored before each dynamic DELETE and must survive
      *> it, after a success (DDEL1, DDEL3) and an invalid key (DDEL0).
      *> §1985:IX.4.3.4 GR6 - the file position indicator is unaffected,
      *> seen through the next READ NEXT (§1985:IX.4.5.4 GR4d: the
      *> first record whose key is greater than the indicator).  DDEL1
      *> removes K01 while the indicator is K02: DN3 must be K03 (an
      *> indicator taken from the key would give K02).  DDEL3 removes
      *> the record AT the indicator: DN4 must be K04 (an invalidated
      *> indicator gives 46).  The invalid DDEL0 (key K00, below every
      *> record) must not move it either: DN5 is K05, not K02.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF3004.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT R ASSIGN TO "g85bf3004r.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS RANDOM
               RECORD KEY IS R-KEY
               ALTERNATE RECORD KEY IS R-ALT
               FILE STATUS IS RS.
           SELECT D ASSIGN TO "g85bf3004d.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS D-KEY
               FILE STATUS IS DS.
       DATA DIVISION.
       FILE SECTION.
       FD R.
       01 R-REC.
           05 R-PRE PIC X(2).
           05 R-KEY PIC X(3).
           05 R-ALT PIC X(3).
       FD D
           RECORD IS VARYING IN SIZE FROM 5 TO 8 CHARACTERS
               DEPENDING ON WL.
       01 D-REC.
           05 D-PRE PIC X(2).
           05 D-KEY PIC X(3).
           05 D-TAIL PIC X(3).
       WORKING-STORAGE SECTION.
       01 RS PIC XX.
       01 DS PIC XX.
       01 WL PIC 9(2).
       PROCEDURE DIVISION.
       DECLARATIVES.
       R-ERR SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON R.
       R-ERR-PARA.
           DISPLAY "R49=" RS.
       END DECLARATIVES.
       MAIN-SECTION SECTION.
       MAIN-PARA.
           OPEN OUTPUT R.
           MOVE "aaK01A01" TO R-REC.
           WRITE R-REC INVALID KEY DISPLAY "WR1-IK".
           MOVE "bbK02A02" TO R-REC.
           WRITE R-REC INVALID KEY DISPLAY "WR2-IK".
           MOVE "ccK03A03" TO R-REC.
           WRITE R-REC INVALID KEY DISPLAY "WR3-IK".
           CLOSE R.
      * GR3, random access: the prime key alone names the record.
           OPEN I-O R.
           MOVE "zzK02A03" TO R-REC.
           DELETE R RECORD
               INVALID KEY DISPLAY "RDEL-IK"
               NOT INVALID KEY DISPLAY "RDEL=" RS
           END-DELETE.
           MOVE "K02" TO R-KEY.
           READ R INVALID KEY DISPLAY "RK02=" RS.
           MOVE "K03" TO R-KEY.
           READ R INVALID KEY DISPLAY "RK03-IK".
           DISPLAY "RK03=" RS " " R-REC.
           MOVE "K09" TO R-KEY.
           DELETE R RECORD
               INVALID KEY DISPLAY "RIK09=" RS
               NOT INVALID KEY DISPLAY "RIK09-NOT-IK"
           END-DELETE.
           CLOSE R.
      * Dynamic access.
           OPEN OUTPUT D.
           MOVE 8 TO WL.
           MOVE "p1K01t01" TO D-REC.
           WRITE D-REC INVALID KEY DISPLAY "WD1-IK".
           MOVE "p2K02t02" TO D-REC.
           WRITE D-REC INVALID KEY DISPLAY "WD2-IK".
           MOVE "p3K03t03" TO D-REC.
           WRITE D-REC INVALID KEY DISPLAY "WD3-IK".
           MOVE "p4K04t04" TO D-REC.
           WRITE D-REC INVALID KEY DISPLAY "WD4-IK".
           MOVE "p5K05t05" TO D-REC.
           WRITE D-REC INVALID KEY DISPLAY "WD5-IK".
           CLOSE D.
           OPEN I-O D.
           READ D NEXT AT END DISPLAY "DN1-ATEND".
           DISPLAY "DN1=" DS " " D-REC.
           READ D NEXT AT END DISPLAY "DN2-ATEND".
           DISPLAY "DN2=" DS " " D-REC.
      * Delete K01, behind the indicator (K02).
           MOVE "##K01###" TO D-REC.
           MOVE 6 TO WL.
           DELETE D RECORD INVALID KEY DISPLAY "DDEL1-IK".
           DISPLAY "DDEL1=" DS " " D-REC "] " WL.
           READ D NEXT AT END DISPLAY "DN3-ATEND".
           DISPLAY "DN3=" DS " " D-REC.
      * Delete K03, the record at the indicator.
           MOVE "@@K03@@@" TO D-REC.
           MOVE 7 TO WL.
           DELETE D RECORD INVALID KEY DISPLAY "DDEL3-IK".
           DISPLAY "DDEL3=" DS " " D-REC "] " WL.
           READ D NEXT AT END DISPLAY "DN4-ATEND".
           DISPLAY "DN4=" DS " " D-REC.
      * An absent key: the invalid key condition.
           MOVE "$$K00$$$" TO D-REC.
           MOVE 5 TO WL.
           DELETE D RECORD
               INVALID KEY DISPLAY "DIK0=" DS
               NOT INVALID KEY DISPLAY "DIK0-NOT-IK"
           END-DELETE.
           DISPLAY "DDEL0=" D-REC "] " WL.
           READ D NEXT AT END DISPLAY "DN5-ATEND".
           DISPLAY "DN5=" DS " " D-REC.
           READ D NEXT AT END DISPLAY "DEND=" DS.
           MOVE "K01" TO D-KEY.
           READ D INVALID KEY DISPLAY "DK01=" DS.
           MOVE "K03" TO D-KEY.
           READ D INVALID KEY DISPLAY "DK03=" DS.
           CLOSE D.
      * GR1: not open in the I-O mode.  The last I-O statement.
           OPEN INPUT R.
           MOVE "K01" TO R-KEY.
           DELETE R RECORD INVALID KEY DISPLAY "R49-IK".
           STOP RUN.
