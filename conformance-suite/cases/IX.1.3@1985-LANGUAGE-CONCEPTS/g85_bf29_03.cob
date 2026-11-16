      *>--- conformance-case
      *> id: g85_bf29_03
      *> rules: [1985-GR-IX.1.3.4-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      * §1985:IX.1.3.4 GR2 - "At End. A sequential READ statement was
      * unsuccessfully executed as a result of an at end condition." An
      * indexed file of two records is read sequentially: the first two
      * READs succeed (NOT AT END, status 00); the third finds no next
      * logical record (§1985:IX.4.5.4 GR4, GR10), so it takes AT END, skips
      * NOT AT END, and the status is the at end class 10 (§1985:IX.1.3.4
      * GRL2.2 a). The leg fails if the third READ takes NOT AT END or sets
      * a status outside class 1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF2903.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IX ASSIGN TO "g85bf2903.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS IX-KEY
               FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD  IX.
       01  IX-REC.
           05 IX-KEY  PIC X(2).
           05 IX-DATA PIC X(3).
       WORKING-STORAGE SECTION.
       01  FS PIC XX.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT IX.
           MOVE "01ONE" TO IX-REC.
           WRITE IX-REC INVALID KEY DISPLAY "W1 INVALID " FS.
           MOVE "02TWO" TO IX-REC.
           WRITE IX-REC INVALID KEY DISPLAY "W2 INVALID " FS.
           CLOSE IX.
           OPEN INPUT IX.
           READ IX AT END DISPLAY "R1 AT END " FS
               NOT AT END DISPLAY "R1 READ " IX-REC " " FS.
           READ IX AT END DISPLAY "R2 AT END " FS
               NOT AT END DISPLAY "R2 READ " IX-REC " " FS.
           READ IX AT END DISPLAY "R3 AT END " FS
               NOT AT END DISPLAY "R3 READ " IX-REC " " FS.
           IF FS (1:1) = "1"
               DISPLAY "R3 CLASS AT END"
           ELSE
               DISPLAY "R3 CLASS OTHER".
           CLOSE IX.
           STOP RUN.
