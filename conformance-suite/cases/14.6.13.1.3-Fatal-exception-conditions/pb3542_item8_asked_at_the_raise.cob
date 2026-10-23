*>--- conformance-case
*> id: pb3542_item8_asked_at_the_raise
*> rules: [GR-14.6.13.1.3-3, GR-14.6.13.1.3-8]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3542 (its sibling, found on the way) - ISO 14.6.13.1.3 item 8 turns on the
      *> condition that was RAISED, not on the file's status after the declarative:
      *>   cite.py: OK 14.6.13.1.3 8) "However, if there is a fatal exception condition for a file
      *>   subject to an active APPLY COMMIT clause, then a ROLLBACK statement is executed
      *>   unconditionally."
      *>   cite.py: OK 14.6.13.1.3 3) "If the exception condition is a fatal EC-I-O exception
      *>   condition, then the rules for 9.1.13, I-O status apply, then ... the following rules
      *>   apply for all fatal exception conditions."
      *> Checking is NOT enabled. The READ of RELF while it is open OUTPUT ('47', fatal) runs the
      *> USE procedure, which CLOSEs RELF ('00') and completes normally; item 8 still applies to
      *> the fatal condition the READ raised. Expected: WS-COUNT and FS back to the 0001 and '00'
      *> the COMMIT saved, and record 2, written after the COMMIT, gone. The defect asked the file's
      *> status after the procedure, found the CLOSE's '00' and rolled nothing back.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P3542AR.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RELF ASSIGN TO "p3542ar.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS RK
               FILE STATUS IS FS.
       I-O-CONTROL.
           APPLY COMMIT ON RELF WS-COUNT.
       DATA DIVISION.
       FILE SECTION.
       FD RELF.
       01 RREC PIC X(10).
       WORKING-STORAGE SECTION.
       01 RK PIC 9(4).
       01 FS PIC XX.
       01 WS-COUNT PIC 9(4) VALUE 0.
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION ON RELF.
       H-P.
           DISPLAY "HANDLED FS=" FS.
           CLOSE RELF.
           DISPLAY "CLOSED FS=" FS.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           OPEN OUTPUT RELF
           MOVE 1 TO RK
           MOVE "AAAA" TO RREC
           WRITE RREC
           ADD 1 TO WS-COUNT
           COMMIT
           MOVE 2 TO RK
           MOVE "BBBB" TO RREC
           WRITE RREC
           ADD 1 TO WS-COUNT
           READ RELF
           DISPLAY "AFTER READ COUNT=" WS-COUNT " FS=" FS
           OPEN INPUT RELF
           MOVE 2 TO RK
           READ RELF
               INVALID KEY DISPLAY "RECORD 2 ABSENT"
               NOT INVALID KEY DISPLAY "RECORD 2=" RREC
           END-READ
           CLOSE RELF
           STOP RUN.
