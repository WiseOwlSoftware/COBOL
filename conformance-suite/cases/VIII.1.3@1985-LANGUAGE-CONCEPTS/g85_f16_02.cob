      *>--- conformance-case
      *> id: g85_f16_02
      *> rules: [1985-GR-VIII.1.3.4-5, 1985-GR-VIII.1.3.5-L2.1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VIII.1.3.4 GR5 - the logic error class of a relative
      *> file. File RS (sequential access) makes the improper
      *> sequences §1985:VIII.1.3.4 GRL2.5 gives one value each: 41
      *> OPEN of an open file, 47 READ of a file open OUTPUT, 42 CLOSE
      *> of a closed file, 43 REWRITE and DELETE in I-O mode with no
      *> successful READ before them, 46 READ after the at end
      *> condition, 49 DELETE of a file open INPUT after a successful
      *> READ. The second cause GR5 names, violating a limit defined
      *> by the user, is leg L44 on file RV: a WRITE of 3 characters
      *> where RECORD IS VARYING allows 4 to 10 is unsuccessful
      *> (§1985:VII.3.8.4 GR10, §1985:VIII.4.9.4 GR8) with 44
      *> (§1985:VIII.1.3.4 GRL2.5 d, item 1, level-2 text).
      *> Lines L41..L44 pin GR5: status key 1 must be 4.
      *> §1985:VIII.1.3.5 GRL2.1 - file DYNF (dynamic access): each
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
      *> action (§1985:VIII.4.8.4 GR6); WiseOwl COBOL continues with
      *> the next executable statement (docs/conformance/3.5).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F1602.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RS ASSIGN TO "g85f1602s.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS FS.
           SELECT DYNF ASSIGN TO "g85f1602d.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS DYNAMIC
               RELATIVE KEY IS RK2
               FILE STATUS IS FS2.
           SELECT RV ASSIGN TO "g85f1602v.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD  RS
           LABEL RECORDS ARE STANDARD.
       01  RSREC               PIC X(8).
       FD  DYNF
           LABEL RECORDS ARE STANDARD.
       01  RDREC               PIC X(8).
       FD  RV
           LABEL RECORDS ARE STANDARD
           RECORD IS VARYING IN SIZE FROM 4 TO 10 CHARACTERS
               DEPENDING ON RL.
       01  RVREC               PIC X(10).
       WORKING-STORAGE SECTION.
       01  FS.
           05 FS-1             PIC X.
           05 FS-2             PIC X.
       01  FS2                 PIC XX.
       01  RK2                 PIC 9(4).
       01  RL                  PIC 99.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D-RS SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON RS RV.
       D-RS-P.
           DISPLAY "USE " FS.
       D-DYNF SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON DYNF.
       D-DYNF-P.
           DISPLAY "USE2 " FS2.
       END DECLARATIVES.
       MAIN-LOGIC SECTION.
       M-41.
           OPEN OUTPUT RS.
           MOVE "REC1" TO RSREC.
           WRITE RSREC.
           DISPLAY "W1 " FS.
           OPEN OUTPUT RS.
           DISPLAY "L41 " FS " KEY1 " FS-1.
       M-47.
           READ RS.
           DISPLAY "L47 " FS " KEY1 " FS-1.
       M-42.
           CLOSE RS.
           DISPLAY "C1 " FS.
           CLOSE RS.
           DISPLAY "L42 " FS " KEY1 " FS-1.
       M-43.
           OPEN I-O RS.
           DISPLAY "OPEN2 " FS.
           MOVE "NEW1" TO RSREC.
           REWRITE RSREC.
           DISPLAY "L43R " FS " KEY1 " FS-1.
           DELETE RS.
           DISPLAY "L43D " FS " KEY1 " FS-1.
       M-46.
           READ RS.
           DISPLAY "R1 " FS " " RSREC.
           READ RS.
           DISPLAY "R2 " FS.
           READ RS.
           DISPLAY "L46 " FS " KEY1 " FS-1.
           CLOSE RS.
       M-49.
           OPEN INPUT RS.
           READ RS.
           DISPLAY "R3 " FS " " RSREC.
           DELETE RS.
           DISPLAY "L49 " FS " KEY1 " FS-1.
           CLOSE RS.
       M-44.
           OPEN OUTPUT RV.
           MOVE "A001" TO RVREC.
           MOVE 3 TO RL.
           WRITE RVREC.
           DISPLAY "L44 " FS " KEY1 " FS-1.
           CLOSE RV.
       M-DYNF.
           OPEN OUTPUT DYNF.
           MOVE 1 TO RK2.
           MOVE "DREC1" TO RDREC.
           WRITE RDREC
               INVALID KEY DISPLAY "IK-W0"
               NOT INVALID KEY DISPLAY "NIK-W0"
           END-WRITE.
           CLOSE DYNF.
           OPEN INPUT DYNF.
           MOVE 2 TO RK2.
           WRITE RDREC
               INVALID KEY DISPLAY "IK-W"
               NOT INVALID KEY DISPLAY "NIK-W"
           END-WRITE.
           DISPLAY "N48 " FS2.
           MOVE 1 TO RK2.
           REWRITE RDREC
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
               INVALID KEY DISPLAY "IK-DYNF"
               NOT INVALID KEY DISPLAY "NIK-DYNF"
           END-READ.
           DISPLAY "N47R " FS2.
           START DYNF KEY IS EQUAL TO RK2
               INVALID KEY DISPLAY "IK-ST"
               NOT INVALID KEY DISPLAY "NIK-ST"
           END-START.
           DISPLAY "N47S " FS2.
           STOP RUN.
