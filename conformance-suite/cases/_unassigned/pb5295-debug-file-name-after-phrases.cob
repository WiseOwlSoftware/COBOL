      *>--- conformance-case
      *> id: pb5295-debug-file-name-after-phrases
      *> rules: []
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5295. X3.23-1985 USE FOR DEBUGGING general rule 2: the
      *> section for file-name-1 runs "(b) after the execution of any READ
      *> statement ... not resulting in the execution of an associated AT
      *> END or INVALID KEY imperative statement" and "(c) after the
      *> execution of any DELETE or START statement that references
      *> file-name-1". The execution of a statement includes its
      *> conditional phrases: a READ's NOT AT END and NOT INVALID KEY
      *> statements and a DELETE's or START's INVALID KEY and NOT INVALID
      *> KEY statements are inside it, and the USE procedure of a failed
      *> one comes before the section (rule 2 b: "after any other
      *> specified USE procedure"). So the section is the LAST thing a
      *> statement does.
      *> Why each leg can fail: a section landed before the phrase
      *> statements prints its line ahead of the statement's own output
      *> on every leg; one landed ahead of the USE procedure prints
      *> before USE on the failed DELETE and START of KU, which have no
      *> INVALID KEY phrase.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5295FP.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. IBM-PC WITH DEBUGGING MODE.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SF ASSIGN TO "pb5295-seq.dat"
               ORGANIZATION IS SEQUENTIAL FILE STATUS IS FS1.
           SELECT KF ASSIGN TO "pb5295-idx.dat"
               ORGANIZATION IS INDEXED ACCESS MODE IS DYNAMIC
               RECORD KEY IS KK FILE STATUS IS FS2.
           SELECT KU ASSIGN TO "pb5295-idu.dat"
               ORGANIZATION IS INDEXED ACCESS MODE IS DYNAMIC
               RECORD KEY IS UK FILE STATUS IS FS3.
       DATA DIVISION.
       FILE SECTION.
       FD SF.
       01 SREC PIC X(4).
       FD KF.
       01 KREC.
          02 KK PIC X(3).
          02 KV PIC X(1).
       FD KU.
       01 UREC.
          02 UK PIC X(3).
          02 UV PIC X(1).
       WORKING-STORAGE SECTION.
       01 FS1 PIC XX.
       01 FS2 PIC XX.
       01 FS3 PIC XX.
       PROCEDURE DIVISION.
       DECLARATIVES.
       DBG SECTION.
           USE FOR DEBUGGING ON SF KF KU.
       DBG-B.
           DISPLAY "  DBG [" DEBUG-NAME (1:2) "]".
       ERR SECTION.
           USE AFTER STANDARD EXCEPTION PROCEDURE ON KU.
       ERR-B.
           DISPLAY "  USE KU " FS3.
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
           OPEN OUTPUT KU.
           MOVE "AAA1" TO UREC.
           WRITE UREC INVALID KEY CONTINUE END-WRITE.
           CLOSE KU.
           DISPLAY "OPEN INPUT SF".
           OPEN INPUT SF.
           DISPLAY "READ SF NOT AT END".
           READ SF AT END DISPLAY "AT END"
               NOT AT END DISPLAY "NOT AT END".
           DISPLAY "READ SF AT END".
           READ SF AT END DISPLAY "AT END"
               NOT AT END DISPLAY "NOT AT END".
           CLOSE SF.
           DISPLAY "OPEN I-O KF".
           OPEN I-O KF.
           DISPLAY "START KF NOT INVALID".
           MOVE "AAA" TO KK.
           START KF KEY = KK INVALID KEY DISPLAY "INVALID"
               NOT INVALID KEY DISPLAY "NOT INVALID".
           DISPLAY "START KF INVALID".
           MOVE "ZZZ" TO KK.
           START KF KEY = KK INVALID KEY DISPLAY "INVALID"
               NOT INVALID KEY DISPLAY "NOT INVALID".
           DISPLAY "READ KF NOT INVALID".
           MOVE "AAA" TO KK.
           READ KF INVALID KEY DISPLAY "INVALID"
               NOT INVALID KEY DISPLAY "NOT INVALID".
           DISPLAY "DELETE KF NOT INVALID".
           DELETE KF INVALID KEY DISPLAY "INVALID"
               NOT INVALID KEY DISPLAY "NOT INVALID".
           DISPLAY "DELETE KF INVALID".
           DELETE KF INVALID KEY DISPLAY "INVALID"
               NOT INVALID KEY DISPLAY "NOT INVALID".
           CLOSE KF.
           DISPLAY "OPEN I-O KU".
           OPEN I-O KU.
           DISPLAY "START KU, no phrase".
           MOVE "ZZZ" TO UK.
           START KU KEY = UK.
           DISPLAY "DELETE KU, no phrase".
           MOVE "ZZZ" TO UREC.
           DELETE KU.
           CLOSE KU.
           DISPLAY "END".
           STOP RUN.
