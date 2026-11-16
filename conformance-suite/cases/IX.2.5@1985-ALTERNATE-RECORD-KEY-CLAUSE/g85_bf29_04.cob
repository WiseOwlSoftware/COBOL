      *>--- conformance-case
      *> id: g85_bf29_04
      *> rules: [1985-GR-IX.2.5.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      * §1985:IX.2.5.4 GR1 - "An ALTERNATE RECORD KEY clause specifies an
      * alternate record key for the file with which this clause is
      * associated." R-ALT is declared as an alternate key, so it is a key
      * of the file: a random READ ... KEY IS R-ALT finds a record by it
      * (K1, §1985:IX.4.5.4 GR15, GR17), a value no record holds is the
      * invalid key 23 (K2), and START ... KEY on it followed by READ NEXT
      * walks the file in alternate-key order (W1-W5). The prime and
      * alternate orders are exact opposites (prime 01..04, alternate
      * AA=04 .. DD=01), so a walk that ignored the alternate key would
      * print 01 first instead of 04.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF2904.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "g85bf2904.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS R-KEY
               ALTERNATE RECORD KEY IS R-ALT
               FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD  F.
       01  R.
           05 R-KEY PIC X(2).
           05 R-ALT PIC X(2).
       WORKING-STORAGE SECTION.
       01  FS PIC XX.
       01  N  PIC 9.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT F.
           MOVE "01DD" TO R.
           WRITE R INVALID KEY DISPLAY "W01 INVALID " FS.
           MOVE "02CC" TO R.
           WRITE R INVALID KEY DISPLAY "W02 INVALID " FS.
           MOVE "03BB" TO R.
           WRITE R INVALID KEY DISPLAY "W03 INVALID " FS.
           MOVE "04AA" TO R.
           WRITE R INVALID KEY DISPLAY "W04 INVALID " FS.
           CLOSE F.
           OPEN INPUT F.
           MOVE "BB" TO R-ALT.
           READ F KEY IS R-ALT INVALID KEY DISPLAY "K1 INVALID " FS
               NOT INVALID KEY DISPLAY "K1 " FS " " R.
           MOVE "ZZ" TO R-ALT.
           READ F KEY IS R-ALT INVALID KEY DISPLAY "K2 INVALID " FS
               NOT INVALID KEY DISPLAY "K2 " FS " " R.
           MOVE "AA" TO R-ALT.
           START F KEY IS EQUAL TO R-ALT
               INVALID KEY DISPLAY "ST INVALID " FS.
           PERFORM WALK-ONE VARYING N FROM 1 BY 1 UNTIL N > 5.
           CLOSE F.
           STOP RUN.
       WALK-ONE.
           READ F NEXT AT END DISPLAY "W" N " AT END " FS
               NOT AT END DISPLAY "W" N " " FS " " R.
