      *>--- conformance-case
      *> id: pb5295-debug-after-phrase-transfer
      *> rules: []
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5295, review finding R1-1. X3.23-1985 USE FOR DEBUGGING
      *> general rule 2: the section for file-name-1 runs "(b) after the
      *> execution of any READ statement ... not resulting in the
      *> execution of an associated AT END or INVALID KEY imperative
      *> statement" and "(c) after the execution of any DELETE or START
      *> statement that references file-name-1". The phrases' statements
      *> are inside the statement, so when one of them transfers control
      *> the section runs before the transfer (rule 7: the nested GO TO
      *> is another statement; the code after it is never reached).
      *> Why each leg can fail: a section landed only after the phrases
      *> prints no DBG line before AT M2 (READ NOT AT END GO TO), before
      *> AT M3 (DELETE INVALID KEY GO TO) or before AT M4 (START INVALID
      *> KEY GO TO).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5295PT.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. IBM-PC WITH DEBUGGING MODE.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SF ASSIGN TO "pb5295t-seq.dat"
               ORGANIZATION IS SEQUENTIAL FILE STATUS IS FS1.
           SELECT KF ASSIGN TO "pb5295t-idx.dat"
               ORGANIZATION IS INDEXED ACCESS MODE IS DYNAMIC
               RECORD KEY IS KK FILE STATUS IS FS2.
       DATA DIVISION.
       FILE SECTION.
       FD SF.
       01 SREC PIC X(4).
       FD KF.
       01 KREC.
          02 KK PIC X(3).
          02 KV PIC X(1).
       WORKING-STORAGE SECTION.
       01 FS1 PIC XX.
       01 FS2 PIC XX.
       PROCEDURE DIVISION.
       DECLARATIVES.
       DBG SECTION.
           USE FOR DEBUGGING ON SF KF.
       DBG-B.
           DISPLAY "  DBG [" DEBUG-NAME (1:2) "]".
       END DECLARATIVES.
       MAIN SECTION.
       M1.
           OPEN OUTPUT SF.
           MOVE "ABCD" TO SREC.
           WRITE SREC.
           CLOSE SF.
           OPEN OUTPUT KF.
           MOVE "AAA1" TO KREC.
           WRITE KREC INVALID KEY CONTINUE END-WRITE.
           CLOSE KF.
           OPEN INPUT SF.
           OPEN I-O KF.
           DISPLAY "READ SF NOT AT END GO TO".
           READ SF AT END DISPLAY "AT END"
               NOT AT END GO TO M2.
           DISPLAY "FELL THROUGH 1".
       M2.
           DISPLAY "AT M2".
           MOVE "ZZZ" TO KK.
           DELETE KF INVALID KEY GO TO M3.
           DISPLAY "FELL THROUGH 2".
       M3.
           DISPLAY "AT M3".
           MOVE "ZZZ" TO KK.
           START KF KEY = KK INVALID KEY GO TO M4.
           DISPLAY "FELL THROUGH 3".
       M4.
           DISPLAY "AT M4".
           CLOSE SF.
           CLOSE KF.
           DISPLAY "END".
           STOP RUN.
