      *>--- conformance-case
      *> id: 1985-GR-IX_4_5_4-18
      *> rules: [1985-GR-IX.4.5.4-18]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> Each WRITE carries INVALID KEY: the file has no USE AFTER
      *> STANDARD EXCEPTION procedure, so §1985:IX.4.9.3 SR3
      *> requires the phrase (kb/Work PB5076).
      *> §1985:IX.4.5.4 GR18 (THE READ STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the triage of the drafts that did not
      *> match (a suspect draft corrected) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RDLEN.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FILE-A ASSIGN TO IDXFILE
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS KEY-A
               FILE STATUS IS STAT-A.
           SELECT FILE-B ASSIGN TO IDXFILE
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS KEY-B
               FILE STATUS IS STAT-B.
       DATA DIVISION.
       FILE SECTION.
       FD  FILE-A
           RECORD IS VARYING IN SIZE FROM 6 TO 20 CHARACTERS.
       01  REC-A.
           05 KEY-A        PIC X(5).
           05 DATA-A       PIC X(15).
       01  REC-A-SHORT.
           05 KEY-A-SHORT  PIC X(5).
           05 DATA-A-SHORT PIC X(3).
       FD  FILE-B
           RECORD IS VARYING IN SIZE FROM 6 TO 20 CHARACTERS.
       01  REC-B.
           05 KEY-B        PIC X(5).
           05 DATA-B       PIC X(15).
       01  REC-B-SHORT.
           05 KEY-B-SHORT  PIC X(5).
           05 DATA-B-SHORT PIC X(3).
       WORKING-STORAGE SECTION.
       01  STAT-A          PIC XX.
       01  STAT-B          PIC XX.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT FILE-A.
           MOVE "AAAAABBBBBCCCCCDDDDD" TO REC-A.
           WRITE REC-A
               INVALID KEY DISPLAY "INVALID KEY".
           MOVE "ZZZZZYYY" TO REC-A-SHORT.
           WRITE REC-A-SHORT
               INVALID KEY DISPLAY "INVALID KEY".
           CLOSE FILE-A.
           OPEN INPUT FILE-B.
           READ FILE-B AT END DISPLAY "EOF".
           DISPLAY STAT-B.
           DISPLAY REC-B.
           READ FILE-B AT END DISPLAY "EOF".
           DISPLAY STAT-B.
           DISPLAY REC-B-SHORT.
           CLOSE FILE-B.
           STOP RUN.
