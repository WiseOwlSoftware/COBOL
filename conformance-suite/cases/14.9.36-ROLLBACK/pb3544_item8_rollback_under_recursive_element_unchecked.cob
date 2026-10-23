*>--- conformance-case
*> id: pb3544_item8_rollback_under_recursive_element_unchecked
*> rules: [GR-14.9.36.4-2, GR-14.6.13.1.3-8, GR-12.4.6.3.4-3]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3544 - the RECURSION arm of item 8's ROLLBACK (wave 1156 A, review lead R1-2):
      *>   cite.py: OK 14.6.13.1.3 8) "However, if there is a fatal exception condition for a file
      *>   subject to an active APPLY COMMIT clause, then a ROLLBACK statement is executed
      *>   unconditionally."
      *>   cite.py: OK 14.9.36.4 2) "If this statement is attempted to be executed under the control
      *>   of a recursive runtime element or a file SORT or MERGE statement, then the exception
      *>   condition EC-FLOW-ROLLBACK is set to exist."
      *>   cite.py: OK 12.4.6.3.4 3) "If a runtime element that contains an APPLY COMMIT clause is
      *>   attempted to be activated under the control of a recursive runtime element, then the
      *>   EC-FLOW-APPLY exception condition is set to exist."
      *> P3544RU names the EXTERNAL RELATIVE file RELF and WS-COUNT in its APPLY COMMIT clause
      *> (active from its activation, 12.4.6.3.4 GR5), writes record 1, COMMITs, writes record 2
      *> and calls the RECURSIVE P3544RR, which calls P3544RS. P3544RS describes RELF with its own
      *> clause (12.4.6.3.4 GR2, 14.8.4.2); activated under P3544RR it stays inactive (GR3, checking
      *> off), but RELF is still subject to P3544RU's active clause. No checking is enabled. P3544RS's
      *> READ of RELF, open OUTPUT, answers '47' (fatal) into FS, which covers it, so the run unit
      *> continues; item 8's ROLLBACK is attempted under the recursive P3544RR, EC-FLOW-ROLLBACK
      *> exists with checking off, and nothing is rolled back (docs/CONFORMANCE.md A.4.3: a
      *> forbidden statement with checking off is not executed). Expected: WS-COUNT keeps 0002 and FS
      *> '47' in P3544RS and after the CALL; P3544RU's own ROLLBACK, with no recursive element active,
      *> then restores 0001, '00' (14.9.36.4 GR3: "It also includes the file status data items") and
      *> the file without record 2. Before PB3544 item 8 rolled back under the recursive element
      *> (IN-S COUNT=0001 FS=00).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P3544RU.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RELF ASSIGN TO "p3544ru.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS RK
               FILE STATUS IS FS.
       I-O-CONTROL.
           APPLY COMMIT ON RELF WS-COUNT.
       DATA DIVISION.
       FILE SECTION.
       FD RELF IS EXTERNAL.
       01 RREC PIC X(10).
       WORKING-STORAGE SECTION.
       01 RK PIC 9(4) EXTERNAL.
       01 FS PIC XX EXTERNAL.
       01 WS-COUNT PIC 9(4) EXTERNAL.
       PROCEDURE DIVISION.
           MOVE 0 TO WS-COUNT
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
           CALL "P3544RR"
           DISPLAY "AFTER CALL COUNT=" WS-COUNT " FS=" FS
           ROLLBACK
           DISPLAY "AFTER ROLLBACK COUNT=" WS-COUNT " FS=" FS
           CLOSE RELF
           OPEN INPUT RELF
           MOVE 2 TO RK
           READ RELF
               INVALID KEY DISPLAY "RECORD 2 ABSENT"
               NOT INVALID KEY DISPLAY "RECORD 2=" RREC
           END-READ
           CLOSE RELF
           STOP RUN.
       END PROGRAM P3544RU.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P3544RR RECURSIVE.
       PROCEDURE DIVISION.
           CALL "P3544RS"
           GOBACK.
       END PROGRAM P3544RR.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P3544RS.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RELF ASSIGN TO "p3544ru.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS RK
               FILE STATUS IS FS.
       I-O-CONTROL.
           APPLY COMMIT ON RELF WS-COUNT.
       DATA DIVISION.
       FILE SECTION.
       FD RELF IS EXTERNAL.
       01 RREC PIC X(10).
       WORKING-STORAGE SECTION.
       01 RK PIC 9(4) EXTERNAL.
       01 FS PIC XX EXTERNAL.
       01 WS-COUNT PIC 9(4) EXTERNAL.
       PROCEDURE DIVISION.
           READ RELF
           DISPLAY "IN-S COUNT=" WS-COUNT " FS=" FS
           GOBACK.
       END PROGRAM P3544RS.
