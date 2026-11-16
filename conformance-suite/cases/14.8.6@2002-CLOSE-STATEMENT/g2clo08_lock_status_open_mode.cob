      *>--- conformance-case
      *> id: g2clo08_lock_status_open_mode
      *> rules: [2002-FMT-14.8.6.1, 2002-GR-14.8.6.3-3,
      *>    2002-GR-14.8.6.3-4, 2002-GR-14.8.6.3-8]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.6.1 -- the 2002 general format, rendered from PDF
      *> page 450: CLOSE {file-name-1 [{REEL|UNIT} [FOR REMOVAL] |
      *> WITH {NO REWIND|LOCK}]}... ; WITH LOCK is the 2002 alternative
      *> later editions removed. The one CLOSE at MULTI writes all four
      *> alternatives in one statement (§2002:14.8.6.3 GR10: as if
      *> separate CLOSEs, in order). WITH and FOR are optional words
      *> (not underlined on p450), so the program also writes the forms
      *> without them: UNIT REMOVAL at MULTI, CLOSE S2 LOCK and
      *> CLOSE S3 NO REWIND at SECOND.
      *> §2002:14.8.6.3 GR3, table 15, with DOC-A.1-24's placement: the
      *> sequential files are (a) Non-unit, the relative file (d).
      *>   (a) CLOSE REEL FOR REMOVAL, CLOSE UNIT = f, non-unit media:
      *>   "The file remains in the open mode, the file position
      *>   indicator is unchanged" -- the later WRITE to S1 is 00 and
      *>   the file holds both records; status 07 (§2002:9.1.12.1 5).
      *>   (a) CLOSE WITH NO REWIND = c,h: closed as a plain CLOSE, 07.
      *>   (d) CLOSE = c: 00, the OPEN I-O after it 00.
      *>   (d) and (a) CLOSE WITH LOCK = c,e: closed (00) and "shall not
      *>   be opened again during the execution of this run unit": the
      *>   OPEN after it is 38 (§2002:9.1.12.5 6).
      *> §2002:14.8.6.3 GR4 -- each CLOSE updates the I-O status: every
      *>   status is set to XX before each CLOSE line, so an untouched
      *>   status prints XX.
      *> §2002:14.8.6.3 GR8 -- after a successful CLOSE without the
      *>   UNIT phrase (plain, NO REWIND, LOCK) the connector is "not
      *>   in an open mode": a second CLOSE is 42 (§2002:14.8.6.3 GR1).
      *>   After CLOSE UNIT / REEL FOR REMOVAL it is still open: the
      *>   next CLOSE is 00.
      *> §2002:9.1.12: 38 and 42 are fatal and the implementor decides
      *> whether execution continues; DOC-A.1-103: it continues, each
      *> file having a FILE STATUS clause and EC-I-O checking being off.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CLO08.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT S1 ASSIGN TO "g2c8s1.dat"
               ORGANIZATION IS SEQUENTIAL FILE STATUS IS ST1.
           SELECT S2 ASSIGN TO "g2c8s2.dat"
               ORGANIZATION IS SEQUENTIAL FILE STATUS IS ST2.
           SELECT S3 ASSIGN TO "g2c8s3.dat"
               ORGANIZATION IS SEQUENTIAL FILE STATUS IS ST3.
           SELECT R1 ASSIGN TO "g2c8r1.dat"
               ORGANIZATION IS RELATIVE ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS STR.
       DATA DIVISION.
       FILE SECTION.
       FD S1.
       01 S1-REC PIC X(4).
       FD S2.
       01 S2-REC PIC X(4).
       FD S3.
       01 S3-REC PIC X(4).
       FD R1.
       01 R1-REC PIC X(4).
       WORKING-STORAGE SECTION.
       01 ST1 PIC XX.
       01 ST2 PIC XX.
       01 ST3 PIC XX.
       01 STR PIC XX.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT R1
           MOVE "RRRR" TO R1-REC
           WRITE R1-REC
           MOVE "XX" TO STR
           CLOSE R1
           DISPLAY "R-CLOSE=" STR
           OPEN I-O R1
           DISPLAY "R-REOPEN=" STR
           OPEN OUTPUT S1 S2 S3
           MOVE "AAAA" TO S1-REC
           WRITE S1-REC
           MOVE "BBBB" TO S2-REC
           WRITE S2-REC
           MOVE "CCCC" TO S3-REC
           WRITE S3-REC
           MOVE "XX" TO ST1 ST2 ST3 STR
           CLOSE S1 REEL FOR REMOVAL S2 UNIT REMOVAL S3 WITH NO REWIND
                 R1 WITH LOCK
           DISPLAY "MULTI=" ST1 " " ST2 " " ST3 " " STR
           MOVE "DDDD" TO S1-REC
           WRITE S1-REC
           DISPLAY "S1-WRITE=" ST1
           MOVE "XX" TO ST1 ST2 ST3 STR
           CLOSE S1
           CLOSE S2 LOCK
           CLOSE S3 NO REWIND
           CLOSE R1
           DISPLAY "SECOND=" ST1 " " ST2 " " ST3 " " STR
           OPEN INPUT S1
           READ S1 AT END CONTINUE END-READ
           DISPLAY "S1-R1=" ST1 " " S1-REC
           READ S1 AT END CONTINUE END-READ
           DISPLAY "S1-R2=" ST1 " " S1-REC
           READ S1 AT END CONTINUE END-READ
           DISPLAY "S1-R3=" ST1
           MOVE "XX" TO ST1
           CLOSE S1
           DISPLAY "S1-CLOSE=" ST1
           OPEN INPUT S2
           DISPLAY "S2-REOPEN=" ST2
           MOVE "XX" TO ST2
           CLOSE S2
           DISPLAY "S2-CLOSE=" ST2
           OPEN INPUT S3
           DISPLAY "S3-REOPEN=" ST3
           READ S3 AT END CONTINUE END-READ
           DISPLAY "S3-R1=" ST3 " " S3-REC
           MOVE "XX" TO ST3
           CLOSE S3 WITH LOCK
           DISPLAY "S3-LOCK=" ST3
           OPEN EXTEND S3
           DISPLAY "S3-RELOCKED=" ST3
           OPEN INPUT R1
           DISPLAY "R-RELOCKED=" STR
           STOP RUN.
