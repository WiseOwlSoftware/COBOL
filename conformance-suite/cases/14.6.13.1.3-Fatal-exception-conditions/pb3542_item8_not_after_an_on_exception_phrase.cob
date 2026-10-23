*>--- conformance-case
*> id: pb3542_item8_not_after_an_on_exception_phrase
*> rules: [GR-14.6.13.1.3-1, GR-14.6.13.1.3-8, GR-14.9.10.4-20]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3542 (its sibling arm, found on the way) - a fatal condition that the statement's
      *> own conditional phrase processes runs no item 8 ROLLBACK:
      *>   cite.py: OK 14.6.13.1.3 1) "If a conditional phrase without the NOT phrase is specified in
      *>   the interrupted statement and the rules for that statement indicate that the fatal
      *>   exception condition is to be processed by the conditional phrase, the procedures for
      *>   nonfatal exceptions as specified in 14.6.13.1.4, Nonfatal exception conditions, apply."
      *>   cite.py: OK 14.9.10.4 20) c) "If the ON EXCEPTION phrase is specified in the DELETE FILE
      *>   statement, any applicable exception processing statements are not executed and control
      *>   is transferred to the imperative-statement-3 specified in the ON EXCEPTION phrase."
      *>   cite.py: OK 14.9.10.4 13) "If the file is open the I-O status value in the file
      *>   connector referenced by file-name-1 is set to '41'."
      *> RELF is open, so DELETE FILE RELF answers '41' (fatal), and its ON EXCEPTION phrase takes
      *> it: item 1 applies and item 8 is not reached. Expected: the phrase sees '41'; WS-COUNT
      *> keeps 0002, FS keeps '41', and record 2 is still in the file. The defect rolled back after
      *> the phrase (0001, '00', record 2 absent).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P3542OE.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RELF ASSIGN TO "p3542oe.dat"
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
           DELETE FILE RELF
               ON EXCEPTION DISPLAY "ON EXCEPTION FS=" FS
           END-DELETE
           DISPLAY "AFTER DELETE COUNT=" WS-COUNT " FS=" FS
           CLOSE RELF
           OPEN INPUT RELF
           MOVE 2 TO RK
           READ RELF
               INVALID KEY DISPLAY "RECORD 2 ABSENT"
               NOT INVALID KEY DISPLAY "RECORD 2=" RREC
           END-READ
           CLOSE RELF
           STOP RUN.
