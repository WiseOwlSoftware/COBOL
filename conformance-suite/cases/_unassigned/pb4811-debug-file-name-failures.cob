      *>--- conformance-case
      *> id: pb4811-debug-file-name-failures
      *> rules: []
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4811, review round 1 R1-1 and R1-2. X3.23-1985 USE FOR
      *> DEBUGGING general rule 2: the section for file-name-1 runs "(a)
      *> after the execution of any OPEN or CLOSE statement that
      *> references file-name-1" (so after one that failed too), "(b)
      *> after the execution of any READ statement (after any other
      *> specified USE procedure) not resulting in the execution of an
      *> associated AT END or INVALID KEY imperative statement", and "(c)
      *> after the execution of any DELETE or START statement that
      *> references file-name-1".
      *> So an unsuccessful READ with no AT END phrase (status 10), a
      *> READ past the end (46), and a READ of a closed file (47) whose
      *> AT END statement does not run (47 is not the at end condition)
      *> each run the USE procedure and then the section, and a READ
      *> whose AT END or INVALID KEY statement ran runs neither.
      *> Why each leg can fail: a section landed only on the arm that
      *> delivered a record never runs after these reads; one landed
      *> after the whole statement runs for the AT END and INVALID KEY
      *> arms too; one that skips a failed OPEN, CLOSE, START or DELETE
      *> misses rule 2 a and c.
      *> The START and DELETE of KF (no USE procedure) show the section
      *> runs with no INVALID KEY phrase at all; those of KU (a USE
      *> procedure too) pin the order: rule 2 c runs the section "after
      *> the execution of" the statement, and the USE procedure of a
      *> failed one is part of that execution, so the section runs after
      *> the USE procedure (docs/CONFORMANCE.md D-DEBUG-FILE; kb/Work
      *> PB5295).
      *> A READ that delivered no record read none into DEBUG-CONTENTS,
      *> which stays spaces (rules 23 c and 11), and an INTO receiver
      *> named by ALL REFERENCES is not referenced by a READ whose INTO
      *> move did not run (rule 5, last sentence): DBX fires on the
      *> READ that delivered a record only.
      *> The debugging sections come first in DECLARATIVES: §1985:XV.3.2.3
      *> SR1 puts them immediately after the header (kb/Work PB5081).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4811FF.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. IBM-PC WITH DEBUGGING MODE.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SF ASSIGN TO "pb4811ff-seq.dat"
               ORGANIZATION IS SEQUENTIAL FILE STATUS IS FS1.
           SELECT KF ASSIGN TO "pb4811ff-idx.dat"
               ORGANIZATION IS INDEXED ACCESS MODE IS DYNAMIC
               RECORD KEY IS KK FILE STATUS IS FS2.
           SELECT KU ASSIGN TO "pb4811ff-idu.dat"
               ORGANIZATION IS INDEXED ACCESS MODE IS DYNAMIC
               RECORD KEY IS UK FILE STATUS IS FS3.
       DATA DIVISION.
       FILE SECTION.
       FD SF.
       01 SREC PIC X(8).
       FD KF.
       01 KREC.
          02 KK PIC X(3).
          02 KV PIC X(5).
       FD KU.
       01 UREC.
          02 UK PIC X(3).
          02 UV PIC X(5).
       WORKING-STORAGE SECTION.
       01 FS1 PIC XX.
       01 FS2 PIC XX.
       01 FS3 PIC XX.
       01 X1 PIC X(8).
       PROCEDURE DIVISION.
       DECLARATIVES.
       DBS SECTION.
           USE FOR DEBUGGING ON SF.
       DBS-B.
           DISPLAY "  DBG [" DEBUG-NAME (1:2) "] [" DEBUG-CONTENTS (1:8)
               "]".
       DBK SECTION.
           USE FOR DEBUGGING ON KF.
       DBK-B.
           DISPLAY "  DBG [" DEBUG-NAME (1:2) "]".
       DBU SECTION.
           USE FOR DEBUGGING ON KU.
       DBU-B.
           DISPLAY "  DBG [" DEBUG-NAME (1:2) "]".
       DBX SECTION.
           USE FOR DEBUGGING ON ALL REFERENCES OF X1.
       DBX-B.
           DISPLAY "  DBX".
       USES SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON SF.
       USES-B.
           DISPLAY "  USE SF " FS1.
       USEU SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON KU.
       USEU-B.
           DISPLAY "  USE KU " FS3.
       END DECLARATIVES.
       MAIN SECTION.
       M1.
           DISPLAY "OPEN INPUT SF, missing".
           OPEN INPUT SF.
           DISPLAY "CLOSE SF, not open".
           CLOSE SF.
           OPEN OUTPUT SF.
           MOVE "ABCDEFGH" TO SREC.
           WRITE SREC.
           CLOSE SF.
           DISPLAY "OPEN INPUT SF".
           OPEN INPUT SF.
           DISPLAY "READ SF, a record".
           READ SF.
           DISPLAY "READ SF, at end, no phrase".
           READ SF.
           DISPLAY "READ SF, past the end, no phrase".
           READ SF.
           DISPLAY "READ SF AT END, past the end".
           READ SF AT END DISPLAY "END".
           DISPLAY "CLOSE SF".
           CLOSE SF.
           DISPLAY "READ SF AT END, closed".
           READ SF AT END DISPLAY "END".
           OPEN INPUT SF.
           READ SF.
           DISPLAY "READ SF AT END, at end".
           READ SF AT END DISPLAY "END".
           CLOSE SF.
           OPEN OUTPUT KF.
           MOVE "AAA11111" TO KREC.
           WRITE KREC INVALID KEY CONTINUE END-WRITE
           .
           MOVE "BBB22222" TO KREC.
           WRITE KREC INVALID KEY CONTINUE END-WRITE
           .
           CLOSE KF.
           OPEN OUTPUT KU.
           MOVE "AAA11111" TO UREC.
           WRITE UREC.
           MOVE "BBB22222" TO UREC.
           WRITE UREC.
           CLOSE KU.
           DISPLAY "CLOSE KF, not open".
           CLOSE KF.
           OPEN I-O KF.
           MOVE "ZZZ" TO KK.
           DISPLAY "START KF ZZZ, no phrase".
           START KF KEY = KK INVALID KEY CONTINUE END-START
           .
           DISPLAY "FS2 " FS2.
           MOVE "QQQ" TO KK.
           DISPLAY "DELETE KF QQQ, no phrase".
           DELETE KF INVALID KEY CONTINUE END-DELETE
           .
           DISPLAY "FS2 " FS2.
           CLOSE KF.
           OPEN I-O KU.
           MOVE "QQQ" TO UK.
           DISPLAY "READ KU QQQ, no phrase".
           READ KU.
           DISPLAY "READ KU QQQ INVALID KEY".
           READ KU INVALID KEY DISPLAY "INVALID".
           MOVE "ZZZ" TO UK.
           DISPLAY "START KU ZZZ, no phrase".
           START KU KEY = UK.
           DISPLAY "FS3 " FS3.
           DISPLAY "DELETE KU ZZZ, no phrase".
           DELETE KU.
           DISPLAY "FS3 " FS3.
           MOVE "AAA" TO UK.
           DISPLAY "START KU AAA".
           START KU KEY = UK.
           DISPLAY "READ KU NEXT, a record".
           READ KU NEXT.
           DISPLAY "READ KU NEXT, a record".
           READ KU NEXT.
           DISPLAY "READ KU NEXT, at end, no phrase".
           READ KU NEXT.
           DISPLAY "READ KU NEXT, past the end, no phrase".
           READ KU NEXT.
           DISPLAY "READ KU NEXT AT END, past the end".
           READ KU NEXT AT END DISPLAY "END".
           CLOSE KU.
           OPEN INPUT SF.
           DISPLAY "READ SF INTO X1, a record".
           READ SF INTO X1.
           DISPLAY "READ SF INTO X1, at end, no phrase".
           READ SF INTO X1.
           DISPLAY "READ SF INTO X1, past the end".
           READ SF INTO X1.
           CLOSE SF.
           DISPLAY "END".
           STOP RUN.
