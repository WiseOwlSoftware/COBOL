      *>--- conformance-case
      *> id: pb5731_indexed_key_format3_odo_minimum_at_maximum
      *> rules: [1985-SR-IX.2.7.3-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5731, review R1-1. §1985:IX.2.7.3 SR4 (cite.py OK: "where
      *> x equals the minimum record size specified for the file") reads the
      *> file's minimum record size, and §1985:VII.3.8.4 GR15 sizes a format
      *> 3 record at the MAXIMUM of its variable length items: R1 = 1 + 5 = 6
      *> characters, R2 = 7, so x = 6 and the key RK at positions 3 to 4
      *> is within it. Written RA=AB RK=KE RC=XYZ, read back by key.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5731IK.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IX ASSIGN TO IXFILE
               ORGANIZATION IS INDEXED
               ACCESS MODE IS RANDOM
               RECORD KEY IS RK.
       DATA DIVISION.
       FILE SECTION.
       FD  IX RECORD CONTAINS 6 TO 7 CHARACTERS.
       01  R1.
           05  RN PIC 9.
           05  RT PIC X OCCURS 1 TO 5 TIMES DEPENDING ON RN.
       01  R2.
           05  RA PIC XX.
           05  RK PIC XX.
           05  RC PIC XXX.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT IX.
           MOVE "ABKEXYZ" TO R2.
           WRITE R2 INVALID KEY DISPLAY "WRITE FAILED".
           CLOSE IX.
           OPEN INPUT IX.
           MOVE "KE" TO RK.
           READ IX INVALID KEY DISPLAY "READ FAILED".
           DISPLAY RA RK RC.
           CLOSE IX.
           STOP RUN.
