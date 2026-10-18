*>--- conformance-case
*> id: pb1880_write_with_lock_under_permanent_error
*> rules: [DOC-A.1-105, GR-14.9.51.4-11]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 1989:2023 §9.1.13.1: a permanent error "remains in effect for all subsequent input-output operations on the
      *> file unless an implementor-defined technique is invoked" (Annex A.1 item 105; this processor's technique is CLOSE,
      *> docs/CONFORMANCE.md DOC-A.1-105). A WRITE ... WITH LOCK is one of those operations, and the lock it requests is
      *> set only "when the execution of the WRITE statement is successful" (§14.9.51.4 GR11), so a WRITE WITH LOCK that
      *> the condition in effect refuses must (1) answer the status that began it, (2) store nothing, and (3) lock nothing.
      *> The permanent error is made the portable way: a random WRITE with relative key 0 answers '34' (§14.9.51.4 GR29 b).
      *>   F   FILE STATUS FS, locks MANUAL WITH LOCK ON MULTIPLE RECORDS, SHARING WITH ALL OTHER; F2 is another file
      *>       connector on the same physical file (the observer) that holds record 1 ("AAAA").
      *>   W0  WRITE key 0 WITH LOCK                 -> '34' (the producer; INVALID KEY does not apply to a '34')
      *>   W1  WRITE key 1 WITH LOCK                 -> '34'. On a healthy connector it is '22' (key 1 exists): the status
      *>       is the condition in effect, not the statement's own; no lock on record 1 is taken.
      *>   W2  WRITE key 2 WITH LOCK                 -> '34'. On a healthy connector it is '00' and locks record 2.
      *>   O1  F2 READ record 1 WITH LOCK            -> '00': F holds no lock on record 1 (a lock set while failing would
      *>       be '51', the record operation conflict of §9.1.13.1)
      *>   O2  F2 READ record 2                      -> '23': the stuck WRITE stored nothing
      *>   CLOSE ends the condition; OPEN I-O; WRITE key 2 WITH LOCK -> '00' (the same statement now succeeds), and
      *>   O3  F2 READ record 2 WITH LOCK            -> '51': F's WITH LOCK does lock in this configuration, so the
      *>       absences above are the condition's doing and nothing else.
      *> No INVALID KEY or NOT INVALID KEY phrase runs for a '34' (§9.1.13.1: for a fatal condition "Any NOT AT END or
      *> NOT INVALID KEY phrase specified for that statement is ignored"; '34' is a permanent error, §9.1.13.6, not an invalid key condition, §9.1.13.5).
      *>   H   no FILE STATUS clause, a USE procedure for the file: the declarative runs for the producing WRITE and again
      *>       for the one the condition refuses, and execution continues (DOC-A.1-103).
      *> WHY EACH LEG CAN FAIL: a replay that did not cover the WITH LOCK path would answer '22' at W1 and '00' at W2,
      *> store record 2 (O2 '00') and lock it (O3 would not differ from the control, but O1 or a later conflict would).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1880WL.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb1880wl.dat"
               ORGANIZATION IS RELATIVE ACCESS MODE IS RANDOM
               RELATIVE KEY IS RK
               LOCK MODE IS MANUAL WITH LOCK ON MULTIPLE RECORDS
               SHARING WITH ALL OTHER
               FILE STATUS IS FS.
           SELECT F2 ASSIGN TO "pb1880wl.dat"
               ORGANIZATION IS RELATIVE ACCESS MODE IS RANDOM
               RELATIVE KEY IS RK2
               LOCK MODE IS MANUAL WITH LOCK ON MULTIPLE RECORDS
               SHARING WITH ALL OTHER
               FILE STATUS IS FS2.
           SELECT H ASSIGN TO "pb1880wlh.dat"
               ORGANIZATION IS RELATIVE ACCESS MODE IS RANDOM
               RELATIVE KEY IS RKH
               LOCK MODE IS MANUAL WITH LOCK ON MULTIPLE RECORDS
               SHARING WITH ALL OTHER.
       DATA DIVISION.
       FILE SECTION.
       FD  F.
       01  R  PIC X(4).
       FD  F2.
       01  R2 PIC X(4).
       FD  H.
       01  HR PIC X(4).
       WORKING-STORAGE SECTION.
       01  FS  PIC XX.
       01  FS2 PIC XX.
       01  RK  PIC 9(4).
       01  RK2 PIC 9(4).
       01  RKH PIC 9(4).
       PROCEDURE DIVISION.
       DECLARATIVES.
       USE-H SECTION.
           USE AFTER STANDARD EXCEPTION PROCEDURE ON H.
       USE-H-P.
           DISPLAY "USE-H".
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           OPEN OUTPUT F2.
           MOVE 1 TO RK2.
           MOVE "AAAA" TO R2.
           WRITE R2.
           CLOSE F2.
           OPEN I-O F.
           OPEN I-O F2.
           MOVE 0 TO RK.
           MOVE "BBBB" TO R.
           WRITE R WITH LOCK
               INVALID KEY DISPLAY "W0 IK"
               NOT INVALID KEY DISPLAY "W0 NIK"
           END-WRITE.
           DISPLAY "W0 " FS.
           MOVE 1 TO RK.
           WRITE R WITH LOCK
               INVALID KEY DISPLAY "W1 IK"
               NOT INVALID KEY DISPLAY "W1 NIK"
           END-WRITE.
           DISPLAY "W1 " FS.
           MOVE 2 TO RK.
           WRITE R WITH LOCK
               INVALID KEY DISPLAY "W2 IK"
               NOT INVALID KEY DISPLAY "W2 NIK"
           END-WRITE.
           DISPLAY "W2 " FS.
           MOVE 1 TO RK2.
           READ F2 WITH LOCK.
           DISPLAY "O1 " FS2.
           MOVE 2 TO RK2.
           READ F2.
           DISPLAY "O2 " FS2.
           CLOSE F.
           DISPLAY "CLOSE " FS.
           OPEN I-O F.
           MOVE 2 TO RK.
           WRITE R WITH LOCK.
           DISPLAY "W2B " FS.
           MOVE 2 TO RK2.
           READ F2 WITH LOCK.
           DISPLAY "O3 " FS2.
           CLOSE F.
           CLOSE F2.
           OPEN OUTPUT H.
           MOVE 0 TO RKH.
           MOVE "CCCC" TO HR.
           WRITE HR WITH LOCK.
           MOVE 2 TO RKH.
           WRITE HR WITH LOCK.
           DISPLAY "H-DONE".
           CLOSE H.
           STOP RUN.
