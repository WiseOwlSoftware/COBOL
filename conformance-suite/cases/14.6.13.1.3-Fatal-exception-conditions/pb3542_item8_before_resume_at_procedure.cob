*>--- conformance-case
*> id: pb3542_item8_before_resume_at_procedure
*> rules: [GR-14.6.13.1.3-3, GR-14.6.13.1.3-8]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3542 - ISO 14.6.13.1.3 item 8's ROLLBACK runs AFTER the file's declarative and
      *> BEFORE the RESUME it ends in transfers control:
      *>   cite.py: OK 14.6.13.1.3 3) "If the exception condition is a fatal EC-I-O exception
      *>   condition, then the rules for 9.1.13, I-O status apply, then if the implementor has not
      *>   specified otherwise, the following rules apply for all fatal exception conditions."
      *>   cite.py: OK 14.6.13.1.3 8) "However, if there is a fatal exception condition for a file
      *>   subject to an active APPLY COMMIT clause, then a ROLLBACK statement is executed
      *>   unconditionally."
      *>   cite.py: OK 14.9.33.3 1) "The RESUME statement may be specified only in a declarative or
      *>   in an imperative statement in a WHEN phrase of an exception-checking PERFORM statement."
      *> Checking is NOT enabled, so the READ of RELF while it is open OUTPUT ('47', fatal) runs the
      *> USE procedure (9.1.13) and then item 8's ROLLBACK, whatever the procedure ends in. Expected:
      *> the procedure sees '47'; at P2, WS-COUNT and FS are back to the 0001 and '00' the COMMIT
      *> saved (14.9.36.4 GR3: "It also includes the file status data items"), and record 2, written
      *> after the COMMIT, is gone. The defect let RESUME AT P2 jump past the ROLLBACK (0002, '47',
      *> record 2 present).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P3542RA.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RELF ASSIGN TO "p3542ra.dat"
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
           RESUME AT P2.
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
           DISPLAY "NOT REACHED".
       P2.
           DISPLAY "AT P2 COUNT=" WS-COUNT " FS=" FS
           CLOSE RELF
           OPEN INPUT RELF
           MOVE 2 TO RK
           READ RELF
               INVALID KEY DISPLAY "RECORD 2 ABSENT"
               NOT INVALID KEY DISPLAY "RECORD 2=" RREC
           END-READ
           CLOSE RELF
           STOP RUN.
