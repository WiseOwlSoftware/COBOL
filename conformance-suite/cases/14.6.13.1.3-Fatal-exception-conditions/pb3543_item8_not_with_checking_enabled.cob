*>--- conformance-case
*> id: pb3543_item8_not_with_checking_enabled
*> rules: [GR-14.6.13.1.3-5, GR-14.6.13.1.3-8]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3543 - ISO 14.6.13.1.3 item 8's ROLLBACK is the CHECKING-NOT-ENABLED arm:
      *>   cite.py: OK 14.6.13.1.3 8) "If checking for the exception condition is not enabled, the
      *>   implementor defines whether or not execution will continue ... However, if there is a
      *>   fatal exception condition for a file subject to an active APPLY COMMIT clause, then a
      *>   ROLLBACK statement is executed unconditionally."
      *>   cite.py: OK 14.6.13.1.3 5) "If checking for the exception condition is enabled and there
      *>   is an applicable USE statement ... the associated declarative is executed", and NOTE 2:
      *>   "The user is able to continue by using a RESUME statement".
      *> EC-I-O checking is ON, so the READ of RELF while it is open OUTPUT ('47', EC-I-O-LOGIC-ERROR,
      *> fatal) is item 5's: the declarative runs and RESUME AT NEXT STATEMENT continues. Item 8 is
      *> not reached, so NOTHING is rolled back. Expected: WS-COUNT keeps the 0002 the second ADD
      *> made, FS keeps '47', and record 2, written after the COMMIT, is still in the file. The
      *> defect rolled back here (0001, '00', record 2 absent).
       >>TURN EC-I-O CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P3543CE.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RELF ASSIGN TO "p3543ce.dat"
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
           DISPLAY "HANDLED FS=" FS " " FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
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
           CLOSE RELF
           OPEN INPUT RELF
           MOVE 2 TO RK
           READ RELF
               INVALID KEY DISPLAY "RECORD 2 ABSENT"
               NOT INVALID KEY DISPLAY "RECORD 2=" RREC
           END-READ
           CLOSE RELF
           STOP RUN.
