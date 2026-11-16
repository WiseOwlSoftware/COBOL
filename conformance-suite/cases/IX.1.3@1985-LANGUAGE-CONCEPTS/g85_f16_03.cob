      *>--- conformance-case
      *> id: g85_f16_03
      *> rules: [1985-GR-IX.1.3.4-5, 1985-GR-IX.1.3.5-L2.1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IX.1.3.4 GR5 - the logic error class of an indexed
      *> file. File SQF (sequential access) makes the improper
      *> sequences §1985:IX.1.3.4 GRL2.5 gives one value each: 41
      *> OPEN of an open file, 47 READ and START of a file open
      *> OUTPUT, 42 CLOSE of a closed file, 43 REWRITE and DELETE in
      *> I-O mode with no successful READ before them, 46 READ after
      *> the at end condition, 49 DELETE of a file open INPUT after a
      *> successful READ, 48 WRITE of a file open INPUT. The second
      *> cause GR5 names, violating a limit defined by the user, is
      *> leg L44 on file XV: a WRITE of 3 characters where RECORD IS
      *> VARYING allows 4 to 10 is unsuccessful (§1985:VII.3.8.4
      *> GR10, §1985:IX.4.9.4 GR8) with 44 (§1985:IX.1.3.4 GRL2.5 d,
      *> item 1, level-2 text). Lines L41..L44 pin GR5: status key 1
      *> must be 4.
      *> §1985:IX.1.3.5 GRL2.1 - file DYNF (dynamic access): each
      *> statement carries INVALID KEY and NOT INVALID KEY and raises
      *> an exception that is not an invalid key condition (48 WRITE
      *> on a file open INPUT, 49 REWRITE and DELETE on it, 47 READ
      *> and START of a closed file). The USE procedure must run
      *> (USE2 line) and control must go on to the next statement:
      *> no IK- line (the condition is not an invalid key) and no
      *> NIK- line (an exception exists, so GRL2.2's NOT INVALID KEY
      *> transfer does not apply). The first WRITE succeeds and shows
      *> the NOT INVALID KEY branch as a contrast.
      *> After a critical 4x status the implementor determines the
      *> action (§1985:IX.4.8.4 GR6); WiseOwl COBOL continues with
      *> the next executable statement (docs/conformance/3.5).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F1603.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SQF ASSIGN TO "g85f1603s.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS SQ-KEY
               FILE STATUS IS FS.
           SELECT DYNF ASSIGN TO "g85f1603d.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS DY-KEY
               FILE STATUS IS FS2.
           SELECT XV ASSIGN TO "g85f1603v.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS XV-KEY
               FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD  SQF
           LABEL RECORDS ARE STANDARD.
       01  SQREC.
           05 SQ-KEY           PIC X(4).
           05 SQ-DATA          PIC X(6).
       FD  DYNF
           LABEL RECORDS ARE STANDARD.
       01  DYREC.
           05 DY-KEY           PIC X(4).
           05 DY-DATA          PIC X(6).
       FD  XV
           LABEL RECORDS ARE STANDARD
           RECORD IS VARYING IN SIZE FROM 4 TO 10 CHARACTERS
               DEPENDING ON RL.
       01  XVREC.
           05 XV-KEY           PIC X(4).
           05 XV-DATA          PIC X(6).
       WORKING-STORAGE SECTION.
       01  FS.
           05 FS-1             PIC X.
           05 FS-2             PIC X.
       01  FS2                 PIC XX.
       01  RL                  PIC 99.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D-SQ SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON SQF XV.
       D-SQ-P.
           DISPLAY "USE " FS.
       D-DY SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON DYNF.
       D-DY-P.
           DISPLAY "USE2 " FS2.
       END DECLARATIVES.
       MAIN-LOGIC SECTION.
       M-41.
           OPEN OUTPUT SQF.
           MOVE "A001" TO SQ-KEY.
           MOVE "DATA1" TO SQ-DATA.
           WRITE SQREC.
           DISPLAY "W1 " FS.
           OPEN OUTPUT SQF.
           DISPLAY "L41 " FS " KEY1 " FS-1.
       M-47.
           READ SQF.
           DISPLAY "L47R " FS " KEY1 " FS-1.
           MOVE "A001" TO SQ-KEY.
           START SQF KEY IS EQUAL TO SQ-KEY.
           DISPLAY "L47S " FS " KEY1 " FS-1.
       M-42.
           CLOSE SQF.
           DISPLAY "C1 " FS.
           CLOSE SQF.
           DISPLAY "L42 " FS " KEY1 " FS-1.
       M-43.
           OPEN I-O SQF.
           DISPLAY "OPEN2 " FS.
           MOVE "A001" TO SQ-KEY.
           MOVE "NEW1" TO SQ-DATA.
           REWRITE SQREC.
           DISPLAY "L43R " FS " KEY1 " FS-1.
           DELETE SQF.
           DISPLAY "L43D " FS " KEY1 " FS-1.
       M-46.
           READ SQF.
           DISPLAY "R1 " FS " " SQREC.
           READ SQF.
           DISPLAY "R2 " FS.
           READ SQF.
           DISPLAY "L46 " FS " KEY1 " FS-1.
           CLOSE SQF.
       M-49.
           OPEN INPUT SQF.
           READ SQF.
           DISPLAY "R3 " FS " " SQREC.
           DELETE SQF.
           DISPLAY "L49 " FS " KEY1 " FS-1.
       M-48.
           MOVE "B001" TO SQ-KEY.
           WRITE SQREC.
           DISPLAY "L48 " FS " KEY1 " FS-1.
           CLOSE SQF.
       M-44.
           OPEN OUTPUT XV.
           MOVE "A001" TO XVREC.
           MOVE 3 TO RL.
           WRITE XVREC.
           DISPLAY "L44 " FS " KEY1 " FS-1.
           CLOSE XV.
       M-DY.
           OPEN OUTPUT DYNF.
           MOVE "K001" TO DY-KEY.
           MOVE "DREC1" TO DY-DATA.
           WRITE DYREC
               INVALID KEY DISPLAY "IK-W0"
               NOT INVALID KEY DISPLAY "NIK-W0"
           END-WRITE.
           CLOSE DYNF.
           OPEN INPUT DYNF.
           MOVE "K002" TO DY-KEY.
           WRITE DYREC
               INVALID KEY DISPLAY "IK-W"
               NOT INVALID KEY DISPLAY "NIK-W"
           END-WRITE.
           DISPLAY "N48 " FS2.
           MOVE "K001" TO DY-KEY.
           REWRITE DYREC
               INVALID KEY DISPLAY "IK-RW"
               NOT INVALID KEY DISPLAY "NIK-RW"
           END-REWRITE.
           DISPLAY "N49R " FS2.
           DELETE DYNF
               INVALID KEY DISPLAY "IK-D"
               NOT INVALID KEY DISPLAY "NIK-D"
           END-DELETE.
           DISPLAY "N49D " FS2.
           CLOSE DYNF.
           READ DYNF
               INVALID KEY DISPLAY "IK-RD"
               NOT INVALID KEY DISPLAY "NIK-RD"
           END-READ.
           DISPLAY "N47R " FS2.
           START DYNF KEY IS EQUAL TO DY-KEY
               INVALID KEY DISPLAY "IK-ST"
               NOT INVALID KEY DISPLAY "NIK-ST"
           END-START.
           DISPLAY "N47S " FS2.
           STOP RUN.
