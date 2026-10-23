*>--- conformance-case
*> id: pb3543_item8_not_after_a_when_phrase
*> rules: [GR-14.6.13.1.3-4, GR-14.6.13.1.3-8, GR-14.9.28.4-14]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3543 - a WHEN phrase of an exception-checking PERFORM that ends in RESUME runs no
      *> item 8 ROLLBACK:
      *>   cite.py: OK 14.6.13.1.3 4) "If the executed statement is within imperative-statement-1 of
      *>   a PERFORM statement that contains a WHEN phrase that specifies the exception-name ...
      *>   the imperative statement in the WHEN phrase is executed", and NOTE 1: "The user is able
      *>   to continue by using a RESUME statement".
      *>   cite.py: OK 14.9.28.4 14) "If checking for exception-name-1 or exception-name-2 is not
      *>   enabled for imperative-statement-1 by a TURN directive, an implicit TURN directive for
      *>   exception-name-1 or exception-name-2 is assumed before the first statement in
      *>   imperative-statement-1."
      *>   cite.py: OK 14.6.13.1.3 8) "If checking for the exception condition is not enabled ..."
      *> The WHEN names EC-I-O-LOGIC-ERROR, so checking for it is enabled in the body and item 4
      *> processes the '47' of the READ of RELF while it is open OUTPUT; item 8 is not reached.
      *> Expected: WS-COUNT keeps 0002, FS keeps '47', record 2 is still in the file. The defect
      *> rolled back after the RESUME (0001, '00', record 2 absent).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P3543WN.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RELF ASSIGN TO "p3543wn.dat"
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
           PERFORM
               READ RELF
               DISPLAY "IN-BODY COUNT=" WS-COUNT " FS=" FS
           WHEN EC-I-O-LOGIC-ERROR
               DISPLAY "WHEN FS=" FS
               RESUME AT NEXT STATEMENT
           END-PERFORM
           DISPLAY "AFTER PERFORM COUNT=" WS-COUNT " FS=" FS
           CLOSE RELF
           OPEN INPUT RELF
           MOVE 2 TO RK
           READ RELF
               INVALID KEY DISPLAY "RECORD 2 ABSENT"
               NOT INVALID KEY DISPLAY "RECORD 2=" RREC
           END-READ
           CLOSE RELF
           STOP RUN.
