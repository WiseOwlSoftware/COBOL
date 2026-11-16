      *>--- conformance-case
      *> id: g85_f16_01
      *> rules: [1985-GR-VII.1.3.5-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.1.3.5 GR4 - the logic error class of a sequential
      *> file: an input-output statement unsuccessfully executed as a
      *> result of an improper sequence of input-output operations.
      *> Each leg makes one improper sequence that only one of the
      *> 4x values of §1985:VII.1.3.5 GRL2.4 names (41 OPEN of an
      *> open file, 47 READ of a file open OUTPUT, 42 CLOSE of a
      *> closed file, 48 WRITE of a closed file, 49 REWRITE of a file
      *> open INPUT after a successful READ, 46 READ after the at end
      *> condition, 43 REWRITE in I-O mode with no READ before it).
      *> The second cause GR4 names, violating a limit defined by the
      *> user, is leg L44 on file SV: a WRITE of 3 characters where
      *> RECORD IS VARYING allows 4 to 10 is unsuccessful
      *> (§1985:VII.3.8.4 GR10, §1985:VII.4.7.4 GR8) with 44
      *> (§1985:VII.1.3.5 GRL2.4 d, item 1, level-2 text).
      *> Every leg must show status key 1 = 4 and the USE procedure
      *> for the file (§1985:VII.4.6.4 GR5 a). After a critical status
      *> the implementor determines the action (§1985:VII.4.6.4 GR6);
      *> WiseOwl COBOL continues with the next executable statement
      *> (docs/conformance/3.5). A leg fails if the statement succeeds
      *> (status 0x), takes another class, or the USE procedure does
      *> not run (UCNT counts its runs: 9, the at end 10 included).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F1601.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SF ASSIGN TO "g85f1601.dat"
               ORGANIZATION IS SEQUENTIAL
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS FS.
           SELECT SV ASSIGN TO "g85f1601v.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD  SF
           LABEL RECORDS ARE STANDARD.
       01  SREC                PIC X(10).
       FD  SV
           LABEL RECORDS ARE STANDARD
           RECORD IS VARYING IN SIZE FROM 4 TO 10 CHARACTERS
               DEPENDING ON RL.
       01  SVREC               PIC X(10).
       WORKING-STORAGE SECTION.
       01  FS.
           05 FS-1             PIC X.
           05 FS-2             PIC X.
       01  UCNT                PIC 9 VALUE 0.
       01  RL                  PIC 99.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D-SF SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON SF SV.
       D-SF-P.
           ADD 1 TO UCNT.
           DISPLAY "USE " FS.
       END DECLARATIVES.
       MAIN-LOGIC SECTION.
       M-41.
           OPEN OUTPUT SF.
           DISPLAY "OPEN1 " FS.
           OPEN OUTPUT SF.
           DISPLAY "L41 " FS " KEY1 " FS-1.
       M-47.
           MOVE "REC1" TO SREC.
           WRITE SREC.
           DISPLAY "W1 " FS.
           READ SF.
           DISPLAY "L47 " FS " KEY1 " FS-1.
       M-42.
           CLOSE SF.
           DISPLAY "C1 " FS.
           CLOSE SF.
           DISPLAY "L42 " FS " KEY1 " FS-1.
       M-48.
           MOVE "REC2" TO SREC.
           WRITE SREC.
           DISPLAY "L48 " FS " KEY1 " FS-1.
       M-49.
           OPEN INPUT SF.
           DISPLAY "OPEN2 " FS.
           READ SF.
           DISPLAY "R1 " FS " " SREC.
           REWRITE SREC.
           DISPLAY "L49 " FS " KEY1 " FS-1.
       M-46.
           READ SF.
           DISPLAY "R2 " FS.
           READ SF.
           DISPLAY "L46 " FS " KEY1 " FS-1.
           CLOSE SF.
       M-43.
           OPEN I-O SF.
           DISPLAY "OPEN3 " FS.
           REWRITE SREC.
           DISPLAY "L43 " FS " KEY1 " FS-1.
           CLOSE SF.
       M-44.
           OPEN OUTPUT SV.
           MOVE "A001" TO SVREC.
           MOVE 3 TO RL.
           WRITE SVREC.
           DISPLAY "L44 " FS " KEY1 " FS-1.
           CLOSE SV.
           DISPLAY "UCNT " UCNT.
           STOP RUN.
