      *>--- conformance-case
      *> id: pb5217_write_beside_odo_record
      *> rules: [GR-14.9.51.4-13, GR-14.9.35.4-15, GR-14.9.51.4-42]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5217 - a WRITE or REWRITE in a file one of whose record
      *> descriptions holds an OCCURS DEPENDING ON table. Every level-1
      *> entry of an FD redefines the same area (13.18.33.4 GR3), so the
      *> record that table's DEPENDING item sizes is that record alone:
      *> its current extent is not the area's, nor another record's.
      *> Each file below has R (4 characters and 1 to 6 table elements,
      *> DEPENDING ON N) and shorter records; N is 1 at every statement,
      *> so R's current extent is 5 of the area's 10 positions.
      *>
      *> No file here has a RECORD clause except VF, so each is fixed
      *> length 10, the largest record (13.18.43.4 GR5 a), docs/CONFORMANCE
      *> DOC-A.1-147; at COBOL-85 the implementor's choice, 1985 II.2.1.4.3).
      *> A WRITE or REWRITE of a record shorter than that transfers 10
      *> bytes, and the content past record-name-1 (14.9.51.4 GR13,
      *> 14.9.35.4 GR15: undefined) is the record area's at those
      *> positions (docs/CONFORMANCE.md 3, D-WRT1). For R itself at one
      *> element that is positions 6-10 of the area (13.18.43.4 GR13 c)
      *> makes R 5 bytes there).
      *>
      *> EXPECTED VALUES (derived from the rules, not from a run). Before
      *> each WRITE the area is filled with N = 6, then N is set to 1:
      *>   S1  WRITE R3 (7): ABCDEFG then area positions 8-10, ZZZ.
      *>   S2  WRITE R3 FROM WS-SRC: 0123456 then ZZZ.
      *>   S3  WRITE R at one element: abcde then area positions 6-10,
      *>       fghij, which the MOVE at six elements left there.
      *>   S4  REWRITE R of record 1 after its READ, R(1:1) set to Q:
      *>       QBCDEFGZZZ - the record read, not QBCDE and spaces.
      *>   RL  relative: WRITE R3 then REWRITE R3 after R3A = WXYZ:
      *>       ABCDEFGZZZ, then WXYZEFGZZZ.
      *>   IX  indexed, RECORD KEY K at positions 5-8 of R4, past R's
      *>       current extent: the WRITE, the WRITE FROM, a keyed READ, a
      *>       REWRITE, a START and a DELETE each find the key the record
      *>       holds (status 00); the deleted key then reads 23.
      *>       Then the abnormal paths, N still 1: a WRITE of zzzzKEY1
      *>       finds KEY1 in the file, so INVALID KEY, 22 (14.9.51.4
      *>       GR42 b), and the record is not written (GR15): KEY1
      *>       still reads ABCDKEY1. KEY9 is in no record: START EQUAL
      *>       (14.9.41.4 GR17 e 2), DELETE (14.9.10.4 GR3) and REWRITE
      *>       (14.9.35.4 GR23) each take INVALID KEY with status 23.
      *>   VF  RECORD VARYING 1 TO 10 DEPENDING ON L, L = 9: WRITE R3
      *>       writes 9 bytes (13.18.43.4 GR13 a): ABCDEFG then area
      *>       positions 8-9, YY; read back through R9 (9 positions).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5217WO.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SF1 ASSIGN TO "pb5217wo-s.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS FS.
           SELECT LF1 ASSIGN TO "pb5217wo-r.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS FS.
           SELECT XF1 ASSIGN TO "pb5217wo-x.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS K
               FILE STATUS IS FS.
           SELECT VF1 ASSIGN TO "pb5217wo-v.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD  SF1.
       01  S-R.
           05 S-RK PIC X(4).
           05 S-RT PIC X OCCURS 1 TO 6 DEPENDING ON N.
       01  S-R3 PIC X(7).
       FD  LF1.
       01  R-R.
           05 R-RK PIC X(4).
           05 R-RT PIC X OCCURS 1 TO 6 DEPENDING ON N.
       01  R-R3.
           05 R-R3A PIC X(4).
           05 FILLER PIC X(3).
       FD  XF1.
       01  X-R.
           05 X-RK PIC X(4).
           05 X-RT PIC X OCCURS 1 TO 6 DEPENDING ON N.
       01  X-R4.
           05 X-R4A PIC X(4).
           05 K     PIC X(4).
       FD  VF1
           RECORD IS VARYING IN SIZE FROM 1 TO 10 DEPENDING ON L.
       01  V-R.
           05 V-RK PIC X(4).
           05 V-RT PIC X OCCURS 1 TO 6 DEPENDING ON N.
       01  V-R3 PIC X(7).
       01  V-R9 PIC X(9).
       WORKING-STORAGE SECTION.
       01  FS PIC XX.
       01  N PIC 9 VALUE 1.
       01  L PIC 99 VALUE 10.
       01  WS-SRC PIC X(7) VALUE "0123456".
       01  WS-X8 PIC X(8) VALUE "wxyzKEY2".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT SF1.
           MOVE 6 TO N.
           MOVE ALL "Z" TO S-R.
           MOVE 1 TO N.
           MOVE "ABCDEFG" TO S-R3.
           WRITE S-R3.
           DISPLAY "S1 W " FS.
           WRITE S-R3 FROM WS-SRC.
           DISPLAY "S2 W " FS.
           MOVE 6 TO N.
           MOVE "abcdefghij" TO S-R.
           MOVE 1 TO N.
           WRITE S-R.
           DISPLAY "S3 W " FS.
           CLOSE SF1.
           OPEN I-O SF1.
           READ SF1 AT END DISPLAY "S4 EOF".
           MOVE 1 TO N.
           MOVE "Q" TO S-RK (1:1).
           REWRITE S-R.
           DISPLAY "S4 RW " FS.
           CLOSE SF1.
           OPEN INPUT SF1.
           MOVE 6 TO N.
           PERFORM 4 TIMES
               READ SF1 AT END DISPLAY "S EOF"
                   NOT AT END DISPLAY "S [" S-R "]"
               END-READ
           END-PERFORM.
           CLOSE SF1.
      *> relative
           OPEN OUTPUT LF1.
           MOVE 6 TO N.
           MOVE ALL "Z" TO R-R.
           MOVE 1 TO N.
           MOVE "ABCDEFG" TO R-R3.
           WRITE R-R3 INVALID KEY DISPLAY "RL IK".
           DISPLAY "RL W " FS.
           CLOSE LF1.
           OPEN I-O LF1.
           READ LF1 AT END DISPLAY "RL EOF".
           MOVE 6 TO N.
           DISPLAY "RL [" R-R "]".
           MOVE 1 TO N.
           MOVE "WXYZ" TO R-R3A.
           REWRITE R-R3.
           DISPLAY "RL RW " FS.
           CLOSE LF1.
           OPEN INPUT LF1.
           READ LF1 AT END DISPLAY "RL EOF".
           MOVE 6 TO N.
           DISPLAY "RL [" R-R "]".
           CLOSE LF1.
      *> indexed, the key past R's current extent
           OPEN OUTPUT XF1.
           MOVE 1 TO N.
           MOVE "abcdKEY1" TO X-R4.
           WRITE X-R4 INVALID KEY DISPLAY "IX IK1".
           DISPLAY "IX W1 " FS.
           WRITE X-R4 FROM WS-X8 INVALID KEY DISPLAY "IX IK2".
           DISPLAY "IX W2 " FS.
           CLOSE XF1.
           OPEN I-O XF1.
           MOVE "KEY1" TO K.
           READ XF1 INVALID KEY DISPLAY "IX RK1".
           DISPLAY "IX R1 " FS " [" X-R4 "]".
           MOVE "ABCD" TO X-R4A.
           REWRITE X-R4 INVALID KEY DISPLAY "IX RWK".
           DISPLAY "IX RW " FS.
           MOVE "KEY2" TO K.
           START XF1 KEY IS EQUAL TO K INVALID KEY DISPLAY "IX STK".
           DISPLAY "IX ST " FS.
           READ XF1 NEXT RECORD AT END DISPLAY "IX EOF".
           DISPLAY "IX R2 " FS " [" X-R4 "]".
           MOVE "KEY2" TO K.
           DELETE XF1 RECORD INVALID KEY DISPLAY "IX DLK".
           DISPLAY "IX DL " FS.
           MOVE "KEY1" TO K.
           READ XF1 INVALID KEY DISPLAY "IX RK3".
           DISPLAY "IX R3 " FS " [" X-R4 "]".
           MOVE "KEY2" TO K.
           READ XF1 INVALID KEY DISPLAY "IX RK4 " FS.
      *> the abnormal paths, each key past R's current extent
           MOVE 1 TO N.
           MOVE "zzzzKEY1" TO X-R4.
           WRITE X-R4 INVALID KEY DISPLAY "IX IK5 " FS
               NOT INVALID KEY DISPLAY "IX W5 " FS
           END-WRITE.
           MOVE "KEY1" TO K.
           READ XF1 INVALID KEY DISPLAY "IX RK6".
           DISPLAY "IX R6 " FS " [" X-R4 "]".
           MOVE "KEY9" TO K.
           START XF1 KEY IS EQUAL TO K
               INVALID KEY DISPLAY "IX STK7 " FS
               NOT INVALID KEY DISPLAY "IX ST7 " FS
           END-START.
           MOVE "KEY9" TO K.
           DELETE XF1 RECORD
               INVALID KEY DISPLAY "IX DLK8 " FS
               NOT INVALID KEY DISPLAY "IX DL8 " FS
           END-DELETE.
           MOVE "yyyyKEY9" TO X-R4.
           REWRITE X-R4
               INVALID KEY DISPLAY "IX RWK9 " FS
               NOT INVALID KEY DISPLAY "IX RW9 " FS
           END-REWRITE.
           CLOSE XF1.
      *> variable length, DEPENDING ON L
           OPEN OUTPUT VF1.
           MOVE 6 TO N.
           MOVE ALL "Y" TO V-R.
           MOVE 1 TO N.
           MOVE "ABCDEFG" TO V-R3.
           MOVE 9 TO L.
           WRITE V-R3.
           DISPLAY "VF W " FS.
           CLOSE VF1.
           OPEN INPUT VF1.
           MOVE 0 TO L.
           READ VF1 AT END DISPLAY "VF EOF".
           DISPLAY "VF " FS " " L " [" V-R9 "]".
           CLOSE VF1.
           STOP RUN.
